using MaxBot.Services;


namespace MaxBot.TUI;

public class TuiAppService
{
    private readonly IAppService _appService;
    private readonly TuiEventBus _eventBus;

    public TuiAppService(IAppService appService, TuiEventBus eventBus)
    {
        _appService = appService;
        _eventBus = eventBus;
    }

    public async IAsyncEnumerable<ChatResponseUpdate> ProcessChatMessageAsync(List<ChatMessage> chatHistory, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var update in _appService.ProcessChatMessageAsync(chatHistory, cancellationToken))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                await _eventBus.PublishAsync(new TextReceivedEvent(update.Text));
            }
            yield return update;
        }
    }
}
