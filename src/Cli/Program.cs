using System.Reflection;
using System.Text;
using CLI;
using FluentResults;
using MaxBot;

// The most important fix - set console encoding to UTF-8
Console.OutputEncoding = Encoding.UTF8;

var exitCode = await Program.Run(args);
return exitCode;

public partial class Program 
{ 
    internal static async Task<int> Run(string[] args, ChatClient? chatClient = null)
    {
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

        if (args.Length == 1 && args[0] == "chat")
        {
            // This is a valid case, but we don't want to show help.
        }
        else if (string.IsNullOrEmpty(options.UserPrompt) && options.Mode == "oneshot")
        {
            CliArgParser.DisplayHelp();
            return 0;
        }

        if (chatClient == null)
        {
            var clientResult = ChatClient.Create(options.ConfigPath, options.ProfileName, App.ConsoleWriteLLMResponseDetails);
            if (clientResult.IsFailed)
            {
                App.ConsoleWriteError(clientResult.ToResult());
                return 1;
            }
            chatClient = clientResult.Value;
        }
        
    var maxClient = chatClient;

    // Show status if requested
    if (options.ShowStatus)
        {
            var temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            // Display active profile and model information
            Console.Write("(");
            Console.Write($"Mode='{options.Mode}', ");
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

        return await new App(maxClient, options.ShowStatus).Run(options.Mode, options.UserPrompt);
    }
}
