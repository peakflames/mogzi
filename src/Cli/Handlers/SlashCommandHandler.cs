using System;
using System.Collections.Generic;
using System.IO;
using Cli.UI;
using MaxBot.Services;
using Microsoft.Extensions.AI;

namespace Cli.Handlers;

public class SlashCommandHandler
{
    private readonly IAppService _appService;
    private string? _currentSessionPath;
    private readonly string _activeProfileName;

    public SlashCommandHandler(IAppService appService, string activeProfileName, string? currentSessionPath)
    {
        _appService = appService;
        _activeProfileName = activeProfileName;
        _currentSessionPath = currentSessionPath;
    }

    public bool Handle(string command, List<ChatMessage> chatHistory)
    {
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

    private void DisplayStatus()
    {
        var temp = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;

        var chatClient = _appService.ChatClient;

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
        chatClient.Config.ToolApprovals = args;
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Tool approval setting updated to '{args}'");
        Console.ResetColor();
    }

    private void ListSessions()
    {
        var sessions = _appService.GetChatSessions();
        ConsoleRenderer.ListChatSessions(sessions, _appService.GetChatSessionsBasePath());
    }

    private void LoadSession(string sessionId, List<ChatMessage> chatHistory)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Invalid session ID. Usage: /load-session [ID]");
            Console.ResetColor();
            return;
        }

        string sessionPath = Path.Combine(_appService.GetChatSessionsBasePath(), sessionId);
        if (!Directory.Exists(sessionPath))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Session not found: {sessionId}");
            Console.ResetColor();
            return;
        }

        if (_currentSessionPath != null)
        {
            _appService.SaveChatHistoryAsync(_currentSessionPath, chatHistory).Wait();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Current session saved to: {_currentSessionPath}");
        }

        _currentSessionPath = sessionPath;
        var loadedHistory = _appService.LoadChatSessionAsync(sessionId, _appService.SystemPrompt).Result;
        
        if (loadedHistory != null && loadedHistory.Count > 0)
        {
            chatHistory.Clear();
            foreach (var message in loadedHistory)
            {
                chatHistory.Add(message);
            }
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Loaded chat session: {sessionId}");
            Console.WriteLine($"Loaded {chatHistory.Count} messages from session");
            Console.ResetColor();
            
            ConsoleRenderer.DisplayChatHistory(chatHistory, _activeProfileName, false, _appService.CalculateTokenMetrics);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Could not load chat history from session: {sessionId}");
            Console.ResetColor();
        }
    }

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
