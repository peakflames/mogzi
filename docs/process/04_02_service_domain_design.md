# Mogzi Service Layer & Domain Design

## Service Layer Design

### IAppService Interface Design

**Core Interface Definition:**
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

**Design Principles:**
- **Single Responsibility**: Each method has a clear, focused purpose
- **Async Operations**: Non-blocking operations for I/O and AI processing
- **Streaming Support**: IAsyncEnumerable for real-time AI response updates
- **Session Management**: Complete lifecycle management for chat sessions
- **Metrics Integration**: Token counting and usage tracking

### AppService Implementation Design

**Core Implementation Pattern:**
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
        return Mogzi.Utils.ApiMetricUtils.GetSimplisticTokenCount(chatHistory);
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

### ChatHistoryService Design

**Service Responsibilities:**
- **File-Based Persistence**: JSON serialization of chat sessions
- **Session Directory Structure**: Base path with individual session folders
- **Async Operations**: All I/O operations are async for performance
- **Error Handling**: Graceful handling of missing or corrupted sessions

**Session Management Pattern:**
```csharp
public class ChatHistoryService
{
    public async Task<List<ChatMessage>?> LoadChatSessionAsync(string sessionId, string systemPrompt)
    {
        var sessionPath = Path.Combine(GetChatSessionsBasePath(), sessionId, "chat_history.json");
        
        if (!File.Exists(sessionPath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(sessionPath);
            var messages = JsonSerializer.Deserialize<List<ChatMessage>>(json, JsonContext.Default.ListChatMessage);
            
            // Update system prompt to current value
            if (messages?.Count > 0 && messages[0].Role == ChatRole.System)
            {
                messages[0] = new ChatMessage(ChatRole.System, systemPrompt);
            }
            
            return messages;
        }
        catch (Exception ex)
        {
            // Log error and return null for graceful degradation
            return null;
        }
    }

    public async Task SaveChatHistoryAsync(string sessionPath, List<ChatMessage> chatHistory)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(sessionPath)!);
        
        var json = JsonSerializer.Serialize(chatHistory, JsonContext.Default.ListChatMessage);
        await File.WriteAllTextAsync(sessionPath, json);
    }
}
```

**Key Features:**
- **Atomic Operations**: File operations are atomic to prevent corruption
- **Directory Management**: Automatic creation of session directories
- **JSON Serialization**: Uses source-generated JsonContext for AOT compatibility
- **Error Recovery**: Graceful handling of corrupted or missing files
- **System Prompt Synchronization**: Ensures loaded sessions use current system prompt

## Domain Layer Design

### Configuration Schema Design

**Core Configuration Model:**
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

**Design Principles:**
- **Hierarchical Structure**: Clear separation between providers, profiles, and global settings
- **Default Values**: Sensible defaults for all configuration properties
- **Validation Support**: Structure supports validation during ChatClient creation
- **Extensibility**: Easy to add new configuration properties

### JSON Serialization Design

**AOT-Compatible Serialization:**
```csharp
[JsonSerializable(typeof(MaxbotConfiguration))]
[JsonSerializable(typeof(List<ApiProvider>))]
[JsonSerializable(typeof(List<Profile>))]
[JsonSerializable(typeof(List<ChatMessage>))]
public partial class JsonContext : JsonSerializerContext
{
}
```

**Serialization Features:**
- **Source Generation**: JsonSerializerContext for AOT compatibility
- **Property Naming**: JsonPropertyName attributes for consistent JSON format
- **Default Values**: Sensible defaults for all configuration properties
- **Validation**: Profile and API provider validation during ChatClient creation

### Working Directory Provider Design

**Abstraction Pattern:**
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

**Design Benefits:**
- **Testability**: Easy to mock for unit testing
- **Flexibility**: Can be replaced with custom implementations
- **Security**: Centralized control over working directory access
- **Consistency**: Single source of truth for current directory

## ChatClient Design

### Factory Pattern Implementation

**Factory Method Signature:**
```csharp
public static Result<ChatClient> Create(
    string configFilePath, 
    string? profileName = null, 
    string? toolApprovals = null, 
    string? mode = "oneshot", 
    Action<string, ConsoleColor>? llmResponseDetailsCallback = null, 
    bool debug = false)
```

**Factory Pattern Benefits:**
- **Error Handling**: Returns Result<T> for functional error handling
- **Validation**: Comprehensive validation before object creation
- **Configuration Loading**: Centralized configuration loading and validation
- **Dependency Resolution**: Resolves all dependencies during creation

### Configuration Loading Design

**Loading Process:**
1. **File Reading**: Reads JSON configuration with error handling
2. **Deserialization**: Uses source-generated JsonSerializerContext
3. **Profile Resolution**: Finds specified profile or defaults to marked default
4. **API Provider Lookup**: Validates API provider exists for selected profile
5. **Error Propagation**: Returns Result<T> for functional error handling

**Configuration Validation:**
```csharp
private static Result<(Profile profile, ApiProvider provider)> ValidateConfiguration(
    MaxbotConfiguration config, 
    string? profileName)
{
    // Find the profile to use
    var profile = string.IsNullOrEmpty(profileName) 
        ? config.Profiles.FirstOrDefault(p => p.Default)
        : config.Profiles.FirstOrDefault(p => p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase));

    if (profile == null)
        return Result.Fail($"Profile '{profileName ?? "default"}' not found in configuration");

    // Find the API provider for this profile
    var apiProvider = config.ApiProviders.FirstOrDefault(ap => 
        ap.Name.Equals(profile.ApiProvider, StringComparison.OrdinalIgnoreCase));

    if (apiProvider == null)
        return Result.Fail($"API provider '{profile.ApiProvider}' not found in configuration");

    return Result.Ok((profile, apiProvider));
}
```

### AI Client Configuration

**OpenAI Client Setup:**
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

**Configuration Features:**
- **Retry Policy**: Automatic retry with exponential backoff
- **Timeout Management**: 10-minute timeout for long operations
- **User Agent**: Identifies requests as coming from Mogzi
- **Function Calling**: Enables AI tool execution
- **Token Limits**: Configurable output token limits
- **Temperature Control**: Deterministic responses with 0.0 temperature

### System Prompt Generation

**Dynamic Prompt System:**
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

**Prompt Features:**
- **Dynamic Generation**: Computed property that regenerates on each access
- **Environment Context**: Includes current time, OS, shell, username, hostname, working directory
- **Configuration Integration**: Includes configuration and mode information
- **Real-Time Updates**: Always reflects current system state

### ChatClient Constructor Design

**Initialization Process:**
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

**Constructor Responsibilities:**
- **Environment Detection**: Platform, shell, user, and hostname detection
- **Tool Instantiation**: Creates all tools with shared configuration
- **Tool Registration**: Registers all tools for AI function calling
- **Configuration Storage**: Stores active profile and provider information
- **Callback Setup**: Configures optional response detail callbacks

## Design Patterns Used

### Dependency Injection Pattern
- **Constructor Injection**: Services receive dependencies through constructors
- **Interface Segregation**: Clean abstractions (IAppService, IWorkingDirectoryProvider)
- **Service Lifetime Management**: Singleton services for shared state

### Factory Pattern
- **ChatClient.Create()**: Factory method with comprehensive error handling
- **Result<T> Returns**: Functional error handling without exceptions
- **Validation Integration**: Configuration validation during creation

### Repository Pattern
- **ChatHistoryService**: Encapsulates chat session persistence logic
- **File-Based Storage**: JSON serialization with atomic operations
- **Error Recovery**: Graceful handling of storage failures

### Strategy Pattern
- **Working Directory Provider**: Pluggable directory resolution
- **Configuration Loading**: Flexible configuration source handling
- **Tool Registration**: Dynamic tool discovery and registration

## Error Handling Strategy

### Functional Error Handling
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

### Service Error Handling
- **Graceful Degradation**: Services continue operation when possible
- **Null Returns**: Return null for missing data rather than throwing
- **Logging Integration**: Structured logging for debugging
- **User Feedback**: Clear error messages for user-facing operations

This service layer and domain design provides a robust foundation for Mogzi's functionality, emphasizing reliability, performance, and maintainability while supporting the complex requirements of an AI-powered CLI application.
