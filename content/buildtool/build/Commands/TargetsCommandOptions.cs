namespace build.Commands;

using global::Extensions.Options.AutoBinder;

[AutoBind("BuildTargets")]
public class TargetsCommandOptions
{
    // based defaults in https://github.com/github/gitignore/blob/master/VisualStudio.gitignore
    public string ArtifactsDirectory { get; set; } = "artifacts";

    // based defaults in https://github.com/github/gitignore/blob/master/VisualStudio.gitignore
    public string TestResultsDirectory { get; set; } = "testresults";
}