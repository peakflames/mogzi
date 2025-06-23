using Cli.Commands;
using MaxBot;
using MaxBot.Services;

namespace Cli;

public class App
{
    private readonly IAppService _appService;
    private readonly bool _showStatus;
    private readonly string _activeProfileName;

    public App(ChatClient maxClient, bool showStatus)
    {
        _showStatus = showStatus;
        _appService = new AppService(maxClient);
        _activeProfileName = maxClient.ActiveProfile.Name;
    }

    public ICommand CreateCommand(string activeMode, string? userPrompt = null, string? loadSession = null)
    {
        return activeMode switch
        {
            "chat" => new ChatCommand(_appService, loadSession, _showStatus, _activeProfileName),
            "oneshot" => new OneShotCommand(_appService, userPrompt, _showStatus),
            "list-sessions" => new ListSessionsCommand(_appService),
            _ => throw new System.ArgumentException($"Invalid mode: {activeMode}", nameof(activeMode)),
        };
    }
}
