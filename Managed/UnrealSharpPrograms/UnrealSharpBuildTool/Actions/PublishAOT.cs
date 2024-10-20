using System.Diagnostics;
using System.Text;
using Mono.Cecil;

namespace UnrealSharpBuildTool.Actions;

public class PublishAOT : BuildToolAction
{
    /// <summary>
    /// Normalizes the path to the standard Flax format (all separators are '/' except for drive 'C:\').
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The normalized path.</returns>
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;
        var chars = path.ToCharArray();

        // Convert all '\' to '/'
        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] == '\\')
                chars[i] = '/';
        }

        // Fix case 'C:/' to 'C:\'
        if (chars.Length > 2 && !char.IsDigit(chars[0]) && chars[1] == ':')
        {
            chars[2] = '\\';
        }

        return new string(chars);
    }
    
    public override bool RunAction()
    {
        
        TargetPlatform platform = Program.Configuration.BuildPlatform;
        TargetArchitecture arch = Program.Configuration.BuildArchitecture;
        TargetConfiguration configuration = TargetConfiguration.Debug;
        
        var runtimeIdentifier = DotNetHelper.GetHostRuntimeIdentifier(platform, arch);
        var runtimeIdentifierParts = runtimeIdentifier.Split('-');
        var enableReflection = true;
        var enableReflectionScan = true;
        var enableStackTrace = true;

        // Find input files
        var inputFiles = Directory.GetFiles("C:\\Users\\oscar\\Desktop\\Lel", "*.dll", SearchOption.TopDirectoryOnly).ToList();
        inputFiles.Sort();
        
        var aotOutputPath = Path.Combine(Program.GetOutputPath(), "Test");
        if (!Directory.Exists(aotOutputPath))
        {
            Directory.CreateDirectory(aotOutputPath);
        }
        
        // TODO: run dotnet nuget installation to get 'runtime.<runtimeIdentifier>.Microsoft.DotNet.ILCompiler' package
        //var ilcRoot = Path.Combine(DotNetSdk.Instance.RootPath, "sdk\\7.0.202\\Sdks\\Microsoft.DotNet.ILCompiler");
        var ilcRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $".nuget\\packages\\runtime.{runtimeIdentifier}.microsoft.dotnet.ilcompiler\\8.0.1");

        // Build ILC args list
        var ilcArgs = new StringBuilder();

        ilcArgs.AppendLine("--nativelib"); // Compile as static or shared library
        if (configuration != TargetConfiguration.Debug)
            ilcArgs.AppendLine("-O"); // Enable optimizations
        if (configuration == TargetConfiguration.Release)
            ilcArgs.AppendLine("--Ot"); // Enable optimizations, favor code speed
        if (configuration != TargetConfiguration.Release)
            ilcArgs.AppendLine("-g"); // Emit debugging information
        string ilcTargetOs = runtimeIdentifierParts[0];
        if (ilcTargetOs == "win")
            ilcTargetOs = "windows";
        ilcArgs.AppendLine("--targetos:" + ilcTargetOs); // Target OS for cross compilation
        ilcArgs.AppendLine("--targetarch:" + runtimeIdentifierParts[1]); // Target architecture for cross compilation
        ilcArgs.AppendLine("-o:" + "C:\\Users\\oscar\\Desktop\\test"); // Output file path
        foreach (var inputFile in inputFiles)
        {
            ilcArgs.AppendLine(inputFile);
            ilcArgs.AppendLine("--root:" + inputFile);
        }
        ilcArgs.AppendLine("--nowarn:\"1701;1702;IL2121;1701;1702\""); // Disable specific warning messages
        ilcArgs.AppendLine("--initassembly:System.Private.CoreLib"); // Assembly(ies) with a library initializer
        ilcArgs.AppendLine("--initassembly:System.Private.TypeLoader");
        if (enableReflectionScan && enableReflection)
        {
            ilcArgs.AppendLine("--scanreflection"); // Scan IL for reflection patterns
        }
        if (enableReflection)
        {
            ilcArgs.AppendLine("--initassembly:System.Private.Reflection.Execution");
        }
        else
        {
            ilcArgs.AppendLine("--initassembly:System.Private.DisabledReflection");
            ilcArgs.AppendLine("--reflectiondata:none");
            ilcArgs.AppendLine("--feature:System.Collections.Generic.DefaultComparers=false");
            ilcArgs.AppendLine("--feature:System.Reflection.IsReflectionExecutionAvailable=false");
        }
        if (enableReflection || enableStackTrace)
            ilcArgs.AppendLine("--initassembly:System.Private.StackTraceMetadata");
        if (enableStackTrace)
            ilcArgs.AppendLine("--stacktracedata"); // Emit data to support generating stack trace strings at runtime
        ilcArgs.AppendLine("--feature:System.Linq.Expressions.CanCompileToIL=false");
        ilcArgs.AppendLine("--feature:System.Linq.Expressions.CanEmitObjectArrayDelegate=false");
        ilcArgs.AppendLine("--feature:System.Linq.Expressions.CanCreateArbitraryDelegates=false");
        // TODO: reference files (-r)
        var referenceFiles = new List<string>();
        referenceFiles.AddRange(Directory.GetFiles(Path.Combine(ilcRoot, "framework"), "*.dll"));
        referenceFiles.AddRange(Directory.GetFiles(Path.Combine(ilcRoot, "sdk"), "*.dll"));
        referenceFiles.Sort();
        foreach (var referenceFile in referenceFiles)
        {
            ilcArgs.AppendLine("--reference:" + referenceFile); // Reference file(s) for compilation
        }
        
        var ilcPath = Path.Combine(ilcRoot, "tools", "ilc.exe");
        if (!File.Exists(ilcPath))
        {
            throw new Exception("Missing ILC " + ilcPath);
        }
        
        string ilcResponsePath = Path.Combine(aotOutputPath, "AOT.ilc.rsp");
        File.WriteAllText(ilcResponsePath, ilcArgs.ToString());
        
        var ilcProcess = new BuildToolProcess(ilcPath);
        ilcProcess.StartInfo.Arguments = $"@\"{ilcResponsePath}\"";
        ilcProcess.StartBuildToolProcess();
        return true;
    }
}