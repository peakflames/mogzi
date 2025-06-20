// using Moq;
// using CLI;
// using MaxBot;
// using Microsoft.Extensions.AI;
// using FluentAssertions;

// namespace Cli.Tests;

// public class AppTests
// {
//     [Fact]
//     public async Task StartOneShotAsync_WithValidPrompt_ShouldReturnExpectedResponse()
//     {
//         // Arrange
//         var mockChatClient = new Mock<IChatClient>();
//         var chatHistory = new List<ChatMessage>();
//         var responseStream = new[] { new TestChatResponseUpdate("Hello, world!") }.ToAsyncEnumerable();

//         mockChatClient.Setup(c => c.GetStreamingResponseAsync(It.IsAny<List<ChatMessage>>(), It.IsAny<ChatOptions>(), default))
//             .Returns(responseStream);
        
//         var clientResult = ChatClient.Create(mockChatClient.Object, "maxbot.config.json", null, null);
//         var app = new App(clientResult.Value, false);

//         var output = new StringWriter();
//         Console.SetOut(output);

//         // Act
//         await app.StartOneShotAsync("hello");

//         // Assert
//         var response = output.ToString();
//         response.Should().Contain("Hello, world!");
//     }

//     [Fact]
//     public async Task StartChatAsync_WithExitCommand_ShouldExit()
//     {
//         // Arrange
//         var mockChatClient = new Mock<IChatClient>();
//         var chatHistory = new List<ChatMessage>();
//         var responseStream = new[] { new TestChatResponseUpdate("Goodbye!") }.ToAsyncEnumerable();

//         mockChatClient.Setup(c => c.GetStreamingResponseAsync(It.IsAny<List<ChatMessage>>(), It.IsAny<ChatOptions>(), default))
//             .Returns(responseStream);
        
//         var clientResult = ChatClient.Create(mockChatClient.Object, "maxbot.config.json", null, null);
//         var app = new App(clientResult.Value, false);

//         var input = new StringReader("exit\n");
//         Console.SetIn(input);

//         var output = new StringWriter();
//         Console.SetOut(output);

//         // Act
//         await app.StartChatAsync();

//         // Assert
//         var response = output.ToString();
//         response.Should().NotContain("Goodbye!");
//     }
// }
