namespace Mogzi.TUI.Commands;

/// <summary>
/// Session management command for listing and managing chat sessions.
/// </summary>
public sealed class SessionCommand : ICommand
{
    public string Name => "session";
    public string Description => "Manage chat sessions (list, info, etc.)";

    public async Task<int> ExecuteAsync(string[] args)
    {
        try
        {
            var parsedArgs = ArgumentParser.Parse(args);

            // Check for help
            if (ArgumentParser.HasFlag(parsedArgs, "help") || ArgumentParser.HasFlag(parsedArgs, "h"))
            {
                ShowHelp();
                return 0;
            }

            // Get the subcommand (first positional argument)
            var subcommand = ArgumentParser.GetString(parsedArgs, "_0", null);

            if (string.IsNullOrEmpty(subcommand))
            {
                AnsiConsole.MarkupLine("[red]Error: Session subcommand is required.[/]");
                AnsiConsole.WriteLine();
                ShowHelp();
                return 1;
            }

            return subcommand.ToLowerInvariant() switch
            {
                "list" => await ExecuteListAsync(),
                "info" => await ExecuteInfoAsync(parsedArgs),
                _ => HandleUnknownSubcommand(subcommand)
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    public void ShowHelp()
    {
        AnsiConsole.MarkupLine("[bold]DESCRIPTION:[/]");
        AnsiConsole.MarkupLine($"    {Description}");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]USAGE:[/]");
        AnsiConsole.MarkupLine("   mogzi session <SUBCOMMAND> [[OPTIONS]]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]SUBCOMMANDS:[/]");
        AnsiConsole.MarkupLine("    list                         List all available chat sessions");
        AnsiConsole.MarkupLine("    info <SESSION_NAME_OR_ID>    Show detailed information about a specific session");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]OPTIONS:[/]");
        AnsiConsole.MarkupLine("    -h, --help                   Show this help message");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]EXAMPLES:[/]");
        AnsiConsole.MarkupLine("   mogzi session list");
        AnsiConsole.MarkupLine("   mogzi session info \"My Project Session\"");
        AnsiConsole.MarkupLine("   mogzi session info 0197e123-4567-7890-abcd-ef1234567890");
        AnsiConsole.MarkupLine("   mogzi session info ef1234567890");
    }

    private async Task<int> ExecuteListAsync()
    {
        try
        {
            // Setup dependency injection with default configuration
            var services = new ServiceCollection();
            ServiceConfiguration.ConfigureServices(services, null, null, "readonly");
            var serviceProvider = services.BuildServiceProvider();

            var sessionManager = serviceProvider.GetRequiredService<SessionManager>();
            var chatClient = serviceProvider.GetRequiredService<ChatClient>();

            // Get all sessions and apply the same limit logic as SessionListProvider
            var allSessions = await sessionManager.ListSessionsAsync();
            var sessionLimit = chatClient.Config.SessionListLimit;
            var sessions = allSessions.Take(sessionLimit).ToList();

            if (sessions.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No sessions found.[/]");
                return 0;
            }

            // Create a table to display sessions with wider columns
            var table = new Table()
                .Title($"Available Chat Sessions (last {sessionLimit} recently used)")
                .Border(TableBorder.Rounded)
                .AddColumn(new TableColumn("Name").Width(25))
                .AddColumn(new TableColumn("ID").Width(15))
                .AddColumn(new TableColumn("Created").Width(18))
                .AddColumn(new TableColumn("Last Modified").Width(18))
                .AddColumn(new TableColumn("Initial Prompt").Width(50));

            foreach (var session in sessions)
            {
                var createdAt = session.CreatedAt.ToString("yyyy-MM-dd HH:mm");
                var lastModified = session.LastModifiedAt.ToString("yyyy-MM-dd HH:mm");
                var initialPrompt = string.IsNullOrEmpty(session.InitialPrompt)
                    ? "[dim]No initial prompt[/]"
                    : Markup.Escape(session.InitialPrompt);

                _ = table.AddRow(
                    Markup.Escape(session.Name),
                    Markup.Escape("..." + session.Id.ToString()[^12..]), // Show last 12 chars of ID
                    createdAt,
                    lastModified,
                    initialPrompt
                );
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[dim]Total: {sessions.Count} session(s)[/]");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]Error listing sessions:[/]");
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    private async Task<int> ExecuteInfoAsync(Dictionary<string, string?> parsedArgs)
    {
        try
        {
            // Get the session name or ID (second positional argument)
            var sessionNameOrId = ArgumentParser.GetString(parsedArgs, "_1", null);

            if (string.IsNullOrEmpty(sessionNameOrId))
            {
                AnsiConsole.MarkupLine("[red]Error: Session name or ID is required for info command.[/]");
                AnsiConsole.MarkupLine("Usage: mogzi session info <SESSION_NAME_OR_ID>");
                return 1;
            }

            // Setup dependency injection with default configuration
            var services = new ServiceCollection();
            ServiceConfiguration.ConfigureServices(services, null, null, "readonly");
            var serviceProvider = services.BuildServiceProvider();

            var sessionManager = serviceProvider.GetRequiredService<SessionManager>();

            // Enhanced session lookup logic
            var loaded = await TryLoadSessionAsync(sessionManager, sessionNameOrId);

            if (!loaded)
            {
                AnsiConsole.MarkupLine($"[red]Error: Session '{sessionNameOrId}' not found.[/]");
                AnsiConsole.MarkupLine("Use 'mogzi session list' to see available sessions.");
                return 1;
            }

            var currentSession = sessionManager.CurrentSession;
            if (currentSession != null)
            {
                AnsiConsole.MarkupLine($"[cyan]Session Information[/]");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[bold]Name:[/] {currentSession.Name}");
                AnsiConsole.MarkupLine($"[bold]ID:[/] {currentSession.Id}");
                AnsiConsole.MarkupLine($"[bold]Created:[/] {currentSession.CreatedAt:yyyy-MM-dd HH:mm:ss UTC}");
                AnsiConsole.MarkupLine($"[bold]Last Modified:[/] {currentSession.LastModifiedAt:yyyy-MM-dd HH:mm:ss UTC}");
                AnsiConsole.MarkupLine($"[bold]Messages:[/] {currentSession.History.Count}");

                if (!string.IsNullOrEmpty(currentSession.InitialPrompt))
                {
                    AnsiConsole.MarkupLine($"[bold]Initial Prompt:[/] {Markup.Escape(currentSession.InitialPrompt)}");
                }

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]To start chatting with this session, use:[/] [cyan]mogzi chat --session \"{currentSession.Name}\"[/]");
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]Error getting session info:[/]");
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    /// <summary>
    /// Enhanced session lookup that supports name, full GUID, and partial GUID matching.
    /// </summary>
    private static async Task<bool> TryLoadSessionAsync(SessionManager sessionManager, string sessionNameOrId)
    {
        // Strategy 1: Try exact session name match first
        if (!Guid.TryParse(sessionNameOrId, out _))
        {
            var loaded = await sessionManager.TryLoadSessionByNameAsync(sessionNameOrId);
            if (loaded)
            {
                return true;
            }
        }

        // Strategy 2: Try full GUID match
        if (Guid.TryParse(sessionNameOrId, out var guid))
        {
            try
            {
                await sessionManager.LoadSessionAsync(sessionNameOrId);
                return true;
            }
            catch (FileNotFoundException)
            {
                // Continue to partial match
            }
            catch (ArgumentException)
            {
                // Continue to partial match
            }
        }

        // Strategy 3: Try partial GUID match using EndsWith
        // Get all sessions and find one whose ID ends with the provided string
        try
        {
            var allSessions = await sessionManager.ListSessionsAsync();
            var matchingSession = allSessions.FirstOrDefault(s =>
                s.Id.ToString().EndsWith(sessionNameOrId, StringComparison.OrdinalIgnoreCase));

            if (matchingSession != null)
            {
                await sessionManager.LoadSessionAsync(matchingSession.Id.ToString());
                return true;
            }
        }
        catch (Exception)
        {
            // If listing sessions fails, we can't do partial matching
        }

        return false;
    }

    private static int HandleUnknownSubcommand(string subcommand)
    {
        AnsiConsole.MarkupLine($"[red]Error: Unknown subcommand '{subcommand}'[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Available subcommands:[/]");
        AnsiConsole.MarkupLine("    list    List all available chat sessions");
        AnsiConsole.MarkupLine("    info    Show detailed information about a specific session");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Use 'mogzi session --help' for more information.");
        return 1;
    }
}
