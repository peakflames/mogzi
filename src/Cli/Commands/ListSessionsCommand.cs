using System.Threading.Tasks;
using Cli.UI;
using MaxBot.Services;

namespace Cli.Commands;

public class ListSessionsCommand : ICommand
{
    private readonly IAppService _appService;

    public ListSessionsCommand(IAppService appService)
    {
        _appService = appService;
    }

    public Task<int> ExecuteAsync()
    {
        try
        {
            var sessions = _appService.GetChatSessions();
            ConsoleRenderer.ListChatSessions(sessions, _appService.GetChatSessionsBasePath());
            return Task.FromResult(0);
        }
        catch (System.Exception ex)
        {
            ConsoleRenderer.ConsoleWriteError($"Error listing chat sessions: {ex.Message}");
            return Task.FromResult(1);
        }
    }
}
