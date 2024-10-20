namespace UnrealSharpBuildTool.Actions;

public abstract class BuildToolAction
{ 
    public static bool InitializeAction()
    {
        BuildToolAction buildToolAction = Program.Configuration.Action switch
        {
            BuildAction.Build => new BuildUserSolution(),
            BuildAction.Clean => new CleanSolution(),
            BuildAction.GenerateProject => new GenerateProject(),
            BuildAction.Rebuild => new RebuildSolution(),
            BuildAction.Weave => new WeaveProject(),
            BuildAction.PackageProject => new PackageProject(),
            BuildAction.GenerateSolution => new GenerateSolution(),
            BuildAction.PublishAOT => new PublishAOT(),
            BuildAction.BuildWeave => new BuildWeave(),
            _ => throw new Exception($"Can't find build action with name \"{Program.Configuration.Action}\"")
        };

        return buildToolAction.RunAction();
    }

    public abstract bool RunAction();
}