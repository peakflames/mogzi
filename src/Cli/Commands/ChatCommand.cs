using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cli.Handlers;
using Cli.UI;
using MaxBot.Services;
using Microsoft.Extensions.AI;

namespace Cli.Commands;

public class ChatCommand : ICommand
{
    private readonly IAppService _appService;
    private readonly string? _sessionId;
    private readonly bool _showStatus;
    private readonly string _activeProfileName;
    private string? _currentSessionPath;

    public ChatCommand(IAppService appService, string? sessionId, bool showStatus, string activeProfileName)
    {
        _appService = appService;
        _sessionId = sessionId;
        _showStatus = showStatus;
        _activeProfileName = activeProfileName;
    }

    public async Task<int> ExecuteAsync()
    {
        try
        {
            var chatHistory = await InitializeChatHistory();
            var slashCommandHandler = new SlashCommandHandler(_appService, _activeProfileName, _currentSessionPath);

            var robotEmoji = char.ConvertFromUtf32(0x1F916);
            var folderEmoji = char.ConvertFromUtf32(0x1F4C2);
            var catHeartEyesEmoji = char.ConvertFromUtf32(0x1F63B);

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                var cwd = Directory.GetCurrentDirectory();
                Console.Write($"\n{catHeartEyesEmoji} Max | {folderEmoji} {cwd} | {robotEmoji}: {_activeProfileName}\n% ");
                Console.ForegroundColor = ConsoleColor.White;
                var userPrompt = Console.ReadLine();
                Console.WriteLine();

                if (string.IsNullOrWhiteSpace(userPrompt) || userPrompt.ToLower() == "exit" || userPrompt.ToLower() == "quit")
                    break;

                if (userPrompt.StartsWith("/"))
                {
                    if (slashCommandHandler.Handle(userPrompt, chatHistory))
                    {
                        continue;
                    }
                }


                chatHistory.Add(new ChatMessage(ChatRole.User, userPrompt));

                // This was encouraged to add to ensure some models to just lie.
                //
                chatHistory.Add(new ChatMessage(ChatRole.User, "HELPFUL REMINDER: By waiting for and carefully considering the tools response after each tool use, Max can react accordingly and make informed decisions about how to proceed with the task. This iterative process helps ensure the overall success and accuracy of Max's work."));

                using var cts = new CancellationTokenSource();
                bool isProcessingRequest = true;

                ConsoleCancelEventHandler cancelHandler = (sender, e) => {
                    e.Cancel = true;
                    if (isProcessingRequest && !cts.IsCancellationRequested)
                    {
                        try
                        {
                            Console.WriteLine("\n\nCancelling API request...");
                            cts.Cancel();
                        }
                        catch (ObjectDisposedException) { }
                    }
                };

                Console.CancelKeyPress += cancelHandler;

                try
                {
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
                    chatHistory.Add(new ChatMessage(ChatRole.Assistant, "[Response was cancelled by user]"));
                }
                catch (Exception ex)
                {
                    ConsoleRenderer.ConsoleWriteError($"\nError during API request: {ex.Message}");
                    chatHistory.Add(new ChatMessage(ChatRole.Assistant, $"[Error: {ex.Message}]"));
                    return 1;
                }
                finally
                {
                    isProcessingRequest = false;
                    Console.CancelKeyPress -= cancelHandler;
                }

                if (_showStatus)
                {
                    ConsoleRenderer.WriteTokenMetrics(chatHistory, _appService.CalculateTokenMetrics);
                }

                if (_currentSessionPath is null)
                {
                    ConsoleRenderer.ConsoleWriteError("Session path is not set.");
                    return 1;
                }
                
                await _appService.SaveChatHistoryAsync(_currentSessionPath, chatHistory);
            }

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
            ConsoleRenderer.ConsoleWriteError($"Error: {ex.Message}");
            return 1;
        }

        return 0;
    }

    private async Task<List<ChatMessage>> InitializeChatHistory()
    {
        List<ChatMessage> chatHistory = [new(ChatRole.System, _appService.SystemPrompt)];

        if (!string.IsNullOrEmpty(_sessionId))
        {
            string sessionPath = Path.Combine(_appService.GetChatSessionsBasePath(), _sessionId);
            if (Directory.Exists(sessionPath))
            {
                _currentSessionPath = sessionPath;
                var loadedHistory = await _appService.LoadChatSessionAsync(_sessionId, _appService.SystemPrompt);

                if (loadedHistory != null && loadedHistory.Count > 0)
                {
                    chatHistory = loadedHistory;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Loaded chat session: {_sessionId}");
                    Console.WriteLine($"Loaded {chatHistory.Count} messages from previous session");
                    Console.ResetColor();
                    ConsoleRenderer.DisplayChatHistory(chatHistory, _activeProfileName, _showStatus, _appService.CalculateTokenMetrics);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Could not load chat history from session: {_sessionId}");
                    Console.WriteLine("Starting a new session instead.");
                    Console.ResetColor();
                    _currentSessionPath = _appService.CreateChatSession();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Session not found: {_sessionId}");
                Console.WriteLine("Starting a new session instead.");
                Console.ResetColor();
                _currentSessionPath = _appService.CreateChatSession();
            }
        }
        else
        {
            _currentSessionPath = _appService.CreateChatSession();
        }

        Console.ResetColor();

        return chatHistory;
    }
}
