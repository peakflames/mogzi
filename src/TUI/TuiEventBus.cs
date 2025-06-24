using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MaxBot.TUI;

public class TuiEventBus
{
    private readonly ConcurrentDictionary<Type, Func<ITuiEvent, Task>> _handlers = new();

    public void Register<T>(Func<T, Task> handler) where T : ITuiEvent
    {
        _handlers[typeof(T)] = async (e) => await handler((T)e);
    }

    public virtual async Task PublishAsync(ITuiEvent domainEvent)
    {
        if (_handlers.TryGetValue(domainEvent.GetType(), out var handler))
        {
            await handler(domainEvent);
        }
    }
}
