﻿using FluentResults;
using MaxBot;
using Microsoft.Extensions.AI;


var aiName = "Max";
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("Welcome to the Max Chat!");
Console.WriteLine();


var clientResult = ChatClient.Create("maxbot.config.json");
if (clientResult.IsFailed)
{
    ConsoleWriteError(clientResult.ToResult());
}
var maxClient = clientResult.Value;

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