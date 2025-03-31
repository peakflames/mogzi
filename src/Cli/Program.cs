using CLI;
using FluentResults;
using MaxBot;

// Parse command line arguments
var argResult   = CliArgParser.Parse(args);
if (argResult.IsFailed)
{
    Console.WriteLine(argResult.ToResult());
    return 1;
}

var options = argResult.Value;

// Show help if requested
if (options.ShowHelp)
{
    CliArgParser.DisplayHelp();
    return 0;
}


var clientResult = ChatClient.Create(options.ConfigPath, options.ProfileName);
if (clientResult.IsFailed)
{
    ConsoleWriteError(clientResult.ToResult());
}
var maxClient = clientResult.Value;

// Handle the active mode
var activeMode = maxClient.Config.DefaultMode;
if (!string.IsNullOrEmpty(options.Mode))
{
    activeMode = options.Mode;
}

// Show status if requested
if (options.ShowStatus)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("MaxBot!");
    Console.WriteLine();
    // Display active profile and model information
    Console.Write("(");
    Console.Write($"Mode: '{activeMode}', ");
    Console.Write($"Profile: '{maxClient.ActiveProfile.Name}', ");
    Console.Write($"Provider: {maxClient.ActiveProfile.ApiProvider}, ");
    Console.Write($"Model: {maxClient.ActiveProfile.ModelId}");
    Console.Write(")\n");
}



return await new App(maxClient, options.ShowStatus).Run(activeMode, options.UserPrompt);


void ConsoleWriteError(Result result, bool exit = true)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error: {result.Errors.First().Message}");
    if (exit)
        Environment.Exit(1);
}


