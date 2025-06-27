

namespace UI.Tests;

public class AppComponentTests
{
    private static (TuiApp app, ServiceProvider provider) SetupTestApp(string testResponse)
    {
        var services = new ServiceCollection();

        // Register configuration and working directory provider
        services.AddSingleton<IWorkingDirectoryProvider>(new MockWorkingDirectoryProvider());
        services.AddSingleton<ILogger<TuiApp>>(new NullLogger<TuiApp>());

        // Create the ChatClient wrapper with our TestChatClient
        var chatClientResult = ChatClient.Create(new TestChatClient(testResponse), "maxbot.config.json");
        Assert.False(chatClientResult.IsFailed, chatClientResult.Errors.FirstOrDefault()?.Message);
        services.AddSingleton(chatClientResult.Value);

        // Register the real services that depend on ChatClient
        services.AddSingleton<IAppService, AppService>();
        
        // Register other UI services and components
        services.AddSingleton<StateManager>();
        services.AddSingleton<HistoryManager>();
        services.AddSingleton<LayoutManager>();
        services.AddSingleton<AppComponent>();
        services.AddSingleton<HeaderComponent>();
        services.AddSingleton<StaticHistoryComponent>();
        services.AddSingleton<DynamicContentComponent>();
        services.AddSingleton<InputComponent>();
        services.AddSingleton<FooterComponent>();

        var provider = services.BuildServiceProvider();
        var app = new TuiApp(provider);

        return (app, provider);
    }

    [Fact]
    public async Task ProcessUserInput_WithTestChatClient_UpdatesHistoryState()
    {
        // Arrange
        var expectedResponse = "This is the response from the test client.";
        var (app, provider) = SetupTestApp(expectedResponse);

        var appComponent = provider.GetRequiredService<AppComponent>();
        var historyManager = provider.GetRequiredService<HistoryManager>();

        var cancellationTokenSource = new CancellationTokenSource();
        var runTask = app.RunAsync(new string[]{}, cancellationTokenSource.Token);

        // Act
        await appComponent.ProcessUserInput("hello");

        // Give the app time to process the message and update state
        await Task.Delay(100);

        // Assert
        var history = historyManager.GetCompletedMessages();
        history.Should().HaveCount(2); // User message + Assistant message
        history.Last().Role.Should().Be(ChatRole.Assistant);
        history.Last().Text.Should().Be(expectedResponse);

        // Clean up
        cancellationTokenSource.Cancel();
        await runTask;
    }

    [Fact]
    public async Task RenderAsync_WithLayoutManager_ReturnsStructuredGrid()
    {
        // Arrange
        var (app, provider) = SetupTestApp("test response");
        var appComponent = provider.GetRequiredService<AppComponent>();
        var terminalSize = new TerminalSize(120, 40);
        var constraints = new LayoutConstraints(terminalSize.Height, terminalSize.Width);
        var context = new RenderContext(constraints, terminalSize);

        // Act
        var result = await appComponent.RenderAsync(context);

        // Assert
        var grid = result.Should().BeOfType<Grid>().Subject;
        grid.Rows.Should().HaveCount(2); // Static Zone, Dynamic Zone

        // Inspect Static Zone
        var staticZone = grid.Rows[0][0].Should().BeOfType<Panel>().Subject;
        staticZone.Header?.Text.Should().Be("Static Zone");

        // Inspect Dynamic Zone
        var dynamicZone = grid.Rows[1][0].Should().BeOfType<Panel>().Subject;
        dynamicZone.Header?.Text.Should().Be("Dynamic Zone");
    }
}
