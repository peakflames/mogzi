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

### Composite Rendering Design

```csharp
private IRenderable CreateInputWithAutocomplete(InputContext context)
{
    var inputPanel = CreateFlexInputComponent(context.CurrentInput);

    if (!context.ShowSuggestions || context.Suggestions.Count == 0)
        return inputPanel;

    var suggestionItems = context.Suggestions.Select((suggestion, index) => 
    {
        var isSelected = index == context.SelectedSuggestionIndex;
        var style = isSelected ? "[blue on white]" : "[dim]";
        var prefix = isSelected ? ">" : " ";

        var description = context.CompletionItems[index].Description;
        return new Markup($"{style}{prefix} {suggestion,-12} {description}[/]");
    }).ToArray();

    var suggestionsPanel = new Panel(new Rows(suggestionItems))
        .Border(BoxBorder.Rounded)
        .BorderColor(Color.Blue)
        .Padding(0, 0);

    return new Rows(inputPanel, suggestionsPanel);
}
```

**UI Design Features:**
- **Layered Composition**: Input field and suggestions rendered as separate components
- **Visual Hierarchy**: Clear distinction between input and suggestions
- **Selection Highlighting**: Visual feedback for current selection
- **Responsive Layout**: Adapts to different suggestion counts and content

### Dynamic Content Updates

```csharp
private IRenderable RenderDynamicContent()
{
    var bottomComponent = _currentState switch
    {
        ChatState.Input when _inputContext.ShowSuggestions => 
            CreateInputWithAutocomplete(_inputContext),
        ChatState.Input => 
            CreateFlexInputComponent(_inputContext.CurrentInput),
        ChatState.Thinking => 
            CreateFlexThinkingComponent(),
        ChatState.ToolExecution => 
            CreateFlexToolExecutionComponent(_toolProgress),
        _ => CreateFlexInputComponent(_inputContext.CurrentInput)
    };

    return new Rows(new Text(""), bottomComponent, new Text(""), CreateFlexFooterComponent());
}
```

**Dynamic Rendering Features:**
- **State-Aware Rendering**: Different UI based on application state
- **Conditional Autocomplete**: Shows suggestions only when appropriate
- **Smooth Transitions**: Seamless switching between input modes
- **Performance Optimization**: Minimal redraws for better responsiveness

## Keyboard Event Integration

### Event-Driven Architecture

```csharp
private async void OnKeyPressed(object? sender, KeyPressEventArgs e)
{
    // Handle autocomplete navigation first
    if (_inputContext.State == InputState.Autocomplete && _inputContext.ShowSuggestions)
    {
        switch (e.Key)
        {
            case ConsoleKey.UpArrow:
                NavigateAutocomplete(up: true);
                e.Handled = true;
                return;

            case ConsoleKey.DownArrow:
                NavigateAutocomplete(up: false);
                e.Handled = true;
                return;

            case ConsoleKey.Tab:
            case ConsoleKey.Enter:
                AcceptAutocompleteSuggestion();
                e.Handled = true;
                return;

            case ConsoleKey.Escape:
                CancelAutocomplete();
                e.Handled = true;
                return;
        }
    }

    // Handle normal input...
}
```

**Event Handling Features:**
- **Priority Processing**: Autocomplete keys handled before normal input
- **Event Marking**: `e.Handled = true` prevents further processing
- **State-Aware Logic**: Different behavior based on input state
- **Clean Separation**: Autocomplete logic isolated from normal input handling

### Navigation Logic

```csharp
private void NavigateAutocomplete(bool up)
{
    if (!_inputContext.ShowSuggestions || _inputContext.Suggestions.Count == 0)
        return;

    if (up)
    {
        _inputContext.SelectedSuggestionIndex = 
            (_inputContext.SelectedSuggestionIndex - 1 + _inputContext.Suggestions.Count) 
            % _inputContext.Suggestions.Count;
    }
    else
    {
        _inputContext.SelectedSuggestionIndex = 
            (_inputContext.SelectedSuggestionIndex + 1) 
            % _inputContext.Suggestions.Count;
    }
}
```

**Navigation Features:**
- **Circular Navigation**: Wraps around at beginning/end of suggestions
- **Bounds Checking**: Safe navigation even with empty suggestion lists
- **Immediate Feedback**: Selection changes reflected instantly in UI

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
