# Session Management Design

## 1. Overview

This document outlines the design for the session management feature in the Mogzi TUI application. The goal is to provide a robust mechanism for persisting chat history across application runs, allowing users to manage multiple conversations and resume them later. This design includes comprehensive attachment handling for images, PDFs, and other file types within chat messages.

## 2. Key Components

### 2.1. Session (Domain Entity)

A new domain entity, `Session`, will be created to represent a single chat conversation with attachment support.

**File:** `src/Mogzi.Core/Domain/Session.cs`

```csharp
namespace Mogzi.Core.Domain;

public class Session
{
    public Guid Id { get; set; } // UUIDv7 for time-ordered generation
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public List<SerializableChatMessage> History { get; set; } = [];
    public string InitialPrompt { get; set; } = string.Empty;
    public SessionUsageMetrics? UsageMetrics { get; set; } // Real-time token usage tracking
}
```

- **Id**: A UUIDv7, which is time-sortable and will be used as the directory name.
- **Name**: A user-friendly name for the session, defaulting to a randomly generated creative name in the format "color_creature" (e.g., "emerald_dragon", "crimson_beholder", "sapphire_elf").
- **CreatedAt/LastModifiedAt**: Timestamps for session management.
- **History**: The list of `SerializableChatMessage` objects for the conversation with attachment metadata.
- **InitialPrompt**: The first user message, truncated to 50 characters for display in the session list.

### 2.2. Enhanced Chat Message Serialization with Tool Execution Support

The existing `SerializableChatMessage` will be enhanced to support attachment metadata and function call/result persistence for complete tool execution replay.

**File:** `src/Mogzi.Core/Domain/ChatHistory.cs`

```csharp
public class SerializableChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? AuthorName { get; set; }
    public string? MessageId { get; set; }
    public List<AttachmentMetadata> Attachments { get; set; } = [];
    public List<FunctionCall> FunctionCalls { get; set; } = [];
    public List<FunctionResult> FunctionResults { get; set; } = [];

    public static SerializableChatMessage FromChatMessage(ChatMessage message, int messageIndex)
    {
        var serializable = new SerializableChatMessage
        {
            Role = message.Role.ToString().ToLower(),
            Content = message.Text,
            AuthorName = message.AuthorName,
            MessageId = message.MessageId
        };

        // Process all content types from message.Contents
        foreach (var content in message.Contents)
        {
            switch (content)
            {
                case ImageContent imageContent:
                    serializable.Attachments.Add(new AttachmentMetadata
                    {
                        OriginalFileName = imageContent.Uri?.ToString() ?? "image.png",
                        ContentType = "image/png",
                        MessageIndex = messageIndex,
                        // Content hash and stored filename will be set during save
                    });
                    break;
                    
                case DataContent dataContent:
                    var extension = GetExtensionFromMimeType(dataContent.MediaType);
                    serializable.Attachments.Add(new AttachmentMetadata
                    {
                        OriginalFileName = $"attachment{extension}",
                        ContentType = dataContent.MediaType ?? "application/octet-stream",
                        MessageIndex = messageIndex,
                        // Content hash and stored filename will be set during save
                    });
                    break;

                case FunctionCallContent functionCallContent:
                    serializable.FunctionCalls.Add(new FunctionCall
                    {
                        CallId = functionCallContent.CallId ?? Guid.NewGuid().ToString(),
                        Name = functionCallContent.Name ?? "unknown",
                        Arguments = functionCallContent.Arguments?.ToDictionary(
                            kvp => kvp.Key, 
                            kvp => kvp.Value) ?? new Dictionary<string, object?>()
                    });
                    break;

                case FunctionResultContent functionResultContent:
                    serializable.FunctionResults.Add(new FunctionResult
                    {
                        CallId = functionResultContent.CallId ?? Guid.NewGuid().ToString(),
                        Result = functionResultContent.Result
                    });
                    break;
            }
        }

        return serializable;
    }
}

public class AttachmentMetadata
{
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty; // {msg-index}-{hash}.{ext}
    public string ContentType { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty; // SHA256 hash for integrity
    public long FileSizeBytes { get; set; }
    public int MessageIndex { get; set; } // Index of message in session history
}

public class FunctionCall
{
    public string CallId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object?> Arguments { get; set; } = [];
}

public class FunctionResult
{
    public string CallId { get; set; } = string.Empty;
    public object? Result { get; set; }
}
```

### 2.3. SessionNameGenerator (Utility)

A utility class for generating creative session names using random color and creature combinations.

**File:** `src/Mogzi.Core/Utils/SessionNameGenerator.cs`

**Key Features**:
- **Random Name Generation**: Combines colors and creatures to create unique, memorable session names
- **Extensive Vocabularies**: Large lists of colors (78 entries) and creatures (300+ entries) for maximum variety
- **Consistent Format**: All names follow the "color_creature" pattern with lowercase and underscore separator
- **Thread-Safe**: Uses static readonly arrays and thread-safe Random instance
- **D&D-Enhanced**: Includes real animals, mythical creatures, D&D races, and monsters for epic combinations
- **Creative Combinations**: Examples include "emerald_dragon", "crimson_beholder", "sapphire_elf", "obsidian_tarrasque"

**Creature Categories**:
- **Real Animals**: Traditional animals from aardvark to zebra
- **Mythical Creatures**: Classic fantasy beings like basilisk, chimera, griffin, phoenix
- **D&D Races**: Player character races including dwarf, elf, tiefling, dragonborn
- **D&D Monsters**: Iconic creatures like beholder, mindflayer, owlbear, tarrasque

**Usage**:
```csharp
string sessionName = SessionNameGenerator.GenerateName();
// Examples: "sapphire_tiger", "violet_phoenix", "copper_beholder", "crimson_elf"
```

### 2.4. SessionManager (Service)

A new service, `SessionManager`, will be responsible for the lifecycle of chat sessions with attachment support.

**File:** `src/Mogzi.TUI/Services/SessionManager.cs`

**Key Responsibilities**:
- Creating new sessions with directory structure
- Loading existing sessions from directory-based storage
- Persisting session metadata and attachment references
- Managing attachment storage with content-based hashing
- Listing all available sessions with metadata
- Handling corrupted session files
- Supporting custom session naming
- Ensuring thread-safety and cross-process concurrency control

**Critical Design Changes**:
- **Directory-Based Storage**: Each session gets its own directory instead of a flat JSON file
- **Attachment Handling**: Separate attachments subdirectory with content-based file naming
- **Enhanced Serialization**: `SerializableChatMessage` extended to include attachment metadata
- **Content Deduplication**: SHA256 hashing prevents duplicate attachment storage
- **Atomic Operations**: Two-phase writes ensure data integrity

### 2.3. HistoryManager (Existing Service)

The existing `HistoryManager` will be modified to work in concert with the `SessionManager`. Instead of holding the state itself, it will operate on the `_currentSession.History` provided by the `SessionManager`.

- **`GetCurrentChatHistory()`**: Will return `_sessionManager.CurrentSession.History`.
- **`AddUserMessage()` / `AddAssistantMessage()`**: Will add messages to the current session's history and trigger `_sessionManager.SaveCurrentSession()`.

## 3. Data Flow and Workflow

### 3.1. Application Startup

1.  `Program.cs` parses command-line arguments.
2.  If a `--session <session_id>` argument is provided, it's passed to the `SessionManager`.
3.  The `SessionManager` is instantiated.
4.  If a `session_id` was provided, `SessionManager.LoadSession(session_id)` is called.
    - The corresponding JSON file is read from `~/.mogzi/chats/`.
    - The content is deserialized into the `_currentSession` object.
    - If the file is corrupted, `HandleCorruptedSession()` is called, and a new session is created.
5.  If no `session_id` was provided, `SessionManager.CreateNewSession()` is called.
    - A new `Session` object is created with a new UUIDv7.
    - An empty file is created in `~/.mogzi/chats/`.
6.  The `HistoryManager` is now ready to use the `_currentSession`.

### 3.2. During Conversation

1.  A user or assistant message is added via `HistoryManager`.
2.  `HistoryManager` adds the `ChatMessage` to `_sessionManager.CurrentSession.History`.
3.  `HistoryManager` calls `await _sessionManager.SaveCurrentSessionAsync()`.
4.  `SessionManager` acquires a file lock using `SemaphoreSlim` to ensure thread safety.
5.  `SessionManager` updates the session's `LastModifiedAt` timestamp.
6.  `SessionManager` serializes the `_currentSession` object to JSON.
7.  `SessionManager` writes the JSON to a temporary file first.
8.  `SessionManager` then atomically replaces the original file with the temporary file.
9.  `SessionManager` releases the file lock.

This atomic file operation pattern ensures that the session file is never left in a partially written state, even if the application crashes during the write operation. The use of `SemaphoreSlim` ensures that only one thread can access the file at a time, preventing race conditions.

### 3.3. Listing Sessions (`/session list`)

1.  The `SlashCommandProcessor` detects the `/session list` command.
2.  It calls `await _sessionManager.ListSessionsAsync()`.
3.  `SessionManager` reads all `.json` files in `~/.mogzi/chats/`.
4.  For each file, it deserializes the `Session` object to retrieve the metadata (`Name`, `LastModifiedAt`, `InitialPrompt`).
5.  It returns a list of these `Session` objects.
6.  The UI component responsible for displaying the list formats and presents the information to the user, showing the custom session name (or creation timestamp if no custom name has been set).

### 3.4. Renaming Sessions (`/session rename`)

The `/session rename` command implements the **Input Continuation Command Pattern** for improved user experience:

**Enhanced UX Flow:**
1.  User types `/session r` and sees autocomplete suggestion for `/session rename`
2.  User presses Tab or Enter to accept the suggestion
3.  `SlashCommandProcessor.RequiresInputContinuation()` returns `true` for `/session rename`
4.  Input field is populated with `/session rename ` (with trailing space)
5.  Cursor is positioned after the space, ready for user to type the new name
6.  User types the desired session name (e.g., "my-project-session")
7.  User presses Enter to submit the complete command: `/session rename my-project-session`

**Backend Processing:**
1.  The `SlashCommandProcessor` detects the complete `/session rename <new_name>` command.
2.  It extracts the new name from the command arguments.
3.  It calls `await _sessionManager.RenameSessionAsync(newName)`.
4.  `SessionManager` acquires the file lock.
5.  It updates the `Name` property of the current session.
6.  It calls `SaveCurrentSessionAsync()` to persist the change.
7.  It releases the file lock.
8.  The UI component displays a confirmation message to the user.

**Command Configuration:**
```csharp
// In SlashCommandProcessor.cs
_commands["/session rename"] = new SlashCommand(
    "/session rename", 
    "Rename the current session", 
    GetSessionRenameComponent, 
    RequiresInputContinuation: true  // Enables input continuation pattern
);
```

**Key UX Benefits:**
- **Seamless Workflow**: No interruption between command selection and name input
- **Clear Intent**: User sees the full command structure before typing the name
- **Consistent Behavior**: Follows the same pattern as other input continuation commands
- **Reduced Friction**: Single continuous input flow instead of separate command/argument steps

## 4. Storage

### 4.1. Directory Structure

**Location**: `~/.mogzi/chats/` (The path will be resolved cross-platform).

**Structure**: Directory-based storage with the following layout:
```
~/.mogzi/chats/
└── {session-uuid}/
    ├── session.json          # Session metadata + text content + attachment metadata
    └── attachments/
        ├── {msg-index}-{attachment-hash}.png
        ├── {msg-index}-{attachment-hash}.pdf
        └── {msg-index}-{attachment-hash}.txt
```

### 4.2. File Formats

- **Session Metadata**: JSON format in `session.json` containing the `Session` object with enhanced `SerializableChatMessage` entries
- **Attachments**: Binary files stored with content-based naming: `{message-index}-{sha256-hash}.{extension}`
- **Directory Names**: UUIDv7 for time-ordered generation and uniqueness

### 4.3. Attachment Naming Strategy

- **Content-Based Hashing**: SHA256 hash of file content prevents duplicates
- **Message Association**: Message index prefix maintains relationship to conversation
- **Original Metadata**: Preserved in `AttachmentMetadata` within session.json
- **Deduplication**: Identical content across messages shares the same file

## 5. Error Handling and Concurrency Control

### 5.1. Error Handling

-   **Corrupted Files**: If a session file cannot be deserialized, the `SessionManager` will rename it to `<session_id>.corrupted` and log an error. The application will then proceed with a new, empty session to prevent a crash.
-   **I/O Errors**: All file operations will be wrapped in `try-catch` blocks to handle permissions issues, disk full errors, etc. Errors will be logged, and the application will continue if possible.
-   **Validation Errors**: Session name validation will ensure that names are not empty or contain invalid characters. Appropriate error messages will be displayed to the user.

### 5.2. Concurrency Control

-   **Thread Safety**: A `SemaphoreSlim` will be used to ensure that only one thread can access the session file at a time. This prevents race conditions when multiple operations try to modify the same session simultaneously.
-   **Atomic File Operations**: To prevent file corruption, the `SessionManager` will use a two-phase write approach:
    1. Write the complete session data to a temporary file.
    2. Delete the original file (if it exists).
    3. Rename the temporary file to the final filename.
    
    This ensures that the file is never left in a partially written state, even if the application crashes during the write operation.
-   **Cross-Process Locking**: When opening files for reading or writing, appropriate `FileShare` flags will be used to prevent multiple processes from modifying the same file simultaneously.

## 6. Integration with Existing Architecture

-   **Dependency Injection**: `SessionManager` will be registered as a singleton service in `ServiceConfiguration.cs`.
-   **TuiContext**: `ITuiContext` will be updated to include a reference to the `SessionManager`.
-   **HistoryManager**: Will be injected with the `SessionManager` to access the current session.
-   **CLI Parsing**: `Program.cs` will need to be updated to handle the `--session` argument.
-   **Slash Commands**: `SlashCommandProcessor` will be updated to handle the new `/session` commands, including:
    - `/session list` - List all available sessions
    - `/session clear` - Clear the current session
    - `/session rename <name>` - Rename the current session

## 7. Tool Execution Display and Message Boundary Detection

### 7.1. Message Boundary Detection System

The session management system implements a sophisticated Message Boundary Detection System that creates separate ChatMessage objects for different content types during streaming responses. This ensures proper message sequencing in both live UI display and session persistence.

**Content Type Classification:**
```csharp
private enum ContentType
{
    None,           // No content or unknown
    Text,           // Regular assistant text response
    FunctionCall,   // Tool invocation
    FunctionResult  // Tool execution result
}
```

**Message Boundary Decision Logic:**
- **Content Type Transitions**: New messages are created when content type changes (Text → Tool → Text)
- **Function Call/Result Separation**: Each tool invocation and result becomes a distinct message
- **Clean UI Boundaries**: Maintains clear visual separation between different content types
- **Complete History Preservation**: All content types preserved for AI context and session replay

### 7.2. Pending/Completed Message Architecture

The `HistoryManager` implements a dual-state message system to handle streaming responses without persisting intermediate chunks:

**Deferred Persistence Pattern:**
```csharp
public class HistoryManager
{
    private readonly List<ChatMessage> _completedMessages = [];
    private readonly List<ChatMessage> _pendingMessages = [];
    
    // Streaming workflow methods
    public void AddPendingAssistantMessage(ChatMessage message);
    public void UpdateLastPendingMessage(ChatMessage message);
    public async Task FinalizeStreamingAsync();
    
    // UI display methods
    public List<ChatMessage> GetAllMessagesForDisplay();
    public List<ChatMessage> GetCurrentChatHistory();
}
```

**Key Benefits:**
- **No Streaming Artifacts**: Only final, consolidated messages are persisted
- **Real-Time UI Updates**: Pending messages shown during streaming for responsive UI
- **Atomic Persistence**: Complete messages written to session storage in single operations
- **Performance Optimization**: Avoids excessive I/O during streaming

### 7.3. Tool Execution Display for Loaded Sessions

When sessions are loaded, the enhanced `RenderMessage()` method in `FlexColumnTuiApp` processes both text content and function calls/results to recreate the complete tool execution display:

**Enhanced Message Rendering:**
```csharp
private IRenderable RenderMessage(ChatMessage message)
{
    var components = new List<IRenderable>();

    // Handle text content with role-based styling
    if (!string.IsNullOrEmpty(message.Text))
    {
        components.Add(CreateTextRenderable(message));
    }

    // Handle function calls and results for tool execution display
    if (message.Contents != null && message.Contents.Count > 0)
    {
        foreach (var content in message.Contents)
        {
            if (content is FunctionCallContent functionCall)
            {
                // Create tool display for function call
                var toolDisplay = ToolExecutionDisplay.CreateToolDisplay(
                    functionCall.Name ?? "Unknown Tool",
                    ToolExecutionStatus.Success,
                    GetToolDescription(functionCall),
                    diff: null,
                    result: null
                );
                components.Add(toolDisplay);
            }
            else if (content is FunctionResultContent functionResult)
            {
                // Parse tool result and create enhanced display
                var toolResponseParser = _serviceProvider.GetRequiredService<ToolResponseParser>();
                var result = functionResult.Result?.ToString() ?? "";
                var toolName = ExtractToolNameFromResult(result) ?? "Tool";
                var toolInfo = toolResponseParser.ParseToolResponse(toolName, result);

                var toolDisplay = ToolExecutionDisplay.CreateToolDisplay(
                    toolInfo.ToolName,
                    toolInfo.Status,
                    toolInfo.Description,
                    diff: null,
                    result: toolInfo.Summary ?? result
                );
                components.Add(toolDisplay);
            }
        }
    }

    // Return appropriate renderable based on content
    return components.Count switch
    {
        0 => new Text(string.Empty),
        1 => components[0],
        _ => new Rows(components)
    };
}
```

**Tool Description Extraction:**
- **Smart Argument Analysis**: Extracts key arguments (file paths, commands) for display
- **Path Simplification**: Shows filenames instead of full paths for readability
- **Content Truncation**: Limits display length to prevent UI overflow
- **Tool-Specific Logic**: Different extraction strategies for different tool types

### 7.4. Session Loading Workflow with Tool Execution

**Complete Session Restoration Process:**
1. **Session Metadata Loading**: Read session.json with all message data
2. **Message Reconstruction**: Convert SerializableChatMessage to ChatMessage with Contents
3. **Function Call/Result Processing**: Restore FunctionCallContent and FunctionResultContent
4. **Tool Display Recreation**: Use same ToolExecutionDisplay components as live sessions
5. **UI Consistency**: Identical visual representation for live and loaded tool executions

**Streaming vs. Loaded Session Comparison:**
- **Live Streaming**: Real-time tool execution with progress indicators and status updates
- **Loaded Sessions**: Static tool execution displays showing final results and status
- **Visual Consistency**: Same ToolExecutionDisplay.CreateToolDisplay() method used for both
- **Complete History**: All tool interactions preserved and displayed accurately

### 7.5. SUPREME OBJECTIVE Achievement

The enhanced session management system achieves the **SUPREME OBJECTIVE**: "Retain exact chat history such that when reloaded, we can replay and restore the entire history back to the AI model to pick up exactly where we left off with all the same history."

**Key Achievements:**
- **Complete Content Preservation**: Text, function calls, and results all persisted
- **Message Boundary Integrity**: Proper message sequencing maintained across sessions
- **Tool Execution Replay**: Full tool interaction history available for AI context
- **UI Fidelity**: Loaded sessions display identically to live sessions
- **Context Continuity**: AI receives complete conversation history including all tool interactions

## 8. Performance Considerations

-   **Asynchronous I/O**: All file operations will use asynchronous methods (`ReadAllTextAsync`, `WriteAllTextAsync`) to avoid blocking the UI thread.
-   **Minimal Serialization**: Only changed fields will be updated before serialization to minimize the amount of data that needs to be processed.
-   **Efficient JSON**: System.Text.Json with source generation will be used for efficient serialization and deserialization.
-   **Prioritizing Reliability**: While performance optimizations will be implemented, the primary focus will be on ensuring data integrity through atomic file operations, even if this introduces some performance overhead.
-   **Deferred Persistence**: Pending message architecture reduces I/O operations during streaming
-   **Tool Display Caching**: ToolResponseParser results cached to avoid redundant parsing
-   **Content Type Detection**: Efficient content type classification minimizes processing overhead
