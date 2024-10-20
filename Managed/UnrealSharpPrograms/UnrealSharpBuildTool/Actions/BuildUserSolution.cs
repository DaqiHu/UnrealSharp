using System.Collections.ObjectModel;

namespace UnrealSharpBuildTool.Actions;

public class BuildUserSolution : BuildSolution
{
    public BuildUserSolution(Collection<string>? extraArguments = null, TargetConfiguration targetConfiguration = TargetConfiguration.Debug) 
        : base(Program.GetScriptFolder(), extraArguments, targetConfiguration)
    {
        
    }
}