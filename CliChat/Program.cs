﻿using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;

static void PrintUsage()
{
    Console.WriteLine("Usage: clichat [directory]");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  directory    Optional path to directory containing markdown files to load");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  -h, --help   Show this help message");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  clichat                  # Start with no files loaded");
    Console.WriteLine("  clichat ~/docs           # Load markdown files from ~/docs");
    Console.WriteLine("  clichat \"C:\\My Docs\"    # Load markdown files from Windows path");
    Environment.Exit(0);
}

// Handle help command
if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h"))
{
    PrintUsage();
    return;
}

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("Welcome to the CliChat!");
Console.WriteLine();

var encounteredError = false;
var OPENAI_API_KEY = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrWhiteSpace(OPENAI_API_KEY))
{
    encounteredError = true;
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("OPENAI_API_KEY environment variable is not set.");
    Console.WriteLine("Please set the environment variable to your OpenAI-Compatible Provider's API key.");
    Console.WriteLine();
} 

var OPENAI_API_BASE = Environment.GetEnvironmentVariable("OPENAI_API_BASE");
if (string.IsNullOrWhiteSpace(OPENAI_API_BASE))
{
    encounteredError = true;
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("OPENAI_API_BASE environment variable is not set.");
    Console.WriteLine("Please set the environment variable to your OpenAI-Compatible Provider's Base URL.");
    Console.WriteLine();
}

var OPENAI_API_MODEL = Environment.GetEnvironmentVariable("OPENAI_API_MODEL");
if (string.IsNullOrWhiteSpace(OPENAI_API_MODEL))
{
    encounteredError = true;
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("OPENAI_API_MODEL environment variable is not set.");
    Console.WriteLine("Please set the environment variable to yourOpenAI-Compatible Provider's Model Id.");
    Console.WriteLine();
}

if (encounteredError)
{
    Console.WriteLine("Exiting...");
    Environment.Exit(1);
}

var chatClient = new OpenAIClient(
    new ApiKeyCredential(OPENAI_API_KEY!),
    new OpenAIClientOptions { 
        Endpoint = new(OPENAI_API_BASE!)
    })
    .AsChatClient(OPENAI_API_MODEL!);

// Detect the current operating system, we need to handle Windows, MacOS, and Linux differently
var operatingSystem = Environment.OSVersion.Platform;
var defaultShell = operatingSystem switch {
    PlatformID.Win32NT => "powershell",
    PlatformID.MacOSX => "zsh",
    _ => "bash"
};

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine($"CliChat> Operating System: {operatingSystem}, Default Shell: {defaultShell}");
Console.WriteLine($"CliChat> OpenAI (BaseUri='{OPENAI_API_BASE}', Model='{OPENAI_API_MODEL}', ApiKey='******')");

var systemPrompt = SystemPrompt.Prompt(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), operatingSystem.ToString(), defaultShell);

List<ChatMessage> chatHistory =
[
    new(ChatRole.System, systemPrompt),
];

// -------------------------------------------------------------------------------------------
// ✅ Load markdown files from specified directory (if provided)
// -------------------------------------------------------------------------------------------
var dataDir = args.Length > 0 ? args[0] : null;

if (dataDir != null)
{
    if (!Directory.Exists(dataDir))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: Directory '{dataDir}' does not exist.");
        Environment.Exit(1);
    }

    Console.WriteLine($"CliChat> Loading files from: {dataDir}");

    // prepare the user prompt and add the all files to the chat history
    chatHistory.Add(new ChatMessage(ChatRole.User, 
    $"""
    I may have some Markdown files in the {dataDir} directory that I will provide you now. Once you have received the content of the file, only respond with "I have loaded the file FILENAME and its chunks".
    """));

    var files = Directory.GetFiles(dataDir, "*.md");

    foreach (var file in files)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"CliChat> Reading file: {file}...");
        var fileContents = File.ReadAllText(file) + Environment.NewLine;
        
        // Report the number words in the file
        var wordCount = fileContents.Split().Length;
        
        // if the wordCount is greater that 10000, then we must send the file contents in chunks of 10000 words each to the AI
        if (wordCount > 10000)
        {
            var userPrompt = $"""The file {file} has {wordCount} words. I will send it to you in 10000 word chunks.""";
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"CliChat> {userPrompt}");

            var chunkSize = 10000;
            var chunks = fileContents.Split(new string[] { " " }, StringSplitOptions.None);
            var chunkId = 0;
            for (int i = 0; i < chunks.Length; i += chunkSize, chunkId++)
            {
                var chunk = string.Join(" ", chunks.Skip(i).Take(chunkSize));
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"CliChat> Sending file {Path.GetFileName(file)}, chunk {chunkId} to AI...");
                chatHistory.Add(new ChatMessage(ChatRole.User,
                $"""
                Filename: {Path.GetFileName(file)}
                Chunk: {chunkId}
                File Contents:
                ```markdown
                {chunk}
                ```
                """));

                
                // Stream the AI Response and add to chat history
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Sending API Request...");
                var chunkResponse = await chatClient.GetResponseAsync(chatHistory);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nClaude:");
                Console.WriteLine(chunkResponse.Message.Text);
                chatHistory.Add(new ChatMessage(ChatRole.Assistant, chunkResponse.Message.Text));
                WriteTokenMetrics(chatHistory);
            }
        }
        else
        {
            Console.WriteLine($"CliChat> File has {wordCount} words. Sending to AI...");

            // add the file contents to the chat history
            chatHistory.Add(new ChatMessage(ChatRole.User,
            $"""
            Filename: {Path.GetFileName(file)}
            File Contents:
            ```markdown
            {fileContents}
            ```
            """));

            // Stream the AI Response and add to chat history
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Sending API Request...");
            var chunkResponse = await chatClient.GetResponseAsync(chatHistory);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nClaude:");
            Console.WriteLine(chunkResponse.Message.Text);
            chatHistory.Add(new ChatMessage(ChatRole.Assistant, chunkResponse.Message.Text));
            WriteTokenMetrics(chatHistory);
        }
    }

    Console.ForegroundColor = ConsoleColor.Yellow;
    if (files.Length == 0)
    {
        Console.WriteLine($"CliChat> No Markdown files found in '{dataDir}'");
        chatHistory.Add(new ChatMessage(ChatRole.User, 
            $"Disregard. I confirmed I do not have Markdown files in '{dataDir}' after all."));
    }
    else
    {
        Console.WriteLine("CliChat> Loaded Markdown files:");
        foreach (var file in files)
        {
            Console.WriteLine($"\t{Path.GetFileName(file)}");
        }
    }
}
else
{
    Console.WriteLine("CliChat> No directory specified. Starting empty chat.");
}

// -------------------------------------------------------------------------------------------
// ✅ Conversational Chat with the AI
// -------------------------------------------------------------------------------------------
while (true)
{
    // Get user prompt and add to chat history
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write("\nUser> ");
    var userPrompt = Console.ReadLine();
    
    // if the user prompt is empty, "exit", or "quit", then exit the chat loop
    if (string.IsNullOrWhiteSpace(userPrompt) || userPrompt.ToLower() == "exit" || userPrompt.ToLower() == "quit")
        break;

    chatHistory.Add(new ChatMessage(ChatRole.User, userPrompt));

    // Stream the AI Response and add to chat history
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.WriteLine("Sending API Request...");
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("\nClaude:");
    var response = "";
    await foreach (var item in chatClient.GetStreamingResponseAsync(chatHistory))
    {
        Console.Write(item.Text);
        response += item.Text;
    }
    chatHistory.Add(new ChatMessage(ChatRole.Assistant, response));
    Console.WriteLine();

    WriteTokenMetrics(chatHistory);
}

/// <summary>
/// Write the token metrics to the console.
/// </summary>
/// <param name="chatHistory">Chat history.</param>
void WriteTokenMetrics(List<ChatMessage> chatHistory)
{   
    var currentForegroundColor = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.DarkMagenta;

    // calculate the percentage of tokens used against the 200K limit
    var tokenCount = GetTokenCount(chatHistory);
    var percentage = (double)tokenCount / 200000 * 100;
    Console.WriteLine($"[I/O Tokens Used: {tokenCount} of 200K, {percentage:N2}%]");
    Console.ForegroundColor = currentForegroundColor;
}

/// <summary>
/// Calculate the number of tokens that the messages would consume.
/// Based on: https://github.com/openai/openai-cookbook/blob/main/examples/How_to_count_tokens_with_tiktoken.ipynb
/// </summary>
/// <param name="messages">Messages to calculate token count for.</param>
/// <returns>Number of tokens</returns>
int GetTokenCount(IEnumerable<ChatMessage> messages)
{
    const int TokensPerMessage = 3;
    const int TokensPerRole = 1;
    const int BaseTokens = 3;
    var disallowedSpecial = new HashSet<string>();

    var tokenCount = BaseTokens;

    var encoding = SharpToken.GptEncoding.GetEncoding("cl100k_base");
    foreach (var message in messages)
    {
        tokenCount += TokensPerMessage;
        tokenCount += TokensPerRole;
        tokenCount += encoding.Encode(message.Text, disallowedSpecial).Count;
    }

    return tokenCount;
}
