using System.Reflection;
using System.Text;
using CLI;
using FluentResults;
using MaxBot;

// The most important fix - set console encoding to UTF-8
Console.OutputEncoding = Encoding.UTF8;

// Parse command line arguments
var argResult = CliArgParser.Parse(args);
if (argResult.IsFailed)
{
    App.ConsoleWriteError(argResult.ToResult());
    return 1;
}

var options = argResult.Value;

// Show help if requested
if (options.ShowHelp)
{
    CliArgParser.DisplayHelp();
    return 0;
}


var clientResult = ChatClient.Create(options.ConfigPath, options.ProfileName, App.ConsoleWriteLLMResponseDetails);
if (clientResult.IsFailed)
{
    App.ConsoleWriteError(clientResult.ToResult());
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
    var temp = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Yellow;
    // Display active profile and model information
    Console.Write("(");
    Console.Write($"Mode='{activeMode}', ");
    Console.Write($"Profile='{maxClient.ActiveProfile.Name}', ");
    Console.Write($"Provider='{maxClient.ActiveProfile.ApiProvider}', ");
    Console.Write($"Model='{maxClient.ActiveProfile.ModelId}'");
    Console.Write(")\n");
    Console.ForegroundColor = temp;
    return 0;
}

if (options.ShowVersion)
{
    Console.WriteLine(CliArgParser.Version);
    return 0;
}


return await new App(maxClient, options.ShowStatus).Run(activeMode, options.UserPrompt);





