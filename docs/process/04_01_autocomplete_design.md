# Mogzi Autocomplete System Design

## Overview

The Mogzi autocomplete system provides intelligent, context-aware completion for file paths and slash commands within the terminal interface. This document details the design decisions, implementation patterns, and user experience considerations that make the autocomplete system both powerful and intuitive.

## Design Philosophy

**Principles:**
- **Non-Intrusive**: Autocomplete enhances the user experience without disrupting the natural flow of typing
- **Context-Aware**: Different completion types activate based on trigger characters and input context
- **Performance-First**: All operations are async and non-blocking to maintain terminal responsiveness
- **Security-Conscious**: File path completion respects working directory boundaries
- **Extensible**: New completion types can be added through the provider pattern

## User Experience Design

### Trigger-Based Activation

**File Path Completion (@):**
```
User types: "Please read @src/Max"
Autocomplete shows:
> Mogzi.TUI/           Directory
  Mogzi.PawPrints/     Directory  
  Mogzi/               Directory
```

**Slash Command Completion (/):**
```
User types: "/he"
Autocomplete shows:
> help                  Show available commands
  history               Show command history
```

### Visual Design

**Input Field with Autocomplete Overlay:**
```
┌─────────────────────────────────────────────┐
│ > Please read @src/Max▋                     │
└─────────────────────────────────────────────┘
┌─────────────────────────────────────────────┐
│ > Mogzi.TUI/           Directory           │
│   Mogzi.PawPrints/     Directory           │
│   Mogzi/               Directory           │
└─────────────────────────────────────────────┘
```

**Selection Highlighting:**
- Selected item: Blue background with white text `[blue on white]`
- Unselected items: Dimmed text `[dim]`
- Selection prefix: `>` for selected, ` ` (space) for unselected

### Keyboard Navigation

**Navigation Keys:**
- `Up Arrow`: Move selection up
- `Down Arrow`: Move selection down
- `Tab` or `Enter`: Accept selected suggestion
- `Escape`: Cancel autocomplete and return to normal input

**Priority Handling:**
When autocomplete is active, navigation keys take precedence over normal input handling, ensuring smooth user interaction.

## Provider Pattern Design

### IAutocompleteProvider Interface

```csharp
public interface IAutocompleteProvider
{
    AutocompleteType Type { get; }
    char TriggerCharacter { get; }
    bool ShouldTrigger(string input, int cursorPosition);
    string ExtractPartial(string input, int cursorPosition);
    Task<List<CompletionItem>> GetSuggestionsAsync(string partialInput);
    (string newInput, int newCursorPos) ReplacePartial(string input, int cursorPos, string completion);
}
```

**Design Rationale:**
- **Type Safety**: `AutocompleteType` enum ensures type-safe provider identification
- **Trigger Logic**: Each provider defines its own activation criteria
- **Async Operations**: `GetSuggestionsAsync` prevents UI blocking during file system operations
- **Cursor Management**: `ReplacePartial` handles complex text replacement with cursor positioning
- **Context Extraction**: `ExtractPartial` intelligently parses input to extract relevant completion context

### AutocompleteManager Coordination

```csharp
public class AutocompleteManager
{
    private readonly List<IAutocompleteProvider> _providers;
    private readonly ILogger<AutocompleteManager> _logger;

    public IAutocompleteProvider? DetectTrigger(string input, int cursorPosition)
    {
        foreach (var provider in _providers)
        {
            if (provider.ShouldTrigger(input, cursorPosition))
                return provider;
        }
        return null;
    }

    public async Task UpdateSuggestionsAsync(InputContext context)
    {
        if (context.ActiveProvider is null)
        {
            context.ClearAutocomplete();
            return;
        }

        var partialInput = context.ActiveProvider.ExtractPartial(context.CurrentInput, context.CursorPosition);
        var suggestions = await context.ActiveProvider.GetSuggestionsAsync(partialInput);

        if (suggestions.Count > 0)
        {
            context.State = InputState.Autocomplete;
            context.ActiveAutocompleteType = context.ActiveProvider.Type;
            context.Suggestions = suggestions.Select(s => s.Text).ToList();
            context.CompletionItems = suggestions;
            context.ShowSuggestions = true;
            context.SelectedSuggestionIndex = 0;
        }
        else
        {
            context.ClearAutocomplete();
        }
    }

    public void AcceptSuggestion(InputContext context)
    {
        if (context.ActiveProvider is null || !context.ShowSuggestions || context.Suggestions.Count == 0)
            return;

        var selectedCompletion = context.Suggestions[context.SelectedSuggestionIndex];
        var (newInput, newCursorPos) = context.ActiveProvider.ReplacePartial(
            context.CurrentInput,
            context.CursorPosition,
            selectedCompletion);

        context.CurrentInput = newInput;
        context.CursorPosition = newCursorPos;
        context.ClearAutocomplete();
    }
}
```

**Design Features:**
- **Provider Discovery**: Iterates through registered providers to find matches
- **State Management**: Updates `InputContext` with autocomplete state
- **Error Isolation**: Exceptions in one provider don't affect others
- **Memory Management**: Clears autocomplete state when not needed

## FilePathProvider Design

### Security-First Architecture

```csharp
private bool IsWithinWorkingDirectory(string path, string workingDir)
{
    try
    {
        var fullPath = Path.GetFullPath(path);
        var fullWorkingDir = Path.GetFullPath(workingDir);
        return fullPath.StartsWith(fullWorkingDir, StringComparison.OrdinalIgnoreCase);
    }
    catch
    {
        return false;
    }
}
```

**Security Considerations:**
- **Path Validation**: All file operations validated within working directory boundaries
- **Directory Traversal Prevention**: Prevents `../` attacks through path normalization
- **Exception Safety**: Security checks never throw exceptions that could reveal information

### Performance Optimization

```csharp
public async Task<List<CompletionItem>> GetSuggestionsAsync(string partialInput)
{
    // Limit results to prevent performance degradation
    var directories = Directory.GetDirectories(searchDirectory)
        .Where(dir => string.IsNullOrEmpty(searchPattern) || 
                     Path.GetFileName(dir).StartsWith(searchPattern, StringComparison.OrdinalIgnoreCase))
        .Take(20);

    var files = Directory.GetFiles(searchDirectory)
        .Where(file => string.IsNullOrEmpty(searchPattern) || 
                      Path.GetFileName(file).StartsWith(searchPattern, StringComparison.OrdinalIgnoreCase))
        .Take(20);

    // Sort by type (directories first) then by name
    return completionItems.OrderBy(c => c.Type).ThenBy(c => c.Text).ToList();
}
```

**Performance Features:**
- **Result Limiting**: Maximum 20 items per category to prevent UI overload
- **Async Operations**: Non-blocking file system operations
- **Efficient Filtering**: Case-insensitive prefix matching
- **Smart Sorting**: Directories listed before files for better UX

### Path Parsing Logic

```csharp
public string ExtractPartial(string input, int cursorPosition)
{
    // Find the @ character
    var atIndex = -1;
    for (var i = cursorPosition - 1; i >= 0; i--)
    {
        var ch = input[i];
        if (ch == '@')
        {
            atIndex = i;
            break;
        }
        else if (char.IsWhiteSpace(ch))
        {
            break;
        }
    }

    if (atIndex == -1)
        return string.Empty;

    // Extract the path part after @
    var pathStart = atIndex + 1;
    if (pathStart >= input.Length)
        return string.Empty;

    var pathEnd = cursorPosition;
    return input[pathStart..pathEnd];
}
```

**Parsing Features:**
- **Backward Search**: Finds trigger character by searching backwards from cursor
- **Word Boundary Detection**: Stops at whitespace to find word boundaries
- **Partial Path Extraction**: Extracts only the relevant path portion for completion

### Cross-Platform Path Handling

```csharp
private string GetRelativePath(string fullPath, string workingDir)
{
    try
    {
        var relativePath = Path.GetRelativePath(workingDir, fullPath);
        return relativePath.Replace('\\', '/'); // Normalize to forward slashes
    }
    catch
    {
        return Path.GetFileName(fullPath);
    }
}
```

**Cross-Platform Features:**
- **Path Normalization**: Converts backslashes to forward slashes for consistency
- **Relative Path Calculation**: Uses `Path.GetRelativePath` for accurate relative paths
- **Fallback Handling**: Graceful degradation when path calculation fails

## SlashCommandProvider Design

### Command Registry Integration

```csharp
public class SlashCommandProvider : IAutocompleteProvider
{
    private readonly SlashCommandProcessor _slashCommandProcessor;

    public async Task<List<CompletionItem>> GetSuggestionsAsync(string partialInput)
    {
        var suggestions = _slashCommandProcessor.GetCommandSuggestions(partialInput);
        var allCommands = _slashCommandProcessor.GetAllCommands();

        var completionItems = suggestions.Select(command =>
        {
            var description = allCommands.GetValueOrDefault(command, "");
            return new CompletionItem(command, description, CompletionItemType.Command);
        }).ToList();

        return await Task.FromResult(completionItems);
    }
}
```

**Integration Features:**
- **Existing Infrastructure**: Leverages `SlashCommandProcessor` for command enumeration
- **Metadata Support**: Includes command descriptions from the processor
- **Async Compatibility**: Wraps synchronous operations in async interface

### Input Parsing with InputUtils

```csharp
public bool ShouldTrigger(string input, int cursorPosition)
{
    return InputUtils.ShouldShowSlashAutocomplete(input, cursorPosition);
}

public string ExtractPartial(string input, int cursorPosition)
{
    return InputUtils.ExtractSlashCommand(input, cursorPosition);
}

public (string newInput, int newCursorPos) ReplacePartial(string input, int cursorPos, string completion)
{
    return InputUtils.ReplaceSlashCommand(input, cursorPos, completion);
}
```

**Utility Integration:**
- **Centralized Logic**: Uses `InputUtils` for consistent text processing
- **Word Boundary Detection**: Proper handling of slash commands at word boundaries
- **Smart Replacement**: Automatic space insertion after command completion

## State Management Design

### InputContext Integration

```csharp
public class InputContext
{
    // Core input state
    public string CurrentInput { get; set; } = string.Empty;
    public int CursorPosition { get; set; } = 0;
    public InputState State { get; set; } = InputState.Normal;

    // Autocomplete state
    public AutocompleteType ActiveAutocompleteType { get; set; } = AutocompleteType.None;
    public IAutocompleteProvider? ActiveProvider { get; set; }
    public List<string> Suggestions { get; set; } = [];
    public List<CompletionItem> CompletionItems { get; set; } = [];
    public int SelectedSuggestionIndex { get; set; } = 0;
    public bool ShowSuggestions { get; set; } = false;

    public void ClearAutocomplete()
    {
        State = InputState.Normal;
        ActiveAutocompleteType = AutocompleteType.None;
        ActiveProvider = null;
        ShowSuggestions = false;
        Suggestions.Clear();
        CompletionItems.Clear();
        SelectedSuggestionIndex = 0;
    }
}
```

**State Design Features:**
- **Unified State**: Single object manages both input and autocomplete state
- **Clean Separation**: Clear distinction between normal and autocomplete modes
- **Memory Efficiency**: State cleared when not needed to prevent memory leaks
- **Type Safety**: Strongly typed properties for all state components

### State Transitions

```
Normal Input → Trigger Detected → Autocomplete Active → Selection Made → Normal Input
     ↑                                      ↓
     ←─────────── Escape Pressed ←──────────┘
```

**Transition Logic:**
- **Trigger Detection**: Automatic transition when trigger character typed
- **Suggestion Updates**: Real-time updates as user continues typing
- **Selection Acceptance**: Transition back to normal after selection
- **Cancellation**: Escape key returns to normal input mode

## Terminal UI Integration

In the refactored component-based architecture, autocomplete is no longer rendered via a monolithic method. Instead, it is handled by two distinct components: `InputPanel` and `AutocompletePanel`.

### Component-Based Rendering

-   **`InputPanel`**: This component is responsible for rendering the user's input text, the prompt symbol (`>`), and the cursor.
-   **`AutocompletePanel`**: This component is responsible for rendering the list of suggestions when autocomplete is active.

The `TuiComponentManager` controls the visibility of these components. The `AutocompletePanel` is only visible when `InputContext.HasSuggestions` is `true`.

### Rendering Logic in `AutocompletePanel`

The `AutocompletePanel`'s `Render` method is responsible for creating the suggestion list UI.

```csharp
// In AutocompletePanel.cs
public override IRenderable Render(IRenderContext context)
{
    if (!IsVisible || !context.InputContext.HasSuggestions)
    {
        return new Text(string.Empty);
    }

    var suggestionItems = context.InputContext.CompletionItems
        .Select((item, index) => 
        {
            var isSelected = index == context.InputContext.SelectedSuggestionIndex;
            var style = isSelected ? "yellow bold" : "dim";
            var prefix = isSelected ? "► " : "  ";
            return new Markup($"[{style}]{prefix}{item.DisplayText,-20} {item.Description}[/]");
        }).ToArray();

    return new Panel(new Rows(suggestionItems))
    {
        Header = new PanelHeader("[blue]Suggestions[/]"),
        Border = BoxBorder.Rounded,
        BorderStyle = Style.Parse("blue")
    };
}
```

**UI Design Features:**
-   **Component Isolation**: Rendering logic is encapsulated within the `AutocompletePanel`.
-   **State-Driven Visibility**: The panel is only rendered when there are suggestions to display.
-   **Rich Formatting**: Uses Spectre.Console's `Panel`, `Rows`, and `Markup` for a rich visual display.
-   **Selection Highlighting**: The currently selected item is highlighted to provide clear visual feedback.

## Keyboard Event Integration

Keyboard input is managed through a coordinated flow involving the `FlexColumnMediator` and the active UI components.

### Mediator-Based Input Distribution

1.  The main application loop captures keyboard events.
2.  Events are passed to the `FlexColumnMediator`.
3.  The mediator determines the active state and forwards the event to the relevant components by calling their `HandleInputAsync` method.

When autocomplete is active, both `InputPanel` and `AutocompletePanel` may handle keyboard events.

### Component Input Handling

The logic for handling navigation and selection is now located within the `HandleInputAsync` methods of the components.

```csharp
// In AutocompletePanel.cs
public override async Task<bool> HandleInputAsync(IRenderContext context, object inputEvent)
{
    if (!IsVisible || inputEvent is not KeyPressEventArgs keyEvent)
    {
        return false;
    }

    switch (keyEvent.Key)
    {
        case ConsoleKey.UpArrow:
            context.AutocompleteManager.NavigateSuggestions(context.InputContext, up: true);
            return true; // Event handled

        case ConsoleKey.DownArrow:
            context.AutocompleteManager.NavigateSuggestions(context.InputContext, up: false);
            return true; // Event handled

        case ConsoleKey.Tab:
        case ConsoleKey.Enter:
            context.AutocompleteManager.AcceptSuggestion(context.InputContext);
            return true; // Event handled

        case ConsoleKey.Escape:
            context.InputContext.ClearAutocomplete();
            return true; // Event handled
    }

    return false; // Event not handled
}
```

**Event Handling Features:**
-   **Decoupled Logic**: Input handling logic is encapsulated within the component that uses it.
-   **Mediator Coordination**: The mediator ensures that events are routed correctly without direct component-to-component coupling.
-   **Clear Responsibility**: The `AutocompletePanel` is responsible for navigation, selection, and cancellation, while the `InputPanel` handles text entry.
-   **Event Propagation Control**: A component returns `true` to signify it has handled the event, stopping further propagation.

### Navigation Logic in AutocompleteManager

The logic for navigating suggestions is centralized in the `AutocompleteManager` to be reusable.

```csharp
// In AutocompleteManager.cs
public void NavigateSuggestions(InputContext context, bool up)
{
    if (!context.HasSuggestions)
        return;

    int count = context.Suggestions.Count;
    if (up)
    {
        context.SelectedSuggestionIndex = (context.SelectedSuggestionIndex - 1 + count) % count;
    }
    else
    {
        context.SelectedSuggestionIndex = (context.SelectedSuggestionIndex + 1) % count;
    }
}
```

**Navigation Features:**
-   **Circular Navigation**: Wraps around at the beginning/end of the suggestion list.
-   **Stateful Update**: Directly modifies the `InputContext` to reflect the new selection.
-   **Centralized Logic**: Keeps the navigation algorithm in one place.

## Error Handling and Resilience

### Provider Error Isolation

```csharp
public async Task UpdateSuggestionsAsync(InputContext context)
{
    try
    {
        if (context.ActiveProvider is null)
        {
            context.ClearAutocomplete();
            return;
        }

        var partialInput = context.ActiveProvider.ExtractPartial(context.CurrentInput, context.CursorPosition);
        var suggestions = await context.ActiveProvider.GetSuggestionsAsync(partialInput);

        // Update context with suggestions...
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error updating autocomplete suggestions");
        context.ClearAutocomplete();
    }
}
```

**Error Handling Features:**
- **Exception Isolation**: Provider errors don't crash the application
- **Graceful Degradation**: Failed operations clear autocomplete state
- **Comprehensive Logging**: Debug information without UI disruption
- **State Recovery**: Application continues normal operation after errors

### File System Error Handling

```csharp
public async Task<List<CompletionItem>> GetSuggestionsAsync(string partialInput)
{
    try
    {
        // File system operations...
    }
    catch (UnauthorizedAccessException)
    {
        _logger.LogDebug("Access denied to directory: {Directory}", searchDirectory);
        return [];
    }
    catch (DirectoryNotFoundException)
    {
        _logger.LogDebug("Directory not found: {Directory}", searchDirectory);
        return [];
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting file path suggestions for partial: {Partial}", partialInput);
        return [];
    }
}
```

**File System Resilience:**
- **Specific Exception Handling**: Different responses for different error types
- **Silent Failures**: File system errors don't disrupt user experience
- **Debug Logging**: Detailed information for troubleshooting
- **Empty Results**: Safe fallback for all error conditions

## Performance Characteristics

### Async Operations

**Non-Blocking Design:**
- All file system operations use `async/await`
- UI remains responsive during directory scanning
- Cancellation support for long-running operations

**Memory Efficiency:**
- Result limiting prevents excessive memory usage
- State cleared when autocomplete not active
- Efficient string operations for text processing

### Optimization Strategies

**File System Performance:**
```csharp
// Limit results to prevent UI overload
.Take(20)

// Efficient filtering with early termination
.Where(dir => string.IsNullOrEmpty(searchPattern) || 
             Path.GetFileName(dir).StartsWith(searchPattern, StringComparison.OrdinalIgnoreCase))
```

**UI Performance:**
- Minimal redraws through conditional rendering
- Efficient Spectre.Console component composition
- Smart state management to prevent unnecessary updates

## Extensibility Design

### Adding New Providers

**Implementation Steps:**
1. Implement `IAutocompleteProvider` interface
2. Register with dependency injection container
3. Define trigger character and completion logic
4. Add to `AutocompleteType` enum

**Example Custom Provider:**
```csharp
public class VariableProvider : IAutocompleteProvider
{
    public AutocompleteType Type => AutocompleteType.Variable;
    public char TriggerCharacter => '$';

    public bool ShouldTrigger(string input, int cursorPosition)
    {
        // Custom trigger logic
    }

    public async Task<List<CompletionItem>> GetSuggestionsAsync(string partialInput)
    {
        // Custom completion logic
    }

    // Other interface methods...
}
```

This design document provides a comprehensive overview of the Mogzi autocomplete system, covering all aspects from user experience to technical implementation details. The design emphasizes performance, security, and extensibility while maintaining a clean and intuitive user interface.
