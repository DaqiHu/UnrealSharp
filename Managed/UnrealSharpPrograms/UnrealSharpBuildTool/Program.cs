using CommandLine;
using UnrealSharpBuildTool.Actions;

namespace UnrealSharpBuildTool;

public static class Program
{
    public static Configuration Configuration = null!;

    public static int Main(string[] args)
    {
        try
        {
            Console.WriteLine(">>> UnrealSharpBuildTool");
            Parser parser = new Parser(with => with.HelpWriter = null);
            ParserResult<Configuration> result = parser.ParseArguments<Configuration>(args);
            
            if (result.Tag == ParserResultType.NotParsed)
            {
                Configuration.PrintHelp(result);
                
                string errors = string.Empty;
                foreach (Error error in result.Errors)
                {
                    if (error is TokenError tokenError)
                    {
                        errors += $"{tokenError.Tag}: {tokenError.Token} \n";
                    }
                }
                
                throw new Exception($"Invalid arguments. Errors: {errors}");
            }
        
            Configuration = result.Value;
            
            if (!BuildToolAction.InitializeAction())
            {
                throw new Exception("Failed to initialize action.");
            }
            
            Console.WriteLine($"UnrealSharpBuildTool executed {Configuration.Action.ToString()} action successfully.");
        }
        catch (Exception exception)
        {
            Console.WriteLine("An error occurred: " + exception.Message + Environment.NewLine + exception.StackTrace);
            return 1;
        }
        
        return 0;
    }
    
    public static string TryGetArgument(string argument)
    {
        return Configuration.TryGetArgument(argument);
    }
    
    public static bool HasArgument(string argument)
    {
        return Configuration.HasArgument(argument);
    }
    
    public static string GetSolutionFile()
    {
        return Path.Combine(GetScriptFolder(), Configuration.ProjectName + ".sln");
    }

    public static string GetUProjectFilePath()
    {
        return Path.Combine(Configuration.ProjectDirectory, Configuration.ProjectName + ".uproject");
    }
    
    public static string GetBuildConfiguration()
    {
        string buildConfig = TryGetArgument("BuildConfig");
        if (string.IsNullOrEmpty(buildConfig))
        {
            buildConfig = "Debug";
        }
        return buildConfig;
    }
    
    public static TargetConfiguration GetBuildConfig()
    {
        string buildConfig = GetBuildConfiguration();
        Enum.TryParse(buildConfig, out TargetConfiguration config);
        return config;
    }
    
    public static string GetBuildConfiguration(TargetConfiguration targetConfiguration)
    {
        return targetConfiguration switch
        {
            TargetConfiguration.Debug => "Debug",
            TargetConfiguration.Release => "Release",
            TargetConfiguration.Publish => "Release",
            _ => "Release"
        };
    }
    
    public static string GetScriptFolder()
    {
        return Path.Combine(Configuration.ProjectDirectory, "Script");
    }
    
    public static string GetProjectDirectory()
    {
        return Configuration.ProjectDirectory;
    }
    
    public static string FixPath(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return path.Replace('/', '\\');
        }
        
        return path;
    }

    public static string GetProjectNameAsManaged()
    {
        return "Managed" + Configuration.ProjectName;
    }
    
    public static string GetOutputPath(string rootDir = "")
    {
        if (string.IsNullOrEmpty(rootDir))
        {
            rootDir = Configuration.ProjectDirectory;
        }
        
        return Path.Combine(rootDir, "Binaries", "Managed");
    }

    public static string GetWeaver()
    {
        return Path.Combine(GetManagedBinariesDirectory(), "UnrealSharpWeaver.dll");
    }

    public static string GetManagedBinariesDirectory()
    {
        return Path.Combine(Configuration.PluginDirectory, "Binaries", "Managed");
    }
    
    public static string GetVersion()
    {
        Version currentVersion = Environment.Version;
        string currentVersionStr = $"{currentVersion.Major}.{currentVersion.Minor}";
        return "net" + currentVersionStr;
    }
}