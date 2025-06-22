using FluentResults;
using MaxBot;
using MaxBot.Domain;
using Microsoft.Extensions.AI;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace CLI;

public class App
{
    private ChatClient maxClient;
    private bool showStatus;
    private readonly ChatHistoryService _chatHistoryService;
    private string? _currentSessionPath;
    // private string aiName = "Max";


    public App(ChatClient maxClient, bool showStatus)
    {
        this.showStatus = showStatus;
        this.maxClient = maxClient;
        _chatHistoryService = new ChatHistoryService();
    }

    public static void ConsoleWriteLLMResponseDetails(string response)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"\n{response}");
        Console.ForegroundColor = originalColor;
    }

    public static void ConsoleWriteError(Result result)
    {
        var temp = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {result.Errors.FirstOrDefault()?.Message}");
        Console.ForegroundColor = temp;
    }

    public async Task<int> Run(string activeMode, string? userPrompt = null, string? loadSession = null)
    {
        var retval = 0;

        if (activeMode == "chat")
        {
            retval = await StartChatAsync(loadSession);
        }
        else if (activeMode == "oneshot")
        {
            retval = await StartOneShotAsync(userPrompt);
        }
        else if (activeMode == "list-sessions")
        {
            retval = ListChatSessions();
        }
        else
        {
            Console.WriteLine($"Invalid mode: {activeMode}");
            return 1;
        }

        return retval;
    }
    
    public int ListChatSessions()
    {
        try
        {
            var sessions = _chatHistoryService.GetChatSessions();
            
            if (sessions.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No chat sessions found.");
                Console.WriteLine($"Sessions are stored in: {_chatHistoryService.GetBasePath()}");
                Console.ResetColor();
                return 0;
            }
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Found {sessions.Count} chat sessions:");
            Console.WriteLine($"Sessions are stored in: {_chatHistoryService.GetBasePath()}");
            Console.WriteLine();
            Console.ResetColor();
            
            foreach (var session in sessions)
            {
                string sessionId = Path.GetFileName(session);
                string sessionPath = session;
                string filePath = Path.Combine(sessionPath, "chatHistory.json");
                
                // Default values in case we can't read the file
                string createdDate = "Unknown";
                string lastUpdatedDate = "Unknown";
                string firstPrompt = "No prompt available";
                int messageCount = 0;
                
                // Try to parse the timestamp from the session ID as a fallback
                if (sessionId.Length >= 15) // Format: YYYYMMDD_HHMMSS
                {
                    try
                    {
                        string year = sessionId.Substring(0, 4);
                        string month = sessionId.Substring(4, 2);
                        string day = sessionId.Substring(6, 2);
                        string hour = sessionId.Substring(9, 2);
                        string minute = sessionId.Substring(11, 2);
                        string second = sessionId.Substring(13, 2);
                        
                        createdDate = $"{year}-{month}-{day} {hour}:{minute}:{second}";
                    }
                    catch
                    {
                        // If parsing fails, keep the default "Unknown"
                    }
                }
                
                // Try to read the chat history file
                if (File.Exists(filePath))
                {
                    try
                    {
                        // Read the file synchronously since we're in a synchronous method
                        string jsonContent = File.ReadAllText(filePath);
                        var historyRoot = JsonSerializer.Deserialize(jsonContent, ChatHistoryContext.Default.ChatHistoryRoot);
                        
                        if (historyRoot != null)
                        {
                            // Get created and updated dates
                            if (!string.IsNullOrEmpty(historyRoot.CreatedAt))
                            {
                                createdDate = historyRoot.CreatedAt;
                            }
                            
                            if (!string.IsNullOrEmpty(historyRoot.LastUpdatedAt))
                            {
                                lastUpdatedDate = historyRoot.LastUpdatedAt;
                            }
                            
                            // Get message count
                            messageCount = historyRoot.Messages.Count;
                            
                            // Find the first user prompt
                            var firstUserMessage = historyRoot.Messages.FirstOrDefault(m => 
                                m.Role.Equals("user", StringComparison.OrdinalIgnoreCase));
                            
                            if (firstUserMessage != null && !string.IsNullOrEmpty(firstUserMessage.Content))
                            {
                                // Use the full prompt text (will be wrapped when displayed)
                                firstPrompt = firstUserMessage.Content;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // If there's an error reading the file, just use the default values
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine($"Warning: Could not read chat history for session {sessionId}: {ex.Message}");
                        Console.ResetColor();
                    }
                }
                
                // Display the card
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("┌─────────────────────────────────────────────────────────────┐");
                Console.WriteLine($"│ Session: {sessionId,-50} │");
                Console.WriteLine("├─────────────────────────────────────────────────────────────┤");
                Console.WriteLine($"│ Created:      {createdDate,-45} │");
                Console.WriteLine($"│ Last Updated: {lastUpdatedDate,-45} │");
                // Calculate padding for message count, ensuring it's never negative
                int messagesPadding = Math.Max(0, 45 - messageCount.ToString().Length - 8);
                Console.WriteLine($"│ Messages:     {messageCount} entries{new string(' ', messagesPadding)} │");
                Console.WriteLine("├─────────────────────────────────────────────────────────────┤");
                // Word wrap the first prompt
                const int maxWidth = 55;
                const string promptPrefix = "│ First Prompt: \"";
                const string continuationPrefix = "│               ";
                
                // Display the first line with the "First Prompt:" prefix
                if (firstPrompt.Length <= maxWidth - promptPrefix.Length - 2) // -2 for the quotes
                {
                    // Short prompt fits on one line
                    int padding = Math.Max(0, 45 - firstPrompt.Length - 2);
                    Console.WriteLine($"{promptPrefix}{firstPrompt}\"{new string(' ', padding)} │");
                }
                else
                {
                    // Implement smarter word wrapping that respects word boundaries
                    // First, split the prompt into words
                    string[] words = firstPrompt.Split(' ');
                    
                    // Build the first line
                    int firstLineMaxLength = maxWidth - promptPrefix.Length - 3; // -3 for the opening quote and space
                    StringBuilder firstLineBuilder = new StringBuilder();
                    int wordIndex = 0;
                    
                    // Add words to the first line until we reach the max length
                    while (wordIndex < words.Length)
                    {
                        if (firstLineBuilder.Length + words[wordIndex].Length + (firstLineBuilder.Length > 0 ? 1 : 0) <= firstLineMaxLength)
                        {
                            if (firstLineBuilder.Length > 0)
                                firstLineBuilder.Append(' ');
                            firstLineBuilder.Append(words[wordIndex]);
                            wordIndex++;
                        }
                        else
                            break;
                    }
                    
                    // Output the first line with proper right-aligned pipe
                    int firstLinePadding = Math.Max(0, 45 - firstLineBuilder.Length - 1);
                    Console.WriteLine($"{promptPrefix}{firstLineBuilder}{new string(' ', firstLinePadding)} │");
                    
                    // Process remaining words for continuation lines
                    if (wordIndex < words.Length)
                    {
                        int continuationMaxLength = maxWidth - continuationPrefix.Length - 3; // -3 for space and right border
                        
                        while (wordIndex < words.Length)
                        {
                            StringBuilder lineBuilder = new StringBuilder();
                            
                            // Add words to the current continuation line
                            while (wordIndex < words.Length)
                            {
                                if (lineBuilder.Length + words[wordIndex].Length + (lineBuilder.Length > 0 ? 1 : 0) <= continuationMaxLength)
                                {
                                    if (lineBuilder.Length > 0)
                                        lineBuilder.Append(' ');
                                    lineBuilder.Append(words[wordIndex]);
                                    wordIndex++;
                                }
                                else
                                    break;
                            }
                            
                            // Output the continuation line
                            if (wordIndex >= words.Length)
                            {
                                // Last line, add the closing quote
                                int padding = Math.Max(0, 45 - lineBuilder.Length - 1);
                                Console.WriteLine($"{continuationPrefix}{lineBuilder}\"{new string(' ', padding)} │");
                            }
                            else
                            {
                                Console.WriteLine($"{continuationPrefix}{lineBuilder} │");
                            }
                        }
                    }
                }
                Console.WriteLine("└─────────────────────────────────────────────────────────────┘");
                Console.WriteLine();
                Console.ResetColor();
            }
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("To load a session, use: max chat -l <Session ID>");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error listing chat sessions: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
        
        return 0;
    }

    public async Task<int> StartChatAsync(string? sessionId = null)
    {
        try
        {
            var robotEmoji = char.ConvertFromUtf32(0x1F916);  // 🤖
            var folderEmoji = char.ConvertFromUtf32(0x1F4C2); // 📂
            
            // Initialize chat history with system prompt
            List<ChatMessage> chatHistory =
            [
                new(ChatRole.System, maxClient.SystemPrompt),
            ];
            
            // Try to load a previous session if specified
            if (!string.IsNullOrEmpty(sessionId))
            {
                string sessionPath = Path.Combine(_chatHistoryService.GetBasePath(), sessionId);
                if (Directory.Exists(sessionPath))
                {
                    _currentSessionPath = sessionPath;
                    // Pass the system prompt to be injected as the first message
                    var loadedHistory = await _chatHistoryService.LoadChatHistoryAsync(sessionPath, maxClient.SystemPrompt);
                    
                    if (loadedHistory != null && loadedHistory.Count > 0)
                    {
                        // Use the loaded history with the system prompt already injected
                        chatHistory = loadedHistory;
                        
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Loaded chat session: {sessionId}");
                        Console.WriteLine($"Loaded {chatHistory.Count} messages from previous session");
                        Console.ResetColor();
                        
                        // Display the loaded chat history to make it look like you just had those chats
                        Console.WriteLine("\n--- Chat History ---");
                        
                        // Skip the system message when displaying
                        for (int i = 1; i < chatHistory.Count; i++)
                        {
                            var message = chatHistory[i];
                            
                            if (message.Role == ChatRole.User)
                            {
                                // Display user message
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.Write($"\n{robotEmoji} Max | {folderEmoji} {Directory.GetCurrentDirectory()}\n% ");
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.WriteLine(message.Text);
                                Console.WriteLine();
                            }
                            else if (message.Role == ChatRole.Assistant)
                            {
                                // Display assistant message
                                Console.ForegroundColor = ConsoleColor.Blue;
                                Console.Write(message.Text);
                                Console.WriteLine();
                                
                                if (showStatus)
                                {
                                    // Calculate tokens up to this point in the conversation
                                    var partialHistory = chatHistory.Take(i + 1).ToList();
                                    WriteTokenMetrics(partialHistory);
                                }
                            }
                        }
                        
                        Console.WriteLine("\n--- End of History ---\n");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Could not load chat history from session: {sessionId}");
                        Console.WriteLine("Starting a new session instead.");
                        Console.ResetColor();
                        
                        // Create a new session since we couldn't load the specified one
                        _currentSessionPath = _chatHistoryService.CreateChatSession();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Session not found: {sessionId}");
                    Console.WriteLine("Starting a new session instead.");
                    Console.ResetColor();
                    
                    // Create a new session since the specified one doesn't exist
                    _currentSessionPath = _chatHistoryService.CreateChatSession();
                }
            }
            else
            {
                // Create a new chat session
                _currentSessionPath = _chatHistoryService.CreateChatSession();
            }
            
            // Display chat session information
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Chat session: {Path.GetFileName(_currentSessionPath)}");
            Console.WriteLine($"Chat history will be saved to: {_currentSessionPath}");
            Console.ResetColor();

            // -------------------------------------------------------------------------------------------
            // Conversational Chat with the AI
            // -------------------------------------------------------------------------------------------
            while (true)
            {
                // Get user prompt and add to chat history
                Console.ForegroundColor = ConsoleColor.Yellow;
                
                // Get the name of the current working directory, but just the final part of the path
                var cwd = Directory.GetCurrentDirectory();

                Console.Write($"\n{robotEmoji} Max | {folderEmoji} {cwd}\n% ");    
                Console.ForegroundColor = ConsoleColor.White;
                var userPrompt = Console.ReadLine();
                Console.WriteLine();
                
                Console.ForegroundColor = ConsoleColor.Blue;
                // if the user prompt is empty, "exit", or "quit", then exit the chat loop
                if (string.IsNullOrWhiteSpace(userPrompt) || userPrompt.ToLower() == "exit" || userPrompt.ToLower() == "quit")
                    break;

                chatHistory.Add(new ChatMessage(ChatRole.User, userPrompt));

                // Stream the AI Response and add to chat history            
                // Console.WriteLine("Sending API Request...");
                // Console.WriteLine($"\n{aiName}:");
                var response = "";
                await foreach (var item in maxClient.ChatClientMEAI.GetStreamingResponseAsync(chatHistory, maxClient.ChatOptions))
                {
                    Console.Write(item.Text);
                    response += item.Text;
                }
                chatHistory.Add(new ChatMessage(ChatRole.Assistant, response));
                Console.WriteLine();

                if (showStatus)
                {
                    WriteTokenMetrics(chatHistory);
                }
                
                // Save chat history after each interaction
                await _chatHistoryService.SaveChatHistoryAsync(_currentSessionPath, chatHistory);
            }
            
            // Final save of chat history before exiting
            if (_currentSessionPath != null)
            {
                await _chatHistoryService.SaveChatHistoryAsync(_currentSessionPath, chatHistory);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Chat history saved to: {_currentSessionPath}");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }

        return 0;
    }

    public async Task<int> StartOneShotAsync(string? userPrompt)
    {
        try
        {
            List<ChatMessage> chatHistory =
            [
                new(ChatRole.System, maxClient.SystemPrompt),
            ];

            // if the user prompt is empty, "exit", or "quit", then exit the chat loop
            if (string.IsNullOrWhiteSpace(userPrompt) || userPrompt.ToLower() == "exit" || userPrompt.ToLower() == "quit")
            {
                return 0;
            }

            chatHistory.Add(new ChatMessage(ChatRole.User, userPrompt));

            var response = "";
            await foreach (var item in maxClient.ChatClientMEAI.GetStreamingResponseAsync(chatHistory, maxClient.ChatOptions))
            {
                Console.Write(item.Text);
                response += item.Text;
            }
            chatHistory.Add(new ChatMessage(ChatRole.Assistant, response));
            Console.WriteLine();

            if (showStatus)
            {
                WriteTokenMetrics(chatHistory);
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }

        return 0;
    }

    private static void WriteTokenMetrics(List<ChatMessage> chatHistory)
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
}
