using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cli.UI;
using MaxBot.Services;
using Microsoft.Extensions.AI;

namespace Cli.Commands;

public class OneShotCommand : ICommand
{
    private readonly IAppService _appService;
    private readonly string? _userPrompt;
    private readonly bool _showStatus;

    public OneShotCommand(IAppService appService, string? userPrompt, bool showStatus)
    {
        _appService = appService;
        _userPrompt = userPrompt;
        _showStatus = showStatus;
    }

    public async Task<int> ExecuteAsync()
    {
        try
        {
            List<ChatMessage> chatHistory =
            [
                new(ChatRole.System, _appService.SystemPrompt),
            ];

            if (string.IsNullOrWhiteSpace(_userPrompt))
            {
                return 0;
            }

            chatHistory.Add(new ChatMessage(ChatRole.User, _userPrompt));

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
        }
        catch (Exception ex)
        {
            ConsoleRenderer.ConsoleWriteError($"Error: {ex.Message}");
            return 1;
        }

        return 0;
    }
}
