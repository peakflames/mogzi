using FluentResults;
using MaxBot;
using MaxBot.Domain;
using MaxBot.Services;
using Microsoft.Extensions.AI;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace CLI;

public class App
{
    private readonly IAppService _appService;
    private readonly bool _showStatus;
    private string? _currentSessionPath;
    private readonly string _activeProfileName;

    public App(ChatClient maxClient, bool showStatus)
    {
        _showStatus = showStatus;
        _appService = new AppService(maxClient);
        _activeProfileName = maxClient.ActiveProfile.Name;
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
            var sessions = _appService.GetChatSessions();
            
            if (sessions.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No chat sessions found.");
                Console.WriteLine($"Sessions are stored in: {_appService.GetChatSessionsBasePath()}");
                Console.ResetColor();
                return 0;
            }
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Found {sessions.Count} chat sessions:");
            Console.WriteLine($"Sessions are stored in: {_appService.GetChatSessionsBasePath()}");
            Console.WriteLine();
            Console.ResetColor();
            
            // Create a list to hold session info for sorting
            var sessionInfoList = new List<(string SessionPath, string LastUpdatedAt)>();
            
            // Collect session info for sorting
            foreach (var session in sessions)
            {
                string sessionPath = session;
                string filePath = Path.Combine(sessionPath, "chatHistory.json");
                string lastUpdatedAt = "0"; // Default for sorting if we can't read the file
                
                // Try to read the last updated date from the chat history file
                if (File.Exists(filePath))
                {
                    try
                    {
                        string jsonContent = File.ReadAllText(filePath);
                        var historyRoot = JsonSerializer.Deserialize(jsonContent, ChatHistoryContext.Default.ChatHistoryRoot);
                        
                        if (historyRoot != null && !string.IsNullOrEmpty(historyRoot.LastUpdatedAt))
                        {
                            lastUpdatedAt = historyRoot.LastUpdatedAt;
                        }
                    }
                    catch
                    {
                        // If there's an error reading the file, just use the default value
                    }
                }
                
                sessionInfoList.Add((sessionPath, lastUpdatedAt));
            }
            
            // Sort sessions by LastUpdatedAt in descending order
            var sortedSessions = sessionInfoList
                .OrderBy(s => s.LastUpdatedAt)
                .Select(s => s.SessionPath)
                .ToList();
            
            // Display sessions in sorted order
            foreach (var session in sortedSessions)
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
                const string rightBorder = "│";
                
                // Display the first line with the "First Prompt:" prefix
                if (firstPrompt.Length <= maxWidth - promptPrefix.Length - 2) // -2 for the quotes
                {
                    // Short prompt fits on one line
                    int padding = Math.Max(0, 45 - firstPrompt.Length - 2);
                    Console.WriteLine($"{promptPrefix}{firstPrompt}\"{new string(' ', padding)} {rightBorder}");
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
                    Console.WriteLine($"{promptPrefix}{firstLineBuilder}{new string(' ', firstLinePadding)} {rightBorder}");
                    
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
                                Console.WriteLine($"{continuationPrefix}{lineBuilder}\"{new string(' ', padding)} {rightBorder}");
                            }
                            else
                            {
                                // Add proper spacing before the right border
                                int padding = Math.Max(0, 45 - lineBuilder.Length);
                                Console.WriteLine($"{continuationPrefix}{lineBuilder}{new string(' ', padding)} {rightBorder}");
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
            var catHeartEyesEmoji = char.ConvertFromUtf32(0x1F63B); // 😻
            
            // Initialize chat history with system prompt
            List<ChatMessage> chatHistory =
            [
                new(ChatRole.System, _appService.SystemPrompt),
            ];
            
            // Try to load a previous session if specified
            if (!string.IsNullOrEmpty(sessionId))
            {
                string sessionPath = Path.Combine(_appService.GetChatSessionsBasePath(), sessionId);
                if (Directory.Exists(sessionPath))
                {
                    _currentSessionPath = sessionPath;
                    // Pass the system prompt to be injected as the first message
                    var loadedHistory = await _appService.LoadChatSessionAsync(sessionId, _appService.SystemPrompt);
                    
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
                                Console.Write($"\n{catHeartEyesEmoji} Max | {folderEmoji} {Directory.GetCurrentDirectory()} | {robotEmoji}: {_activeProfileName}\n% ");
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
                                
                                if (_showStatus)
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
                        _currentSessionPath = _appService.CreateChatSession();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Session not found: {sessionId}");
                    Console.WriteLine("Starting a new session instead.");
                    Console.ResetColor();
                    
                    // Create a new session since the specified one doesn't exist
                    _currentSessionPath = _appService.CreateChatSession();
                }
            }
            else
            {
                // Create a new chat session
                _currentSessionPath = _appService.CreateChatSession();
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

                Console.Write($"\n{catHeartEyesEmoji} Max | {folderEmoji} {cwd} | {robotEmoji}: {_activeProfileName}\n% ");    
                Console.ForegroundColor = ConsoleColor.White;
                var userPrompt = Console.ReadLine();
                Console.WriteLine();
                
                Console.ForegroundColor = ConsoleColor.Blue;
                // if the user prompt is empty, "exit", or "quit", then exit the chat loop
                if (string.IsNullOrWhiteSpace(userPrompt) || userPrompt.ToLower() == "exit" || userPrompt.ToLower() == "quit")
                    break;
                
                // Check if the input is a slash command
                if (userPrompt.StartsWith("/"))
                {
                    // Handle slash command
                    if (HandleSlashCommand(userPrompt, chatHistory))
                    {
                        // Command was handled, continue to next iteration
                        continue;
                    }
                }

                chatHistory.Add(new ChatMessage(ChatRole.User, userPrompt));

                // Create a cancellation token source that can be used to cancel the API request
                using var cts = new CancellationTokenSource();
                
                // Create a flag to track if we're currently processing an API request
                bool isProcessingRequest = true;
                
                // Register a console cancel event handler to handle Ctrl+C
                ConsoleCancelEventHandler cancelHandler = (sender, e) => {
                    // Prevent the process from terminating
                    e.Cancel = true;
                    
                    // Only try to cancel if we're still processing the request
                    if (isProcessingRequest && !cts.IsCancellationRequested && !cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            Console.WriteLine("\n\nCancelling API request...");
                            cts.Cancel();
                        }
                        catch (ObjectDisposedException)
                        {
                            // Ignore if the CancellationTokenSource has been disposed
                            Console.WriteLine("\n\nRequest already completed.");
                        }
                    }
                };
                
                // Add the event handler
                Console.CancelKeyPress += cancelHandler;
                
                try
                {
                    // Stream the AI Response and add to chat history
                    var response = "";
                    await foreach (var item in _appService.ProcessChatMessageAsync(chatHistory, cts.Token))
                    {
                        Console.Write(item.Text);
                        response += item.Text;
                    }
                    chatHistory.Add(new ChatMessage(ChatRole.Assistant, response));
                    Console.WriteLine();
                }
                catch (OperationCanceledException)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\nAPI request was cancelled.");
                    Console.ResetColor();
                    
                    // Add a placeholder message to the chat history
                    chatHistory.Add(new ChatMessage(ChatRole.Assistant, "[Response was cancelled by user]"));
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\nError during API request: {ex.Message}");
                    Console.ResetColor();
                    
                    // Add a placeholder message to the chat history
                    chatHistory.Add(new ChatMessage(ChatRole.Assistant, $"[Error: {ex.Message}]"));
                }
                finally
                {
                    // Mark that we're no longer processing a request
                    isProcessingRequest = false;
                    
                    // Remove the event handler
                    Console.CancelKeyPress -= cancelHandler;
                }

                if (_showStatus)
                {
                    WriteTokenMetrics(chatHistory);
                }
                
                // Save chat history after each interaction
                await _appService.SaveChatHistoryAsync(_currentSessionPath, chatHistory);
            }
            
            // Final save of chat history before exiting
            if (_currentSessionPath != null)
            {
                await _appService.SaveChatHistoryAsync(_currentSessionPath, chatHistory);
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
                new(ChatRole.System, _appService.SystemPrompt),
            ];

            // if the user prompt is empty, "exit", or "quit", then exit the chat loop
            if (string.IsNullOrWhiteSpace(userPrompt) || userPrompt.ToLower() == "exit" || userPrompt.ToLower() == "quit")
            {
                return 0;
            }

            chatHistory.Add(new ChatMessage(ChatRole.User, userPrompt));

            // Create a cancellation token source that can be used to cancel the API request
            using var cts = new CancellationTokenSource();
            
            // Create a flag to track if we're currently processing an API request
            bool isProcessingRequest = true;
            
            // Register a console cancel event handler to handle Ctrl+C
            ConsoleCancelEventHandler cancelHandler = (sender, e) => {
                // Prevent the process from terminating
                e.Cancel = true;
                
                // Only try to cancel if we're still processing the request
                if (isProcessingRequest && !cts.IsCancellationRequested && !cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        Console.WriteLine("\n\nCancelling API request...");
                        cts.Cancel();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Ignore if the CancellationTokenSource has been disposed
                        Console.WriteLine("\n\nRequest already completed.");
                    }
                }
            };
            
            // Add the event handler
            Console.CancelKeyPress += cancelHandler;
            
            try
            {
                // Stream the AI Response and add to chat history
                var response = "";
                await foreach (var item in _appService.ProcessChatMessageAsync(chatHistory, cts.Token))
                {
                    Console.Write(item.Text);
                    response += item.Text;
                }
                chatHistory.Add(new ChatMessage(ChatRole.Assistant, response));
                Console.WriteLine();
            }
            catch (OperationCanceledException)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\nAPI request was cancelled.");
                Console.ResetColor();
                
                // Add a placeholder message to the chat history
                chatHistory.Add(new ChatMessage(ChatRole.Assistant, "[Response was cancelled by user]"));
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nError during API request: {ex.Message}");
                Console.ResetColor();
                
                // Add a placeholder message to the chat history
                chatHistory.Add(new ChatMessage(ChatRole.Assistant, $"[Error: {ex.Message}]"));
            }
            finally
            {
                // Mark that we're no longer processing a request
                isProcessingRequest = false;
                
                // Remove the event handler
                Console.CancelKeyPress -= cancelHandler;
            }

            if (_showStatus)
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

    private void WriteTokenMetrics(List<ChatMessage> chatHistory)
    {   
        var currentForegroundColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;

        // calculate the percentage of tokens used against the 200K limit
        var tokenCount = _appService.CalculateTokenMetrics(chatHistory);
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

    /// <summary>
    /// Handles slash commands in chat mode
    /// </summary>
    /// <param name="command">The command string (including the slash)</param>
    /// <param name="chatHistory">The current chat history</param>
    /// <returns>True if the command was handled, false otherwise</returns>
    private bool HandleSlashCommand(string command, List<ChatMessage> chatHistory)
    {
        // Split the command into parts (command and arguments)
        var parts = command.Split(' ', 2);
        var cmd = parts[0].ToLower();
        var args = parts.Length > 1 ? parts[1] : string.Empty;

        switch (cmd)
        {
            case "/status":
                DisplayStatus();
                return true;
            case "/tool-approval":
                SetToolApproval(args);
                return true;
            case "/sessions":
                ListSessions();
                return true;
            case "/load-session":
                LoadSession(args, chatHistory);
                return true;
            case "/help":
                DisplayHelp();
                return true;
            default:
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Unknown command: {cmd}. Type /help for available commands.");
                Console.ResetColor();
                return true;
        }
    }

    /// <summary>
    /// Displays the current status (similar to --status flag)
    /// </summary>
    private void DisplayStatus()
    {
        var temp = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;

        var chatClient = _appService.ChatClient;

        // Display active profile and model information
        Console.WriteLine("Active Configuration:");
        Console.Write("  (");
        Console.Write($"Mode='chat', ");
        Console.Write($"Profile='{_activeProfileName}', ");
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
    }

    /// <summary>
    /// Sets the tool approval setting
    /// </summary>
    /// <param name="args">The arguments (readonly or all)</param>
    private void SetToolApproval(string args)
    {
        if (string.IsNullOrWhiteSpace(args) || (args != "readonly" && args != "all"))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Invalid value. Usage: /tool-approval [readonly|all]");
            Console.ResetColor();
            return;
        }

        var chatClient = _appService.ChatClient;
        
        // Update the tool approval setting
        chatClient.Config.ToolApprovals = args;
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Tool approval setting updated to '{args}'");
        Console.ResetColor();
    }

    /// <summary>
    /// Lists available chat sessions
    /// </summary>
    private void ListSessions()
    {
        ListChatSessions();
    }

    /// <summary>
    /// Loads a chat session
    /// </summary>
    /// <param name="sessionId">The session ID to load</param>
    /// <param name="chatHistory">The current chat history</param>
    private void LoadSession(string sessionId, List<ChatMessage> chatHistory)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Invalid session ID. Usage: /load-session [ID]");
            Console.ResetColor();
            return;
        }

        // Check if the session exists
        string sessionPath = Path.Combine(_appService.GetChatSessionsBasePath(), sessionId);
        if (!Directory.Exists(sessionPath))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Session not found: {sessionId}");
            Console.ResetColor();
            return;
        }

        // Save current session
        if (_currentSessionPath != null)
        {
            _appService.SaveChatHistoryAsync(_currentSessionPath, chatHistory).Wait();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Current session saved to: {_currentSessionPath}");
        }

        // Load the new session
        _currentSessionPath = sessionPath;
        var loadedHistory = _appService.LoadChatSessionAsync(sessionId, _appService.SystemPrompt).Result;
        
        if (loadedHistory != null && loadedHistory.Count > 0)
        {
            // Clear the current chat history and add the loaded messages
            chatHistory.Clear();
            foreach (var message in loadedHistory)
            {
                chatHistory.Add(message);
            }
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Loaded chat session: {sessionId}");
            Console.WriteLine($"Loaded {chatHistory.Count} messages from session");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Could not load chat history from session: {sessionId}");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Displays help for slash commands
    /// </summary>
    private void DisplayHelp()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Available Slash Commands:");
        Console.WriteLine("  /status                 - Display current configuration");
        Console.WriteLine("  /tool-approval [mode]   - Set tool approval mode (readonly|all)");
        Console.WriteLine("  /sessions               - List available chat sessions");
        Console.WriteLine("  /load-session [ID]      - Load a specific chat session");
        Console.WriteLine("  /help                   - Display this help message");
        Console.WriteLine();
        Console.WriteLine("Other Commands:");
        Console.WriteLine("  exit, quit              - Exit the chat");
        Console.ResetColor();
    }
}
