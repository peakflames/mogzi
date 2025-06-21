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
        var argResult = await CliArgParser.ParseAsync(args);
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
        else if (args.Length == 1 && options.ShowVersion)
        {
            Console.WriteLine(CliArgParser.Version);
            return 0;
        }
        else if (args.Length == 1 && options.ShowVersion)
        {
            Console.WriteLine(CliArgParser.Version);
            return 0;
        }
        else if (string.IsNullOrEmpty(options.UserPrompt) && options.Mode == "oneshot" && !options.ShowStatus)
        {
            CliArgParser.DisplayHelp();
            return 0;
        }

        if (chatClient == null)
        {
            var clientResult = ChatClient.Create(options.ConfigPath, options.ProfileName, options.ToolApprovals, options.Mode, App.ConsoleWriteLLMResponseDetails);
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
            Console.WriteLine("Active Configuration:");
            Console.Write("  (");
            Console.Write($"Mode='{options.Mode}', ");
            Console.Write($"Profile='{maxClient.ActiveProfile.Name}', ");
            Console.Write($"Provider='{maxClient.ActiveProfile.ApiProvider}', ");
            Console.Write($"Model='{maxClient.ActiveProfile.ModelId}'");
            Console.Write(")\n\n");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Available Providers:");
            Console.ForegroundColor = temp;
            foreach (var provider in maxClient.Config.ApiProviders)
            {
                Console.WriteLine($"  - {provider.Name}");
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\nAvailable Profiles:");
            Console.ForegroundColor = temp;

            // Table Header
            Console.WriteLine($"  {"Name",-20} {"Provider",-15} {"Model ID",-30} {"Active",-8} {"Default",-8}");
            Console.WriteLine($"  {"----",-20} {"--------",-15} {"--------",-30} {"------",-8} {"-------",-8}");

            foreach (var profile in maxClient.Config.Profiles)
            {
                var isActive = profile.Name == maxClient.ActiveProfile.Name ? "Yes" : "";
                var isDefault = profile.Default ? "Yes" : "";
                Console.WriteLine($"  {profile.Name,-20} {profile.ApiProvider,-15} {profile.ModelId,-30} {isActive,-8} {isDefault,-8}");
            }

            Console.WriteLine();
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
