using System.Collections.ObjectModel;

namespace UnrealSharpBuildTool.Actions;

public class BuildSolution : BuildToolAction
{
    private readonly TargetConfiguration _targetConfiguration;
    private readonly string _folder;
    private readonly Collection<string>? _extraArguments;
    
    public BuildSolution(string folder, Collection<string>? extraArguments = null, TargetConfiguration targetConfiguration = TargetConfiguration.Debug)
    {
        _folder = Program.FixPath(folder);
        _targetConfiguration = targetConfiguration;
        _extraArguments = extraArguments;
    }
    
    public override bool RunAction()
    {
        if (!Directory.Exists(_folder))
        {
            throw new Exception($"Couldn't find the solution file at \"{_folder}\"");
        }
        
        BuildToolProcess buildSolutionProcess = new BuildToolProcess();
        
        if (_targetConfiguration == TargetConfiguration.Publish)
        {
            buildSolutionProcess.StartInfo.ArgumentList.Add("publish");
        }
        else
        {
            buildSolutionProcess.StartInfo.ArgumentList.Add("build");
        }
        
        buildSolutionProcess.StartInfo.ArgumentList.Add($"{_folder}");
        
        buildSolutionProcess.StartInfo.ArgumentList.Add("--configuration");
        buildSolutionProcess.StartInfo.ArgumentList.Add(Program.GetBuildConfiguration(_targetConfiguration));
        
        if (_extraArguments != null)
        {
            foreach (var argument in _extraArguments)
            {
                buildSolutionProcess.StartInfo.ArgumentList.Add(argument);
            }
        }

        return buildSolutionProcess.StartBuildToolProcess();
    }
}