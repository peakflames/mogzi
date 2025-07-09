namespace Mogzi.TUI.Components;

/// <summary>
/// Displays system status information in a structured panel format.
/// Shows application details, configuration, and runtime information.
/// </summary>
/// <remarks>
/// Initializes a new instance of StatusPanel.
/// </remarks>
/// <param name="chatClient">Optional chat client for configuration display</param>
public class StatusPanel(ChatClient? chatClient = null) : ITuiComponent
{
    private readonly ChatClient? _chatClient = chatClient;

    public string Name => "StatusPanel";
    public bool IsVisible { get; set; } = true;

    public IRenderable Render(IRenderContext context)
    {
        var statusTable = new Table();
        _ = statusTable.AddColumn("Property");
        _ = statusTable.AddColumn("Value");
        statusTable.Border = TableBorder.Rounded;
        _ = statusTable.BorderColor(Color.Cyan1);

        _ = statusTable.AddRow("Application", "[green]Mogzi TUI[/]");
        _ = statusTable.AddRow("Version", $"[blue]{GetApplicationVersion()}[/]");
        _ = statusTable.AddRow("Status", "[green]Running[/]");
        _ = statusTable.AddRow("Working Directory", $"[dim]{Environment.CurrentDirectory}[/]");
        _ = statusTable.AddRow("Platform", $"[dim]{Environment.OSVersion.Platform}[/]");
        _ = statusTable.AddRow("Runtime", $"[dim].NET {Environment.Version}[/]");

        if (_chatClient != null)
        {
            _ = statusTable.AddRow("", ""); // Empty row for spacing
            _ = statusTable.AddRow("[bold]Configuration[/]", "");
            _ = statusTable.AddRow("Active Profile", $"[yellow]{_chatClient.ActiveProfile.Name}[/]");
            _ = statusTable.AddRow("Model", $"[cyan]{_chatClient.ActiveProfile.ModelId}[/]");
            _ = statusTable.AddRow("API Provider", $"[magenta]{_chatClient.ActiveApiProvider.Name}[/]");
            _ = statusTable.AddRow("Tool Approvals", $"[orange3]{_chatClient.Config.ToolApprovals}[/]");
        }

        var statusPanel = new Panel(statusTable)
            .Header(" System Status ")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Cyan1)
            .Padding(1, 0);

        // Try to get the profiles table
        var profilesTable = ProfileTableUtilities.CreateProfilesTable();

        if (profilesTable != null)
        {
            // Create a vertical group with both status and profiles with minimal spacing
            var group = new Rows(
                statusPanel,
                Text.Empty, // Single empty line for spacing
                profilesTable
            );

            return group;
        }
        else
        {
            // If no profiles table available, just return the status panel
            return statusPanel;
        }
    }

    public Task<bool> HandleInputAsync(IRenderContext context, object inputEvent)
    {
        // Status panel doesn't handle input events
        return Task.FromResult(false);
    }

    public Task InitializeAsync(IRenderContext context)
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the application version from the assembly.
    /// </summary>
    /// <returns>The application version string.</returns>
    private static string GetApplicationVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "UNKNOWN";
        }
        catch
        {
            return "UNKNOWN";
        }
    }
}
