﻿﻿﻿﻿﻿﻿﻿using FluentResults;
using MaxBot;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Parse command line arguments
var options = ParseCommandLine(args);

// Show help if requested
if (options.ShowHelp)
{
    DisplayHelp();
    return;
}

var aiName = "Max";
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("Welcome to the Max Chat!");
Console.WriteLine();

var clientResult = ChatClient.Create(options.ConfigPath, options.ProfileName);
if (clientResult.IsFailed)
{
    ConsoleWriteError(clientResult.ToResult());
}
var maxClient = clientResult.Value;

// Display active profile and model information
Console.WriteLine($"Using profile: '{maxClient.ActiveProfile.Name}' (Provider: {maxClient.ActiveProfile.ApiProvider}, Model: {maxClient.ActiveProfile.ModelId})");
Console.WriteLine();

try
{
    List<ChatMessage> chatHistory =
    [
        new(ChatRole.System, maxClient.SystemPrompt),
    ];

    // -------------------------------------------------------------------------------------------
    // Conversational Chat with the AI
    // -------------------------------------------------------------------------------------------
    while (true)
    {
        // Get user prompt and add to chat history
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("\n> ");
        var userPrompt = Console.ReadLine();
        
        // if the user prompt is empty, "exit", or "quit", then exit the chat loop
        if (string.IsNullOrWhiteSpace(userPrompt) || userPrompt.ToLower() == "exit" || userPrompt.ToLower() == "quit")
            break;

        chatHistory.Add(new ChatMessage(ChatRole.User, userPrompt));

        // Stream the AI Response and add to chat history            
        Console.WriteLine("Sending API Request...");
        Console.WriteLine($"\n{aiName}:");
        var response = "";
        await foreach (var item in maxClient.ChatClientMEAI.GetStreamingResponseAsync(chatHistory))
        {
            Console.Write(item.Text);
            response += item.Text;
        }
        chatHistory.Add(new ChatMessage(ChatRole.Assistant, response));
        Console.WriteLine();

        WriteTokenMetrics(chatHistory);
    }
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}


void ConsoleWriteError(Result result, bool exit = true)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error: {result.Errors.First().Message}");
    if (exit)
        Environment.Exit(1);
}


void WriteTokenMetrics(List<ChatMessage> chatHistory)
{   
    var currentForegroundColor = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Yellow;

    // calculate the percentage of tokens used against the 200K limit
    var tokenCount = MaxBot.Utils.ApiMetricUtils.GetSimplisticTokenCount(chatHistory);
    var percentage = (double)tokenCount / 200000 * 100;
    // if (OPENAI_API_MODEL!.Contains("claude"))
    // {
    //     Console.WriteLine($"[I/O Tokens Used: {tokenCount} of 200K, {percentage:N2}%]");
    // }
    // else
    // {
    Console.WriteLine($"[Tokens Used: {tokenCount}]");
    // }
    Console.ForegroundColor = currentForegroundColor;
}

// Parse command line arguments and return options
CommandLineOptions ParseCommandLine(string[] args)
{
    string configPath = "maxbot.config.json";
    string? profileName = null;
    bool showHelp = false;

    for (int i = 0; i < args.Length; i++)
    {
        if ((args[i] == "-c" || args[i] == "--config") && i + 1 < args.Length)
        {
            configPath = args[i + 1];
            i++; // Skip the next argument
        }
        else if ((args[i] == "-p" || args[i] == "--profile") && i + 1 < args.Length)
        {
            profileName = args[i + 1];
            i++; // Skip the next argument
        }
        else if (args[i] == "-h" || args[i] == "--help")
        {
            showHelp = true;
        }
    }

    return new CommandLineOptions
    {
        ConfigPath = configPath,
        ProfileName = profileName,
        ShowHelp = showHelp
    };
}

// Display help information
void DisplayHelp()
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("MaxBot CLI - Chat with AI models");
    Console.WriteLine();
    Console.WriteLine("Usage: max [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  -c, --config <path>    Path to the configuration file (default: maxbot.config.json)");
    Console.WriteLine("  -p, --profile <name>   Name of the profile to use (overrides default profile in config)");
    Console.WriteLine("  -h, --help             Display this help message");
    Console.ResetColor();
    Environment.Exit(0);
}

// Define a record to hold command-line options
public record CommandLineOptions
{
    public string ConfigPath { get; init; } = "maxbot.config.json";
    public string? ProfileName { get; init; }
    public bool ShowHelp { get; init; }
}
