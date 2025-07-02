# MaxBot CLI Architecture

## Solution Structure

**Project Organization:**
- **MaxBot** - Core domain, services, and business logic library
- **MaxBot.TUI** - Text User Interface application (main executable)
- **MaxBot.PawPrints** - Terminal interface abstraction library
- **MaxBot.Tests** - Unit and integration tests

**Technology Stack:**
- .NET 9 with C# for cross-platform development and AOT compilation
- Spectre.Console for rich terminal UI rendering
- Microsoft.Extensions.AI for AI model integration and function calling
- FluentResults for functional error handling
- OpenAI SDK for AI service integration
- SharpToken for token counting and metrics
- Microsoft.Extensions.DependencyInjection for service registration

## Core Architecture

**Component Hierarchy:**
```mermaid
graph TD
    A[Program.cs] --> B[ServiceCollection]
    B --> C[FlexColumnTuiApp]
    C --> D[IScrollbackTerminal]
    C --> E[AdvancedKeyboardHandler]
    C --> F[SlashCommandProcessor]
    C --> G[HistoryManager]
    C --> H[StateManager]
    C --> AC[AutocompleteManager]
    
    AC --> FP[FilePathProvider]
    AC --> SCP[SlashCommandProvider]
    FP --> IAP[IAutocompleteProvider]
    SCP --> IAP
    
    C --> IC[InputContext]
    IC --> IS[InputState]
    IC --> CT[CompletionItem]
    
    I[IAppService] --> J[ChatClient]
    J --> K[SystemTools]
    J --> L[DiffPatchTools]
    J --> M[ReadFileTool]
    J --> N[WriteFileTool]
    J --> O[EditTool]
    J --> P[LSTool]
    J --> Q[GrepTool]
    J --> R[ShellTool]
    
    C --> I
    I --> S[ChatHistoryService]
    J --> T[Microsoft.Extensions.AI.IChatClient]
```

**Data Flow Architecture:**
```mermaid
sequenceDiagram
    participant User
    participant FlexColumnTuiApp
    participant IAppService
    participant ChatClient
    participant IChatClient
    participant Tools

    User->>FlexColumnTuiApp: Types message
    FlexColumnTuiApp->>IAppService: ProcessChatMessageAsync
    IAppService->>ChatClient: GetStreamingResponseAsync
    ChatClient->>IChatClient: GetStreamingResponseAsync
    IChatClient->>External: OpenAI API Request
    
    loop Streaming Response
        External-->>IChatClient: Content chunk
        IChatClient-->>ChatClient: ChatResponseUpdate
        ChatClient-->>IAppService: IAsyncEnumerable<ChatResponseUpdate>
        IAppService-->>FlexColumnTuiApp: Update UI
        
        alt Tool Call Required
            IChatClient->>Tools: Execute AIFunction
            Tools-->>IChatClient: Tool result
            IChatClient->>External: Continue with result
        end
    end
    
    IChatClient-->>FlexColumnTuiApp: Final response
```

## Service Layer Architecture

**Dependency Injection Pattern:**
```csharp
// Service registration in ServiceConfiguration.cs
public static void ConfigureServices(IServiceCollection services)
{
    // Add logging - file logging only to keep UI clean
    services.AddLogging(builder =>
    {
        builder.AddProvider(new FileLoggerProvider(LogLevel.Warning));
        builder.SetMinimumLevel(LogLevel.Warning);
    });

    // Add Spectre.Console
    services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);

    // Add core services
    services.AddSingleton<IWorkingDirectoryProvider, DefaultWorkingDirectoryProvider>();
    
    // Create ChatClient with error handling
    var chatClientResult = ChatClient.Create(
        "maxbot.config.json",
        null, // Use default profile
        null,
        "chat",
        (details, color) => {},
        false
    );

    if (chatClientResult.IsSuccess)
    {
        services.AddSingleton(chatClientResult.Value);
    }
    else
    {
        throw new InvalidOperationException($"Failed to create ChatClient: {string.Join(", ", chatClientResult.Errors.Select(e => e.Message))}");
    }

    services.AddSingleton<IAppService, AppService>();
    services.AddSingleton<HistoryManager>();
    services.AddSingleton<StateManager>();

    // Add autocomplete services
    services.AddSingleton<AutocompleteManager>();
    services.AddSingleton<IAutocompleteProvider, FilePathProvider>();
    services.AddSingleton<IAutocompleteProvider, SlashCommandProvider>();

    // Add TUI infrastructure components
    services.AddSingleton<FlexColumnTuiApp>();
    services.AddSingleton<IScrollbackTerminal, ScrollbackTerminal>();
}
```

**Service Lifecycle Management:**
- **Singleton Services**: All core services use singleton lifetime for shared state
- **Factory Pattern**: ChatClient.Create() factory method with comprehensive error handling
- **Interface Segregation**: Clean abstractions (IAppService, IScrollbackTerminal, IWorkingDirectoryProvider)
- **Service Validation**: ChatClient creation validates configuration before service registration
- **Error Propagation**: Service registration failures throw InvalidOperationException with detailed error messages

**Service Dependencies:**
```csharp
// IAppService implementation with constructor injection
public class AppService : IAppService
{
    private readonly ChatClient _chatClient;
    private readonly ChatHistoryService _chatHistoryService;

    public AppService(ChatClient chatClient)
    {
        _chatClient = chatClient;
        _chatHistoryService = new ChatHistoryService(); // Direct instantiation
    }
}
```

**Error Handling Architecture:**
- **FluentResults Pattern**: Used throughout for functional error handling without exceptions
- **Result<T> Returns**: ChatClient.Create() returns Result<ChatClient> for safe initialization
- **Exception Boundaries**: Try-catch blocks in tool implementations with structured error responses
- **Service Validation**: Configuration validation during service registration prevents runtime failures
- **Graceful Degradation**: Services handle missing dependencies and configuration errors appropriately

## Domain Layer Architecture

**Configuration Management:**
```csharp
// JSON serialization with source generation for AOT compatibility
[JsonSerializable(typeof(MaxbotConfigurationRoot))]
public partial class MaxbotConfigurationContext : JsonSerializerContext

// Hierarchical configuration structure
MaxbotConfigurationRoot -> MaxbotConfiguration -> ApiProvider[], Profile[]
```

**Domain Entities:**
- **MaxbotConfiguration**: Central configuration with API providers, profiles, and tool approvals
- **ApiProvider**: External service configuration (OpenAI, custom endpoints)
- **Profile**: User-specific model and provider combinations
- **ChatHistory**: Message persistence and session management
- **ApiMetrics**: Token counting and usage tracking

**Working Directory Security:**
- **IWorkingDirectoryProvider**: Abstraction for secure path operations
- **DefaultWorkingDirectoryProvider**: Implementation with security boundaries
- **Path Validation**: All file operations validated against working directory

## Tool Architecture

**Tool Registration Pattern:**
```csharp
// Individual tool classes with AIFunction factory pattern
public class ReadTextFileTool
{
    private readonly MaxbotConfiguration _config;
    private readonly Action<string, ConsoleColor>? _llmResponseDetailsCallback;
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider;

    public AIFunction GetTool()
    {
        return AIFunctionFactory.Create(
            ReadTextFile,
            new AIFunctionFactoryOptions
            {
                Name = "read_text_file",
                Description = "Reads and returns the content of a text file from the local filesystem..."
            });
    }
}

// Centralized tool registration in ChatClient constructor
var allTools = new List<AITool>();
allTools.AddRange(SystemTools.GetTools().Cast<AITool>());
allTools.AddRange(DiffPatchTools.GetTools().Cast<AITool>());
allTools.Add(ReadTextFileTool.GetTool());
allTools.Add(ReadImageFileTool.GetTool());
allTools.Add(WriteFileTool.GetTool());
allTools.Add(EditTool.GetTool());
allTools.Add(LSTool.GetTool());
allTools.Add(GrepTool.GetTool());
allTools.Add(ShellTool.GetTool());

ChatOptions = new ChatOptions { Tools = allTools };
```

**Tool Security Model:**
```csharp
// Working directory validation in every file tool
private bool IsPathInWorkingDirectory(string absolutePath, string workingDirectory)
{
    try
    {
        var normalizedAbsolutePath = Path.GetFullPath(absolutePath);
        var normalizedWorkingDirectory = Path.GetFullPath(workingDirectory);

        // Platform-specific case sensitivity handling
        return normalizedAbsolutePath.StartsWith(normalizedWorkingDirectory, 
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 
                StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }
    catch { return false; }
}

// Tool approval checking
if (requiresApproval && _config.ToolApprovals.Equals("readonly", StringComparison.OrdinalIgnoreCase))
{
    return "Execution of this command requires approval. Please run with --tool-approvals all...";
}
```

**Tool Implementation Pattern:**
- **Constructor Injection**: MaxbotConfiguration, callback delegates, and IWorkingDirectoryProvider
- **Parameter Validation**: ValidateParameters() method with comprehensive input checking
- **Security Boundaries**: IsPathInWorkingDirectory() and HasReadPermission() validation
- **Structured Responses**: XML-formatted responses with status, error handling, and metadata
- **Error Handling**: Try-catch blocks with specific exception types and debug information
- **Callback Integration**: Optional llmResponseDetailsCallback for UI feedback

**Tool Response Format:**
```xml
<tool_response tool_name="read_text_file">
    <notes>Successfully read text file path/to/file.txt
Total lines: 150
Content size: 4567 characters</notes>
    <result status="SUCCESS" absolute_path="/full/path/to/file.txt" sha256_checksum="abc123..." />
    <content_on_disk>actual file content here</content_on_disk>
</tool_response>

<!-- Error response format -->
<tool_response tool_name="read_text_file">
    <result status="FAILED" />
    <error>File not found: /path/to/missing/file.txt</error>
</tool_response>
```

**Available Tools:**
- **SystemTools**: execute_command (with platform detection), attempt_completion (task completion)
- **DiffPatchTools**: generate_code_patch, apply_code_patch, preview_patch_application
- **ReadTextFileTool**: Secure file reading with range support (offset/limit parameters)
- **ReadImageFileTool**: Image file reading with base64 encoding
- **WriteFileTool**: File creation and modification with backup and validation
- **EditTool**: In-place file editing with search/replace operations
- **LSTool**: Directory listing and file system exploration with filtering
- **GrepTool**: Text search across files with regex support
- **ShellTool**: Command execution with cross-platform shell detection

**Tool Security Architecture:**
- **Path Validation**: All file operations validate paths are within working directory
- **Permission Checking**: File access permissions validated before operations
- **Input Sanitization**: Parameter validation prevents injection attacks
- **Error Message Security**: Generic error messages prevent information disclosure
- **Approval System**: Two-tier approval system (readonly/all) for operation control
- **Debug Mode**: Conditional detailed error information for development

## Integration Architecture

**AI Service Integration:**
```csharp
// Microsoft.Extensions.AI abstraction layer
IChatClient chatClient = new OpenAIClient(credentials, options)
    .GetChatClient(modelId)
    .AsIChatClient()
    .AsBuilder()
    .ConfigureOptions(options => options.MaxOutputTokens = 16000)
    .UseFunctionInvocation()
    .Build();
```

**Cross-Platform Support:**
- **Platform Detection**: Runtime.InteropServices.RuntimeInformation for OS-specific behavior
- **Shell Selection**: Automatic shell detection (PowerShell/Zsh/Bash)
- **Path Handling**: Cross-platform path operations with proper separators

**Configuration Loading:**
- **JSON Configuration**: File-based configuration with validation
- **Environment Integration**: System information injection (username, hostname, working directory)
- **Profile Management**: Multiple AI provider and model configurations

## Autocomplete Architecture

**Provider Pattern with Trigger-Based System:**
```csharp
// Core autocomplete interface with provider pattern
public interface IAutocompleteProvider
{
    AutocompleteType Type { get; }
    char TriggerCharacter { get; }
    bool ShouldTrigger(string input, int cursorPosition);
    string ExtractPartial(string input, int cursorPosition);
    Task<List<CompletionItem>> GetSuggestionsAsync(string partialInput);
    (string newInput, int newCursorPos) ReplacePartial(string input, int cursorPos, string completion);
}

// Autocomplete coordination service
public class AutocompleteManager
{
    public IAutocompleteProvider? DetectTrigger(string input, int cursorPosition);
    public async Task UpdateSuggestionsAsync(InputContext context);
    public void AcceptSuggestion(InputContext context);
}
```

**Autocomplete Flow Architecture:**
```mermaid
sequenceDiagram
    participant User
    participant FlexColumnTuiApp
    participant InputContext
    participant AutocompleteManager
    participant Provider
    participant FileSystem

    User->>FlexColumnTuiApp: Types trigger character (@, /)
    FlexColumnTuiApp->>InputContext: UpdateAutocompleteState()
    InputContext->>AutocompleteManager: DetectTrigger()
    AutocompleteManager->>Provider: ShouldTrigger()
    Provider-->>AutocompleteManager: true/false
    
    alt Trigger Detected
        AutocompleteManager->>Provider: ExtractPartial()
        Provider-->>AutocompleteManager: partial input
        AutocompleteManager->>Provider: GetSuggestionsAsync()
        Provider->>FileSystem: Scan directories/commands
        FileSystem-->>Provider: Results
        Provider-->>AutocompleteManager: List<CompletionItem>
        AutocompleteManager->>InputContext: Update suggestions
        InputContext-->>FlexColumnTuiApp: Show autocomplete UI
    end
    
    User->>FlexColumnTuiApp: Selects suggestion (Tab/Enter)
    FlexColumnTuiApp->>AutocompleteManager: AcceptSuggestion()
    AutocompleteManager->>Provider: ReplacePartial()
    Provider-->>AutocompleteManager: (newInput, newCursorPos)
    AutocompleteManager->>InputContext: Update input and cursor
```

**Input Context State Management:**
```csharp
// Unified input state with autocomplete integration
public class InputContext
{
    public string CurrentInput { get; set; }
    public int CursorPosition { get; set; }
    public InputState State { get; set; } // Normal, Autocomplete
    public AutocompleteType ActiveAutocompleteType { get; set; }
    public IAutocompleteProvider? ActiveProvider { get; set; }
    public List<CompletionItem> CompletionItems { get; set; }
    public bool ShowSuggestions { get; set; }
    public int SelectedSuggestionIndex { get; set; }
}
```

**Provider Implementations:**

**FilePathProvider Architecture:**
- **Security-First Design**: All file operations validated within working directory boundaries
- **Async File System Operations**: Non-blocking directory scanning with Task<List<CompletionItem>>
- **Performance Optimization**: Results limited to 20 items, sorted by type (directories first)
- **Cross-Platform Path Handling**: Normalized forward slashes, relative path calculation
- **Trigger Logic**: '@' character detection with word boundary validation
- **Smart Path Extraction**: Handles partial paths, directory separators, and cursor positioning

```csharp
// FilePathProvider security and performance patterns
private bool IsWithinWorkingDirectory(string path, string workingDir)
{
    var fullPath = Path.GetFullPath(path);
    var fullWorkingDir = Path.GetFullPath(workingDir);
    return fullPath.StartsWith(fullWorkingDir, StringComparison.OrdinalIgnoreCase);
}

// Efficient file system scanning with limits
var directories = Directory.GetDirectories(searchDirectory)
    .Where(dir => string.IsNullOrEmpty(searchPattern) || 
                 Path.GetFileName(dir).StartsWith(searchPattern, StringComparison.OrdinalIgnoreCase))
    .Take(20);
```

**SlashCommandProvider Architecture:**
- **Command Registry Integration**: Leverages existing SlashCommandProcessor for command enumeration
- **Trigger Detection**: '/' character at word boundaries using InputUtils helper methods
- **Command Metadata**: Provides descriptions and usage information for each command
- **Text Replacement**: Smart command replacement with automatic space insertion

**Terminal UI Integration:**
```csharp
// Composite UI rendering with autocomplete overlay
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
        return new Markup($"{style}{prefix} {suggestion}[/]");
    }).ToArray();

    var suggestionsPanel = new Panel(new Rows(suggestionItems))
        .Border(BoxBorder.Rounded)
        .BorderColor(Color.Blue);

    return new Rows(inputPanel, suggestionsPanel);
}
```

**Keyboard Event Integration:**
- **Event-Driven Architecture**: Integrates with AdvancedKeyboardHandler's event system
- **Priority Handling**: Autocomplete navigation (Up/Down/Tab/Enter/Escape) takes precedence over normal input
- **State-Aware Processing**: Different key behaviors based on InputState (Normal vs Autocomplete)
- **Graceful Cancellation**: Escape key clears autocomplete state and returns to normal input

**Error Handling and Resilience:**
- **Provider Isolation**: Exceptions in one provider don't affect others or crash the application
- **Graceful Degradation**: Failed autocomplete operations clear state and continue normal input
- **Comprehensive Logging**: Debug-level logging for troubleshooting without UI disruption
- **Memory Management**: Autocomplete state cleared when not needed to prevent memory leaks

**Extensibility Architecture:**
- **Plugin Pattern**: New autocomplete types can be added by implementing IAutocompleteProvider
- **Dependency Injection**: Providers automatically discovered and registered through DI container
- **Type Safety**: AutocompleteType enum ensures type-safe provider identification
- **Metadata Support**: CompletionItem includes type, description, and extensible metadata

**Performance Characteristics:**
- **Non-Blocking Operations**: All file system and command enumeration operations are async
- **Efficient Text Processing**: String operations optimized for cursor position tracking
- **Minimal UI Updates**: Only updates autocomplete UI when suggestions change
- **Resource Limits**: Built-in limits prevent excessive memory usage or long operations

## User Selection Architecture

**Provider Pattern for Interactive Commands:**
```csharp
// Core interface for commands requiring user selection
public interface IUserSelectionProvider
{
    string Command { get; }
    string Description { get; }
    Task<List<CompletionItem>> GetSelectionsAsync();
    Task OnSelectionAsync(string selection);
}

// Manager for orchestrating the selection process
public class UserSelectionManager
{
    public bool IsSelectionModeActive { get; }
    public void DetectAndActivate(string input);
    public async Task UpdateSelectionsAsync();
    public async Task AcceptSelectionAsync();
    public void Deactivate();
}
```

**User Selection Flow Architecture:**
```mermaid
sequenceDiagram
    participant User
    participant FlexColumnTuiApp
    participant SlashCommandProcessor
    participant UserSelectionManager
    participant Provider

    User->>FlexColumnTuiApp: Types interactive command (e.g., /tool-approvals)
    FlexColumnTuiApp->>SlashCommandProcessor: TryProcessCommand()
    SlashCommandProcessor->>FlexColumnTuiApp: Raises InteractiveCommandRequested event
    FlexColumnTuiApp->>UserSelectionManager: DetectAndActivate()
    UserSelectionManager->>Provider: GetSelectionsAsync()
    Provider-->>UserSelectionManager: List<CompletionItem>
    UserSelectionManager->>FlexColumnTuiApp: Update UI with selection options
    
    alt User navigates and selects
        User->>FlexColumnTuiApp: Presses Up/Down/Enter
        FlexColumnTuiApp->>UserSelectionManager: Navigate or AcceptSelectionAsync()
        UserSelectionManager->>Provider: OnSelectionAsync()
        Provider-->>UserSelectionManager: Updates state (e.g., config)
        UserSelectionManager->>FlexColumnTuiApp: Deactivates and returns to normal input
    end
```

**Input Context State Management:**
- The `InputState` enum is extended with a `UserSelection` state to differentiate between normal input, autocomplete, and interactive command selection.
- The `FlexColumnTuiApp`'s rendering and keyboard handling logic changes based on this state, ensuring that arrow keys navigate the selection list and Enter confirms the choice.

## State Management Architecture

**Application State:**
- **ChatState Enum**: Input, Thinking, ToolExecution states
- **StateManager**: Centralized state coordination
- **HistoryManager**: Chat session persistence and loading
- **Event-Driven Updates**: Real-time UI updates via IAsyncEnumerable

**Terminal State Management:**
- **ScrollbackTerminal**: Static content rendering with dynamic updates
- **Cursor Management**: Hide/show cursor during operations
- **Content Clearing**: Efficient dynamic content updates without full redraws

## Diff/Patch Architecture

**Unified Diff System:**
```csharp
// Domain models for diff representation
public class UnifiedDiff
{
    public string OriginalFile { get; set; }
    public string ModifiedFile { get; set; }
    public List<DiffHunk> Hunks { get; set; }
}

public class DiffHunk
{
    public int OriginalStart { get; set; }
    public int OriginalCount { get; set; }
    public int ModifiedStart { get; set; }
    public int ModifiedCount { get; set; }
    public List<DiffLine> Lines { get; set; }
}
```

**Patch Application Strategies:**
- **PatchApplicator**: Exact line matching for precise patch application
- **FuzzyPatchApplicator**: Fuzzy matching with configurable strategies for resilient patching
- **Strategy Pattern**: Multiple fuzzy matching implementations (LineOffsetStrategy, WhitespaceNormalizationStrategy)

**Fuzzy Matching Architecture:**
```csharp
public interface IFuzzyMatchingStrategy
{
    FuzzyMatchResult FindBestMatch(List<string> fileLines, List<DiffLine> hunkLines, int preferredLocation);
}

// Longest Common Subsequence algorithm for fuzzy matching
public class LongestCommonSubsequence
{
    public static LcsResult<T> Calculate<T>(IList<T> sequence1, IList<T> sequence2, IEqualityComparer<T>? comparer = null)
}
```

**Key Architectural Patterns:**
- **Dependency Injection**: Service-oriented design with Microsoft.Extensions.DI
- **Streaming Architecture**: IAsyncEnumerable<ChatResponseUpdate> for real-time updates
- **Factory Pattern**: ChatClient creation with comprehensive error handling
- **Repository Pattern**: ChatHistoryService for session persistence
- **Strategy Pattern**: Multiple fuzzy matching strategies for diff operations
- **Command Pattern**: Tool execution with structured request/response
- **Observer Pattern**: Event-driven keyboard handling and UI updates
- **Functional Error Handling**: FluentResults throughout the application
- **Algorithm Pattern**: LCS algorithm implementation for fuzzy patch matching
