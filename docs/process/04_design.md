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
```csharp
public class AppService : IAppService
{
    private readonly ChatClient _chatClient;
    private readonly ChatHistoryService _chatHistoryService;

    public AppService(ChatClient chatClient)
    {
        _chatClient = chatClient;
        _chatHistoryService = new ChatHistoryService();
    }

    public IAsyncEnumerable<ChatResponseUpdate> ProcessChatMessageAsync(List<ChatMessage> chatHistory, CancellationToken cancellationToken)
    {
        // Ensure the first message is the system prompt with the current value
        if (chatHistory.Count > 0 && chatHistory[0].Role == ChatRole.System)
        {
            // Update the system message with the current system prompt
            chatHistory[0] = new ChatMessage(ChatRole.System, _chatClient.SystemPrompt);
        }
        
        return _chatClient.ChatClientMEAI.GetStreamingResponseAsync(chatHistory, _chatClient.ChatOptions, cancellationToken);
    }

    public int CalculateTokenMetrics(List<ChatMessage> chatHistory)
    {
        return MaxBot.Utils.ApiMetricUtils.GetSimplisticTokenCount(chatHistory);
    }
}
```

**Implementation Patterns:**
- **Constructor Injection**: Takes ChatClient and creates ChatHistoryService directly
- **System Prompt Management**: Dynamic system prompt injection into chat history
- **Session Management**: Delegates to ChatHistoryService for persistence operations
- **Streaming Processing**: Returns IAsyncEnumerable<ChatResponseUpdate> for real-time updates
- **Token Metrics**: Uses ApiMetricUtils for simplistic token counting
- **System Message Updates**: Ensures current system prompt is always used in conversations

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
```csharp
// System prompt is now a computed property that regenerates each time it's accessed
public string SystemPrompt => Promptinator.GetSystemPrompt(
    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
    OperatingSystem.ToString(),
    DefaultShell,
    Username,
    Hostname,
    Directory.GetCurrentDirectory(),
    Config,
    _mode);
```

**ChatClient Constructor Design:**
```csharp
private ChatClient(IChatClient chatClient, MaxbotConfiguration config, Profile activeProfile, ApiProvider activeApiProvider, string mode, Action<string, ConsoleColor>? llmResponseDetailsCallback = null)
{
    ChatClientMEAI = chatClient;
    Config = config;
    ActiveProfile = activeProfile;
    ActiveApiProvider = activeApiProvider;
    _mode = mode;

    // Platform detection and environment setup
    OperatingSystem = Environment.OSVersion.Platform;
    DefaultShell = OperatingSystem switch
    {
        PlatformID.Win32NT => "powershell",
        PlatformID.MacOSX => "zsh",
        _ => "bash"
    };

    Username = Environment.UserName;
    Hostname = System.Net.Dns.GetHostName();

    // Tool instantiation with dependency injection
    SystemTools = new SystemTools(config, llmResponseDetailsCallback);
    DiffPatchTools = new DiffPatchTools(config, llmResponseDetailsCallback);
    ReadTextFileTool = new ReadTextFileTool(config, llmResponseDetailsCallback);
    ReadImageFileTool = new ReadImageFileTool(config, llmResponseDetailsCallback);
    WriteFileTool = new WriteFileTool(config, llmResponseDetailsCallback);
    EditTool = new EditTool(config, llmResponseDetailsCallback);
    LSTool = new LSTool(config, llmResponseDetailsCallback);
    GrepTool = new GrepTool(config, llmResponseDetailsCallback);
    ShellTool = new ShellTool(config, llmResponseDetailsCallback);

    // Tool registration and ChatOptions configuration
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
}
```

**Design Patterns:**
- **Dynamic Generation**: Computed property that regenerates on each access
- **Environment Context**: Includes current time, OS, shell, username, hostname, working directory
- **Configuration Integration**: Includes configuration and mode information
- **Promptinator Integration**: Uses centralized prompt generation system
- **Tool Composition**: All tools instantiated with shared configuration and callback
- **Centralized Registration**: All tools registered in ChatOptions for AI function calling

## Tool Implementation Design

**Base Tool Pattern:**
```csharp
public class ReadTextFileTool
{
    private readonly MaxbotConfiguration _config;
    private readonly Action<string, ConsoleColor>? _llmResponseDetailsCallback;
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider;

    public ReadTextFileTool(MaxbotConfiguration config, Action<string, ConsoleColor>? llmResponseDetailsCallback = null, IWorkingDirectoryProvider? workingDirectoryProvider = null)
    {
        _config = config;
        _llmResponseDetailsCallback = llmResponseDetailsCallback;
        _workingDirectoryProvider = workingDirectoryProvider ?? new DefaultWorkingDirectoryProvider();
    }

    public AIFunction GetTool()
    {
        return AIFunctionFactory.Create(
            ReadTextFile,
            new AIFunctionFactoryOptions
            {
                Name = "read_text_file",
                Description = "Reads and returns the content of a text file from the local filesystem. Supports reading specific line ranges for large files."
            });
    }
    
    public async Task<string> ReadTextFile(
        [Description("The absolute path to the text file to read. Relative paths are not supported.")] string absolute_path,
        [Description("Optional: The 0-based line number to start reading from. Requires 'limit' to be set.")] int? offset = null,
        [Description("Optional: Maximum number of lines to read. Use with 'offset' to paginate through large files.")] int? limit = null)
    {
        // Implementation with comprehensive validation and error handling
    }
}
```

**Tool Security Implementation:**
```csharp
// Comprehensive parameter validation
private string? ValidateParameters(string path, int? offset, int? limit)
{
    if (string.IsNullOrWhiteSpace(path))
        return "Path cannot be empty or whitespace";

    // Check if path is absolute
    if (!Path.IsPathRooted(path))
        return $"File path must be absolute, but was relative: {path}. You must provide an absolute path.";

    // Check for invalid characters
    var invalidChars = Path.GetInvalidPathChars();
    if (path.Any(c => invalidChars.Contains(c)))
        return "Path contains invalid characters";

    // Validate offset and limit parameters
    if (offset.HasValue && offset.Value < 0)
        return "Offset must be a non-negative number";

    if (limit.HasValue && limit.Value <= 0)
        return "Limit must be a positive number";

    if (offset.HasValue && !limit.HasValue)
        return "When offset is specified, limit must also be specified";

    return null;
}

// Working directory security enforcement
private bool IsPathInWorkingDirectory(string absolutePath, string workingDirectory)
{
    try
    {
        var normalizedAbsolutePath = Path.GetFullPath(absolutePath);
        var normalizedWorkingDirectory = Path.GetFullPath(workingDirectory);

        // Check if the path is exactly the working directory
        if (string.Equals(normalizedAbsolutePath, normalizedWorkingDirectory, 
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            return true;
        }

        // Ensure working directory ends with directory separator for subdirectory comparison
        if (!normalizedWorkingDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
            !normalizedWorkingDirectory.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
        {
            normalizedWorkingDirectory += Path.DirectorySeparatorChar;
        }

        return normalizedAbsolutePath.StartsWith(normalizedWorkingDirectory, 
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }
    catch
    {
        return false;
    }
}

// File permission validation
private bool HasReadPermission(FileInfo fileInfo)
{
    try
    {
        // Try to open the file for reading to check permissions
        using var stream = fileInfo.OpenRead();
        return true;
    }
    catch (UnauthorizedAccessException)
    {
        return false;
    }
    catch
    {
        return false;
    }
}

// Tool approval checking with configuration integration
if (requiresApproval && _config.ToolApprovals.Equals("readonly", StringComparison.OrdinalIgnoreCase))
{
    var msg = "Execution of this command requires approval. Please run with --tool-approvals all or use the /tool-approval slash command to grant permission.";
    if (_config.Debug)
    {
        _llmResponseDetailsCallback?.Invoke(msg, ConsoleColor.DarkGray);
    }
    return msg;
}
```

**Security Design Principles:**
- **Path Validation**: All file operations validate paths are within working directory
- **Parameter Validation**: Comprehensive input validation prevents injection attacks
- **Permission Checking**: File access permissions validated before operations
- **Error Message Security**: Generic error messages prevent information disclosure
- **Approval System**: Two-tier approval system (readonly/all) for operation control
- **Debug Mode**: Conditional detailed error information for development
- **Cross-Platform Security**: Platform-specific case sensitivity handling

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
