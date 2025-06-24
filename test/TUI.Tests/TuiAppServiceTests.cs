using MaxBot.Services;
using MaxBot.TUI;
using Microsoft.Extensions.AI;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace TUI.Tests;

public class TuiAppServiceTests
{
    [Fact]
    public async Task ProcessChatMessageAsync_Should_Publish_TextReceivedEvent()
    {
        // Arrange
        var mockAppService = new Mock<IAppService>();
        var mockEventBus = new Mock<TuiEventBus>();
        var tuiAppService = new TuiAppService(mockAppService.Object, mockEventBus.Object);
        var chatHistory = new List<ChatMessage>();
        var cancellationToken = new CancellationToken();

        var updates = new[]
        {
            new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = new List<AIContent> { new TextContent("Hello") } },
            new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = new List<AIContent> { new TextContent(", World!") } }
        };

        mockAppService.Setup(s => s.ProcessChatMessageAsync(chatHistory, cancellationToken))
            .Returns(updates.ToAsyncEnumerable());

        // Act
        await foreach (var _ in tuiAppService.ProcessChatMessageAsync(chatHistory, cancellationToken))
        {
            // Consuming the stream
        }

        // Assert
        mockEventBus.Verify(bus => bus.PublishAsync(It.Is<TextReceivedEvent>(e => e.Text == "Hello")), Times.Once);
        mockEventBus.Verify(bus => bus.PublishAsync(It.Is<TextReceivedEvent>(e => e.Text == ", World!")), Times.Once);
    }
}

// Helper to convert an array to an IAsyncEnumerable for mocking
public static class TestExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            yield return item;
        }
        await Task.CompletedTask;
    }
}
