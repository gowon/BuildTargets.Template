namespace build;

using System.CommandLine;
using System.CommandLine.Invocation;
using Bullseye;
using Commands;
using static Bullseye.Targets;
using static SimpleExec.Command;

internal static class Program
{
    private const string ArtifactsDirectory = ".artifacts";
    private const string TestResultsDirectory = ".test-results";

    private static readonly Option<string> AdditionalArgsOption = new(new[] { "--additional-args", "-a" },
        "Additional arguments to supply for target execution");

    private static readonly Option<string> ConfigurationOption =
        new(new[] { "--configuration", "-C" }, () => "Release", "The configuration to run the target");

    private static async Task<int> Main(string[] args)
    {
        var command = new RootCommand { AdditionalArgsOption, ConfigurationOption };

        command.ImportBullseyeConfigurations();

        command.SetHandler(async context =>
        {
            Target(Targets.RestoreTools, async () => { await RunAsync("dotnet", "tool restore"); });

            Target(Targets.CleanArtifactsOutput, () =>
            {
                if (Directory.Exists(ArtifactsDirectory))
                {
                    Directory.Delete(ArtifactsDirectory, true);
                }
            });

            Target(Targets.CleanTestsOutput, () =>
            {
                if (Directory.Exists(TestResultsDirectory))
                {
                    Directory.Delete(TestResultsDirectory, true);
                }
            });

            Target(Targets.CleanBuildOutput, async () =>
            {
                var configuration = context.ParseResult.GetValueForOption(ConfigurationOption);
                await RunAsync("dotnet", $"clean -c {configuration} -v m --nologo");
            });

            Target(Targets.CleanAll,
                DependsOn(Targets.CleanArtifactsOutput, Targets.CleanTestsOutput, Targets.CleanBuildOutput));

            Target(Targets.Build, DependsOn(Targets.CleanBuildOutput), async () =>
            {
                var configuration = context.ParseResult.GetValueForOption(ConfigurationOption);
                await RunAsync("dotnet", $"build -c {configuration} --nologo");
            });

            Target(Targets.Pack, DependsOn(Targets.CleanArtifactsOutput, Targets.Build), async () =>
            {
                var configuration = context.ParseResult.GetValueForOption(ConfigurationOption);
                await RunAsync("dotnet",
                    $"pack -c {configuration} -o {Directory.CreateDirectory(ArtifactsDirectory).FullName} --no-build --nologo");
            });

            Target(Targets.PublishArtifacts, DependsOn(Targets.Pack), () => Console.WriteLine("publish artifacts"));

            Target("default", DependsOn(Targets.RunTests, Targets.PublishArtifacts));

            Target(Targets.RunTests, DependsOn(Targets.CleanTestsOutput, Targets.Build), async () =>
            {
                var configuration = context.ParseResult.GetValueForOption(ConfigurationOption);
                await RunAsync("dotnet",
                    $"test -c {configuration} --no-build --nologo --collect:\"XPlat Code Coverage\" --results-directory {TestResultsDirectory}");
            });

            Target(Targets.RunTestsCoverage, DependsOn(Targets.RestoreTools, Targets.RunTests), () =>
                Run("dotnet",
                    $"reportgenerator -reports:{TestResultsDirectory}/**/*cobertura.xml -targetdir:{TestResultsDirectory}/coveragereport -reporttypes:HtmlSummary"));

            Target(Targets.Ping, async () => { await context.InvokeCommandAsync<PingCommand>(); });


            await command.RunBullseyeAndExitAsync(context);
        });

        return await command.InvokeAsync(args);
    }

    private static void ImportBullseyeConfigurations(this Command command)
    {
        // translate from Bullseye to System.CommandLine
        command.Add(new Argument<string[]>("targets")
        {
            Description =
                "A list of targets to run or list. If not specified, the \"default\" target will be run, or all targets will be listed. Target names may be abbreviated. For example, \"b\" for \"build\"."
        });

        foreach (var (aliases, description) in Options.Definitions)
            command.Add(new Option<bool>(aliases.ToArray(), description));
    }

    private static async Task<int> InvokeCommandAsync<TCommand>(this InvocationContext context)
        where TCommand : Command, new()
    {
        var additionalArguments = context.ParseResult.GetValueForOption(AdditionalArgsOption);
        return await new TCommand().InvokeAsync(additionalArguments!, context.Console);
    }

    private static async Task RunBullseyeAndExitAsync(this Command command, InvocationContext context)
    {
        var targets = context.ParseResult.CommandResult.Tokens.Select(token => token.Value);
        var options = new Options(Options.Definitions.Select(d => (d.Aliases[0],
            context.ParseResult.GetValueForOption(command.Options.OfType<Option<bool>>()
                .Single(o => o.HasAlias(d.Aliases[0]))))));
        await RunTargetsAndExitAsync(targets, options);
    }
}

internal static class Targets
{
    public const string RunTestsCoverage = "run-tests-coverage";
    public const string RestoreTools = "restore-tools";
    public const string CleanBuildOutput = "clean-build-output";
    public const string CleanArtifactsOutput = "clean-artifacts-output";
    public const string CleanTestsOutput = "clean-test-output";
    public const string CleanAll = "clean";
    public const string Build = "build";
    public const string RunTests = "run-tests";
    public const string Pack = "pack";
    public const string PublishArtifacts = "publish-artifacts";
    public const string Ping = "ping";
}