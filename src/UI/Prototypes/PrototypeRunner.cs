namespace UI.Prototypes;

/// <summary>
/// Simple console application to run the Live widget prototype.
/// This demonstrates the Live widget approach before implementing the full system.
/// </summary>
public static class PrototypeRunner
{
    /// <summary>
    /// Runs the prototype demonstration.
    /// </summary>
    public static async Task<int> RunPrototypeAsync(string[] args)
    {
        try
        {
            // Determine which prototype to run
            var prototypeType = args.Length > 1 ? args[1] : "flex";
            
            AnsiConsole.WriteLine("[bold green]Starting MaxBot UI Prototypes...[/]");
            AnsiConsole.WriteLine($"[dim]Running prototype: {prototypeType}[/]");
            AnsiConsole.WriteLine();
            
            // Wait a moment for user to read
            await Task.Delay(1500);
            
            // Run the appropriate prototype
            using var cts = new CancellationTokenSource();
            
            // Handle Ctrl+C gracefully
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };
            
            switch (prototypeType.ToLower())
            {
                case "column":
                default:
                    await FlexColumnPrototype.RunAsync(cts.Token);
                    break;
            }
                        
            return 0;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.Markup("[yellow]Prototype cancelled by user.[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }
}
