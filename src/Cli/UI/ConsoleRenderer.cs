using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using MaxBot.Domain;
using Microsoft.Extensions.AI;

namespace Cli.UI;

public static class ConsoleRenderer
{
    public static void ConsoleWriteLLMResponseDetails(string response)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"\n{response}");
        Console.ForegroundColor = originalColor;
    }

    public static void ConsoleWriteError(string message)
    {
        var temp = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {message}");
        Console.ForegroundColor = temp;
    }

    public static void DisplayChatHistory(List<ChatMessage> chatHistory, string activeProfileName, bool showStatus, Func<List<ChatMessage>, int> calculateTokenMetrics)
    {
        var robotEmoji = char.ConvertFromUtf32(0x1F916);
        var folderEmoji = char.ConvertFromUtf32(0x1F4C2);
        var catHeartEyesEmoji = char.ConvertFromUtf32(0x1F63B);

        for (int i = 1; i < chatHistory.Count; i++)
        {
            var message = chatHistory[i];

            if (message.Role == ChatRole.User)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"\n{catHeartEyesEmoji} Max | {folderEmoji} {Directory.GetCurrentDirectory()} | {robotEmoji}: {activeProfileName}\n% ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(message.Text);
                Console.WriteLine();
            }
            else if (message.Role == ChatRole.Assistant)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write(message.Text);
                Console.WriteLine();

                if (showStatus)
                {
                    var partialHistory = chatHistory.Take(i + 1).ToList();
                    WriteTokenMetrics(partialHistory, calculateTokenMetrics);
                }
            }
        }
    }

    public static void WriteTokenMetrics(List<ChatMessage> chatHistory, Func<List<ChatMessage>, int> calculateTokenMetrics)
    {
        var currentForegroundColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;

        var tokenCount = calculateTokenMetrics(chatHistory);
        Console.WriteLine($"[Tokens Used: {tokenCount}]");
        
        Console.ForegroundColor = currentForegroundColor;
    }

    public static void ListChatSessions(List<string> sessions, string sessionsBasePath)
    {
        if (sessions.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No chat sessions found.");
            Console.WriteLine($"Sessions are stored in: {sessionsBasePath}");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Found {sessions.Count} chat sessions:");
        Console.WriteLine($"Sessions are stored in: {sessionsBasePath}");
        Console.WriteLine();
        Console.ResetColor();

        var sessionInfoList = new List<(string SessionPath, string LastUpdatedAt)>();

        foreach (var session in sessions)
        {
            string sessionPath = session;
            string filePath = Path.Combine(sessionPath, "chatHistory.json");
            string lastUpdatedAt = "0";

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
                    // Ignore errors
                }
            }
            sessionInfoList.Add((sessionPath, lastUpdatedAt));
        }

        var sortedSessions = sessionInfoList
            .OrderBy(s => s.LastUpdatedAt)
            .Select(s => s.SessionPath)
            .ToList();

        foreach (var session in sortedSessions)
        {
            RenderSessionCard(session);
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("To load a session, use: max chat -l <Session ID>");
        Console.ResetColor();
    }

    private static void RenderSessionCard(string sessionPath)
    {
        string sessionId = Path.GetFileName(sessionPath);
        string filePath = Path.Combine(sessionPath, "chatHistory.json");

        string createdDate = "Unknown";
        string lastUpdatedDate = "Unknown";
        string firstPrompt = "No prompt available";
        int messageCount = 0;

        if (sessionId.Length >= 15)
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
                // Ignore parsing errors
            }
        }

        if (File.Exists(filePath))
        {
            try
            {
                string jsonContent = File.ReadAllText(filePath);
                var historyRoot = JsonSerializer.Deserialize(jsonContent, ChatHistoryContext.Default.ChatHistoryRoot);

                if (historyRoot != null)
                {
                    if (!string.IsNullOrEmpty(historyRoot.CreatedAt))
                        createdDate = historyRoot.CreatedAt;
                    if (!string.IsNullOrEmpty(historyRoot.LastUpdatedAt))
                        lastUpdatedDate = historyRoot.LastUpdatedAt;
                    
                    messageCount = historyRoot.Messages.Count;
                    
                    var firstUserMessage = historyRoot.Messages.FirstOrDefault(m => m.Role.Equals("user", StringComparison.OrdinalIgnoreCase));
                    if (firstUserMessage != null && !string.IsNullOrEmpty(firstUserMessage.Content))
                    {
                        firstPrompt = firstUserMessage.Content;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"Warning: Could not read chat history for session {sessionId}: {ex.Message}");
                Console.ResetColor();
            }
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("┌─────────────────────────────────────────────────────────────┐");
        Console.WriteLine($"│ Session: {sessionId,-50} │");
        Console.WriteLine("├─────────────────────────────────────────────────────────────┤");
        Console.WriteLine($"│ Created:      {createdDate,-45} │");
        Console.WriteLine($"│ Last Updated: {lastUpdatedDate,-45} │");
        int messagesPadding = Math.Max(0, 45 - messageCount.ToString().Length - 8);
        Console.WriteLine($"│ Messages:     {messageCount} entries{new string(' ', messagesPadding)} │");
        Console.WriteLine("├─────────────────────────────────────────────────────────────┤");
        
        const int maxWidth = 55;
        const string promptPrefix = "│ First Prompt: \"";
        const string continuationPrefix = "│               ";
        const string rightBorder = "│";

        if (firstPrompt.Length <= maxWidth - promptPrefix.Length - 2)
        {
            int padding = Math.Max(0, 45 - firstPrompt.Length - 2);
            Console.WriteLine($"{promptPrefix}{firstPrompt}\"{new string(' ', padding)} {rightBorder}");
        }
        else
        {
            string[] words = firstPrompt.Split(' ');
            int firstLineMaxLength = maxWidth - promptPrefix.Length - 3;
            StringBuilder firstLineBuilder = new StringBuilder();
            int wordIndex = 0;

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

            int firstLinePadding = Math.Max(0, 45 - firstLineBuilder.Length - 1);
            Console.WriteLine($"{promptPrefix}{firstLineBuilder}{new string(' ', firstLinePadding)} {rightBorder}");

            if (wordIndex < words.Length)
            {
                int continuationMaxLength = maxWidth - continuationPrefix.Length - 3;
                while (wordIndex < words.Length)
                {
                    StringBuilder lineBuilder = new StringBuilder();
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

                    if (wordIndex >= words.Length)
                    {
                        int padding = Math.Max(0, 45 - lineBuilder.Length - 1);
                        Console.WriteLine($"{continuationPrefix}{lineBuilder}\"{new string(' ', padding)} {rightBorder}");
                    }
                    else
                    {
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
}
