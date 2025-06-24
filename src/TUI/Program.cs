
// Parse command line arguments
var argResult = await ArgumentParser.ParseAsync(args);
if (argResult.IsFailed)
{
    ConsoleRenderer.ConsoleWriteError(argResult.Errors.FirstOrDefault()?.Message ?? "Unknown error");
    return 1;
}

var options = argResult.Value;

// Show help if requested
if (options.ShowHelp)
{
    ArgumentParser.DisplayHelp();
    return 0;
}

if (args.Length == 1 && args[0] == "chat")
{
    // This is a valid case, but we don't want to show help.
}
else if (args.Length == 1 && options.ShowVersion)
{
    Console.WriteLine(ArgumentParser.Version);
    return 0;
}
else if (args.Length == 1 && options.ShowVersion)
{
    Console.WriteLine(ArgumentParser.Version);
    return 0;
}
else if (string.IsNullOrEmpty(options.UserPrompt) && options.Mode == "oneshot" && !options.ShowStatus)
{
    ArgumentParser.DisplayHelp();
    return 0;
}



// Set debug flag in config if specified
var clientResult = ChatClient.Create(
    options.ConfigPath, 
    options.ProfileName, 
    options.ToolApprovals, 
    options.Mode, 
    ConsoleRenderer.ConsoleWriteLLMResponseDetails, 
    options.Debug);
if (clientResult.IsFailed)
{
    ConsoleRenderer.ConsoleWriteError(clientResult.Errors.FirstOrDefault()?.Message ?? "Unknown error");
    return 1;
}

var chatClient = clientResult.Value;

// Show status if requested
if (options.ShowStatus)
{
    var temp = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Yellow;

    // Display active profile and model information
    Console.WriteLine("Active Configuration:");
    Console.Write("  (");
    Console.Write($"Mode='{options.Mode}', ");
    Console.Write($"Profile='{chatClient.ActiveProfile.Name}', ");
    Console.Write($"Provider='{chatClient.ActiveProfile.ApiProvider}', ");
    Console.Write($"Model='{chatClient.ActiveProfile.ModelId}', ");
    Console.Write($"ToolApprovals='{chatClient.Config.ToolApprovals}'");
    Console.Write(")\n\n");

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("Available Providers:");
    Console.ForegroundColor = temp;
    foreach (var provider in chatClient.Config.ApiProviders)
    {
        Console.WriteLine($"  - {provider.Name}");
    }

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("\nAvailable Profiles:");
    Console.ForegroundColor = temp;

    // Table Header
    Console.WriteLine($"  {"Name",-20} {"Provider",-15} {"Model ID",-30} {"Active",-8} {"Default",-8}");
    Console.WriteLine($"  {"----",-20} {"--------",-15} {"--------",-30} {"------",-8} {"-------",-8}");

    foreach (var profile in chatClient.Config.Profiles)
    {
        var isActive = profile.Name == chatClient.ActiveProfile.Name ? "Yes" : "";
        var isDefault = profile.Default ? "Yes" : "";
        Console.WriteLine($"  {profile.Name,-20} {profile.ApiProvider,-15} {profile.ModelId,-30} {isActive,-8} {isDefault,-8}");
    }

    Console.WriteLine();
    Console.ForegroundColor = temp;
    return 0;
}

if (options.ShowVersion)
{
    Console.WriteLine(ArgumentParser.Version);
    return 0;
}


var appService = new AppService(chatClient);

var console = AnsiConsole.Create(new AnsiConsoleSettings());
var eventBus = new TuiEventBus();
var renderer = new ConsoleRenderer(console, eventBus);
var tuiAppService = new TuiAppService(appService, eventBus);

// This will be the main loop of the application.
// For now, we'll just publish a few events to demonstrate the new cards.

await eventBus.PublishAsync(new FileReadEvent("/path/to/some/file.txt"));
await Task.Delay(1000);
await eventBus.PublishAsync(new FilePatchedEvent("/path/to/another/file.txt", "--- a/file.txt\n+++ b/file.txt\n@@ -1,3 +1,3 @@\n-old line\n+new line\n same line"));

// In a real application, we would have a loop here that waits for user input
// and publishes events accordingly.
console.MarkupLine("[dim]Press any key to exit...[/]");
System.Console.ReadKey(true);

return 0;