# MaxBot CLI Design Details

## Service Layer Design

**IAppService Interface Design:**
```csharp
public interface IAppService
{
    string SystemPrompt { get; }
    ChatOptions ChatOptions { get; }
    ChatClient ChatClient { get; }
    
    string CreateChatSession();
    Task<List<ChatMessage>?> LoadChatSessionAsync(string sessionId, string systemPrompt);
    List<string> GetChatSessions();
    string GetChatSessionsBasePath();
    Task SaveChatHistoryAsync(string sessionPath, List<ChatMessage> chatHistory);
    IAsyncEnumerable<ChatResponseUpdate> ProcessChatMessageAsync(List<ChatMessage> chatHistory, CancellationToken cancellationToken);
    int CalculateTokenMetrics(List<ChatMessage> chatHistory);
}
```

**AppService Implementation Design:**
- **Constructor Injection**: Takes ChatClient and creates ChatHistoryService
- **System Prompt Management**: Dynamic system prompt generation via ChatClient
- **Session Management**: Delegates to ChatHistoryService for persistence
- **Streaming Processing**: Returns IAsyncEnumerable<ChatResponseUpdate> for real-time updates
- **Token Metrics**: Uses ApiMetricUtils for token counting

**ChatHistoryService Design:**
- **File-Based Persistence**: JSON serialization of chat sessions
- **Session Directory Structure**: Base path with individual session folders
- **Async Operations**: All I/O operations are async for performance
- **Error Handling**: Graceful handling of missing or corrupted sessions

## Domain Layer Design

**Configuration Schema Design:**
```csharp
public class MaxbotConfiguration
{
    public string DefaultMode { get; set; } = "oneshot";
    public List<ApiProvider> ApiProviders { get; set; } = [];
    public List<Profile> Profiles { get; set; } = [];
    public string ToolApprovals { get; set; } = "readonly";
    public bool Debug { get; set; } = false;
}

public class ApiProvider
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
}

public class Profile
{
    public bool Default { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiProvider { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
}
```

**JSON Serialization Design:**
- **Source Generation**: JsonSerializerContext for AOT compatibility
- **Property Naming**: JsonPropertyName attributes for consistent JSON format
- **Default Values**: Sensible defaults for all configuration properties
- **Validation**: Profile and API provider validation during ChatClient creation

**Working Directory Provider Design:**
```csharp
public interface IWorkingDirectoryProvider
{
    string GetCurrentDirectory();
}

public class DefaultWorkingDirectoryProvider : IWorkingDirectoryProvider
{
    public string GetCurrentDirectory() => Directory.GetCurrentDirectory();
}
```

## ChatClient Design

**Factory Pattern Implementation:**
```csharp
public static Result<ChatClient> Create(
    string configFilePath, 
    string? profileName = null, 
    string? toolApprovals = null, 
    string? mode = "oneshot", 
    Action<string, ConsoleColor>? llmResponseDetailsCallback = null, 
    bool debug = false)
```

**Configuration Loading Design:**
- **File Reading**: Reads JSON configuration with error handling
- **Deserialization**: Uses source-generated JsonSerializerContext
- **Profile Resolution**: Finds specified profile or defaults to marked default
- **API Provider Lookup**: Validates API provider exists for selected profile
- **Error Propagation**: Returns Result<T> for functional error handling

**AI Client Configuration:**
```csharp
chatClient = new OpenAIClient(
        new ApiKeyCredential(apiKey),
        new OpenAIClientOptions
        {
            Endpoint = new(baseUrl),
            RetryPolicy = new ClientRetryPolicy(3),
            NetworkTimeout = TimeSpan.FromSeconds(600),
            UserAgentApplicationId = "maxbot"
        }
    )
    .GetChatClient(modelId)
    .AsIChatClient()
    .AsBuilder()
    .ConfigureOptions(options => options.MaxOutputTokens = 16000)
    .ConfigureOptions(options => options.Temperature = 0.0f)
    .UseFunctionInvocation()
    .Build();
```

**System Prompt Generation:**
- **Dynamic Generation**: Computed property that regenerates on each access
- **Environment Context**: Includes current time, OS, shell, username, hostname, working directory
- **Configuration Integration**: Includes configuration and mode information
- **Promptinator Integration**: Uses centralized prompt generation system

## Tool Implementation Design

**Base Tool Pattern:**
```csharp
public class ReadFileTool
{
    private readonly MaxbotConfiguration _config;
    private readonly Action<string, ConsoleColor>? _llmResponseDetailsCallback;
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider;

    public AIFunction GetTool() => AIFunctionFactory.Create(ReadFile, options);
    
    public async Task<string> ReadFile(
        [Description("...")] string absolute_path,
        [Description("...")] int? offset = null,
        [Description("...")] int? limit = null)
}
```

**Tool Security Implementation:**
- **Path Validation**: IsPathInWorkingDirectory() method for security boundaries
- **Parameter Validation**: ValidateParameters() method for input sanitization
- **Permission Checking**: HasReadPermission() for file access validation
- **Error Responses**: Structured XML error responses with security context

**Tool Response Format:**
```xml
<tool_response tool_name="read_file">
    <notes>Successfully read file path/to/file.txt</notes>
    <result status="SUCCESS" absolute_path="..." sha256_checksum="..." />
    <content_on_disk>file content here</content_on_disk>
</tool_response>
```

**Tool Approval System:**
- **Configuration-Based**: ToolApprovals setting controls execution permissions
- **Readonly Mode**: Blocks potentially destructive operations
- **All Mode**: Allows all tool executions
- **Runtime Checking**: Each tool checks approval status before execution

## TUI Application Design

**Dependency Injection Setup:**
```csharp
private static void ConfigureServices(IServiceCollection services, string[] args)
{
    services.AddLogging(builder => {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Information);
    });
    
    services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);
    services.AddSingleton<IWorkingDirectoryProvider, DefaultWorkingDirectoryProvider>();
    
    var chatClientResult = ChatClient.Create("maxbot.config.json", null, null, "chat", (details, color) => {}, false);
    if (chatClientResult.IsSuccess)
        services.AddSingleton(chatClientResult.Value);
    
    services.AddSingleton<IAppService, AppService>();
    services.AddSingleton<HistoryManager>();
    services.AddSingleton<StateManager>();
    services.AddSingleton<FlexColumnTuiApp>();
    services.AddSingleton<IScrollbackTerminal, ScrollbackTerminal>();
}
```

**Application Lifecycle:**
- **Service Provider Creation**: Builds DI container with all services
- **FlexColumnTuiApp Execution**: Main application loop with async operation
- **Exception Handling**: Catches OperationCanceledException and general exceptions
- **Resource Cleanup**: Proper disposal of service provider

## Terminal Interface Design

**IScrollbackTerminal Interface:**
```csharp
public interface IScrollbackTerminal
{
    void Initialize();
    void WriteStatic(IRenderable content, bool isUpdatable = false);
    Task StartDynamicDisplayAsync(Func<IRenderable> dynamicContentProvider, CancellationToken cancellationToken);
    void Shutdown();
}
```

**ScrollbackTerminal Implementation:**
- **Thread Safety**: Lock-based synchronization for concurrent access
- **Content Management**: Separate tracking of static, updatable, and dynamic content
- **Cursor Management**: Hide/show cursor during operations
- **Content Clearing**: Efficient ANSI escape sequences for content updates
- **Line Counting**: Accurate line counting for proper cursor positioning

**Dynamic Content Updates:**
```csharp
private void UpdateDynamic(IRenderable content)
{
    lock (_lock)
    {
        ClearDynamicContent();
        
        // Measure content for line counting
        var writer = new StringWriter();
        var measuringConsole = AnsiConsole.Create(new AnsiConsoleSettings { 
            Out = new AnsiConsoleOutput(writer), 
            ColorSystem = ColorSystemSupport.NoColors 
        });
        measuringConsole.Write(content);
        
        _dynamicContentLineCount = output.Split(newlines).Length;
        _console.Write(content);
    }
}
```

## State Management Design

**ChatState Enumeration:**
```csharp
public enum ChatState
{
    Input,          // Ready for user input
    Thinking,       // AI processing request
    ToolExecution   // Tool execution in progress
}
```

**StateManager Design:**
- **Centralized State**: Single source of truth for application state
- **Event-Driven Updates**: Notifies components of state changes
- **Thread Safety**: Proper synchronization for concurrent access
- **State Validation**: Ensures valid state transitions

**HistoryManager Design:**
- **Session Persistence**: Manages chat session storage and retrieval
- **Memory Management**: Efficient handling of large chat histories
- **Async Operations**: Non-blocking I/O for session operations
- **Error Recovery**: Graceful handling of corrupted or missing sessions

## Error Handling Design

**FluentResults Integration:**
```csharp
public static Result<ChatClient> Create(...)
{
    try
    {
        // Configuration loading and validation
        return new ChatClient(...);
    }
    catch (Exception ex)
    {
        return Result.Fail($"Failed to create ChatClient: {ex.Message}");
    }
}
```

**Tool Error Handling:**
- **Structured Responses**: XML-formatted error responses with context
- **Security Error Messages**: Generic messages to prevent information disclosure
- **Debug Information**: Detailed error information when debug mode enabled
- **Exception Boundaries**: Try-catch blocks around all external operations

**Application Error Handling:**
- **Top-Level Handlers**: Program.cs catches and handles fatal errors
- **Graceful Degradation**: Application continues operation when possible
- **User Feedback**: Clear error messages displayed to users
- **Logging Integration**: Structured logging for debugging and monitoring

## Cross-Platform Design

**Platform Detection:**
```csharp
OperatingSystem = Environment.OSVersion.Platform;
DefaultShell = OperatingSystem switch
{
    PlatformID.Win32NT => "powershell",
    PlatformID.MacOSX => "zsh",
    _ => "bash"
};
```

**Shell Command Execution:**
```csharp
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    fileName = "cmd.exe";
    arguments = $"/C \"{command}\"";
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
{
    fileName = "/bin/zsh";
    arguments = $"-c \"{command}\"";
}
else // Linux and Unix-like systems
{
    fileName = "/bin/bash";
    arguments = $"-c \"{command}\"";
}
```

**Path Handling:**
- **Cross-Platform Paths**: Path.GetFullPath() and Path.DirectorySeparatorChar
- **Case Sensitivity**: Platform-specific string comparisons for path validation
- **Security Boundaries**: Consistent working directory enforcement across platforms

## Performance Design

**AOT Compilation Support:**
- **Source Generation**: JsonSerializerContext for reflection-free JSON
- **PublishAot**: Enabled in project configuration for native compilation
- **Minimal Dependencies**: Careful selection of AOT-compatible libraries

**Memory Management:**
- **Streaming Responses**: IAsyncEnumerable for memory-efficient AI responses
- **Efficient Rendering**: Minimal redraws and content updates in terminal
- **Resource Disposal**: Proper cleanup of streams, processes, and services

**Token Optimization:**
- **Token Counting**: SharpToken integration for accurate token metrics
- **System Prompt Optimization**: Dynamic generation to include only current context
- **History Management**: Efficient storage and retrieval of chat sessions

## Diff/Patch System Design

**Unified Diff Domain Model:**
```csharp
public class UnifiedDiff
{
    public string OriginalFile { get; set; } = string.Empty;
    public string ModifiedFile { get; set; } = string.Empty;
    public List<DiffHunk> Hunks { get; set; } = [];
}

public class DiffHunk
{
    public int OriginalStart { get; set; }
    public int OriginalCount { get; set; }
    public int ModifiedStart { get; set; }
    public int ModifiedCount { get; set; }
    public List<DiffLine> Lines { get; set; } = [];
}

public class DiffLine
{
    public DiffLineType Type { get; set; }
    public string Content { get; set; } = string.Empty;
}

public enum DiffLineType { Context, Added, Removed }
```

**Patch Application Design:**
```csharp
public class PatchApplicator
{
    public PatchResult ApplyPatch(string content, UnifiedDiff patch)
    {
        var lines = content.Split(newlines, StringSplitOptions.None).ToList();
        
        // Apply hunks in reverse order to maintain line numbers
        foreach (var hunk in patch.Hunks.OrderByDescending(h => h.OriginalStart))
        {
            var hunkResult = ApplyHunk(lines, hunk);
            if (!hunkResult.Success)
                return new PatchResult { Success = false, Error = hunkResult.Error };
        }
        
        return new PatchResult { Success = true, ModifiedContent = string.Join(Environment.NewLine, lines) };
    }
}
```

**Fuzzy Matching Strategy Design:**
```csharp
public interface IFuzzyMatchingStrategy
{
    FuzzyMatchResult FindBestMatch(List<string> fileLines, List<DiffLine> hunkLines, int preferredLocation);
}

public class LineOffsetStrategy : IFuzzyMatchingStrategy
{
    // Searches within offset range around preferred location
}

public class WhitespaceNormalizationStrategy : IFuzzyMatchingStrategy
{
    // Normalizes whitespace differences for matching
}
```

**Longest Common Subsequence Implementation:**
- **Dynamic Programming**: Classic LCS algorithm for sequence comparison
- **Generic Implementation**: Works with any type T with custom equality comparers
- **Performance Optimization**: Efficient memory usage for large sequences
- **Match Quality Scoring**: Provides confidence scores for fuzzy matches

**DiffPatchTools Integration:**
```csharp
public class DiffPatchTools
{
    public List<AIFunction> GetTools() => [
        AIFunctionFactory.Create(GenerateCodePatch, options),
        AIFunctionFactory.Create(ApplyCodePatch, options),
        AIFunctionFactory.Create(PreviewPatchApplication, options)
    ];
    
    // Tool implementations with comprehensive error handling
    // Security validation for all file operations
    // Structured XML responses with detailed results
}
```

**Patch Result Design:**
```csharp
public class PatchResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? ModifiedContent { get; set; }
    public List<AppliedHunk> AppliedHunks { get; set; } = [];
    public DiffHunk? ConflictingHunk { get; set; }
}

public class AppliedHunk
{
    public int AppliedAtLine { get; set; }
    public int LinesAdded { get; set; }
    public int LinesRemoved { get; set; }
    public bool UsedFuzzyMatching { get; set; }
    public double MatchConfidence { get; set; }
}
```

## System Prompt Design

**Dynamic Prompt Generation:**
```csharp
public static string GetSystemPrompt(
    string currentDataTime, 
    string userOperatingSystem, 
    string userShell, 
    string username, 
    string hostname, 
    string currentWorkingDirectory, 
    MaxbotConfiguration config, 
    string mode)
```

**Prompt Structure:**
- **Identity and Personality**: MaxBot character definition and interaction style
- **Tool Use Guidelines**: Comprehensive tool usage protocols and best practices
- **Security Protocols**: Tool approval system and working directory enforcement
- **User Environment Context**: Dynamic injection of system information
- **Task Completion Protocol**: Structured approach to task execution and completion

**Key Prompt Features:**
- **Verification Protocol**: Mandatory write verification for file operations
- **Tool Communication**: Standardized emoji-prefixed tool announcements
- **Error Handling**: Explicit error reporting and recovery procedures
- **Ground Truth Principle**: File system as authoritative source of truth
- **Iterative Execution**: Step-by-step tool usage with result validation

**Environment Integration:**
- **Operating System**: Platform-specific behavior and shell selection
- **Working Directory**: Security boundary enforcement
- **Tool Approvals**: Runtime permission checking
- **Debug Mode**: Conditional detailed logging and error information
- **User Context**: Username, hostname, and current directory injection
