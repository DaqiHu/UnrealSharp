using System.Reflection;
using System.Runtime.Loader;
using System.IO;
using System.Security.Cryptography;

namespace UnrealSharp.Plugins;

public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    // Experiment only: an attacker can recover this key from the client loader.
    private static readonly byte[] EncryptedMagic = "USENC01!"u8.ToArray();
    private static readonly byte[] EncryptionKey = Convert.FromBase64String("vEIgilzLwbfCqFSalR3G21TyoWxx1y6MNmJJ/ZgFKd8=");

    public PluginLoadContext(string pluginName, AssemblyDependencyResolver resolver, bool isCollectible) : base(pluginName, isCollectible)
    {
        _resolver = resolver;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (string.IsNullOrEmpty(assemblyName.Name))
        {
            return null;
        }
        
        Assembly? loadedAssembly = AssemblyCache.GetAssembly(assemblyName.Name!, this);
        if (loadedAssembly != null)
        {
            return loadedAssembly;
        }
        
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        
        Assembly? newAssembly;
        if (string.IsNullOrEmpty(assemblyPath))
        {
            newAssembly = Default.LoadFromAssemblyName(assemblyName);
        }
        else
        {
            using FileStream assemblyFile = File.Open(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            string pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
        
            byte[] image = File.ReadAllBytes(assemblyPath);
            if (image.AsSpan().StartsWith(EncryptedMagic))
            {
                if (image.Length < 36)
                {
                    throw new InvalidDataException("Truncated encrypted assembly");
                }
                byte[] plaintext = new byte[image.Length - 36];
                using AesGcm cipher = new(EncryptionKey, 16);
                try
                {
                    cipher.Decrypt(image.AsSpan(8, 12), image.AsSpan(36),
                        image.AsSpan(20, 16), plaintext, EncryptedMagic);
                }
                catch (AuthenticationTagMismatchException error)
                {
                    string? evidence = Environment.GetEnvironmentVariable("UNREALSHARP_BENCH_CRYPTO_EVIDENCE");
                    if (!string.IsNullOrEmpty(evidence))
                    {
                        File.WriteAllText(evidence, System.Text.Json.JsonSerializer.Serialize(new
                        {
                            Reason = error.GetType().FullName,
                            Assembly = Path.GetFileName(assemblyPath),
                            CipherSha256 = Convert.ToHexString(SHA256.HashData(image))
                        }));
                    }
                    throw;
                }
                try
                {
                    using MemoryStream decryptedAssembly = new(plaintext, writable: false);
                    newAssembly = LoadFromStream(decryptedAssembly);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(plaintext);
                }
            }
            else if (File.Exists(pdbPath))
            {
                using FileStream pdbFile = File.Open(pdbPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                newAssembly = LoadFromStream(assemblyFile, pdbFile);
            }
            else
            {
                newAssembly = LoadFromAssemblyPath(assemblyPath);
            }
        }

        AssemblyCache.AddAssembly(newAssembly);
        return newAssembly;
    }
}
