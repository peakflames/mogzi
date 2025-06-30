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

----------------------------------------

TITLE: Rendering JSON with Spectre.Console.Json in C#
DESCRIPTION: This C# snippet demonstrates how to parse a JSON string using `JsonText` and render it to the console within a `Panel` using `AnsiConsole.Write`. It showcases basic JSON structure including nested objects, arrays, and different data types, providing a visually appealing output.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/json.md#_snippet_1

LANGUAGE: csharp
CODE:
```
using Spectre.Console.Json;

var json = new JsonText(
    """
    { 
        "hello": 32, 
        "world": { 
            "foo": 21, 
            "bar": 255,
            "baz": [
                0.32, 0.33e-32,
                0.42e32, 0.55e+32,
                {
                    "hello": "world",
                    "lol": null
                }
            ]
        } 
    }
    """);

AnsiConsole.Write(
    new Panel(json)
        .Header("Some JSON in a panel")
        .Collapse()
        .RoundedBorder()
        .BorderColor(Color.Yellow));
```

----------------------------------------

TITLE: Rendering BreakdownChart at Full Console Width in C#
DESCRIPTION: This example shows how to render a `BreakdownChart` to occupy the full width of the console by using the `.FullSize()` method. It adds the same set of items as the basic example, but adapts its display to the available console space.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/breakdownchart.md#_snippet_1

LANGUAGE: C#
CODE:
```
AnsiConsole.Write(new BreakdownChart()
    .FullSize()
    .AddItem("SCSS", 80, Color.Red)
    .AddItem("HTML", 28.3, Color.Blue)
    .AddItem("C#", 22.6, Color.Green)
    .AddItem("JavaScript", 6, Color.Yellow)
    .AddItem("Ruby", 6, Color.LightGreen)
    .AddItem("Shell", 0.1, Color.Aqua));
```

----------------------------------------

TITLE: Allowing Optional User Input in C#
DESCRIPTION: This snippet demonstrates how to make a prompt optional using the `AllowEmpty()` method on `TextPrompt<string>`. This allows the user to press Enter without providing any input, which can be useful for fields that are not strictly required.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/prompts/text.md#_snippet_8

LANGUAGE: C#
CODE:
```
// Ask the user to enter the password
var color = AnsiConsole.Prompt(
    new TextPrompt<string>("[[Optional]] Favorite color?")
        .AllowEmpty());

// Echo the color back to the terminal
Console.WriteLine(string.IsNullOrWhiteSpace(color)
    ? "You're right, all colors are beautiful"
    : $"I agree. {color} is a very beautiful color");
```

----------------------------------------

TITLE: Configuring PowerShell for Unicode and Emoji Support
DESCRIPTION: This PowerShell command configures the console's input and output encoding to UTF-8, enabling proper display of Unicode characters and and emojis. It is recommended to add this command to the profile.ps1 file for persistent configuration in PowerShell sessions.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/best-practices.md#_snippet_1

LANGUAGE: powershell
CODE:
```
[console]::InputEncoding = [console]::OutputEncoding = [System.Text.UTF8Encoding]::new()
```

----------------------------------------

TITLE: Sequential LiveRenderable Calls (Complies with Spectre1020) - C#
DESCRIPTION: This C# example shows the correct way to use multiple LiveRenderables by ensuring they run sequentially. The `AnsiConsole.Progress()` renderable completes its tasks before the `AnsiConsole.Status()` renderable is initiated. This sequential execution prevents conflicts and ensures consistent console output, adhering to the Spectre1020 rule.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/analyzer/rules/spectre1020.md#_snippet_1

LANGUAGE: csharp
CODE:
```
AnsiConsole.Progress().Start(ctx => {
    // run progress and complete tasks
});

AnsiConsole.Status().Start("Running status afterwards...", statusCtx => {});
```

----------------------------------------

TITLE: Using Static AnsiConsole Helper in C#
DESCRIPTION: This C# code snippet demonstrates a violation of the Spectre1010 rule. It uses the static AnsiConsole.WriteLine method, even though an IAnsiConsole instance is available via dependency injection. This practice reduces testability and prevents upstream callers from customizing console behavior.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/analyzer/rules/spectre1010.md#_snippet_0

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
        AnsiConsole.WriteLine("Running...");
    }

}
```

----------------------------------------

TITLE: Defining an Array Command Option in C#
DESCRIPTION: This C# snippet demonstrates how to define a command option that can accept multiple values, accumulating them into an array. When the `-n` or `--name` option is specified multiple times on the command line, `Spectre.Console.Cli` collects all provided values into the `Names` string array.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/settings.md#_snippet_6

LANGUAGE: C#
CODE:
```
[CommandOption("-n|--name <VALUES>")]
public string[] Names { get; set; }
```

----------------------------------------

TITLE: Customizing Help Text Styling in Spectre.Console.Cli
DESCRIPTION: This snippet demonstrates how to apply custom styling to the generated help text in a Spectre.Console.Cli application. It sets the HelpProviderStyles property on the config.Settings object to a new HelpProviderStyle instance, specifically making the description header bold. This allows developers to override the default theme.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/command-help.md#_snippet_0

LANGUAGE: csharp
CODE:
```
config.Settings.HelpProviderStyles = new HelpProviderStyle()
{
    Description = new DescriptionStyle()
    {
        Header = "bold",
    },
};
```

----------------------------------------

TITLE: Styling and Aligning Grid Cells in C#
DESCRIPTION: This example illustrates how to apply styles and alignment to individual cells within a Spectre.Console Grid. It uses Text objects with Style and justification methods (LeftJustified, Centered, RightJustified) to customize the appearance and positioning of text within grid cells.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/grid.md#_snippet_1

LANGUAGE: csharp
CODE:
```
var grid = new Grid();
        
// Add columns 
grid.AddColumn();
grid.AddColumn();
grid.AddColumn();

// Add header row 
grid.AddRow(new Text[]{
    new Text("Header 1", new Style(Color.Red, Color.Black)).LeftJustified(),
    new Text("Header 2", new Style(Color.Green, Color.Black)).Centered(),
    new Text("Header 3", new Style(Color.Blue, Color.Black)).RightJustified()
});

// Add content row 
grid.AddRow(new Text[]{
    new Text("Row 1").LeftJustified(),
    new Text("Row 2").Centered(),
    new Text("Row 3").RightJustified()
});

// Write centered cell grid contents to Console
AnsiConsole.Write(grid);
```

----------------------------------------

TITLE: Calling AnsiConsole.Confirm within Spectre.Console Progress (C#)
DESCRIPTION: This snippet demonstrates a violation of the Spectre1021 rule by calling `AnsiConsole.Confirm` (a prompt) inside an active `AnsiConsole.Progress` renderable. This concurrent operation can lead to corrupt console output due to conflicting ANSI sequence usage, as both components attempt to control the console's drawing.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/analyzer/rules/Spectre1021.md#_snippet_0

LANGUAGE: csharp
CODE:
```
AnsiConsole.Progress().Start(ctx =>
{
    // code to update progress bar
    var answer = AnsiConsole.Confirm("Continue?");
});
```

----------------------------------------

TITLE: Configuring Custom Progress Bar Columns in Spectre.Console
DESCRIPTION: This snippet demonstrates how to customize the display of a Spectre.Console progress bar by specifying a collection of `ProgressColumn` types. It shows how to include columns like task description, progress bar, percentage, remaining time, spinner, downloaded amount, and transfer speed, while also controlling auto-refresh, auto-clear, and hiding completed tasks.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/live/progress.md#_snippet_2

LANGUAGE: csharp
CODE:
```
AnsiConsole.Progress()
    .AutoRefresh(false) // Turn off auto refresh
    .AutoClear(false)   // Do not remove the task list when done
    .HideCompleted(false)   // Hide tasks as they are completed
    .Columns(new ProgressColumn[] 
    {
        new TaskDescriptionColumn(),    // Task description
        new ProgressBarColumn(),        // Progress bar
        new PercentageColumn(),         // Percentage
        new RemainingTimeColumn(),      // Remaining time
        new SpinnerColumn(),            // Spinner
        new DownloadedColumn(),         // Downloaded
        new TransferSpeedColumn(),      // Transfer speed
    })
    .Start(ctx =>
    {
        // Omitted
    });
```

----------------------------------------

TITLE: Styling JSON Output with Spectre.Console.Json in C#
DESCRIPTION: This C# example illustrates how to customize the colors of different JSON elements (braces, brackets, colons, commas, strings, numbers, booleans, and nulls) using the fluent API provided by `JsonText`. This allows for highly readable and visually distinct JSON representations in the console.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/json.md#_snippet_2

LANGUAGE: csharp
CODE:
```
AnsiConsole.Write(
    new JsonText(json)
        .BracesColor(Color.Red)
        .BracketColor(Color.Green)
        .ColonColor(Color.Blue)
        .CommaColor(Color.Red)
        .StringColor(Color.Green)
        .NumberColor(Color.Blue)
        .BooleanColor(Color.Red)
        .NullColor(Color.Green));
```

----------------------------------------

TITLE: Rendering a Basic Bar Chart with Spectre.Console (C#)
DESCRIPTION: This snippet demonstrates the fundamental way to create and display a bar chart using Spectre.Console.BarChart. It sets the chart width, a centered label, and adds individual items with their labels, values, and colors.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/barchart.md#_snippet_0

LANGUAGE: csharp
CODE:
```
AnsiConsole.Write(new BarChart()
    .Width(60)
    .Label("[green bold underline]Number of fruits[/]")
    .CenterLabel()
    .AddItem("Apple", 12, Color.Yellow)
    .AddItem("Orange", 54, Color.Green)
    .AddItem("Banana", 33, Color.Red));
```

----------------------------------------

TITLE: Escaping Markup Format Characters in Spectre.Console (C#)
DESCRIPTION: This code shows how to escape the special `[` and `]` characters within Spectre.Console markup. By doubling the brackets (e.g., `[[` or `]]`), they are rendered literally instead of being interpreted as style delimiters.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/markup.md#_snippet_3

LANGUAGE: csharp
CODE:
```
AnsiConsole.Markup("[[Hello]] "); // [Hello]
AnsiConsole.Markup("[red][[World]][/]"); // [World]
```

----------------------------------------

TITLE: Setting Background Colors in Spectre.Console Markup (C#)
DESCRIPTION: This snippet shows how to apply background colors to text using the `on` prefix within markup tags (e.g., `on blue`). It demonstrates setting both a specific foreground and background color, and using `default` for the foreground with a specific background.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/markup.md#_snippet_7

LANGUAGE: csharp
CODE:
```
AnsiConsole.Markup("[bold yellow on blue]Hello[/]");
AnsiConsole.Markup("[default on blue]World[/]");
```

----------------------------------------

TITLE: Implementing Custom Validation for Command Settings in C#
DESCRIPTION: This snippet shows how to implement custom validation logic by overriding the `Validate` method within a `CommandSettings` class. This method allows for complex validation rules beyond simple type checks, returning `ValidationResult.Error` with a message if validation fails, or `ValidationResult.Success` otherwise. Here, it ensures the `Name` property has a minimum length.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/settings.md#_snippet_9

LANGUAGE: C#
CODE:
```
public class Settings : CommandSettings
{
    [Description("The name to display")]
    [CommandArgument(0, "[Name]")]
    public string? Name { get; init; }

    public override ValidationResult Validate()
    {
        return Name.Length < 2
            ? ValidationResult.Error("Names must be at least two characters long")
            : ValidationResult.Success();
    }
}
```

----------------------------------------

TITLE: Basic Usage of Async Spinner Extension in C#
DESCRIPTION: This snippet demonstrates the fundamental ways to use the Spectre.Console Async Spinner Extension. It shows how to apply a spinner to a void Task, a Task<T> to capture a result, and how to customize the spinner's appearance and target a specific IAnsiConsole instance.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/live/async.md#_snippet_0

LANGUAGE: csharp
CODE:
```
// Basic usage with void Task
await someTask.Spinner();

// With generic Task<T>
var result = await someTaskWithResult.Spinner(
    Spinner.Known.Star,
    new Style(foreground: Color.Green));

// With custom console
await someTask.Spinner(
    Spinner.Known.Dots,
    style: Style.Plain,
    ansiConsole: customConsole);
```

----------------------------------------

TITLE: Adding Bar Chart Items by Implementing IBarChartItem in Spectre.Console (C#)
DESCRIPTION: This snippet illustrates how to use custom data types as bar chart items by implementing the IBarChartItem interface. It defines a Fruit class that conforms to the interface, allowing direct addition of Fruit objects to the BarChart.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/barchart.md#_snippet_2

LANGUAGE: csharp
CODE:
```
public sealed class Fruit : IBarChartItem
{
    public string Label { get; set; }
    public double Value { get; set; }
    public Color? Color { get; set; }

    public Fruit(string label, double value, Color? color = null)
    {
        Label = label;
        Value = value;
        Color = color;
    }
}

// Create a list of fruits
var items = new List<Fruit>
{
    new Fruit("Apple", 12, Color.Yellow),
    new Fruit("Orange", 54, Color.Red),
    new Fruit("Banana", 33, Color.Green),
};

// Render bar chart
AnsiConsole.Write(new BarChart()
    .Width(60)
    .Label("[green bold underline]Number of fruits[/]")
    .CenterLabel()
    .AddItem(new Fruit("Mango", 3))
    .AddItems(items));
```

----------------------------------------

TITLE: Advanced Async Spinner Extension Examples in C#
DESCRIPTION: This example illustrates more advanced usage of the Spectre.Console Async Spinner Extension. It covers applying a basic spinner to a delayed task, customizing the spinner's style and type for a task returning a value, and using a custom IAnsiConsole instance with a custom spinner animation sequence.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/live/async.md#_snippet_1

LANGUAGE: csharp
CODE:
```
// Basic spinner with default settings
await Task.Delay(1000)
    .Spinner(Spinner.Known.Dots);

// Customized spinner with style
var result = await CalculateSomething()
    .Spinner(
        Spinner.Known.Star,
        new Style(foreground: Color.Green));

// Using with a custom console
await ProcessData()
    .Spinner(
        new Spinner(new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" }, 80),
        new Style(foreground: Color.Blue),
        customConsole);
```

----------------------------------------

TITLE: Invoking myapp animal command
DESCRIPTION: These examples demonstrate how to use the 'myapp animal' command to create or modify animal entries. They show variations for 'dog' and 'horse' types, specifying names, age, and boolean attributes like '--good-boy' or '--IsAlive'.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Help/Root_Examples_Leafs_Eight.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
myapp animal dog --name Rufus --age 12 --good-boy
```

LANGUAGE: Shell
CODE:
```
myapp animal dog --name Luna
```

LANGUAGE: Shell
CODE:
```
myapp animal dog --name Charlie
```

LANGUAGE: Shell
CODE:
```
myapp animal dog --name Bella
```

LANGUAGE: Shell
CODE:
```
myapp animal dog --name Daisy
```

LANGUAGE: Shell
CODE:
```
myapp animal dog --name Milo
```

LANGUAGE: Shell
CODE:
```
myapp animal horse --name Brutus
```

LANGUAGE: Shell
CODE:
```
myapp animal horse --name Sugar --IsAlive false
```

----------------------------------------

TITLE: Using the myapp dog Command (Shell)
DESCRIPTION: Demonstrates various ways to invoke the `myapp dog` command. This command is used to define a dog, accepting a `--name` (string), an optional `--age` (integer), and an optional `--good-boy` (boolean flag) parameter. It's primarily used for registering or describing dog entities within the application.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Help/Root_Examples_Children_Twelve.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
myapp dog --name Rufus --age 12 --good-boy
```

LANGUAGE: Shell
CODE:
```
myapp dog --name Luna
```

LANGUAGE: Shell
CODE:
```
myapp dog --name Charlie
```

LANGUAGE: Shell
CODE:
```
myapp dog --name Bella
```

LANGUAGE: Shell
CODE:
```
myapp dog --name Daisy
```

LANGUAGE: Shell
CODE:
```
myapp dog --name Milo
```

----------------------------------------

TITLE: Applying Custom Styles to Columns (C#)
DESCRIPTION: This snippet illustrates how to apply individual `Style` objects to `Text` widgets within `Spectre.Console.Columns`, allowing for custom foreground and background colors for each column. This provides fine-grained control over the appearance of each rendered column.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/columns.md#_snippet_2

LANGUAGE: csharp
CODE:
```
// Create a list of Items, apply separate styles to each
var columns = new List<Text>(){
    new Text("Item 1", new Style(Color.Red, Color.Black)),
    new Text("Item 2", new Style(Color.Green, Color.Black)),
    new Text("Item 3", new Style(Color.Blue, Color.Black))
};

// Renders each item with own style
AnsiConsole.Write(new Columns(columns));
```

----------------------------------------

TITLE: Embedding Grids within Grids in C#
DESCRIPTION: This snippet demonstrates the advanced capability of embedding one Spectre.Console Grid inside another. It creates a main grid and an embedded grid, then adds the embedded grid as a cell content to a row in the main grid, showcasing complex layout possibilities.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/grid.md#_snippet_2

LANGUAGE: csharp
CODE:
```
var grid = new Grid();
        
// Add columns 
grid.AddColumn();
grid.AddColumn();
grid.AddColumn();

// Add header row 
grid.AddRow(new Text[]{
    new Text("Header 1", new Style(Color.Red, Color.Black)).LeftJustified(),
    new Text("Header 2", new Style(Color.Green, Color.Black)).Centered(),
    new Text("Header 3", new Style(Color.Blue, Color.Black)).RightJustified()
});

var embedded = new Grid();

embedded.AddColumn();
embedded.AddColumn();

embedded.AddRow(new Text("Embedded I"), new Text("Embedded II"));
embedded.AddRow(new Text("Embedded III"), new Text("Embedded IV"));

// Add content row 
grid.AddRow(
    new Text("Row 1").LeftJustified(),
    new Text("Row 2").Centered(),
    embedded
);

// Write centered cell grid contents to Console
AnsiConsole.Write(grid);
```

----------------------------------------

TITLE: Loading and Rendering an Image with CanvasImage in C#
DESCRIPTION: This C# snippet demonstrates how to load an image using `CanvasImage`, set its maximum display width, and then render it to the console using `AnsiConsole.Write`. It requires the `Spectre.Console.ImageSharp` package.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/canvas-image.md#_snippet_1

LANGUAGE: csharp
CODE:
```
// Load an image
var image = new CanvasImage("cake.png");

// Set the max width of the image.
// If no max width is set, the image will take
// up as much space as there is available.
image.MaxWidth(16);

// Render the image to the console
AnsiConsole.Write(image);
```

----------------------------------------

TITLE: Shortening Exception Parts with Spectre.Console C#
DESCRIPTION: This snippet shows how to shorten specific parts of an exception, such as paths, types, and methods, using ExceptionFormats flags. It also enables clickable hyperlinks for paths, if supported by the terminal. The 'ex' parameter is the System.Exception object.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/exceptions.md#_snippet_1

LANGUAGE: csharp
CODE:
```
AnsiConsole.WriteException(ex, 
    ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes |
    ExceptionFormats.ShortenMethods | ExceptionFormats.ShowLinks);
```

----------------------------------------

TITLE: Displaying Help Information for myapp (Shell)
DESCRIPTION: Provides an example of how to retrieve the help information for the `myapp` command, demonstrating the use of the `--help` option.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Help/CommandExamples.Output.verified.txt#_snippet_1

LANGUAGE: Shell
CODE:
```
myapp animal --help
```

----------------------------------------

TITLE: myapp cat Command-Line Usage and Options
DESCRIPTION: This snippet details the command-line interface for the 'myapp cat' command, including its arguments, options, and subcommands. It specifies how to provide the number of legs, set the animal's alive status, define its name, and configure its agility. It also outlines the 'lion' subcommand with its specific 'teeth' argument.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Help/Branch.Output.verified.txt#_snippet_0

LANGUAGE: CLI
CODE:
```
USAGE:
    myapp cat [LEGS] [OPTIONS] <COMMAND>

ARGUMENTS:
    [LEGS]    The number of legs

OPTIONS:
                             DEFAULT
    -h, --help                          Prints help information
    -a, --alive                         Indicates whether or not the animal is alive
    -n, --name <VALUE>
        --agility <VALUE>    10         The agility between 0 and 100

COMMANDS:
    lion <TEETH>    The lion command
```

----------------------------------------

TITLE: Defining an Array Command Argument (Argument Vector) in C#
DESCRIPTION: This C# snippet shows how to define a command argument that accepts multiple values as an array, known as an argument vector. The `Name` property, declared as `string[]`, will capture all subsequent unparsed command-line arguments into an array. This feature is limited to one argument vector per command and must be the last argument.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/settings.md#_snippet_5

LANGUAGE: C#
CODE:
```
[CommandArgument(0, "[name]")]
public string[] Name { get; set; }
```

----------------------------------------

TITLE: Rendering Two Items in Columns (C#)
DESCRIPTION: This snippet demonstrates the basic usage of `Spectre.Console.Columns` to render two `Text` widgets side-by-side in separate columns to the console. It shows how to directly pass `Text` instances to the `Columns` constructor.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/columns.md#_snippet_0

LANGUAGE: csharp
CODE:
```
// Render two items on separate columns to Console
AnsiConsole.Write(new Columns(
            new Text("Item 1"),
            new Text("Item 2")
        ));
```

----------------------------------------

TITLE: Customizing Command Help and Behavior in C#
DESCRIPTION: This snippet shows how to customize the help output and behavior of a specific command. It demonstrates adding an alias, a descriptive text, an example for help screens, and marking the command as hidden so it won't appear in default help listings but remains executable.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/commandApp.md#_snippet_2

LANGUAGE: C#
CODE:
```
var app = new CommandApp();
app.Configure(config =>
{
    config.AddCommand<FileSizeCommand>("size")
        .IsHidden()
        .WithAlias("file-size")
        .WithDescription("Gets the file size for a directory.")
        .WithExample(new[] {"size", "c:\\windows", "--pattern", "*.dll"});
});
```

----------------------------------------

TITLE: Configuring Commands with CommandApp in Spectre.Console.Cli (C#)
DESCRIPTION: This snippet illustrates how to configure commands using the `CommandApp` instance. It shows how to add a command, assign aliases, provide a description for help rendering, and define examples for user guidance, enhancing the command-line interface's usability.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/commands.md#_snippet_1

LANGUAGE: C#
CODE:
```
var app = new CommandApp();
app.Configure(config =>
{
    config.AddCommand<HelloCommand>("hello")
        .WithAlias("hola")
        .WithDescription("Say hello")
        .WithExample("hello", "Phil")
        .WithExample("hello", "Phil", "--count", "4");
});
```

----------------------------------------

TITLE: Creating a Panel instance in C#
DESCRIPTION: This snippet demonstrates how to create a basic `Panel` instance in C# by passing a string to its constructor, which sets the initial content of the panel. This is the fundamental step to use the `Panel` widget.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/panel.md#_snippet_0

LANGUAGE: C#
CODE:
```
var panel = new Panel("Hello World");
```

----------------------------------------

TITLE: Escaping Markup with Markup.Escape Static Method (C#)
DESCRIPTION: Similar to the extension method, this snippet shows using the static `Markup.Escape` method to escape strings containing potential markup characters. This ensures that the provided string is treated as literal text when rendered by Spectre.Console.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/markup.md#_snippet_5

LANGUAGE: csharp
CODE:
```
AnsiConsole.Markup("[red]{0}[/]", Markup.Escape("Hello [World]"));
```

----------------------------------------

TITLE: General Usage of myapp Command-Line Tool - Shell
DESCRIPTION: This snippet illustrates the general syntax for invoking the 'myapp' command-line tool. It shows that 'myapp' can be followed by optional global options and a required command.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Help/RootExamples_Leafs.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
myapp [OPTIONS] <COMMAND>
```

----------------------------------------

TITLE: Customizing Exception Output Styling with Spectre.Console C#
DESCRIPTION: This snippet demonstrates how to customize the styling of exception output using ExceptionSettings. It allows overriding default colors for various exception components like messages, methods, and paths, providing fine-grained control over presentation. The 'ex' parameter is the System.Exception object.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/exceptions.md#_snippet_2

LANGUAGE: csharp
CODE:
```
AnsiConsole.WriteException(ex, new ExceptionSettings
{
    Format = ExceptionFormats.ShortenEverything | ExceptionFormats.ShowLinks,
    Style = new ExceptionStyle
    {
        Exception = new Style().Foreground(Color.Grey),
        Message = new Style().Foreground(Color.White),
        NonEmphasized = new Style().Foreground(Color.Cornsilk1),
        Parenthesis = new Style().Foreground(Color.Cornsilk1),
        Method = new Style().Foreground(Color.Red),
        ParameterName = new Style().Foreground(Color.Cornsilk1),
        ParameterType = new Style().Foreground(Color.Red),
        Path = new Style().Foreground(Color.Red),
        LineNumber = new Style().Foreground(Color.Cornsilk1)
    }
});
```

----------------------------------------

TITLE: Rendering Basic Rows with Spectre.Console (C#)
DESCRIPTION: This snippet demonstrates the fundamental usage of `Spectre.Console.Rows` to display multiple `Text` items on separate horizontal lines in the console. It shows how to pass individual `Text` objects directly to the `Rows` constructor for simple, sequential rendering.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/rows.md#_snippet_0

LANGUAGE: csharp
CODE:
```
// Render two items on separate rows to Console
AnsiConsole.Write(new Rows(
            new Text("Item 1"),
            new Text("Item 2")
        ));
```

----------------------------------------

TITLE: Configuring Live Display Options in Spectre.Console
DESCRIPTION: This snippet illustrates how to configure various options for the `LiveDisplay` widget, such as `AutoClear`, `Overflow`, and `Cropping`. These settings control the behavior of the live display when it finishes or when content exceeds the available space. This requires `Spectre.Console`.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/live/live-display.md#_snippet_2

LANGUAGE: csharp
CODE:
```
var table = new Table().Centered();

AnsiConsole.Live(table)
    .AutoClear(false)   // Do not remove when done
    .Overflow(VerticalOverflow.Ellipsis) // Show ellipsis when overflowing
    .Cropping(VerticalOverflowCropping.Top) // Crop overflow at top
    .Start(ctx =>
    {
        // Omitted
    });
```

----------------------------------------

TITLE: Applying Basic Padding with Spectre.Console Padder (C#)
DESCRIPTION: This snippet demonstrates how to apply basic padding to individual Text elements using Spectre.Console.Padder. It shows how to set right, bottom, and top padding, and then arranges these padded elements within a Grid before writing the entire structure to the console.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/padder.md#_snippet_0

LANGUAGE: csharp
CODE:
```
// Create three text elements
var paddedText_I = new Text("Padded Text I", new Style(Color.Red, Color.Black));
var paddedText_II = new Text("Padded Text II", new Style(Color.Green, Color.Black));
var paddedText_III = new Text("Padded Text III", new Style(Color.Blue, Color.Black));

// Apply padding to the three text elements
var pad_I = new Padder(paddedText_I).PadRight(16).PadBottom(0).PadTop(4);
var pad_II = new Padder(paddedText_II).PadBottom(0).PadTop(2);
var pad_III = new Padder(paddedText_III).PadLeft(16).PadBottom(0).PadTop(0);

// Insert padded elements within single-row grid
var grid = new Grid();

grid.AddColumn();
grid.AddColumn();
grid.AddColumn();

grid.AddRow(pad_I, pad_II, pad_III);

// Write grid and it's padded contents to the Console
AnsiConsole.Write(grid);
```

----------------------------------------

TITLE: Setting Table Borders in Spectre.Console (C#)
DESCRIPTION: This code snippet illustrates how to customize the border style of a Spectre.Console table. It shows examples of setting the border to `None`, `Ascii`, `Square`, and `Rounded` using the `Border()` method with different `TableBorder` enumeration values.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/table.md#_snippet_1

LANGUAGE: csharp
CODE:
```
// Sets the border
table.Border(TableBorder.None);
table.Border(TableBorder.Ascii);
table.Border(TableBorder.Square);
table.Border(TableBorder.Rounded);
```

----------------------------------------

TITLE: Displaying CLI Usage for 'myapp' Application
DESCRIPTION: This snippet illustrates the command-line syntax for 'myapp', detailing its required positional arguments (<FOO>, <BAR>, <BAZ>, <CORGI>) and an optional argument ([QUX]), along with standard help and version options.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Help/Version.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
USAGE:
    myapp <FOO> <BAR> <BAZ> <CORGI> [QUX] [OPTIONS]

ARGUMENTS:
    <FOO>
    <BAR>
    <BAZ>
    <CORGI>
    [QUX]

OPTIONS:
    -h, --help       Prints help information
    -v, --version    Prints version information
```

----------------------------------------

TITLE: Nesting Padded Elements within Spectre.Console Padder (C#)
DESCRIPTION: This example illustrates how to nest Padder instances, applying padding to individual Text elements first, then arranging them in a Grid, and finally applying additional padding to the entire Grid itself. This demonstrates hierarchical padding for complex layouts.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/padder.md#_snippet_1

LANGUAGE: csharp
CODE:
```
// Create two text elements
var paddedText_I = new Text("Padded Text I", new Style(Color.Red, Color.Black));
var paddedText_II = new Text("Padded Text II", new Style(Color.Blue, Color.Black));

// Create, apply padding on text elements
var pad_I = new Padder(paddedText_I).PadRight(2).PadBottom(0).PadTop(0);
var pad_II = new Padder(paddedText_II).PadLeft(2).PadBottom(0).PadTop(0);

// Insert the text elements into a single row grid
var grid = new Grid();

grid.AddColumn();
grid.AddColumn();

grid.AddRow(pad_I, pad_II);

// Apply horizontal and vertical padding on the grid
var paddedGrid = new Padder(grid).Padding(4,1);

// Write the padded grid to the Console
AnsiConsole.Write(paddedGrid);
```

----------------------------------------

TITLE: Executing Dog Command with Parameters - Shell
DESCRIPTION: Demonstrates how to use the 'dog' command with specific parameters like name, age, and a boolean flag.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Help/RootExamples.Output.verified.txt#_snippet_1

LANGUAGE: Shell
CODE:
```
myapp dog --name Rufus --age 12 --good-boy
```

----------------------------------------

TITLE: Setting Column Content Alignment in Spectre.Console (C#)
DESCRIPTION: This snippet demonstrates how to set the horizontal alignment for content within a specific column of a Spectre.Console table. It accesses the column via `table.Columns[index]` and applies alignment using `Alignment()` or convenience methods like `LeftAligned()`, `Centered()`, and `RightAligned()`.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/table.md#_snippet_6

LANGUAGE: csharp
CODE:
```
table.Columns[0].Alignment(Justify.Right);
table.Columns[0].LeftAligned();
table.Columns[0].Centered();
table.Columns[0].RightAligned();
```

----------------------------------------

TITLE: Adding Bar Chart Items with a Converter in Spectre.Console (C#)
DESCRIPTION: This example shows how to populate a BarChart from an existing collection of data using a converter function. It iterates through a list of tuples, transforming each into a BarChartItem on the fly, providing flexibility for custom data structures.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/barchart.md#_snippet_1

LANGUAGE: csharp
CODE:
```
// Create a list of fruits
var items = new List<(string Label, double Value)>
{
    ("Apple", 12),
    ("Orange", 54),
    ("Banana", 33),
};

// Render bar chart
AnsiConsole.Write(new BarChart()
    .Width(60)
    .Label("[green bold underline]Number of fruits[/]")
    .CenterLabel()
    .AddItems(items, (item) => new BarChartItem(
        item.Label, item.Value, Color.Yellow)));
```

----------------------------------------

TITLE: Applying a Value Formatter to BreakdownChart Numbers in C#
DESCRIPTION: This example demonstrates how to customize the display format of numerical values within a `BreakdownChart` using `UseValueFormatter`. It shows both a multi-line and a chained approach to apply a custom formatting function (e.g., `N0` for no decimal places) to the chart's values.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/breakdownchart.md#_snippet_7

LANGUAGE: C#
CODE:
```
var chart = new BreakdownChart();
chart.UseValueFormater(value => value.ToString("N0"));

// This can be simplified as extension methods are chainable.
var chart = new BreakdownChart().UseValueFormatter(v => v.ToString("N0"));
```

----------------------------------------

TITLE: Using the myapp horse Command (Shell)
DESCRIPTION: Illustrates different invocations of the `myapp horse` command. This command is used to define a horse, accepting a `--name` (string) and an optional `--IsAlive` (boolean) parameter. It's typically used for registering or describing horse entities, including their living status.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Help/Root_Examples_Children_Twelve.Output.verified.txt#_snippet_1

LANGUAGE: Shell
CODE:
```
myapp horse --name Brutus
```

LANGUAGE: Shell
CODE:
```
myapp horse --name Sugar --IsAlive false
```

LANGUAGE: Shell
CODE:
```
myapp horse --name Cash
```

LANGUAGE: Shell
CODE:
```
myapp horse --name Dakota
```

LANGUAGE: Shell
CODE:
```
myapp horse --name Cisco
```

LANGUAGE: Shell
CODE:
```
myapp horse --name Spirit
```

----------------------------------------

TITLE: Examples of `myapp` Command Invocation (Shell)
DESCRIPTION: These examples demonstrate various ways to invoke the `myapp` command with different combinations of arguments and options, showcasing common use cases for specifying dog names, age, and 'good-boy' status.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Help/Default_Examples.Output.verified.txt#_snippet_1

LANGUAGE: Shell
CODE:
```
myapp --name Rufus --age 12 --good-boy
```

LANGUAGE: Shell
CODE:
```
myapp --name Luna
```

LANGUAGE: Shell
CODE:
```
myapp --name Charlie
```

LANGUAGE: Shell
CODE:
```
myapp --name Bella
```

LANGUAGE: Shell
CODE:
```
myapp --name Daisy
```

LANGUAGE: Shell
CODE:
```
myapp --name Milo
```

----------------------------------------

TITLE: Setting Panel Border in Spectre.Console (C#)
DESCRIPTION: This snippet illustrates how to apply the Rounded border style to a Panel instance in Spectre.Console. It creates a new Panel with content and then sets its Border property to BoxBorder.Rounded, giving the panel a rounded visual frame.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/appendix/borders.md#_snippet_1

LANGUAGE: csharp
CODE:
```
var panel = new Panel("Hello World");
panel.Border = BoxBorder.Rounded;
```

----------------------------------------

TITLE: Using myapp horse command - Shell
DESCRIPTION: Illustrates different ways to use the 'myapp horse' command, including setting the horse's name and its 'IsAlive' status. This command is primarily used for managing horse-related entries in the application.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Help/Root_Examples.Output.verified.txt#_snippet_1

LANGUAGE: Shell
CODE:
```
myapp horse --name Brutus
```

LANGUAGE: Shell
CODE:
```
myapp horse --name Sugar --IsAlive false
```

LANGUAGE: Shell
CODE:
```
myapp horse --name Cash
```

LANGUAGE: Shell
CODE:
```
myapp horse --name Dakota
```

LANGUAGE: Shell
CODE:
```
myapp horse --name Cisco
```

LANGUAGE: Shell
CODE:
```
myapp horse --name Spirit
```

----------------------------------------

TITLE: Setting Table Content Alignment in Spectre.Console (C#)
DESCRIPTION: This snippet illustrates how to set the overall horizontal alignment for the content within a Spectre.Console table. It shows using the `Alignment()` method with `Justify` enum values, as well as convenience methods like `RightAligned()`, `Centered()`, and `LeftAligned()`.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/table.md#_snippet_5

LANGUAGE: csharp
CODE:
```
table.Alignment(Justify.Right);
table.RightAligned();
table.Centered();
table.LeftAligned();
```

----------------------------------------

TITLE: myapp CLI Usage, Options, and Animal Command Examples
DESCRIPTION: This snippet provides a comprehensive overview of the 'myapp' command-line tool. It includes the general usage pattern, specific examples for the 'animal dog' subcommand with various attributes, and definitions for the help option and the 'animal' command itself.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Help/Root_Examples_Leafs.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
myapp [OPTIONS] <COMMAND>
```

LANGUAGE: Shell
CODE:
```
myapp animal dog --name Rufus --age 12 --good-boy
```

LANGUAGE: Shell
CODE:
```
myapp animal dog --name Luna
```

LANGUAGE: Shell
CODE:
```
myapp animal dog --name Charlie
```

LANGUAGE: Shell
CODE:
```
myapp animal dog --name Bella
```

LANGUAGE: Shell
CODE:
```
myapp animal dog --name Daisy
```

LANGUAGE: Shell
CODE:
```
-h, --help    Prints help information
```

LANGUAGE: Shell
CODE:
```
animal    The animal command
```

----------------------------------------

TITLE: Implementing a Custom Spinner in Spectre.Console (C#)
DESCRIPTION: This snippet illustrates how to create a custom spinner by inheriting from the Spinner base class in Spectre.Console. It defines a spinner named MySpinner that cycles through the characters 'A', 'B', and 'C' with an interval of 100 milliseconds per frame. The IsUnicode property is set to false as the frames are simple ASCII characters.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/appendix/spinners.md#_snippet_1

LANGUAGE: csharp
CODE:
```
public sealed class MySpinner : Spinner
{
    // The interval for each frame
    public override TimeSpan Interval => TimeSpan.FromMilliseconds(100);
    
    // Whether or not the spinner contains unicode characters
    public override bool IsUnicode => false;

    // The individual frames of the spinner
    public override IReadOnlyList<string> Frames => 
        new List<string>
        {
            "A", "B", "C",
        };
}
```

----------------------------------------

TITLE: Adjusting Table Width Expansion in Spectre.Console (C#)
DESCRIPTION: This snippet demonstrates how to control the width behavior of a Spectre.Console table. The `Expand()` method makes the table occupy as much available space as possible, while `Collapse()` makes it take up the minimal required width.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/table.md#_snippet_2

LANGUAGE: csharp
CODE:
```
// Table will take up as much space as it can
// with respect to other things.
table.Expand();

// Table will take up minimal width
table.Collapse();
```

----------------------------------------

TITLE: Displaying `myapp` Command Usage (Shell)
DESCRIPTION: This snippet illustrates the general usage syntax for the `myapp` command, showing required arguments (`<AGE>`) and optional arguments (`[LEGS]`) and options (`[OPTIONS]`). It defines the basic structure for invoking the command.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Help/Default_Examples.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
myapp <AGE> [LEGS] [OPTIONS]
```

----------------------------------------

TITLE: Executing Horse Command with Parameters - Shell
DESCRIPTION: Shows how to use the 'horse' command with a name parameter.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Help/RootExamples.Output.verified.txt#_snippet_2

LANGUAGE: Shell
CODE:
```
myapp horse --name Brutus
```

----------------------------------------

TITLE: myapp cat Command Usage - Shell
DESCRIPTION: Illustrates the general usage of the `myapp cat` command, showing its arguments (`LEGS`), options (`OPTIONS`), and a placeholder for subcommands (`COMMAND`). This is the top-level structure for interacting with the `cat` functionality.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Help/Command.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
myapp cat [LEGS] [OPTIONS] <COMMAND>
```

----------------------------------------

TITLE: Configuring CommandApp for Debugging in C#
DESCRIPTION: This snippet demonstrates how to configure the CommandApp to propagate exceptions and validate examples specifically when the application is compiled in DEBUG mode, aiding in development and debugging.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/commandApp.md#_snippet_0

LANGUAGE: C#
CODE:
```
var app = new CommandApp<FileSizeCommand>();
app.Configure(config =>
{
#if DEBUG
    config.PropagateExceptions();
    config.ValidateExamples();
#endif
});
```

----------------------------------------

TITLE: Adding Items from IEnumerable to Columns (C#)
DESCRIPTION: This example shows how to populate `Spectre.Console.Columns` from an `IEnumerable<Text>` (specifically a `List<Text>`), rendering each list item in its own column. This approach is useful for dynamically generating columns from a collection of data.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/columns.md#_snippet_1

LANGUAGE: csharp
CODE:
```
// Create a list of Items
var columns = new List<Text>(){
        new Text("Item 1"),
        new Text("Item 2"),
        new Text("Item 3")
    };

// Render each item in list on separate line
AnsiConsole.Write(new Columns(columns));
```

----------------------------------------

TITLE: Updating Using Statements for Spectre.Console.Cli (C#)
DESCRIPTION: This snippet illustrates the necessary change in C# `using` statements, replacing the old `Spectre.Cli` namespace with the new `Spectre.Console.Cli` namespace to reflect the library's relocation and ensure proper code compilation.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/migration.md#_snippet_2

LANGUAGE: diff
CODE:
```
- using Spectre.Cli;
+ using Spectre.Console.Cli;
```

----------------------------------------

TITLE: Styling Calendar Header in C#
DESCRIPTION: This example demonstrates how to apply a custom style to the calendar's header. It uses `HeaderStyle` with `Style.Parse("blue bold")` to make the header text blue and bold, enhancing its visual presentation in the terminal.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/calendar.md#_snippet_3

LANGUAGE: csharp
CODE:
```
var calendar = new Calendar(2020, 10);
calendar.HeaderStyle(Style.Parse("blue bold"));
AnsiConsole.Write(calendar);
```

----------------------------------------

TITLE: Example: Specifying a Horse Animal with Name - Shell
DESCRIPTION: This example shows another use case of the 'animal' command, specifically for a 'horse' subcommand, where only the name attribute is provided. It highlights the flexibility of command arguments.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Help/RootExamples_Leafs.Output.verified.txt#_snippet_2

LANGUAGE: Shell
CODE:
```
myapp animal horse --name Brutus
```

----------------------------------------

TITLE: Specifying Colors by Name, Hex, and RGB in Markup (C#)
DESCRIPTION: This snippet demonstrates the flexibility of specifying colors in Spectre.Console markup. It shows how to use named colors (`red`), hexadecimal color codes (`#ff0000`), and RGB function notation (`rgb(255,0,0)`) to achieve the same color output.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/markup.md#_snippet_9

LANGUAGE: csharp
CODE:
```
AnsiConsole.Markup("[red]Foo[/] ");
AnsiConsole.Markup("[#ff0000]Bar[/] ");
AnsiConsole.Markup("[rgb(255,0,0)]Baz[/] ");
```

----------------------------------------

TITLE: Escaping Markup with EscapeMarkup Extension Method (C#)
DESCRIPTION: This snippet demonstrates using the `EscapeMarkup` extension method to safely embed strings that might contain markup-like characters. It automatically escapes special characters, ensuring the string is rendered literally within a styled context.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/markup.md#_snippet_4

LANGUAGE: csharp
CODE:
```
AnsiConsole.Markup("[red]{0}[/]", "Hello [World]".EscapeMarkup());
```

----------------------------------------

TITLE: Styling TextPath Segments via Extension Methods (Style) in Spectre.Console (C#)
DESCRIPTION: This snippet shows a fluent API approach to styling `TextPath` segments using extension methods that accept `Style` objects. This allows chaining multiple style configurations directly after the constructor, providing a more compact and readable way to apply styles.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/text-path.md#_snippet_4

LANGUAGE: csharp
CODE:
```
var path = new TextPath("C:/This/Path/Is/Too/Long/To/Fit/In/The/Area.txt")
    .RootStyle(new Style(foreground: Color.Red))
    .SeparatorStyle(new Style(foreground: Color.Green))
    .StemStyle(new Style(foreground: Color.Blue))
    .LeafStyle(new Style(foreground: Color.Yellow));
```

----------------------------------------

TITLE: Styling Rule with Extension Method in C#
DESCRIPTION: Illustrates using the `RuleStyle()` extension method to apply a custom style to the rule line, offering a more fluent and readable way to set the style compared to direct property assignment.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/rule.md#_snippet_5

LANGUAGE: csharp
CODE:
```
var rule = new Rule("[red]Hello[/]");
rule.RuleStyle("red dim");
AnsiConsole.Write(rule);
```

----------------------------------------

TITLE: Configuring Spectre.Console Status Display
DESCRIPTION: This snippet shows how to configure the Spectre.Console Status control before starting a task. It demonstrates setting AutoRefresh to false, specifying a custom Spinner, and applying a SpinnerStyle, requiring manual ctx.Refresh() calls within the task to update the display.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/live/status.md#_snippet_2

LANGUAGE: csharp
CODE:
```
AnsiConsole.Status()
    .AutoRefresh(false)
    .Spinner(Spinner.Known.Star)
    .SpinnerStyle(Style.Parse("green bold"))
    .Start("Thinking...", ctx => 
    {
        // Omitted
        ctx.Refresh();
    });
```

----------------------------------------

TITLE: Registering a Custom Help Provider in Spectre.Console.Cli
DESCRIPTION: This C# code demonstrates how to register a custom implementation of IHelpProvider with a Spectre.Console.Cli application. Inside the Configure method of the CommandApp, the SetHelpProvider method is called, passing an instance of CustomHelpProvider. This allows developers to completely replace the default help generation logic.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/command-help.md#_snippet_2

LANGUAGE: csharp
CODE:
```
using Spectre.Console.Cli;

namespace Help;

public static class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandApp<DefaultCommand>();

        app.Configure(config =>
        {
            // Register the custom help provider
            config.SetHelpProvider(new CustomHelpProvider(config.Settings));
        });

        return app.Run(args);
    }
}
```

----------------------------------------

TITLE: Basic Usage of BreakdownChart in C#
DESCRIPTION: This snippet demonstrates the fundamental way to create and render a `BreakdownChart` to the console using `AnsiConsole.Write`. It initializes the chart with a specified width and adds multiple items, each defined by a label, a value, and a color.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/breakdownchart.md#_snippet_0

LANGUAGE: C#
CODE:
```
AnsiConsole.Write(new BreakdownChart()
    .Width(60)
    // Add item is in the order of label, value, then color.
    .AddItem("SCSS", 80, Color.Red)
    .AddItem("HTML", 28.3, Color.Blue)
    .AddItem("C#", 22.6, Color.Green)
    .AddItem("JavaScript", 6, Color.Yellow)
    .AddItem("Ruby", 6, Color.LightGreen)
    .AddItem("Shell", 0.1, Color.Aqua));
```

----------------------------------------

TITLE: Configuring Tree Guide Lines with Spectre.Console (C#)
DESCRIPTION: These snippets demonstrate how to customize the appearance of guide lines in a Spectre.Console `Tree` using various `TreeGuide` options, including ASCII, default, double, and bold lines, to enhance visual hierarchy.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/tree.md#_snippet_3

LANGUAGE: csharp
CODE:
```
// ASCII guide lines
var root = new Tree("Root")
    .Guide(TreeGuide.Ascii);
```

LANGUAGE: csharp
CODE:
```
// Default guide lines
var root = new Tree("Root")
    .Guide(TreeGuide.Line);
```

LANGUAGE: csharp
CODE:
```
// Double guide lines
var root = new Tree("Root")
    .Guide(TreeGuide.DoubleLine);
```

LANGUAGE: csharp
CODE:
```
// Bold guide lines
var root = new Tree("Root")
    .Guide(TreeGuide.BoldLine);
```

----------------------------------------

TITLE: Creating a Basic Rule in C#
DESCRIPTION: Demonstrates how to instantiate the `Rule` class and render a simple horizontal line to the terminal without a title using `AnsiConsole.Write`.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/rule.md#_snippet_0

LANGUAGE: csharp
CODE:
```
var rule = new Rule();
AnsiConsole.Write(rule);
```

----------------------------------------

TITLE: Applying Custom Styles to Rows with Spectre.Console (C#)
DESCRIPTION: This snippet demonstrates how to apply unique `Style` objects to individual `Text` items within a list before rendering them as rows using `Spectre.Console.Rows`. This allows for fine-grained control over the appearance of each row, such as setting distinct foreground and background colors.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/rows.md#_snippet_2

LANGUAGE: csharp
CODE:
```
// Create a list of Items, apply separate styles to each
var rows = new List<Text>(){
    new Text("Item 1", new Style(Color.Red, Color.Black)),
    new Text("Item 2", new Style(Color.Green, Color.Black)),
    new Text("Item 3", new Style(Color.Blue, Color.Black))
};

// Renders each item with own style
AnsiConsole.Write(new Rows(rows));
```

----------------------------------------

TITLE: Setting a Rule Title in C#
DESCRIPTION: Shows how to create a `Rule` with a title, including markup for styling, and then write it to the console. The title text '[red]Hello[/]' will be displayed in red.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/rule.md#_snippet_1

LANGUAGE: csharp
CODE:
```
var rule = new Rule("[red]Hello[/]");
AnsiConsole.Write(rule);
```

LANGUAGE: text
CODE:
```
───────────────────────────────── Hello ─────────────────────────────────
```

----------------------------------------

TITLE: Setting Calendar Culture in C#
DESCRIPTION: This example shows how to localize the calendar's weekdays by setting its culture. After initializing the `Calendar` with a date, the `Culture` method is called with a culture string (e.g., 'sv-SE' for Swedish) to display localized day names.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/calendar.md#_snippet_1

LANGUAGE: csharp
CODE:
```
var calendar = new Calendar(2020,10);
calendar.Culture("sv-SE");
AnsiConsole.Write(calendar);
```

----------------------------------------

TITLE: Setting Minimum Size for a Layout Pane in Spectre.Console (C#)
DESCRIPTION: This snippet shows how to set a minimum size constraint for a specific layout pane (identified by its name, "Left") using the `MinimumSize` method. This ensures the pane will not shrink below the specified character width, providing a baseline size.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/layout.md#_snippet_1

LANGUAGE: C#
CODE:
```
layout["Left"].MinimumSize(10);
```

----------------------------------------

TITLE: Using the 'dog' Command in myapp (Shell)
DESCRIPTION: Demonstrates various ways to use the 'dog' command within the 'myapp' CLI. It shows how to specify a dog's name, age, and a 'good-boy' flag, illustrating different parameter combinations for defining or interacting with dog entities.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Help/Root_Examples_Children_Eight.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
myapp dog --name Rufus --age 12 --good-boy
```

LANGUAGE: Shell
CODE:
```
myapp dog --name Luna
```

LANGUAGE: Shell
CODE:
```
myapp dog --name Charlie
```

LANGUAGE: Shell
CODE:
```
myapp dog --name Bella
```

LANGUAGE: Shell
CODE:
```
myapp dog --name Daisy
```

LANGUAGE: Shell
CODE:
```
myapp dog --name Milo
```

----------------------------------------

TITLE: Handling Secret Input with Custom Mask in C#
DESCRIPTION: This example extends secret input by demonstrating how to specify a custom mask character (e.g., a hyphen '-') using `Secret('-')`. This allows for flexibility in how masked input is visually represented to the user.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/prompts/text.md#_snippet_7

LANGUAGE: C#
CODE:
```
// Ask the user to enter the password
var password = AnsiConsole.Prompt(
    new TextPrompt<string>("Enter password:")
        .Secret('-'));

// Echo the password back to the terminal
Console.WriteLine($"Your password is {password}");
```

----------------------------------------

TITLE: Drawing Primitives with Spectre.Console Canvas in C#
DESCRIPTION: This snippet demonstrates how to create a Canvas widget, draw various primitive shapes like a cross and a border using the SetPixel method to manipulate individual pixels (coxels), and then render the canvas to the console. It requires the Spectre.Console library for console rendering capabilities.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/canvas.md#_snippet_0

LANGUAGE: csharp
CODE:
```
// Create a canvas
var canvas = new Canvas(16, 16);

// Draw some shapes
for(var i = 0; i < canvas.Width; i++)
{
    // Cross
    canvas.SetPixel(i, i, Color.White);
    canvas.SetPixel(canvas.Width - i - 1, i, Color.White);

    // Border
    canvas.SetPixel(i, 0, Color.Red);
    canvas.SetPixel(0, i, Color.Green);
    canvas.SetPixel(i, canvas.Height - 1, Color.Blue);
    canvas.SetPixel(canvas.Width - 1, i, Color.Yellow);
}

// Render the canvas
AnsiConsole.Write(canvas);
```

----------------------------------------

TITLE: Applying Custom Styles to a Rule in C#
DESCRIPTION: Shows how to apply a custom style to the rule line itself using the `Style` property and `Style.Parse` method, making the line appear red and dim in the terminal output.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/rule.md#_snippet_4

LANGUAGE: csharp
CODE:
```
var rule = new Rule("[red]Hello[/]");
rule.Style = Style.Parse("red dim");
AnsiConsole.Write(rule);
```

----------------------------------------

TITLE: Updating Exception Namespace for Spectre.Console.Cli (C#)
DESCRIPTION: This snippet shows how to update `using` statements for exception handling, moving from the deprecated `Spectre.Cli.Exceptions` namespace to the consolidated `Spectre.Console.Cli` namespace, which now contains all exception types.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/migration.md#_snippet_3

LANGUAGE: diff
CODE:
```
- using Spectre.Cli.Exceptions;
+ using Spectre.Console.Cli;
```

----------------------------------------

TITLE: Setting Ratio for a Layout Pane in Spectre.Console (C#)
DESCRIPTION: This snippet illustrates how to define a proportional size for a layout pane using the `Ratio` method. The ratio determines how available space is distributed among sibling panes, allowing for flexible resizing based on content or window size.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/layout.md#_snippet_2

LANGUAGE: C#
CODE:
```
layout["Left"].Ratio(2);
```

----------------------------------------

TITLE: Setting Panel Header in C#
DESCRIPTION: This C# snippet shows how to set the header of a `Panel` using the `Header` property. A new `PanelHeader` instance is assigned with the desired text, adding a title to the rendered box. This enhances the panel's descriptive capabilities.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/panel.md#_snippet_1

LANGUAGE: C#
CODE:
```
panel.Header = new PanelHeader("Some text");
```

----------------------------------------

TITLE: Invoking myapp CLI Commands
DESCRIPTION: Demonstrates various ways to invoke the 'myapp' application, specifically focusing on the 'dog' command with different parameters. It illustrates how to specify a dog's name, age, and a boolean flag, showcasing common command-line argument patterns.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Help/Root_Examples_Children.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
myapp dog --name Rufus --age 12 --good-boy
```

LANGUAGE: Shell
CODE:
```
myapp dog --name Luna
```

LANGUAGE: Shell
CODE:
```
myapp dog --name Charlie
```

LANGUAGE: Shell
CODE:
```
myapp dog --name Bella
```

LANGUAGE: Shell
CODE:
```
myapp dog --name Daisy
```

----------------------------------------

TITLE: Rendering a TextPath in Spectre.Console (C#)
DESCRIPTION: This snippet demonstrates the basic usage of the `TextPath` class to render a file path to the console. It initializes a `TextPath` instance with a given string and then writes it using `AnsiConsole.Write()`, which automatically handles path shrinking to fit the display area.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/text-path.md#_snippet_0

LANGUAGE: csharp
CODE:
```
var path = new TextPath("C:/This/Path/Is/Too/Long/To/Fit/In/The/Area.txt");

AnsiConsole.Write(path);
```

----------------------------------------

TITLE: Suppressing Spectre1021 Warning in C#
DESCRIPTION: This snippet demonstrates how to suppress the Spectre1021 warning using `#pragma warning disable` and `#pragma warning restore` directives in C#. This is typically used when the violation is intentional or understood and deemed acceptable for a specific code section, allowing the compiler to ignore the warning.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/analyzer/rules/Spectre1021.md#_snippet_2

LANGUAGE: csharp
CODE:
```
#pragma warning disable Spectre1021 // <Rule name>

#pragma warning restore Spectre1021 // <Rule name>
```

----------------------------------------

TITLE: Suppressing Spectre1000 Warning in C#
DESCRIPTION: This snippet demonstrates how to suppress and restore the Spectre1000 warning using C# preprocessor directives. This is useful for temporarily disabling the warning in specific code sections where using `System.Console` might be unavoidable or intentionally desired, without affecting the rest of the project's compliance.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/analyzer/rules/spectre1000.md#_snippet_0

LANGUAGE: csharp
CODE:
```
#pragma warning disable Spectre1000 // Use AnsiConsole instead of System.Console

#pragma warning restore Spectre1000 // Use AnsiConsole instead of System.Console
```

----------------------------------------

TITLE: Removing Spectre.Cli NuGet Package (CLI)
DESCRIPTION: This command removes the `Spectre.Cli` NuGet package reference from the current project, which is the essential first step in migrating to the `Spectre.Console` library.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/migration.md#_snippet_0

LANGUAGE: text
CODE:
```
> dotnet remove package Spectre.Cli
```

----------------------------------------

TITLE: Hiding a Layout Pane in Spectre.Console (C#)
DESCRIPTION: This snippet shows how to make a specific layout pane invisible using the `Invisible` method. Hidden panes do not consume space in the layout and are not rendered, useful for dynamic UI adjustments.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/layout.md#_snippet_4

LANGUAGE: C#
CODE:
```
layout["Left"].Invisible();
```

----------------------------------------

TITLE: Showing a Layout Pane in Spectre.Console (C#)
DESCRIPTION: This snippet demonstrates how to make a previously hidden layout pane visible again using the `Visible` method. Once visible, the pane will be rendered and consume space within the layout according to its size and ratio settings.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/layout.md#_snippet_5

LANGUAGE: C#
CODE:
```
layout["Left"].Visible();
```

----------------------------------------

TITLE: Aligning Rule Title to the Left in C#
DESCRIPTION: Illustrates how to explicitly set the justification of a rule's title to the left using the `Justification` property. This example uses `Justify.Left` to position the title at the beginning of the rule.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/rule.md#_snippet_2

LANGUAGE: csharp
CODE:
```
var rule = new Rule("[red]Hello[/]");
rule.Justification = Justify.Left;
AnsiConsole.Write(rule);
```

LANGUAGE: text
CODE:
```
── Hello ────────────────────────────────────────────────────────────────
```

----------------------------------------

TITLE: Rendering Custom FIGlet Text with Spectre.Console (C#)
DESCRIPTION: This snippet illustrates how to render FIGlet text using a custom font. It first loads a font from a file named 'starwars.flf' using FigletFont.Load and then applies this custom font to the FigletText instance before writing it to the console, also left-justified and colored red.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/figlet.md#_snippet_1

LANGUAGE: csharp
CODE:
```
var font = FigletFont.Load("starwars.flf");

AnsiConsole.Write(
    new FigletText(font, "Hello")
        .LeftJustified()
        .Color(Color.Red));
```

----------------------------------------

TITLE: Displaying Percentages in BreakdownChart in C#
DESCRIPTION: This snippet illustrates how to configure a `BreakdownChart` to display percentage signs next to the values. The `.ShowPercentage()` method enables this feature, providing a clearer representation of each item's proportion within the chart.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/breakdownchart.md#_snippet_2

LANGUAGE: C#
CODE:
```
AnsiConsole.Write(new BreakdownChart()
    .ShowPercentage()
    .AddItem("SCSS", 80, Color.Red)
    .AddItem("HTML", 28.3, Color.Blue)
    .AddItem("C#", 22.6, Color.Green)
    .AddItem("JavaScript", 6, Color.Yellow)
    .AddItem("Ruby", 6, Color.LightGreen)
    .AddItem("Shell", 0.1, Color.Aqua));
```

----------------------------------------

TITLE: Styling TextPath Segments via Properties in Spectre.Console (C#)
DESCRIPTION: This example demonstrates how to apply custom styles to different segments of a `TextPath` (root, separator, stem, leaf) using individual style properties. Each segment's style is set to a new `Style` object with a specific foreground color, allowing granular visual customization.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/text-path.md#_snippet_3

LANGUAGE: csharp
CODE:
```
var path = new TextPath("C:/This/Path/Is/Too/Long/To/Fit/In/The/Area.txt");

path.RootStyle = new Style(foreground: Color.Red);
path.SeparatorStyle = new Style(foreground: Color.Green);
path.StemStyle = new Style(foreground: Color.Blue);
path.LeafStyle = new Style(foreground: Color.Yellow);
```

----------------------------------------

TITLE: Enabling Panel Expansion in C#
DESCRIPTION: This C# snippet shows how to enable the `Expand` property of a `Panel`, causing it to automatically adjust its width to fill the entire console width. By default, the panel's width is content-based, but setting `Expand = true` overrides this behavior, making the panel span the full console width.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/panel.md#_snippet_4

LANGUAGE: C#
CODE:
```
panel.Expand = true;
```

----------------------------------------

TITLE: Rebasing a Feature Branch with Git
DESCRIPTION: This sequence of Git commands demonstrates how to rebase a local feature branch against the latest upstream `main` branch, resolve conflicts, and push the updated branch to the origin. This process is often required when maintainers request an update to the latest code before further review. The `--force` option may be necessary for pushing the rebased branch.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/CONTRIBUTING.md#_snippet_2

LANGUAGE: Git
CODE:
```
git fetch upstream
git checkout main
git rebase upstream/main
git checkout your-branch
git rebase main
git push origin your-branch
git push origin your-branch --force
```

----------------------------------------

TITLE: Rendering Rows from IEnumerable with Spectre.Console (C#)
DESCRIPTION: This example illustrates how to render a collection of `Text` items from an `IEnumerable` (specifically a `List<Text>`) as distinct rows in the console using `Spectre.Console.Rows`. It's useful for dynamically displaying lists of data where each item should occupy its own line.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/rows.md#_snippet_1

LANGUAGE: csharp
CODE:
```
// Create a list of Items
var rows = new List<Text>(){
        new Text("Item 1"),
        new Text("Item 2"),
        new Text("Item 3")
    };

// Render each item in list on separate line
AnsiConsole.Write(new Rows(rows));
```

----------------------------------------

TITLE: Adding an Event to Calendar in C#
DESCRIPTION: This snippet shows how to mark a specific date as an event within the calendar. The `AddCalendarEvent` method is used with the year, month, and day (2020, October, 11) to highlight that date, indicating an associated event.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/calendar.md#_snippet_4

LANGUAGE: csharp
CODE:
```
var calendar = new Calendar(2020,10);
calendar.AddCalendarEvent(2020, 10, 11);
AnsiConsole.Write(calendar);
```

----------------------------------------

TITLE: Styling a Tree with Spectre.Console (C#)
DESCRIPTION: This snippet shows how to apply a custom style to the root node of a Spectre.Console `Tree`, affecting its appearance, such as background and foreground colors.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/tree.md#_snippet_2

LANGUAGE: csharp
CODE:
```
var root = new Tree("Root")
    .Style("white on red");
```

----------------------------------------

TITLE: Suppressing Spectre.Console Analyzer Warnings (C#)
DESCRIPTION: This snippet demonstrates how to suppress specific Spectre.Console analyzer warnings using #pragma directives in C#. It shows how to disable a warning for a block of code and then re-enable it, preventing the analyzer from reporting violations within that region.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/analyzer/rules/RULE_TEMPLATE.md#_snippet_0

LANGUAGE: csharp
CODE:
```
#pragma warning disable Spectre1000 // <Rule name>
#pragma warning restore Spectre1000 // <Rule name>
```

----------------------------------------

TITLE: Handling Unknown Command Errors in Shell
DESCRIPTION: This snippet shows the output when an unknown command, 'bat', is attempted in a shell environment. The system reports an 'Unknown command' error and provides a 'Did you mean' suggestion, indicating a common typo or similar command.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Parsing/UnknownCommand/Test_4.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
Error: Unknown command 'bat'.

       bat 14
       ^^^ Did you mean 'cat'?
```

----------------------------------------

TITLE: Setting TextPath Alignment via Extension Method in Spectre.Console (C#)
DESCRIPTION: This snippet illustrates an alternative, more fluent way to set the alignment of a `TextPath` using an extension method. The `RightJustified()` method is chained directly after the `TextPath` constructor, providing a concise way to configure alignment.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/text-path.md#_snippet_2

LANGUAGE: csharp
CODE:
```
var path = new TextPath("C:/This/Path/Is/Too/Long/To/Fit/In/The/Area.txt")
    .RightJustified();
```

----------------------------------------

TITLE: Disabling Column Text Wrapping in Spectre.Console (C#)
DESCRIPTION: This snippet illustrates how to prevent text from wrapping within a specific column of a Spectre.Console table. Calling the `NoWrap()` method on a column ensures that its content stays on a single line, potentially causing truncation if the content exceeds the column width.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/table.md#_snippet_8

LANGUAGE: csharp
CODE:
```
// Disable column wrapping
table.Columns[0].NoWrap();
```

----------------------------------------

TITLE: Handling Unknown CLI Command 'bat' in Shell
DESCRIPTION: This snippet shows an error message from a command-line interface when an unknown command 'bat' is invoked. The system suggests 'cat' as a possible alternative, indicating a typo or incorrect command usage. This highlights the CLI's error-handling and suggestion capabilities.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Parsing/UnknownCommand/Test_3.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
dog bat 14
```

----------------------------------------

TITLE: Rendering a Basic Calendar in C#
DESCRIPTION: This snippet demonstrates how to create and render a basic calendar to the terminal using Spectre.Console. It initializes a `Calendar` instance with a specific year and month (2020, October) and then writes it to the console using `AnsiConsole.Write`.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/calendar.md#_snippet_0

LANGUAGE: csharp
CODE:
```
var calendar = new Calendar(2020,10);
AnsiConsole.Write(calendar);
```

----------------------------------------

TITLE: Customizing Event Highlight Style in C#
DESCRIPTION: This example demonstrates how to change the visual style of highlighted calendar events. After adding an event, the `HighlightStyle` method is used with `Style.Parse("yellow bold")` to render the event date in yellow and bold text.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/calendar.md#_snippet_5

LANGUAGE: csharp
CODE:
```
var calendar = new Calendar(2020, 10);
calendar.AddCalendarEvent(2020, 10, 11);
calendar.HighlightStyle(Style.Parse("yellow bold"));
AnsiConsole.Write(calendar);
```

----------------------------------------

TITLE: Demonstrating Missing CLI Option Value Error - Console Output
DESCRIPTION: This snippet captures the console output when a command-line option, such as `--foo`, is invoked without its required value. The error message clearly indicates the missing value and points to the exact location using a caret `^`, guiding the user to correct the input.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Parsing/OptionWithoutName/Test_2.Output.verified.txt#_snippet_0

LANGUAGE: Console Output
CODE:
```
Error: Expected an option value.

       dog --foo=
                 ^ Did you forget the option value?
```

----------------------------------------

TITLE: Rendering Default FIGlet Text with Spectre.Console (C#)
DESCRIPTION: This snippet demonstrates how to render 'Hello' as FIGlet text using the default font provided by Spectre.Console. It shows how to left-justify the text and apply a red color using the FigletText class and AnsiConsole.Write.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/figlet.md#_snippet_0

LANGUAGE: csharp
CODE:
```
AnsiConsole.Write(
    new FigletText("Hello")
        .LeftJustified()
        .Color(Color.Red));
```

----------------------------------------

TITLE: Adding Items to BreakdownChart with a Converter in C#
DESCRIPTION: This example demonstrates how to add a collection of custom data items to a `BreakdownChart` using a converter function. It defines a list of tuples and then uses `AddItems` with a lambda expression to map each tuple to a `BreakdownChartItem`, allowing flexible data integration.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/breakdownchart.md#_snippet_5

LANGUAGE: C#
CODE:
```
var items = new List<(string Label, double Value, Color color)>
{
    ("Apple", 12, Color.Green),
    ("Orange", 54, Color.Orange1),
    ("Banana", 33, Color.Yellow),
};

// Render the chart
AnsiConsole.Write(new BreakdownChart()
    .FullSize()
    .ShowPercentage()
    .AddItems(items, (item) => new BreakdownChartItem(
        item.Label, item.Value, item.color)));
```

----------------------------------------

TITLE: Setting Panel Padding in C#
DESCRIPTION: This C# snippet demonstrates how to set the internal padding of a `Panel` using the `Padding` property. A `Padding` instance is created with uniform values (2, 2, 2, 2) for left, top, right, and bottom padding, respectively, controlling the space between content and border.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/panel.md#_snippet_3

LANGUAGE: C#
CODE:
```
panel.Padding = new Padding(2, 2, 2, 2);
```

----------------------------------------

TITLE: Handling Missing Option Value in Shell Command
DESCRIPTION: This snippet illustrates a common command-line error where an option requiring a value is provided without one. The output indicates the exact location of the missing value and suggests a possible cause.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Parsing/OptionWithoutName/Test_4.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
dog -f=
```

----------------------------------------

TITLE: Manipulating and Rendering an Image with ImageSharp in C#
DESCRIPTION: This C# example shows how to load an image, set its max width, apply a bilinear resampler, and then use ImageSharp's processing API to mutate the image (e.g., grayscale, rotate, entropy crop) before rendering it to the console. It leverages the `Spectre.Console.ImageSharp` package for advanced image manipulation.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/canvas-image.md#_snippet_2

LANGUAGE: csharp
CODE:
```
// Load an image
var image = new CanvasImage("cake.png");
image.MaxWidth(32);

// Set a sampler that will be used when scaling the image.
image.BilinearResampler();

// Mutate the image using ImageSharp
image.Mutate(ctx => ctx.Grayscale().Rotate(-45).EntropyCrop());

// Render the image to the console
AnsiConsole.Write(image);
```

----------------------------------------

TITLE: Using Emojis in Spectre.Console Markup and Constants (C#)
DESCRIPTION: This snippet demonstrates two ways to use emojis with Spectre.Console: directly within markup strings using a colon-prefixed name, and by referencing known emoji constants from the `Emoji.Known` class. It shows how to render emojis in terminal output.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/appendix/emojis.md#_snippet_0

LANGUAGE: csharp
CODE:
```
// Markup
AnsiConsole.MarkupLine("Hello :globe_showing_europe_africa:!");

// Constant
var hello = "Hello " + Emoji.Known.GlobeShowingEuropeAfrica;
```

----------------------------------------

TITLE: Collapsing a Tree Node with Spectre.Console (C#)
DESCRIPTION: This snippet illustrates how to collapse a specific node within a Spectre.Console `Tree` structure, preventing its children from being displayed by default.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/tree.md#_snippet_1

LANGUAGE: csharp
CODE:
```
root.AddNode("Label").Collapse();
```

----------------------------------------

TITLE: Checking Staged Whitespace Changes with Git
DESCRIPTION: This Git command is used to identify and report unnecessary whitespace errors in files that have already been staged for commit. It's crucial for reviewing changes before the final commit, ensuring no unwanted whitespace is included.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/CONTRIBUTING.md#_snippet_1

LANGUAGE: Git
CODE:
```
git diff --cached --check
```

----------------------------------------

TITLE: Checking Unstaged Whitespace Changes with Git
DESCRIPTION: This Git command is used to identify and report unnecessary whitespace errors in files that are not yet staged for commit. It helps ensure code cleanliness and and adherence to style guidelines before a commit is finalized.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/CONTRIBUTING.md#_snippet_0

LANGUAGE: Git
CODE:
```
git diff --check
```

----------------------------------------

TITLE: Setting Table Border in Spectre.Console (C#)
DESCRIPTION: This snippet demonstrates how to apply the SimpleHeavy border style to a Table instance in Spectre.Console. It initializes a new Table object and then assigns TableBorder.SimpleHeavy to its Border property, customizing the table's visual appearance.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/appendix/borders.md#_snippet_0

LANGUAGE: csharp
CODE:
```
var table = new Table();
table.Border = TableBorder.SimpleHeavy;
```

----------------------------------------

TITLE: Defining a Hidden Command Option in C#
DESCRIPTION: This C# snippet shows how to define a command option that will not be displayed in the generated help text. By setting `IsHidden = true` on the `CommandOption` attribute, the `--hidden-opt` parameter remains functional but is omitted from user-facing documentation, useful for internal or deprecated options.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/settings.md#_snippet_3

LANGUAGE: C#
CODE:
```
[CommandOption("--hidden-opt", IsHidden = true)]
public bool? HiddenOpt { get; set; }
```

----------------------------------------

TITLE: Calling nested LiveRenderables (Violates Spectre1020) - C#
DESCRIPTION: This C# snippet demonstrates a violation of the Spectre1020 rule by attempting to start an `AnsiConsole.Status()` renderable while an `AnsiConsole.Progress()` renderable is already active. This concurrent execution of LiveRenderables leads to competition for console resources and can corrupt the output, as they both rely on exclusive ANSI sequence control.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/analyzer/rules/spectre1020.md#_snippet_0

LANGUAGE: csharp
CODE:
```
AnsiConsole.Progress().Start(ctx => {
    AnsiConsole.Status().Start("Running status too...", statusCtx => {});
});
```

----------------------------------------

TITLE: Hiding Calendar Header in C#
DESCRIPTION: This snippet illustrates how to hide the header of the rendered calendar. After creating a `Calendar` instance, the `HideHeader` method is invoked before writing the calendar to the console, removing the month and year display.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/calendar.md#_snippet_2

LANGUAGE: csharp
CODE:
```
var calendar = new Calendar(2020,10);
calendar.HideHeader();
AnsiConsole.Write(calendar);
```

----------------------------------------

TITLE: Defining Subcommands for myapp (Shell)
DESCRIPTION: Outlines the available subcommands under `myapp`, specifically `dog` which takes an `AGE` argument, and `horse`.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Help/CommandExamples.Output.verified.txt#_snippet_4

LANGUAGE: Shell
CODE:
```
dog <AGE>    The dog command
```

LANGUAGE: Shell
CODE:
```
horse        The horse command
```

----------------------------------------

TITLE: Replacing Emojis in Text with Spectre.Console (C#)
DESCRIPTION: This code illustrates how to replace emoji shortcodes (e.g., `:birthday_cake:`) within a string with their actual emoji characters using the `Emoji.Replace` method. This is useful for processing user input or dynamic content.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/appendix/emojis.md#_snippet_1

LANGUAGE: csharp
CODE:
```
var phrase = "Mmmm :birthday_cake:";
var rendered = Emoji.Replace(phrase);
```

----------------------------------------

TITLE: Displaying Application Usage Syntax (Shell)
DESCRIPTION: Shows the general syntax for invoking the `myapp` command, including the `animal` subcommand, an optional `LEGS` argument, and general `OPTIONS` and `COMMANDS` placeholders.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Help/CommandExamples.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
myapp animal [LEGS] [OPTIONS] <COMMAND>
```

----------------------------------------

TITLE: Displaying Row Separators in Spectre.Console Tables (C#)
DESCRIPTION: This snippet shows how to enable the display of separator lines between each row in a Spectre.Console table. Calling the `ShowRowSeparators()` method on the table instance will render horizontal lines between rows, improving readability for large datasets.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/table.md#_snippet_10

LANGUAGE: csharp
CODE:
```
// Shows separator between each row
table.ShowRowSeparators();
```

----------------------------------------

TITLE: Handling Missing Option Value in Shell Commands
DESCRIPTION: This snippet illustrates a common command-line parsing error where an option (`--foo`) is expected to have a value, but it is missing. The error message, likely from a command-line parsing library like Spectre.Console, indicates that the option value was forgotten, prompting the user to provide it.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Parsing/OptionWithoutName/Test_3.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
dog --foo:
```

----------------------------------------

TITLE: Handling Missing Option Value in Shell Command
DESCRIPTION: This snippet demonstrates an error message generated when a command-line option, such as '-n', is used without providing an associated value. The error indicates that the option 'name' was expected to have a value, but none was supplied, highlighting the exact position of the missing value.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Parsing/NoValueForOption/Test_2.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
dog -n
```

----------------------------------------

TITLE: Running Documentation Preview Site (PowerShell)
DESCRIPTION: This command executes the PowerShell script responsible for building and launching a local preview of the documentation site using Statiq. It requires the .NET Core SDK and npm to be installed and configured to function correctly.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/README.md#_snippet_0

LANGUAGE: PowerShell
CODE:
```
> Preview.ps1
```

----------------------------------------

TITLE: Setting Fixed Column Width in Spectre.Console (C#)
DESCRIPTION: This snippet demonstrates how to set a fixed width for a specific column in a Spectre.Console table. The `Width()` method, applied to a column, allows specifying the desired width in cells, overriding automatic sizing for that column.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/table.md#_snippet_9

LANGUAGE: csharp
CODE:
```
// Set the column width
table.Columns[0].Width(15);
```

----------------------------------------

TITLE: Example: Specifying a Dog Animal with Attributes - Shell
DESCRIPTION: This example demonstrates how to use the 'animal' command with the 'dog' subcommand, providing specific attributes like name, age, and a boolean flag for 'good-boy'. This shows passing arguments and flags.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Help/RootExamples_Leafs.Output.verified.txt#_snippet_1

LANGUAGE: Shell
CODE:
```
myapp animal dog --name Rufus --age 12 --good-boy
```

----------------------------------------

TITLE: Illustrating CLI Flag Assignment Error - Shell
DESCRIPTION: This snippet demonstrates an incorrect attempt to assign a value to a command-line flag (`-a=indeterminate`). The accompanying error message indicates that flags in this context do not accept direct value assignments, highlighting a common CLI usage constraint. This is an example of an invalid command.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Parsing/CannotAssignValueToFlag/Test_2.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
dog -a=indeterminate foo
```

----------------------------------------

TITLE: Illustrating Flag Assignment Error in Shell
DESCRIPTION: This snippet shows a command-line invocation where a flag (`-a`) is incorrectly followed by a value (`foo`), resulting in an error indicating that the flag cannot be assigned a value. This typically occurs when a flag is a boolean switch or does not expect an argument, and the parser detects an invalid assignment.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Parsing/CannotAssignValueToFlag/Test_2.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
dog -a foo
```

----------------------------------------

TITLE: Defining Global Options for myapp (Shell)
DESCRIPTION: Lists the available global options for the `myapp` command, including `--help` for printing help information and `--alive` to indicate if an animal is alive.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Help/CommandExamples.Output.verified.txt#_snippet_3

LANGUAGE: Shell
CODE:
```
-h, --help     Prints help information
```

LANGUAGE: Shell
CODE:
```
-a, --alive    Indicates whether or not the animal is alive
```

----------------------------------------

TITLE: Available Commands for myapp - Shell
DESCRIPTION: Details the specific commands supported by 'myapp', including 'dog' which requires an age argument, and 'horse'.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Help/Hidden_Commands.Output.verified.txt#_snippet_2

LANGUAGE: Shell
CODE:
```
dog <AGE>
```

LANGUAGE: Shell
CODE:
```
horse
```

----------------------------------------

TITLE: Global Options for myapp - Shell
DESCRIPTION: Lists the standard global options available for 'myapp', such as help and version information.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Help/Hidden_Commands.Output.verified.txt#_snippet_1

LANGUAGE: Shell
CODE:
```
-h, --help
```

LANGUAGE: Shell
CODE:
```
-v, --version
```

----------------------------------------

TITLE: lion Subcommand Usage - Shell
DESCRIPTION: Details the usage of the `lion` subcommand, which is available under `myapp cat`. It specifies the required `TEETH` argument for the `lion` command. This command is nested under the main `cat` command.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Help/Command.Output.verified.txt#_snippet_1

LANGUAGE: Shell
CODE:
```
lion <TEETH>
```

----------------------------------------

TITLE: Hiding Table Headers in Spectre.Console (C#)
DESCRIPTION: This code snippet shows how to hide all column headers in a Spectre.Console table. Calling the `HideHeaders()` method on the table instance will prevent the headers from being rendered.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/table.md#_snippet_3

LANGUAGE: csharp
CODE:
```
// Hides all column headers
table.HideHeaders();
```

----------------------------------------

TITLE: Invalid Short Option Name in Shell Command
DESCRIPTION: This snippet demonstrates an error message from a command-line parsing library (likely Spectre.Console, given the project context) indicating that a short option name contains an invalid character. The `0` in `-f0o` is highlighted as the problematic character, as short options typically consist of a single alphanumeric character following the hyphen.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Parsing/InvalidShortOptionName/Test_1.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
dog -f0o
```

----------------------------------------

TITLE: Setting Explicit Size for a Layout Pane in Spectre.Console (C#)
DESCRIPTION: This snippet demonstrates how to set an exact, fixed size for a layout pane using the `Size` method. This overrides any ratio or minimum size constraints for that pane, ensuring it always occupies the specified character width.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/layout.md#_snippet_3

LANGUAGE: C#
CODE:
```
layout["Left"].Size(32);
```

----------------------------------------

TITLE: Styling TextPath Segments via Extension Methods (Color) in Spectre.Console (C#)
DESCRIPTION: This example demonstrates the most concise way to apply colors to `TextPath` segments using dedicated color-setting extension methods. These methods directly accept `Color` enums, simplifying the styling process for common foreground color requirements.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/text-path.md#_snippet_5

LANGUAGE: csharp
CODE:
```
var path = new TextPath("C:/This/Path/Is/Too/Long/To/Fit/In/The/Area.txt")
    .RootColor(Color.Red)
    .SeparatorColor(Color.Green)
    .StemColor(Color.Blue)
    .LeafColor(Color.Yellow);
```

----------------------------------------

TITLE: Suppressing Spectre1020 Warning - C#
DESCRIPTION: This C# snippet illustrates how to suppress the Spectre1020 warning using `#pragma warning disable` and `#pragma warning restore` directives. These directives can be used to temporarily disable specific compiler warnings for a block of code, allowing developers to bypass the rule if they have a specific reason or are aware of the implications.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/analyzer/rules/spectre1020.md#_snippet_2

LANGUAGE: csharp
CODE:
```
#pragma warning disable Spectre1020 // <Rule name>

#pragma warning restore Spectre1020 // <Rule name>
```

----------------------------------------

TITLE: Rendering Emojis with Spectre.Console Markup (C#)
DESCRIPTION: This example illustrates how to render emojis in Spectre.Console markup using shortcodes (e.g., `:globe_showing_europe_africa:`). Spectre.Console automatically converts these shortcodes into their corresponding emoji characters in supported terminals.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/markup.md#_snippet_8

LANGUAGE: csharp
CODE:
```
AnsiConsole.Markup("Hello :globe_showing_europe_africa:!");
```

----------------------------------------

TITLE: Invalid Long Option Name Example (Shell)
DESCRIPTION: This snippet demonstrates an invalid command-line argument where a long option name (`--1foo`) starts with a digit. Command-line parsing libraries often enforce rules that option names must not begin with a number, leading to an 'Invalid long option name' error.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Parsing/LongOptionNameStartWithDigit/Test_1.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
dog --1foo
```

----------------------------------------

TITLE: Installing Spectre.Console.Testing NuGet Package
DESCRIPTION: This command demonstrates how to add the `Spectre.Console.Testing` NuGet package to a .NET project. This package provides test harnesses for unit testing console applications built with Spectre.Console, enabling developers to write comprehensive tests for their console applications.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/unit-testing.md#_snippet_0

LANGUAGE: text
CODE:
```
> dotnet add package Spectre.Console.Testing
```

----------------------------------------

TITLE: Unknown Command Error in Shell
DESCRIPTION: This snippet shows the output when an unknown command 'cat' is executed in a shell environment. The error message clearly indicates that the command is not found or recognized by the system.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Parsing/UnknownCommand/Test_1.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
cat 14
```

----------------------------------------

TITLE: Setting TextPath Alignment via Property in Spectre.Console (C#)
DESCRIPTION: This example shows how to explicitly set the alignment of a `TextPath` using its `Alignment` property. The path is initialized, its `Alignment` is set to `Justify.Right`, and then it's rendered to the console, appearing right-aligned within the available space.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/text-path.md#_snippet_1

LANGUAGE: csharp
CODE:
```
var path = new TextPath("C:/This/Path/Is/Too/Long/To/Fit/In/The/Area.txt");
path.Alignment = Justify.Right;

AnsiConsole.Write(path);
```

----------------------------------------

TITLE: Handling Missing Option Value in Shell
DESCRIPTION: This snippet demonstrates a common command-line parsing error where an option (--name) is provided without an accompanying value. It highlights how a command-line utility might report such an error, indicating that a value was expected but not found.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Parsing/NoValueForOption/Test_1.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
dog --name
```

----------------------------------------

TITLE: Handling Missing Option Value in CLI - Shell
DESCRIPTION: This snippet demonstrates an error message from a command-line tool when an option (`-f`) that expects a value is invoked without providing one. The error message clearly indicates that an option value was expected and suggests it might have been forgotten.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Parsing/OptionWithoutName/Test_4.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
dog -f=
```

----------------------------------------

TITLE: Handling Missing Option Value in CLI
DESCRIPTION: This snippet shows a command-line error message indicating that the '-f' option requires a value, which was not supplied. The caret '^' points to the missing value, and a helpful suggestion is provided, typical of robust command-line parsing libraries like Spectre.Console.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Parsing/OptionWithoutName/Test_5.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
dog -f:
```

----------------------------------------

TITLE: Displaying General CLI Usage - Shell
DESCRIPTION: Illustrates the general syntax for invoking 'myapp', showing the placement of options and commands.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Help/RootExamples.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
myapp [OPTIONS] <COMMAND>
```

----------------------------------------

TITLE: Adding Spectre.Console NuGet Package (CLI)
DESCRIPTION: This command adds the `Spectre.Console` NuGet package to the current project, providing the updated functionality that replaces the deprecated `Spectre.Cli` library.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/migration.md#_snippet_1

LANGUAGE: text
CODE:
```
> dotnet add package Spectre.Console
```

----------------------------------------

TITLE: Defining Optional Argument [LEGS] (Shell)
DESCRIPTION: Describes the optional `[LEGS]` argument, which specifies the number of legs for the animal. This argument is part of the `animal` subcommand's syntax.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Help/CommandExamples.Output.verified.txt#_snippet_2

LANGUAGE: Shell
CODE:
```
[LEGS]    The number of legs
```

----------------------------------------

TITLE: Removing All Help Text Styling in Spectre.Console.Cli
DESCRIPTION: This snippet shows how to remove all styling from the automatically generated help text in Spectre.Console.Cli. By setting config.Settings.HelpProviderStyles to null, the application will display unstyled help, which can be beneficial for maximum accessibility.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/cli/command-help.md#_snippet_1

LANGUAGE: csharp
CODE:
```
config.Settings.HelpProviderStyles = null;
```

----------------------------------------

TITLE: General CLI Usage for myapp - Shell
DESCRIPTION: Illustrates the basic syntax for invoking the 'myapp' application, showing that it accepts global options and a specific command.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Help/Hidden_Commands.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
myapp [OPTIONS] <COMMAND>
```

----------------------------------------

TITLE: Illustrating Missing Option Value Error (Shell)
DESCRIPTION: This snippet shows a command-line invocation that triggers an error in an application (likely using Spectre.Console) because the '-n' option, which expects a value, is provided without one. The error message clearly indicates the missing value.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Parsing/NoValueForOption/Test_2.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
dog -n
```

----------------------------------------

TITLE: Aligning Rule Title with Extension Method in C#
DESCRIPTION: Demonstrates an alternative, more concise way to left-align a rule's title using the `LeftJustified()` extension method, achieving the same result as setting the `Justification` property directly.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/rule.md#_snippet_3

LANGUAGE: csharp
CODE:
```
var rule = new Rule("[red]Hello[/]");
rule.LeftJustified();
AnsiConsole.Write(rule);
```

LANGUAGE: text
CODE:
```
── Hello ────────────────────────────────────────────────────────────────
```

----------------------------------------

TITLE: Lion Command Usage Syntax - Shell
DESCRIPTION: This snippet presents the full command-line syntax for invoking the 'lion' command. It shows that 'lion' is a subcommand of 'myapp cat', includes optional parameters like '[LEGS]' and '[OPTIONS]', and highlights the mandatory '<TEETH>' argument.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Help/Leaf.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
myapp cat [LEGS] lion <TEETH> [OPTIONS]
```

----------------------------------------

TITLE: Using myapp dog command - Shell
DESCRIPTION: Demonstrates various invocations of the 'myapp dog' command, showing how to specify the dog's name, age, and other attributes. This command is used to manage dog-related entries within the application.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Help/Root_Examples.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
myapp dog --name Rufus --age 12 --good-boy
```

LANGUAGE: Shell
CODE:
```
myapp dog --name Luna
```

LANGUAGE: Shell
CODE:
```
myapp dog --name Charlie
```

LANGUAGE: Shell
CODE:
```
myapp dog --name Bella
```

LANGUAGE: Shell
CODE:
```
myapp dog --name Daisy
```

LANGUAGE: Shell
CODE:
```
myapp dog --name Milo
```

----------------------------------------

TITLE: Using the 'horse' Command in myapp (Shell)
DESCRIPTION: Illustrates how to use the 'horse' command in the 'myapp' CLI. Examples include specifying a horse's name and its 'IsAlive' status, showcasing how to define or interact with horse entities with different attributes.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Help/Root_Examples_Children_Eight.Output.verified.txt#_snippet_1

LANGUAGE: Shell
CODE:
```
myapp horse --name Brutus
```

LANGUAGE: Shell
CODE:
```
myapp horse --name Sugar --IsAlive false
```

----------------------------------------

TITLE: Installing Spectre.Console.ImageSharp NuGet Package
DESCRIPTION: This command adds the `Spectre.Console.ImageSharp` NuGet package to your .NET project, enabling image rendering capabilities in the console application. It's a prerequisite for using `CanvasImage`.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/canvas-image.md#_snippet_0

LANGUAGE: text
CODE:
```
> dotnet add package Spectre.Console.ImageSharp
```

----------------------------------------

TITLE: Defining CLI Usage for Horse Command in Spectre.Console
DESCRIPTION: This snippet defines the command-line usage for the `horse` command. It specifies the executable name `myapp`, the command `horse`, an optional positional argument `[LEGS]` for the number of legs, and a placeholder for `[OPTIONS]` representing various flags.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Help/Root_Command.QuestionMark.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
myapp horse [LEGS] [OPTIONS]
```

----------------------------------------

TITLE: Handling Unknown Command Line Options (Shell)
DESCRIPTION: This snippet shows a shell command `dog --unknown` which attempts to use an undefined option. The output indicates an 'Unknown option' error, highlighting the problematic part of the command.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Parsing/UnknownOption/Test_1.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
dog --unknown
```

----------------------------------------

TITLE: Remapping or Adding Emojis in Spectre.Console (C#)
DESCRIPTION: This snippet demonstrates how to remap an existing emoji shortcode to a different emoji character using `Emoji.Remap`. It then shows that both `AnsiConsole.MarkupLine` and `Emoji.Replace` will respect the newly defined mapping when rendering or processing strings.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/appendix/emojis.md#_snippet_2

LANGUAGE: csharp
CODE:
```
// Remap the emoji
Emoji.Remap("globe_showing_europe_africa", "😄");

// Render markup
AnsiConsole.MarkupLine("Hello :globe_showing_europe_africa:!");

// Replace emojis in string
var phrase = "Hello :globe_showing_europe_africa:!";
var rendered = Emoji.Replace(phrase);
```

----------------------------------------

TITLE: Hiding Tag Values in BreakdownChart in C#
DESCRIPTION: This snippet shows how to hide only the numerical values next to the tags in a `BreakdownChart`, while keeping the labels visible. The `.HideTagValues()` method provides this granular control over the chart's display.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/breakdownchart.md#_snippet_4

LANGUAGE: C#
CODE:
```
AnsiConsole.Write(new BreakdownChart()
    .HideTagValues()
    .AddItem("SCSS", 80, Color.Red)
    .AddItem("HTML", 28.3, Color.Blue)
    .AddItem("C#", 22.6, Color.Green)
    .AddItem("JavaScript", 6, Color.Yellow)
    .AddItem("Ruby", 6, Color.LightGreen)
    .AddItem("Shell", 0.1, Color.Aqua));
```

----------------------------------------

TITLE: Handling Unknown Command-Line Options in Shell
DESCRIPTION: This snippet demonstrates an error message generated when an unknown option, '-u', is passed to the 'dog' command-line utility. It highlights how the system indicates the problematic part of the command.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Parsing/UnknownOption/Test_2.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
dog -u
```

----------------------------------------

TITLE: Hiding Tags in BreakdownChart in C#
DESCRIPTION: This example demonstrates how to hide the tags (labels and values) that typically display alongside the chart segments. The `.HideTag()` method removes these visual elements, useful when a more compact or minimalist chart is desired.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/docs/input/widgets/breakdownchart.md#_snippet_3

LANGUAGE: C#
CODE:
```
AnsiConsole.Write(new BreakdownChart()
    .HideTag()
    .AddItem("SCSS", 80, Color.Red)
    .AddItem("HTML", 28.3, Color.Blue)
    .AddItem("C#", 22.6, Color.Green)
    .AddItem("JavaScript", 6, Color.Yellow)
    .AddItem("Ruby", 6, Color.LightGreen)
    .AddItem("Shell", 0.1, Color.Aqua));
```

----------------------------------------

TITLE: Incorrect Flag Value Assignment in CLI
DESCRIPTION: This snippet demonstrates an incorrect usage pattern where a value is attempted to be assigned to a flag (`--alive`) that does not accept direct value assignment, resulting in an error. The error message 'Can't assign value' indicates the problem.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Parsing/CannotAssignValueToFlag/Test_1.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
dog --alive=indeterminate foo
```

----------------------------------------

TITLE: Handling Invalid Long Options in Shell Commands
DESCRIPTION: This snippet demonstrates an error message generated when an invalid long option name (`--f`) is used with a command (`dog`). The error suggests a correction to a valid short option (`-f`). This highlights how command-line parsers provide helpful feedback for incorrect syntax.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Parsing/LongOptionNameIsOneCharacter/Test_1.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
dog --f
```

----------------------------------------

TITLE: Unknown Option Error in Shell Command
DESCRIPTION: This snippet shows a command-line execution that results in an 'Unknown option' error. It demonstrates how a utility, in this case 'dog', responds when an unrecognized flag ('-u') is passed as an argument.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Parsing/UnknownOption/Test_2.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
dog -u
```

----------------------------------------

TITLE: Triggering Unknown Option Error - Shell
DESCRIPTION: This shell command demonstrates how to trigger an 'Unknown option' error in a command-line interface. The `dog` command is invoked with `--unknown`, which is presumed to be an invalid flag, leading to the displayed error message. This highlights the CLI's validation of input parameters.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Parsing/UnknownOption/Test_1.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
dog --unknown
```

----------------------------------------

TITLE: Demonstrating Missing CLI Option Value - Shell
DESCRIPTION: This snippet shows a command-line invocation where the '--foo' option is provided but its value is omitted. This action typically leads to an 'Expected an option value' error, highlighting how a CLI framework like Spectre.Console might report such an issue to the user.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Parsing/OptionWithoutName/Test_2.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
dog --foo=
```

----------------------------------------

TITLE: Invoking CLI Command with Missing Option Value - Shell
DESCRIPTION: This shell command `dog --foo:` is an example of an invalid invocation for a Spectre.Console-based CLI application. It demonstrates the scenario where an option (`--foo`) is provided without its expected value, leading to a 'Expected an option value' error. This highlights the validation mechanism for command-line arguments.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Parsing/OptionWithoutName/Test_3.Output.verified.txt#_snippet_0

LANGUAGE: shell
CODE:
```
dog --foo:
```

----------------------------------------

TITLE: Command Causing Missing Value Error
DESCRIPTION: This shell command demonstrates an incorrect usage where the '--name' option is provided without a subsequent value. When processed by a command-line parser like Spectre.Console, this input triggers a validation error indicating the missing argument.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Parsing/NoValueForOption/Test_1.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
dog --name
```

----------------------------------------

TITLE: Invalid Short Option Name in CLI Command - Shell
DESCRIPTION: This snippet demonstrates an error where a short command-line option (`-f0o`) contains an invalid character ('0') immediately following the option flag, which is not permitted for short option names. The error message points to the specific invalid character, indicating that short options typically expect a single alphanumeric character or a specific format.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Parsing/InvalidShortOptionName/Test_1.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
dog -f0o
```

----------------------------------------

TITLE: Invalid Long Option Name in CLI (Shell)
DESCRIPTION: This snippet demonstrates an attempt to use a long option name that starts with a digit (`--1foo`), which is considered invalid. The accompanying error message indicates that option names cannot begin with a digit, highlighting a common validation rule in command-line parsing libraries like Spectre.Console.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Parsing/LongOptionNameStartWithDigit/Test_1.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
dog --1foo
```

----------------------------------------

TITLE: Demonstrating Invalid Long Option in Shell
DESCRIPTION: This snippet shows a shell command that triggers an 'Invalid long option name' error due to an invalid character (€) in the option name. It illustrates how an improperly formed command-line argument can lead to parsing failures.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Cli.Tests/Expectations/Parsing/LongOptionNameContainSymbol/Test_1.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
dog --f€oo
```

----------------------------------------

TITLE: Invalid Long Option Error in CLI
DESCRIPTION: This snippet illustrates a common command-line parsing error where an invalid long option `--f` is used. The error message indicates that the option name is incorrect and suggests the correct short option `-f`, highlighting robust argument parsing and user-friendly error feedback.
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Cli/Parsing/LongOptionNameIsOneCharacter/Test_1.Output.verified.txt#_snippet_0

LANGUAGE: Shell
CODE:
```
dog --f
```

----------------------------------------

TITLE: Example Donut JSON Structure
DESCRIPTION: This JSON snippet defines a 'donut' object with properties like ID, type, name, price per unit, and boolean flags. It includes nested objects for 'batters' and an array of 'topping' objects, demonstrating complex data hierarchies and various numeric formats (e.g., scientific notation).
SOURCE: https://github.com/spectreconsole/spectre.console/blob/main/src/Tests/Spectre.Console.Tests/Expectations/Widgets/Json/Render_Json.Output.verified.txt#_snippet_0

LANGUAGE: JSON
CODE:
```
{
   "id": "0001",
   "type": "donut",
   "name": "Cake",
   "ppu": 0.55,
   "foo": true,
   "bar": false,
   "qux": 32,
   "corgi": null,
   "batters": {
      "batter": [
         {
            "id": "1001",
            "type": "Regular",
            "min": 0
         },
         {
            "id": "1002",
            "type": "Chocolate",
            "min": 0.32
         },
         {
            "id": "1003",
            "min": 12.32,
            "type": "Blueberry"
         },
         {
            "id": "1004",
            "min": 0.32E-12,
            "type": "Devil's Food"
         }
      ]
   },
   "topping": [
      {
         "id": "5001",
         "min": 0.32e-12,
         "type": "None"
      },
      {
         "id": "5002",
         "min": 0.32E+12,
         "type": "Glazed"
      },
      {
         "id": "5005",
         "min": 0.32e+12,
         "type": "Sugar"
      },
      {
         "id": "5007",
         "min": 0.32e12,
         "type": "Powdered Sugar"
      },
      {
         "id": "5006",
         "type": "Chocolate with Sprinkles"
      },
      {
         "id": "5003",
         "type": "Chocolate"
      },
      {
         "id": "5004",
         "type": "Maple"
      }
   ]
}
```