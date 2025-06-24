using System.Threading.Tasks;
using Xunit;
using MaxBot.TUI;

namespace TUI.Tests;

public class TuiEventBusTests
{
    private class TestEvent : ITuiEvent
    {
        public bool Handled { get; set; }
    }

    [Fact]
    public async Task PublishAsync_Should_Call_Registered_Handler()
    {
        // Arrange
        var eventBus = new TuiEventBus();
        var testEvent = new TestEvent();
        var handlerCalled = false;

        eventBus.Register<TestEvent>(e =>
        {
            handlerCalled = true;
            e.Handled = true;
            return Task.CompletedTask;
        });

        // Act
        await eventBus.PublishAsync(testEvent);

        // Assert
        Assert.True(handlerCalled);
        Assert.True(testEvent.Handled);
    }
}
