TITLE: Testing Interactive Commands with Mocked User Inputs in C#
DESCRIPTION: This snippet illustrates how to test an interactive `Spectre.Console` command (`InteractiveCommand`) by mocking user inputs. It uses `TestConsole` to simulate key presses and text inputs for `MultiSelectionPrompt`, `SelectionPrompt`, and `Ask` prompts, then verifies the command's output based on these simulated interactions. This allows for comprehensive testing of interactive CLI flows.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/unit-testing.md#_snippet_2

LANGUAGE: C#
CODE:
```
public sealed class InteractiveCommandTests
{
    private sealed class InteractiveCommand : Command
    {
        private readonly IAnsiConsole _console;

        public InteractiveCommand(IAnsiConsole console)
        {
            _console = console;
        }

        public override int Execute(CommandContext context)
        {
            var fruits = _console.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("What are your [green]favorite fruits[/]?")
                    .NotRequired() // Not required to have a favorite fruit
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more fruits)[/]")
                    .InstructionsText(
                        "[grey](Press [blue]<space>[/] to toggle a fruit, " +
                        "[green]<enter>[/] to accept)[/]")
                    .AddChoices(new[] {
                        "Apple", "Apricot", "Avocado",
                        "Banana", "Blackcurrant", "Blueberry",
                        "Cherry", "Cloudberry", "Coconut",
                    }));

            var fruit = _console.Prompt(
                new SelectionPrompt<string>()
                    .Title("What's your [green]favorite fruit[/]?")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more fruits)[/]")
                    .AddChoices(new[] {
                        "Apple", "Apricot", "Avocado",
                        "Banana", "Blackcurrant", "Blueberry",
                        "Cherry", "Cloudberry", "Cocunut",
                    }));

            var name = _console.Ask<string>("What's your name?");

            _console.WriteLine($"[{string.Join(',', fruits)};{fruit};{name}]");

            return 0;
        }
    }

    [Fact]
    public void InteractiveCommand_WithMockedUserInputs_ProducesExpectedOutput()
    {
        // Given
        TestConsole console = new();
        console.Interactive();

        // Your mocked inputs must always end with "Enter" for each prompt!

        // Multi selection prompt: Choose first option
        console.Input.PushKey(ConsoleKey.Spacebar);
        console.Input.PushKey(ConsoleKey.Enter);

        // Selection prompt: Choose second option
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        // Ask text prompt: Enter name
        console.Input.PushTextWithEnter("Spectre Console");

        var app = new CommandAppTester(null, new CommandAppTesterSettings(), console);
        app.SetDefaultCommand<InteractiveCommand>();

        // When
        var result = app.Run();

        // Then
        result.ExitCode.ShouldBe(0);
        result.Output.EndsWith("[Apple;Apricot;Spectre Console]");
    }
}
```

----------------------------------------

TITLE: Rendering a Basic Table with Spectre.Console (C#)
DESCRIPTION: This snippet demonstrates how to create a basic table using Spectre.Console. It involves instantiating a `Table` object, adding columns (including one with custom alignment), populating rows with various renderable types like strings, Markup, and Panels, and finally rendering the table to the console using `AnsiConsole.Write`.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/table.md#_snippet_0

LANGUAGE: csharp
CODE:
```
// Create a table
var table = new Table();

// Add some columns
table.AddColumn("Foo");
table.AddColumn(new TableColumn("Bar").Centered());

// Add some rows
table.AddRow("Baz", "[green]Qux[/]");
table.AddRow(new Markup("[blue]Corgi[/]"), new Panel("Waldo"));

// Render the table to the console
AnsiConsole.Write(table);
```

----------------------------------------

TITLE: Installing Spectre.Console NuGet Package
DESCRIPTION: This command demonstrates how to install the Spectre.Console NuGet package into a .NET project using the dotnet CLI. It adds the package reference to the project file, making the library available for use in C# applications.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/README.md#_snippet_0

LANGUAGE: Shell
CODE:
```
dotnet add package Spectre.Console
```

----------------------------------------

TITLE: Validating User Input with TextPrompt<int> in C#
DESCRIPTION: This example demonstrates how to add custom validation logic to a `TextPrompt<int>` using the `Validate` method. The validation function checks if the input number is too low, too high, or correct, providing specific error messages to the user until a valid input is provided.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/prompts/text.md#_snippet_5

LANGUAGE: C#
CODE:
```
// Ask the user to guess the secret number
var number = AnsiConsole.Prompt(
    new TextPrompt<int>("What's the secret number?")
      .Validate((n) => n switch
      {
          < 50 => ValidationResult.Error("Too low"),
          50 => ValidationResult.Success(),
          > 50 => ValidationResult.Error("Too high"),
      }));

// Echo the user's success back to the terminal
Console.WriteLine($"Correct! The secret number is {number}.");
```

----------------------------------------

TITLE: Implementing Asynchronous Progress Bar with Spectre.Console
DESCRIPTION: This example illustrates how to use `StartAsync` for an asynchronous progress bar in Spectre.Console. It defines two tasks and simulates work using `Task.Delay`, incrementing task progress periodically. This approach is ideal for I/O-bound or long-running operations that benefit from `async/await`.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/live/progress.md#_snippet_1

LANGUAGE: csharp
CODE:
```
// Asynchronous
await AnsiConsole.Progress()
    .StartAsync(async ctx =>
    {
        // Define tasks
        var task1 = ctx.AddTask("[green]Reticulating splines[/]");
        var task2 = ctx.AddTask("[green]Folding space[/]");

        while (!ctx.IsFinished)
        {
            // Simulate some work
            await Task.Delay(250);

            // Increment
            task1.Increment(1.5);
            task2.Increment(0.5);
        }
    });
```

----------------------------------------

TITLE: Using AnsiConsole Convenience Methods for Markup (C#)
DESCRIPTION: This snippet demonstrates the `AnsiConsole.Markup` and `AnsiConsole.MarkupLine` convenience methods. These methods allow direct output of styled text to the console without explicitly creating `Markup` instances, simplifying common use cases.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/markup.md#_snippet_2

LANGUAGE: csharp
CODE:
```
AnsiConsole.Markup("[underline green]Hello[/] ");
AnsiConsole.MarkupLine("[bold]World[/]");
```

----------------------------------------

TITLE: Configuring and Running a Spectre.Console.Cli Application (C#)
DESCRIPTION: This C# Program.Main method demonstrates how to configure and run a command-line application using Spectre.Console.Cli. It initializes a CommandApp, defines a command branch for 'add' with subcommands 'package' and 'reference', and then executes the application with the provided arguments. This setup ties together the previously defined settings and command classes, forming the complete CLI structure.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/introduction.md#_snippet_2

LANGUAGE: C#
CODE:
```
using Spectre.Console.Cli;

namespace MyApp
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandApp();

            app.Configure(config =>
            {
                config.AddBranch<AddSettings>("add", add =>
                {
                    add.AddCommand<AddPackageCommand>("package");
                    add.AddCommand<AddReferenceCommand>("reference");
                });
            });

            return app.Run(args);
        }
    }
}
```

----------------------------------------

TITLE: Defining Multiple Positional Command Arguments in C#
DESCRIPTION: This C# snippet illustrates how to define multiple positional command arguments using the `CommandArgument` attribute. It shows a required `firstName` argument at position 0 and an optional `lastName` argument at position 1, demonstrating how `Spectre.Console.Cli` handles ordered arguments and optionality via bracket notation.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/settings.md#_snippet_1

LANGUAGE: C#
CODE:
```
[CommandArgument(0, "<firstName>")]
public string FirstName { get; set; }

[CommandArgument(1, "[lastName]")]
public string? LastName { get; set; }
```

----------------------------------------

TITLE: Applying Basic Styles with Spectre.Console Markup (C#)
DESCRIPTION: This snippet demonstrates how to apply basic text styles like bold and color (yellow, red) to console output using the `Markup` class in Spectre.Console. Styles are defined within square brackets and closed with `[/]`.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/markup.md#_snippet_0

LANGUAGE: csharp
CODE:
```
AnsiConsole.Write(new Markup("[bold yellow]Hello[/] [red]World![/]"));
```

----------------------------------------

TITLE: Implementing Commands for Spectre.Console.Cli (C#)
DESCRIPTION: These C# classes implement the command logic for 'add package' and 'add reference' operations within a Spectre.Console.Cli application. Each command inherits from Command<TSettings> and overrides the Execute method, which receives a CommandContext and the specific settings object. These commands depend on the previously defined AddPackageSettings and AddReferenceSettings classes.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/introduction.md#_snippet_1

LANGUAGE: C#
CODE:
```
public class AddPackageCommand : Command<AddPackageSettings>
{
    public override int Execute(CommandContext context, AddPackageSettings settings)
    {
        // Omitted
        return 0;
    }
}

public class AddReferenceCommand : Command<AddReferenceSettings>
{
    public override int Execute(CommandContext context, AddReferenceSettings settings)
    {
        // Omitted
        return 0;
    }
}
```

----------------------------------------

TITLE: Composing a Command Tree with Spectre.Console.Cli in C#
DESCRIPTION: This C# code demonstrates how to compose the previously defined settings and commands into a hierarchical command-line application using `Spectre.Console.Cli`. It initializes a `CommandApp` and configures a branch for the "add" command, then adds "package" and "reference" subcommands, associating them with their respective command classes. This setup defines how user input is parsed and dispatched to the correct command.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/composing.md#_snippet_2

LANGUAGE: C#
CODE:
```
using Spectre.Console.Cli;

namespace MyApp
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandApp();

            app.Configure(config =>
            {
                config.AddBranch<AddSettings>("add", add =>
                {
                    add.AddCommand<AddPackageCommand>("package");
                    add.AddCommand<AddReferenceCommand>("reference");
                });
            });

            return app.Run(args);
        }
    }
}
```

----------------------------------------

TITLE: Using MarkupInterpolated for Safe Interpolated Strings (C#)
DESCRIPTION: This example demonstrates `AnsiConsole.MarkupInterpolated`, which automatically escapes values within interpolated string 'holes'. This prevents unintended markup interpretation when embedding variables that might contain special characters, ensuring safe and correct rendering.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/markup.md#_snippet_6

LANGUAGE: csharp
CODE:
```
string hello = "Hello [World]";
AnsiConsole.MarkupInterpolated($"[red]{hello}[/]");
```

----------------------------------------

TITLE: Defining Command Settings with Arguments and Options in C#
DESCRIPTION: This C# snippet defines a `CommandSettings` class for `Spectre.Console.Cli` commands. It demonstrates how to declare a positional command argument (`[name]`) and a command option (`-c|--count`) using `CommandArgument` and `CommandOption` attributes, respectively. This class serves as a blueprint for parsing command-line inputs into structured settings.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/settings.md#_snippet_0

LANGUAGE: C#
CODE:
```
public sealed class MyCommandSettings : CommandSettings
{
    [CommandArgument(0, "[name]")]
    public string? Name { get; set; }

    [CommandOption("-c|--count")]
    public int? Count { get; set; }
}
```

----------------------------------------

TITLE: Integrating Dependency Injection with CommandApp in C#
DESCRIPTION: This code demonstrates how to set up dependency injection for a CommandApp. It involves registering services with a ServiceCollection, creating a custom ITypeRegistrar to adapt the DI framework, and then passing this registrar to the CommandApp constructor to enable automatic resolution of command dependencies.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/commandApp.md#_snippet_3

LANGUAGE: C#
CODE:
```
var registrations = new ServiceCollection();
registrations.AddSingleton<IGreeter, HelloWorldGreeter>();

// Create a type registrar and register any dependencies.
// A type registrar is an adapter for a DI framework.
var registrar = new MyTypeRegistrar(registrations);

// Create a new command app with the registrar
// and run it with the provided arguments.
var app = new CommandApp<DefaultCommand>(registrar);
return app.Run(args);
```

----------------------------------------

TITLE: Defining a Basic Command in Spectre.Console.Cli (C#)
DESCRIPTION: This snippet demonstrates how to define a basic command in `Spectre.Console.Cli` by inheriting from `Command<TSettings>`. It includes a nested `Settings` class to define command arguments and an `Execute` method that processes the command logic, outputting a formatted message using `AnsiConsole.MarkupLine`.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/commands.md#_snippet_0

LANGUAGE: C#
CODE:
```
public class HelloCommand : Command<HelloCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[Name]")]
        public string Name { get; set; }
    }


    public override int Execute(CommandContext context, Settings settings)
    {
        AnsiConsole.MarkupLine($"Hello, [blue]{settings.Name}[/]");
        return 0;
    }
}
```

----------------------------------------

TITLE: Testing Panel Rendering with TestConsole in C#
DESCRIPTION: This C# test demonstrates how to render a `Panel` widget to a `TestConsole` and then assert its output. It uses `TestConsole` to capture the console's written content, allowing for programmatic validation of rendered UI elements.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/unit-testing.md#_snippet_3

LANGUAGE: csharp
CODE:
```
    [TestMethod]
    public void Should_Render_Panel()
    {
        // Given
        var console = new TestConsole();

        // When
        console.Write(new Panel(new Text("Hello World")));

        // Then
        Assert.AreEqual(console.Output, """"
┌─────────────┐
│ Hello World │
└─────────────┘

"""");
    }
```

----------------------------------------

TITLE: Initializing Command Settings with Constructor in C#
DESCRIPTION: This snippet demonstrates how to initialize `CommandSettings` using a constructor. The constructor's parameter name (`name`) must match the corresponding property name (`Name`) in the settings class, allowing for dependency injection-like behavior. This approach ensures that the `Name` property is set upon object creation.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/settings.md#_snippet_7

LANGUAGE: C#
CODE:
```
public class Settings : CommandSettings
{
    public Settings(string[] name)
    {
        Name = name;
    }

    [Description("The name to display")]
    [CommandArgument(0, "[Name]")]
    public string? Name { get; }
}
```

----------------------------------------

TITLE: Defining Command Settings in Spectre.Console.Cli (C#)
DESCRIPTION: These C# classes define the command-line arguments and options for 'add', 'add package', and 'add reference' commands using Spectre.Console.Cli. AddSettings serves as a base for common arguments, while AddPackageSettings and AddReferenceSettings extend it to include specific options and arguments for their respective subcommands. Dependencies include Spectre.Console.Cli for CommandSettings, CommandArgument, and CommandOption attributes.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/introduction.md#_snippet_0

LANGUAGE: C#
CODE:
```
public class AddSettings : CommandSettings
{
    [CommandArgument(0, "[PROJECT]")]
    public string Project { get; set; }
}

public class AddPackageSettings : AddSettings
{
    [CommandArgument(0, "<PACKAGE_NAME>")]
    public string PackageName { get; set; }

    [CommandOption("-v|--version <VERSION>")]
    public string Version { get; set; }
}

public class AddReferenceSettings : AddSettings
{
    [CommandArgument(0, "<PROJECT_REFERENCE>")]
    public string ProjectReference { get; set; }
}
```

----------------------------------------

TITLE: Updating Live Display Asynchronously with Spectre.Console
DESCRIPTION: This example shows how to use `AnsiConsole.Live` with `StartAsync` for asynchronous updates. It adds columns to a `Table` with an asynchronous delay, refreshing the display after each addition. This requires `Spectre.Console` and `System.Threading.Tasks`.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/live/live-display.md#_snippet_1

LANGUAGE: csharp
CODE:
```
var table = new Table().Centered();

await AnsiConsole.Live(table)
    .StartAsync(async ctx => 
    {
        table.AddColumn("Foo");
        ctx.Refresh();
        await Task.Delay(1000);

        table.AddColumn("Bar");
        ctx.Refresh();
        await Task.Delay(1000);
    });
```

----------------------------------------

TITLE: Using IAnsiConsole Instance for Output in C#
DESCRIPTION: This C# code snippet demonstrates adherence to the Spectre1010 rule. It correctly utilizes the injected _ansiConsole instance (of type IAnsiConsole) for writing output, rather than the static AnsiConsole helper. This approach enhances testability and allows for flexible customization of console features.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/analyzer/rules/spectre1010.md#_snippet_1

LANGUAGE: csharp
CODE:
```
class Example
{
    private IAnsiConsole _ansiConsole;

    public Example(IAnsiConsole ansiConsole) 
    {
        _ansiConsole = ansiConsole;
    }

    public Run()
    {
        _ansiConsole.WriteLine("Running...");
    }

}
```

----------------------------------------

TITLE: Implementing Command Validation in Spectre.Console.Cli (C#)
DESCRIPTION: This snippet demonstrates how to implement custom validation logic within a command by overriding the `Validate` method. It shows an example of checking for the existence of a file path using an injected `_fileSystem` dependency and returning a `ValidationResult.Error` if the path is not found.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/commands.md#_snippet_2

LANGUAGE: C#
CODE:
```
public override ValidationResult Validate(CommandContext context, Settings settings)
{
    if (_fileSystem.IO.File.Exists(settings.Path))
    {
        return ValidationResult.Error($"Path not found - {settings.Path}");
    }

    return base.Validate(context, settings);
}
```

----------------------------------------

TITLE: Prompting for Multiple Selections with Spectre.Console (C#)
DESCRIPTION: This C# code snippet illustrates the usage of `MultiSelectionPrompt` from the Spectre.Console library. It configures a prompt to ask the user for their favorite fruits, allowing multiple selections. Key configurations include setting a title, making the selection optional (`NotRequired`), defining the number of items per page (`PageSize`), and providing interactive instructions. Finally, it iterates through and prints the selected fruits.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/prompts/multiselection.md#_snippet_0

LANGUAGE: C#
CODE:
```
// Ask for the user's favorite fruits
var fruits = AnsiConsole.Prompt(
    new MultiSelectionPrompt<string>()
        .Title("What are your [green]favorite fruits[/]?")
        .NotRequired() // Not required to have a favorite fruit
        .PageSize(10)
        .MoreChoicesText("[grey](Move up and down to reveal more fruits)[/]")
        .InstructionsText(
            "[grey](Press [blue]<space>[/] to toggle a fruit, " +
            "[green]<enter>[/] to accept)[/]")
        .AddChoices(new[] {
            "Apple", "Apricot", "Avocado",
            "Banana", "Blackcurrant", "Blueberry",
            "Cherry", "Cloudberry", "Coconut",
        }));

// Write the selected fruits to the terminal
foreach (string fruit in fruits)
{
    AnsiConsole.WriteLine(fruit);
}
```

----------------------------------------

TITLE: Implementing a File Size Command with Spectre.Console.Cli in C#
DESCRIPTION: This C# snippet demonstrates how to set up a basic Spectre.Console.Cli application with a single command. It defines a `FileSizeCommand` inheriting from `Command<T>` and its associated `Settings` class, which uses attributes like `CommandArgument` and `CommandOption` to define command-line parameters. The `Execute` method processes these settings to calculate and display the total size of files based on user-provided path and pattern, showcasing how strongly-typed settings are consumed.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/getting-started.md#_snippet_0

LANGUAGE: csharp
CODE:
```
var app = new CommandApp<FileSizeCommand>();
return app.Run(args);

internal sealed class FileSizeCommand : Command<FileSizeCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Path to search. Defaults to current directory.")]
        [CommandArgument(0, "[searchPath]")]
        public string? SearchPath { get; init; }

        [CommandOption("-p|--pattern")]
        public string? SearchPattern { get; init; }

        [CommandOption("--hidden")]
        [DefaultValue(true)]
        public bool IncludeHidden { get; init; }
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        var searchOptions = new EnumerationOptions
        {
            AttributesToSkip = settings.IncludeHidden
                ? FileAttributes.Hidden | FileAttributes.System
                : FileAttributes.System
        };

        var searchPattern = settings.SearchPattern ?? "*.*";
        var searchPath = settings.SearchPath ?? Directory.GetCurrentDirectory();
        var files = new DirectoryInfo(searchPath)
            .GetFiles(searchPattern, searchOptions);

        var totalFileSize = files
            .Sum(fileInfo => fileInfo.Length);

        AnsiConsole.MarkupLine($"Total file size for [green]{searchPattern}[/] files in [green]{searchPath}[/]: [blue]{totalFileSize:N0}[/] bytes");

        return 0;
    }
}
```

----------------------------------------

TITLE: Prompting User for Selection with SelectionPrompt in C#
DESCRIPTION: This C# snippet demonstrates how to use `SelectionPrompt<T>` from Spectre.Console to prompt a user to select a single item from a predefined list of choices. It configures the prompt with a title, page size, and a 'more choices' text, then displays the user's selection using `AnsiConsole.WriteLine`.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/prompts/selection.md#_snippet_0

LANGUAGE: csharp
CODE:
```
// Ask for the user's favorite fruit
var fruit = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("What's your [green]favorite fruit[/]?")
        .PageSize(10)
        .MoreChoicesText("[grey](Move up and down to reveal more fruits)[/]")
        .AddChoices(new[] {
            "Apple", "Apricot", "Avocado", 
            "Banana", "Blackcurrant", "Blueberry",
            "Cherry", "Cloudberry", "Cocunut",
        }));

// Echo the fruit back to the terminal
AnsiConsole.WriteLine($"I agree. {fruit} is tasty!");
```

----------------------------------------

TITLE: Example CLI Invocations for Spectre.Console.Cli App
DESCRIPTION: This snippet provides examples of how the compiled `Spectre.Console.Cli` application (`app.exe`) can be invoked from the command line. It demonstrates various scenarios, including running the application with no arguments (using defaults), providing a positional argument for the search path, and combining the path with optional flags like `--pattern` and `--hidden` to filter files.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/getting-started.md#_snippet_1

LANGUAGE: text
CODE:
```
app.exe
app.exe c:\windows
app.exe c:\windows --pattern *.dll
app.exe c:\windows --hidden --pattern *.dll
```

----------------------------------------

TITLE: Updating Live Display Synchronously with Spectre.Console
DESCRIPTION: This snippet demonstrates how to use `AnsiConsole.Live` to update a `Table` widget in-place synchronously. It adds columns to the table with a delay, refreshing the display after each addition. This requires `Spectre.Console` and `System.Threading`.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/live/live-display.md#_snippet_0

LANGUAGE: csharp
CODE:
```
var table = new Table().Centered();

AnsiConsole.Live(table)
    .Start(ctx => 
    {
        table.AddColumn("Foo");
        ctx.Refresh();
        Thread.Sleep(1000);

        table.AddColumn("Bar");
        ctx.Refresh();
        Thread.Sleep(1000);
    });
```

----------------------------------------

TITLE: Using Spectre.Console Markup in Tables (C#)
DESCRIPTION: This example illustrates how `Markup` can be used within other Spectre.Console renderables, specifically a `Table`. It shows adding columns with styled text, demonstrating `Markup`'s `IRenderable` implementation for rich content in complex layouts.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/markup.md#_snippet_1

LANGUAGE: csharp
CODE:
```
var table = new Table();
table.AddColumn(new TableColumn(new Markup("[yellow]Foo[/]")));
table.AddColumn(new TableColumn("[blue]Bar[/]"));
AnsiConsole.Write(table);
```

----------------------------------------

TITLE: Testing User Input with TestConsoleInput in C#
DESCRIPTION: This C# test illustrates how to simulate user input for a `TextPrompt` using `TestConsoleInput` via `TestConsole`. It pushes predefined text to the console's input stream and then validates that the prompt's output reflects the simulated user interaction and choice.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/unit-testing.md#_snippet_4

LANGUAGE: csharp
CODE:
```
    [TestMethod]
    public void Should_Select_Orange()
    {
        // Given
        var console = new TestConsole();
        console.Input.PushTextWithEnter("Orange");

        // When
        console.Prompt(
            new TextPrompt<string>("Favorite fruit?")
                .AddChoice("Banana")
                .AddChoice("Orange"));

        // Then
        Assert.AreEqual(console.Output, "Favorite fruit? [Banana/Orange]: Orange\n");
    }
```

----------------------------------------

TITLE: Defining Init-Only Properties for Command Settings in C#
DESCRIPTION: This snippet illustrates the use of init-only properties for `CommandSettings`. This C# 9 feature allows properties to be set only during object initialization, providing immutability while still allowing external assignment during construction. It's a concise alternative to constructor-based initialization for simple settings.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/settings.md#_snippet_8

LANGUAGE: C#
CODE:
```
public class Settings : CommandSettings
{
    [Description("The name to display")]
    [CommandArgument(0, "[Name]")]
    public string? Name { get; init; }
}
```

----------------------------------------

TITLE: Testing Basic Command Output with CommandAppTester in C#
DESCRIPTION: This snippet demonstrates how to test a simple `Spectre.Console` command (`HelloWorldCommand`) using `CommandAppTester`. It shows how to inject `IAnsiConsole` for testability, execute the command, and then assert on the `ExitCode` and `Output` produced by the command, ensuring the command behaves as expected.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/unit-testing.md#_snippet_1

LANGUAGE: C#
CODE:
```
    /// <summary>
    /// A Spectre.Console Command
    /// </summary>
    public class HelloWorldCommand : Command
    {
        private readonly IAnsiConsole _console;

        public HelloWorldCommand(IAnsiConsole console)
        {
            // nb. AnsiConsole should not be called directly by the command
            // since this doesn't play well with testing. Instead,
            // the command should inject a IAnsiConsole and use that.

            _console = console;
        }

        public override int Execute(CommandContext context)
        {
            _console.WriteLine("Hello world.");
            return 0;
        }
    }

    [TestMethod]
    public void Should_Output_Hello_World()
    {
        // Given
        var app = new CommandAppTester();
        app.SetDefaultCommand<HelloWorldCommand>();

        // When
        var result = app.Run();

        // Then
        Assert.AreEqual(result.ExitCode, 0);
        Assert.AreEqual(result.Output, "Hello world.");
    }
```

----------------------------------------

TITLE: Writing Basic Exceptions with Spectre.Console C#
DESCRIPTION: This snippet demonstrates the basic usage of AnsiConsole.WriteException to output an exception to the terminal. It improves readability by providing color-coded output. The 'ex' parameter is the System.Exception object to be displayed.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/exceptions.md#_snippet_0

LANGUAGE: csharp
CODE:
```
AnsiConsole.WriteException(ex);
```

----------------------------------------

TITLE: Implementing Synchronous Progress Bar with Spectre.Console
DESCRIPTION: This snippet demonstrates how to create and manage a synchronous progress bar using Spectre.Console. It initializes two tasks, 'Reticulating splines' and 'Folding space', and continuously increments their progress until all tasks are finished. This method is suitable for operations that do not require asynchronous waiting.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/live/progress.md#_snippet_0

LANGUAGE: csharp
CODE:
```
// Synchronous
AnsiConsole.Progress()
    .Start(ctx => 
    {
        // Define tasks
        var task1 = ctx.AddTask("[green]Reticulating splines[/]");
        var task2 = ctx.AddTask("[green]Folding space[/]");

        while(!ctx.IsFinished) 
        {
            task1.Increment(1.5);
            task2.Increment(0.5);
        }
    });
```

----------------------------------------

TITLE: Implementing Command Execution Logic in C#
DESCRIPTION: These C# classes implement the command logic for `AddPackageCommand` and `AddReferenceCommand` using `Spectre.Console.Cli`. Each class inherits from `Command<TSettings>` and overrides the `Execute` method, which receives the `CommandContext` and the specific settings object. This separation ensures that the command's behavior is distinct from its argument/option definitions.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/composing.md#_snippet_1

LANGUAGE: C#
CODE:
```
public class AddPackageCommand : Command<AddPackageSettings>
{
    public override int Execute(CommandContext context, AddPackageSettings settings)
    {
        // Omitted
        return 0;
    }
}

public class AddReferenceCommand : Command<AddReferenceSettings>
{
    public override int Execute(CommandContext context, AddReferenceSettings settings)
    {
        // Omitted
        return 0;
    }
}
```

----------------------------------------

TITLE: Basic Grid Usage in C#
DESCRIPTION: This snippet demonstrates the fundamental steps to create and display a grid using Spectre.Console. It initializes a new Grid, adds three columns, and then populates two rows with string content before writing the grid to the console.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/grid.md#_snippet_0

LANGUAGE: csharp
CODE:
```
var grid = new Grid();
        
// Add columns 
grid.AddColumn();
grid.AddColumn();
grid.AddColumn();

// Add header row 
grid.AddRow(new string[]{"Header 1", "Header 2", "Header 3"});
grid.AddRow(new string[]{"Row 1", "Row 2", "Row 3"});

// Write to Console
AnsiConsole.Write(grid);
```

----------------------------------------

TITLE: Applying Inline Markup Styling (C#)
DESCRIPTION: Shows how to use AnsiConsole.Markup to apply inline styling to text. The string "[maroon on blue]Hello[/]" uses Spectre.Console's markup syntax to set the foreground color to maroon and the background color to blue for the word 'Hello'.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/appendix/colors.md#_snippet_1

LANGUAGE: C#
CODE:
```
AnsiConsole.Markup("[maroon on blue]Hello[/]")
```

----------------------------------------

TITLE: Custom Exception Handler with Return Value in Spectre.Console.Cli (C#)
DESCRIPTION: This example shows how to use `config.SetExceptionHandler()` with a `Func<Exception, ITypeResolver?, int>` delegate to provide a custom exception handling mechanism. The handler receives the exception and an optional `ITypeResolver`, allowing custom logging or output. The integer value returned by the `Func` delegate is used as the application's exit code.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/exceptions.md#_snippet_1

LANGUAGE: csharp
CODE:
```
using Spectre.Console.Cli;

namespace MyApp
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandApp<FileSizeCommand>();

            app.Configure(config =>
            {
                config.SetExceptionHandler((ex, resolver) =>
                {
                    AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
                    return -99;
                });
            });

            return app.Run(args);
        }
    }
}
```

----------------------------------------

TITLE: Using Synchronous Status with Spectre.Console
DESCRIPTION: This snippet demonstrates how to use the AnsiConsole.Status().Start() method for synchronous long-running tasks. It shows how to update the status message and spinner style dynamically within the task context, simulating work with Thread.Sleep(). This is suitable for blocking operations.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/live/status.md#_snippet_0

LANGUAGE: csharp
CODE:
```
// Synchronous
AnsiConsole.Status()
    .Start("Thinking...", ctx => 
    {
        // Simulate some work
        AnsiConsole.MarkupLine("Doing some work...");
        Thread.Sleep(1000);
        
        // Update the status and spinner
        ctx.Status("Thinking some more");
        ctx.Spinner(Spinner.Known.Star);
        ctx.SpinnerStyle(Style.Parse("green"));

        // Simulate some work
        AnsiConsole.MarkupLine("Doing some more work...");
        Thread.Sleep(2000);
    });
```

----------------------------------------

TITLE: Defining Command Settings with Inheritance in C#
DESCRIPTION: These C# classes define the settings (arguments and options) for the `add` command and its subcommands (`package`, `reference`) using `Spectre.Console.Cli`. `AddSettings` serves as a base class for common arguments like `Project`, while `AddPackageSettings` and `AddReferenceSettings` inherit from it to add specific arguments (`PackageName`, `ProjectReference`) and options (`Version`). This structure allows for reusable and hierarchical definition of command-line parameters.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/composing.md#_snippet_0

LANGUAGE: C#
CODE:
```
public class AddSettings : CommandSettings
{
    [CommandArgument(0, "[PROJECT]")]
    public string Project { get; set; }
}

public class AddPackageSettings : AddSettings
{
    [CommandArgument(0, "<PACKAGE_NAME>")]
    public string PackageName { get; set; }

    [CommandOption("-v|--version <VERSION>")]
    public string Version { get; set; }
}

public class AddReferenceSettings : AddSettings
{
    [CommandArgument(0, "<PROJECT_REFERENCE>")]
    public string ProjectReference { get; set; }
}
```

----------------------------------------

TITLE: Handling Secret Input with Default Mask in C#
DESCRIPTION: This snippet shows how to securely capture sensitive information, such as a password, using `TextPrompt<string>` with the `Secret()` method. This method automatically masks the user's input with asterisks (`*`) as they type, preventing it from being displayed on the screen.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/prompts/text.md#_snippet_6

LANGUAGE: C#
CODE:
```
// Ask the user to enter the password
var password = AnsiConsole.Prompt(
    new TextPrompt<string>("Enter password:")
        .Secret());

// Echo the password back to the terminal
Console.WriteLine($"Your password is {password}");
```

----------------------------------------

TITLE: Adding Items to BreakdownChart via IBreakdownChartItem Interface in C#
DESCRIPTION: This snippet illustrates how to integrate custom data types into a `BreakdownChart` by implementing the `IBreakdownChartItem` interface. It defines a `Fruit` class that conforms to the interface, allowing instances of `Fruit` to be directly added to the chart using `AddItem` and `AddItems` methods.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/breakdownchart.md#_snippet_6

LANGUAGE: C#
CODE:
```
public sealed class Fruit : IBreakdownChartItem
{
    public string Label { get; set; }
    public double Value { get; set; }
    public Color Color { get; set; }

    public Fruit(string label, double value, Color color)
    {
        Label = label;
        Value = value;
        Color = color;
    }
}

// Create a list of fruits
var items = new List<Fruit>
{
    new Fruit("Apple", 12, Color.Green),
    new Fruit("Orange", 54, Color.Orange1),
    new Fruit("Banana", 33, Color.Yellow),
};

// Render chart
AnsiConsole.Write(new BreakdownChart()
.Width(60)
.AddItem(new Fruit("Mango", 3, Color.Orange4))
.AddItems(items));
```

----------------------------------------

TITLE: Displaying Formatted Text with Spectre.Console in C#
DESCRIPTION: This C# snippet shows a basic 'Hello World' example using Spectre.Console. It imports the `Spectre.Console` namespace and uses `AnsiConsole.Markup` to print text with rich formatting, specifically an underlined red 'Hello', to the console. This demonstrates a fundamental usage of Spectre.Console for styled output.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/quick-start.md#_snippet_1

LANGUAGE: csharp
CODE:
```
using Spectre.Console;

public static class Program
{
    public static void Main(string[] args)
    {
        AnsiConsole.Markup("[underline red]Hello[/] World!");
    }
}
```

----------------------------------------

TITLE: Creating Clickable Links with Spectre.Console Markup (C#)
DESCRIPTION: This example shows how to create clickable hyperlinks in Spectre.Console using the `[link]` markup tag. It demonstrates both displaying the URL directly as a link and providing custom display text for the link, enhancing console interactivity.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/markup.md#_snippet_10

LANGUAGE: csharp
CODE:
```
AnsiConsole.Markup("[link]https://spectreconsole.net[/]");
AnsiConsole.Markup("[link=https://spectreconsole.net]Spectre Console Documentation[/]");
```

----------------------------------------

TITLE: Installing Spectre.Console NuGet Packages
DESCRIPTION: This snippet demonstrates how to add the necessary Spectre.Console and Spectre.Console.Cli NuGet packages to a .NET project using the `dotnet add package` command. These packages are essential prerequisites for utilizing the Spectre.Console library's features.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/quick-start.md#_snippet_0

LANGUAGE: text
CODE:
```
> dotnet add package Spectre.Console
> dotnet add package Spectre.Console.Cli
```

----------------------------------------

TITLE: Propagating Exceptions in Spectre.Console.Cli (C#)
DESCRIPTION: This snippet demonstrates how to propagate exceptions in Spectre.Console.Cli by calling `config.PropagateExceptions()`. This re-throws exceptions, requiring the `app.Run()` method to be wrapped in a `try-catch` block. The catch block handles the exception, outputs a user-friendly message using `AnsiConsole.WriteException`, and explicitly sets the application's exit code.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/exceptions.md#_snippet_0

LANGUAGE: csharp
CODE:
```
using Spectre.Console.Cli;

namespace MyApp
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandApp<FileSizeCommand>();

            app.Configure(config =>
            {
                config.PropagateExceptions();
            });

            try
            {
                return app.Run(args);
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
                return -99;
            }
        }
    }
}
```

----------------------------------------

TITLE: Enabling Native AOT Support in .NET Project File (XML)
DESCRIPTION: This XML snippet demonstrates how to enable Native AOT (Ahead-of-Time) compilation for a .NET application by adding the <PublishAot> property to a <PropertyGroup> in the project file. Enabling AOT results in faster startup times and smaller memory footprints for self-contained applications.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/best-practices.md#_snippet_0

LANGUAGE: xml
CODE:
```
<PropertyGroup>
    <PublishAot>true</PublishAot>
</PropertyGroup>
```

----------------------------------------

TITLE: Using a Known Spinner with Spectre.Console Status (C#)
DESCRIPTION: This snippet demonstrates how to use a predefined spinner, specifically Spinner.Known.Star, with the AnsiConsole.Status method in Spectre.Console. It initiates a status display that shows the spinner while a background operation (omitted in this example) is performed. This is useful for indicating ongoing processes to the user.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/appendix/spinners.md#_snippet_0

LANGUAGE: csharp
CODE:
```
AnsiConsole.Status()
    .Spinner(Spinner.Known.Star)
    .Start("Thinking...", ctx => {
        // Omitted
    });
```

----------------------------------------

TITLE: Installing Spectre.Console.Json NuGet Package
DESCRIPTION: This command installs the Spectre.Console.Json NuGet package, which is required to add JSON rendering capabilities to your .NET console application. It's the first step to enable JSON superpowers in your console app.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/json.md#_snippet_0

LANGUAGE: text
CODE:
```
> dotnet add package Spectre.Console.Json
```

----------------------------------------

TITLE: Custom Exception Handler without Return Value in Spectre.Console.Cli (C#)
DESCRIPTION: This snippet illustrates using `config.SetExceptionHandler()` with an `Action<Exception, ITypeResolver?>` delegate for custom exception handling. Similar to the `Func` overload, it allows custom logic for handling exceptions and outputting messages. However, since it's an `Action`, no return value is provided, and the application's exit code will default to -1.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/exceptions.md#_snippet_2

LANGUAGE: csharp
CODE:
```
using Spectre.Console.Cli;

namespace MyApp
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandApp<FileSizeCommand>();

            app.Configure(config =>
            {
                config.SetExceptionHandler((ex, resolver) =>
                {
                    AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
                });
            });

            return app.Run(args);
        }
    }
}
```

----------------------------------------

TITLE: Defining a Boolean Flag Command Option in C#
DESCRIPTION: This C# snippet demonstrates how to define a boolean command option that acts as a flag. When the `--debug` switch is present on the command line, the `Debug` property is automatically set to `true`, even without an explicit value. This simplifies command-line parsing for boolean parameters.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/settings.md#_snippet_2

LANGUAGE: C#
CODE:
```
[CommandOption("--debug")]
public bool? Debug { get; set; }
```

----------------------------------------

TITLE: Confirming User Input with TextPrompt<bool> in C#
DESCRIPTION: This snippet demonstrates how to use `TextPrompt<bool>` to ask the user for a 'y/n' confirmation. It utilizes `AddChoice` to define the boolean options and `WithConverter` to map them to 'y' or 'n' for display, with 'y' as the default value.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/prompts/text.md#_snippet_0

LANGUAGE: C#
CODE:
```
// Ask the user to confirm
var confirmation = AnsiConsole.Prompt(
    new TextPrompt<bool>("Run prompt example?")
        .AddChoice(true)
        .AddChoice(false)
        .DefaultValue(true)
        .WithConverter(choice => choice ? "y" : "n"));

// Echo the confirmation back to the terminal
Console.WriteLine(confirmation ? "Confirmed" : "Declined");
```

----------------------------------------

TITLE: Creating and Populating a Tree with Spectre.Console (C#)
DESCRIPTION: This snippet demonstrates how to create a hierarchical tree structure using the `Tree` widget in Spectre.Console. It shows adding various nodes, including a `Table` and a `Calendar` widget, and then rendering the complete tree to the console.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/tree.md#_snippet_0

LANGUAGE: csharp
CODE:
```
// Create the tree
var root = new Tree("Root");

// Add some nodes
var foo = root.AddNode("[yellow]Foo[/]");
var table = foo.AddNode(new Table()
    .RoundedBorder()
    .AddColumn("First")
    .AddColumn("Second")
    .AddRow("1", "2")
    .AddRow("3", "4")
    .AddRow("5", "6"));

table.AddNode("[blue]Baz[/]");
foo.AddNode("Qux");

var bar = root.AddNode("[yellow]Bar[/]");
bar.AddNode(new Calendar(2020, 12)
    .AddCalendarEvent(2020, 12, 12)
    .HideHeader());

// Render the tree
AnsiConsole.Write(root);
```

----------------------------------------

TITLE: Creating and Rendering a Basic Layout in Spectre.Console (C#)
DESCRIPTION: This snippet demonstrates how to create a hierarchical layout using `Spectre.Console.Layout`, split it into columns and rows, update a specific layout pane with content (a `Panel` containing `Markup`), and finally render the entire layout to the console using `AnsiConsole.Write`.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/layout.md#_snippet_0

LANGUAGE: C#
CODE:
```
// Create the layout
var layout = new Layout("Root")
    .SplitColumns(
        new Layout("Left"),
        new Layout("Right")
            .SplitRows(
                new Layout("Top"),
                new Layout("Bottom")));

// Update the left column
layout["Left"].Update(
    new Panel(
        Align.Center(
            new Markup("Hello [blue]World![/]"),
            VerticalAlignment.Middle))
        .Expand());

// Render the layout
AnsiConsole.Write(layout);
```

----------------------------------------

TITLE: Adding Multiple Commands to CommandApp in C#
DESCRIPTION: This example illustrates how to register multiple distinct commands (e.g., 'add', 'commit', 'rebase') with the CommandApp, enabling a multi-verb command-line interface similar to 'git' or 'dotnet'. Each command is mapped to a specific type.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/commandApp.md#_snippet_1

LANGUAGE: C#
CODE:
```
var app = new CommandApp();
app.Configure(config =>
{
    config.AddCommand<AddCommand>("add");
    config.AddCommand<CommitCommand>("commit");
    config.AddCommand<RebaseCommand>("rebase");
});
```

----------------------------------------

TITLE: Confirming User Input with ConfirmationPrompt in C#
DESCRIPTION: This example shows a simpler way to get a boolean confirmation from the user using `ConfirmationPrompt`. This prompt automatically handles the 'y/n' input and conversion, providing a more streamlined approach for simple confirmations.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/prompts/text.md#_snippet_1

LANGUAGE: C#
CODE:
```
// Ask the user to confirm
var confirmation = AnsiConsole.Prompt(
    new ConfirmationPrompt("Run prompt example?"));

// Echo the confirmation back to the terminal
Console.WriteLine(confirmation ? "Confirmed" : "Declined");
```

----------------------------------------

TITLE: Getting Simple Input with AnsiConsole.Ask<T> in C#
DESCRIPTION: This example presents a more concise way to get simple, strongly typed input using the `AnsiConsole.Ask<T>` shorthand method. It simplifies the syntax for common input scenarios, making the code cleaner.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/prompts/text.md#_snippet_3

LANGUAGE: C#
CODE:
```
// Ask the user a couple of simple questions
var name = AnsiConsole.Ask<string>("What's your name?");
var age = AnsiConsole.Ask<int>("What's your age?");

// Echo the name and age back to the terminal
AnsiConsole.WriteLine($"So you're {name} and you're {age} years old");
```

----------------------------------------

TITLE: Creating a Style with Foreground Color (C#)
DESCRIPTION: Demonstrates how to create a new Style object in C# by specifying a foreground color using the Color.Maroon enumeration. This style can then be applied to text for terminal output.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/appendix/colors.md#_snippet_0

LANGUAGE: C#
CODE:
```
new Style(foreground: Color.Maroon)
```

----------------------------------------

TITLE: Getting Simple Text and Integer Input with TextPrompt<T> in C#
DESCRIPTION: This snippet illustrates how to use `TextPrompt<string>` and `TextPrompt<int>` to collect basic string and integer inputs from the user. It demonstrates the strongly typed nature of `TextPrompt`, automatically parsing the input to the specified type.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/prompts/text.md#_snippet_2

LANGUAGE: C#
CODE:
```
// Ask the user a couple of simple questions
var name = AnsiConsole.Prompt(
    new TextPrompt<string>("What's your name?"));
var age = AnsiConsole.Prompt(
    new TextPrompt<int>("What's your age?"));

// Echo the name and age back to the terminal
AnsiConsole.WriteLine($"So you're {name} and you're {age} years old");
```

----------------------------------------

TITLE: Configuring Column Padding in Spectre.Console (C#)
DESCRIPTION: This snippet shows how to configure the left and right padding for individual columns in a Spectre.Console table. It demonstrates setting padding separately with `PadLeft()` and `PadRight()`, chaining them, or using the shorthand `Padding()` method for identical left/right padding.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/table.md#_snippet_7

LANGUAGE: csharp
CODE:
```
// Set padding individually
table.Columns[0].PadLeft(3);
table.Columns[0].PadRight(5);

// Or chained together
table.Columns[0].PadLeft(3).PadRight(5);

// Or with the shorthand method if the left and right 
// padding are identical. Vertical padding is ignored.
table.Columns[0].Padding(4, 0);
```

----------------------------------------

TITLE: Prompting User with Predefined Choices in C#
DESCRIPTION: This snippet shows how to present the user with a list of predefined choices using `TextPrompt<string>` and `AddChoices`. It also demonstrates setting a `DefaultValue`, which is automatically selected if the user presses Enter without typing.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/prompts/text.md#_snippet_4

LANGUAGE: C#
CODE:
```
// Ask for the user's favorite fruit
var fruit = AnsiConsole.Prompt(
    new TextPrompt<string>("What's your favorite fruit?")
      .AddChoices(["Apple", "Banana", "Orange"])
      .DefaultValue("Orange"));

// Echo the fruit back to the terminal
Console.WriteLine($"I agree. {fruit} is tasty!");
```

----------------------------------------

TITLE: Using Asynchronous Status with Spectre.Console
DESCRIPTION: This snippet illustrates the asynchronous usage of the Spectre.Console Status control with AnsiConsole.Status().StartAsync(). It's designed for async/await patterns, allowing non-blocking operations to display progress. The task context (ctx) can be used to update the status and spinner.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/live/status.md#_snippet_1

LANGUAGE: csharp
CODE:
```
// Asynchronous
await AnsiConsole.Status()
    .StartAsync("Thinking...", async ctx => 
    {
        // Omitted
    });
```

----------------------------------------

TITLE: Setting a Default Value for a Command Option in C#
DESCRIPTION: This C# snippet demonstrates how to assign a default value to a command option using the `DefaultValue` attribute. If the user does not specify the `-c` or `--count` option on the command line, the `Count` property will automatically be initialized to `1`, providing a sensible fallback value.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/settings.md#_snippet_4

LANGUAGE: C#
CODE:
```
[CommandOption("-c|--count")]
[DefaultValue(1)]
public int Count { get; set; }
```

----------------------------------------

TITLE: Correctly Using AnsiConsole.Confirm after Spectre.Console Progress (C#)
DESCRIPTION: This example shows the correct approach to avoid Spectre1021 violations. The `AnsiConsole.Confirm` prompt is called only after the initial `AnsiConsole.Progress` renderable has completed, ensuring exclusive use of ANSI sequences and preventing output corruption. State can be persisted and reapplied if the progress needs to resume after the prompt.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/analyzer/rules/Spectre1021.md#_snippet_1

LANGUAGE: csharp
CODE:
```
AnsiConsole.Progress().Start(ctx =>
{
    // code to update progress bar

    // persist state to restart progress after asking question   
});

var answer = AnsiConsole.Confirm("Continue?");

AnsiConsole.Progress().Start(ctx =>
{
    // apply persisted state
    // code to update progress bar

```

----------------------------------------

TITLE: Setting Fixed Table Width in Spectre.Console (C#)
DESCRIPTION: This snippet demonstrates how to set a fixed width for a Spectre.Console table. The `Width()` method allows specifying the desired width in cells, overriding automatic sizing.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/table.md#_snippet_4

LANGUAGE: csharp
CODE:
```
// Sets the table width to 50 cells
table.Width(50);
```

----------------------------------------

TITLE: Setting Panel Border Styles in C#
DESCRIPTION: This C# snippet illustrates how to customize the border style of a `Panel` by assigning different `BoxBorder` enumeration values to its `Border` property. It provides examples for various border types like ASCII, Square, Rounded, Heavy, Double, and None, allowing visual customization of the panel's frame.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/panel.md#_snippet_2

LANGUAGE: C#
CODE:
```
panel.Border = BoxBorder.Ascii;
panel.Border = BoxBorder.Square;
panel.Border = BoxBorder.Rounded;
panel.Border = BoxBorder.Heavy;
panel.Border = BoxBorder.Double;
panel.Border = BoxBorder.None;
```