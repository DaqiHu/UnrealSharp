using System.Reflection;
using CommandLine;
using CommandLine.Text;

namespace UnrealSharpBuildTool;

public enum BuildAction
{
    Build,
    Clean,
    GenerateProject,
    Rebuild,
    Weave,
    PackageProject,
    GenerateSolution,
    BuildWeave,
    PublishAOT,
}

public class Configuration
{
    [Option("Action", Required = true, HelpText = "The action the build tool should process. Possible values: Build, Clean, GenerateProject, Rebuild, Weave, PackageProject, GenerateSolution, BuildWeave.")]
    public BuildAction Action { get; set; }
    
    [Option("DotNetPath", Required = false, HelpText = "The path to the dotnet.exe")]
    public string? DotNetPath { get; set; }
    
    [Option("ProjectDirectory", Required = true, HelpText = "The directory where the .uproject file resides.")]
    public string ProjectDirectory { get; set; }
    
    [Option("PluginDirectory", Required = false, HelpText = "The UnrealSharp plugin directory.")]
    public string PluginDirectory { get; set; }
    
    [Option("EngineDirectory", Required = false, HelpText = "The Unreal Engine directory.")]
    public string EngineDirectory { get; set; }
    
    [Option("ProjectName", Required = true, HelpText = "The name of the Unreal Engine project.")]
    public string ProjectName { get; set; }
    
    [Option("AdditionalArgs", Required = false, HelpText = "Additional key-value arguments for the build tool.")]
    public IEnumerable<string> AdditionalArgs { get; set; }
    
    /// <summary>
    /// The target configuration to build. If not specified builds all supported configurations.
    /// </summary>
    [Option("BuildConfigurations", HelpText = "The target configuration to build. If not specified builds all supported configurations.")]
    public TargetConfiguration BuildConfiguration { get; set; }

    /// <summary>
    /// The target platform to build. If not specified builds all supported platforms.
    /// </summary>
    [Option("buildplatforms", HelpText = "The target platform to build. If not specified builds all supported platforms.")]
    public TargetPlatform BuildPlatform { get; set; }

    /// <summary>
    /// The target platform architecture to build. If not specified builds all valid architectures.
    /// </summary>
    [Option("arch", HelpText = "The target platform architecture to build. If not specified builds all valid architectures.")]
    public TargetArchitecture BuildArchitecture { get; set; }
    
    public string TryGetArgument(string argument)
    {
        foreach (var arg in AdditionalArgs)
        {
            if (!arg.StartsWith(argument))
            {
                continue;
            }
            
            return arg.Substring(argument.Length + 1);
        }
        
        return string.Empty;
    }
    
    public bool HasArgument(string argument)
    {
        foreach (var arg in AdditionalArgs)
        {
            if (arg.StartsWith(argument))
            {
                return true;
            }
        }
        return false;
    }

    public static void PrintHelp(ParserResult<Configuration> result)
    {
        string name = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
        Console.Error.WriteLine($"Usage: {name} [options]");
        Console.Error.WriteLine("Options:");

        var helpText = HelpText.AutoBuild(result, h => h, e => e);
        Console.WriteLine(helpText);
    }
}