using MaxBot.TUI.App;
using MaxBot.TUI.Infrastructure;

namespace MaxBot.TUI.Commands;

/// <summary>
/// Interactive chat command that launches the TUI.
/// </summary>
public sealed class ChatCommand : ICommand
{
    public string Name => "chat";
    public string Description => "Start interactive chat mode (default)";

    public async Task<int> ExecuteAsync(string[] args)
    {
        try
        {
            var parsedArgs = ArgumentParser.Parse(args);
            
            // Setup dependency injection
            var services = new ServiceCollection();
            ServiceConfiguration.ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();
            
            // Create and run the FlexColumn TUI application
            var app = serviceProvider.GetRequiredService<FlexColumnTuiApp>();
            
            // Convert parsed args back to string array format for compatibility
            var tuiArgs = BuildArgsFromParsed(parsedArgs);
            
            return await app.RunAsync(tuiArgs);
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.WriteLine("Application cancelled by user.");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    public void ShowHelp()
    {
        AnsiConsole.MarkupLine("[bold]DESCRIPTION:[/]");
        AnsiConsole.MarkupLine($"    {Description}");
        AnsiConsole.WriteLine();
        
        AnsiConsole.MarkupLine("[bold]USAGE:[/]");
        AnsiConsole.MarkupLine("    max chat [[OPTIONS]]");
        AnsiConsole.WriteLine();
        
        AnsiConsole.MarkupLine("[bold]OPTIONS:[/]");
        AnsiConsole.MarkupLine("    -v, --verbosity <LEVEL>    Set the verbosity level (quiet, minimal, normal, detailed, diagnostic)");
        AnsiConsole.MarkupLine("        --config <PATH>        Path to the configuration file (default: maxbot.config.json)");
        AnsiConsole.MarkupLine("        --profile <NAME>       Configuration profile to use");
        AnsiConsole.MarkupLine("    -h, --help                 Show this help message");
        AnsiConsole.WriteLine();
        
        AnsiConsole.MarkupLine("[bold]EXAMPLES:[/]");
        AnsiConsole.MarkupLine("    max chat");
        AnsiConsole.MarkupLine("    max chat --verbosity normal");
        AnsiConsole.MarkupLine("    max chat --profile development");
    }

    private static string[] BuildArgsFromParsed(Dictionary<string, string?> parsedArgs)
    {
        var args = new List<string>();
        
        var verbosity = ArgumentParser.GetString(parsedArgs, "verbosity") ?? 
                       ArgumentParser.GetString(parsedArgs, "v");
        if (!string.IsNullOrEmpty(verbosity) && verbosity != "quiet")
        {
            args.Add("--verbosity");
            args.Add(verbosity);
        }
        
        var config = ArgumentParser.GetString(parsedArgs, "config");
        if (!string.IsNullOrEmpty(config) && config != "maxbot.config.json")
        {
            args.Add("--config");
            args.Add(config);
        }
        
        var profile = ArgumentParser.GetString(parsedArgs, "profile");
        if (!string.IsNullOrEmpty(profile))
        {
            args.Add("--profile");
            args.Add(profile);
        }
        
        return args.ToArray();
    }
}
