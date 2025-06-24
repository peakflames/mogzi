using Spectre.Console;
using MaxBot.TUI;

namespace MaxBot.TUI;

public static class Demo
{
    public static void ShowCards()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold blue]TUI Phase 2 Card Demonstrations[/]");
        AnsiConsole.WriteLine();

        // Demo 1: CommandCard
        AnsiConsole.MarkupLine("[bold]1. CommandCard - Running Command[/]");
        var runningCommand = new CommandCard("dotnet build", CommandStatus.Running, "Building project...\nRestoring packages...");
        AnsiConsole.Write(runningCommand.GetRenderable());
        AnsiConsole.WriteLine();

        // Demo 2: CommandCard - Success
        AnsiConsole.MarkupLine("[bold]2. CommandCard - Successful Command[/]");
        var successCommand = new CommandCard("dotnet test", CommandStatus.Success, "Test run successful.\nTotal tests: 15\nPassed: 15\nFailed: 0");
        AnsiConsole.Write(successCommand.GetRenderable());
        AnsiConsole.WriteLine();

        // Demo 3: CommandCard - Error
        AnsiConsole.MarkupLine("[bold]3. CommandCard - Failed Command[/]");
        var errorCommand = new CommandCard("npm install", CommandStatus.Error, "npm ERR! Cannot resolve dependency\nnpm ERR! Package not found");
        AnsiConsole.Write(errorCommand.GetRenderable());
        AnsiConsole.WriteLine();

        // Demo 4: RichContentCard
        AnsiConsole.MarkupLine("[bold]4. RichContentCard - Rich Content[/]");
        var richContent = "[bold underline]AI Response[/]\n\nI've analyzed your code and found:\n\n• [green]Good practices[/] in error handling\n• [yellow]Potential improvements[/] in performance\n• [red]Critical issues[/] in security\n\n[bold]Next steps:[/]\n1. Fix security vulnerabilities\n2. Optimize database queries\n3. Add unit tests";
        var richContentCard = new RichContentCard(richContent);
        AnsiConsole.Write(richContentCard.GetRenderable());
        AnsiConsole.WriteLine();

        // Demo 5: ApiStatusIndicator - Active
        AnsiConsole.MarkupLine("[bold]5. ApiStatusIndicator - API Call in Progress[/]");
        var activeApi = new ApiStatusIndicator(1250, TimeSpan.FromSeconds(45));
        AnsiConsole.Write(activeApi.GetRenderable());
        AnsiConsole.WriteLine();

        // Demo 6: ApiStatusIndicator - Completed
        AnsiConsole.MarkupLine("[bold]6. ApiStatusIndicator - API Call Completed[/]");
        var completedApi = new ApiStatusIndicator(2100, TimeSpan.FromMinutes(2).Add(TimeSpan.FromSeconds(15)));
        completedApi.Complete();
        AnsiConsole.Write(completedApi.GetRenderable());
        AnsiConsole.WriteLine();

        // AnsiConsole.MarkupLine("[dim]Press any key to exit...[/]");
        // Console.ReadKey();
    }
}
