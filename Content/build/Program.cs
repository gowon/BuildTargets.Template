namespace build;

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Hosting;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Reflection;
using Extensions.Options.AutoBinder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal static class Program
{
    public static string DefaultConfigFile = "buildconfig.json";

    public static Option<FileInfo> ConfigurationFileGlobalOption =
        new("--config-file", "Specify configuration file");

    private static async Task<int> Main(string[] args)
    {
        try
        {
            var command = new RootCommand();
            RegisterCommands(command);
            command.AddGlobalOption(ConfigurationFileGlobalOption);

            command.SetHandler(context =>
            {
                // ref: https://github.com/dotnet/command-line-api/issues/1537
                context.HelpBuilder.CustomizeLayout(_ => HelpBuilder.Default.GetLayout().Skip(1));
                context.HelpBuilder.Write(context.ParseResult.CommandResult.Command,
                    context.Console.Out.CreateTextWriter());
            });

            var parser = new CommandLineBuilder(command)
                .UseHost(CreateHostBuilder)
                .UseDefaults()
                .UseExceptionHandler((exception, context) =>
                {
                    Console.WriteLine($"Unhandled exception occurred: {exception.Message}");
                    context.ExitCode = 1;
                })
                .Build();

            return await parser.InvokeAsync(args);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Application terminated unexpectedly: {exception.Message}");
            return 1;
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                var fileInfo = hostingContext.GetInvocationContext().ParseResult
                    .GetValueForOption(ConfigurationFileGlobalOption);

                // https://www.hanselman.com/blog/how-do-i-find-which-directory-my-net-core-console-application-was-started-in-or-is-running-from
                // https://stackoverflow.com/a/97491/7644876
                var basePath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName);

                var configFilePath = fileInfo is { Exists: true }
                    ? fileInfo.FullName
                    : DefaultConfigFile;

                config
                    .SetBasePath(basePath!)
                    .AddJsonFile(configFilePath, true, false)
                    .AddEnvironmentVariables();
            })
            .ConfigureLogging((context, builder) =>
            {
                builder.ClearProviders();
                builder.AddConsole().SetMinimumLevel(LogLevel.Trace);
            })
            .ConfigureServices((hostContext, services) => { services.AutoBindOptions(); });
    }

    private static void RegisterCommands(Command rootCommand)
    {
        var types = Assembly.GetExecutingAssembly().GetTypes().Where(type =>
            type.IsClass &&
            !type.IsAbstract &&
            type.IsSubclassOf(typeof(Command)) &&
            type.GetConstructors().Any(info => !info.GetParameters().Any()));

        foreach (var type in types) rootCommand.AddCommand((Command)Activator.CreateInstance(type)!);
    }
}