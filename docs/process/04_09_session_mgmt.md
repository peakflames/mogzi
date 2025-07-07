# Session Management Design

## 1. Overview

This document outlines the design for the session management feature in the Mogzi TUI application. The goal is to provide a robust mechanism for persisting chat history across application runs, allowing users to manage multiple conversations and resume them later.

## 2. Key Components

### 2.1. Session (Domain Entity)

A new domain entity, `Session`, will be created to represent a single chat conversation.

**File:** `src/Mogzi.Core/Domain/Session.cs`

```csharp
namespace Mogzi.Core.Domain;

public class Session
{
    public Guid Id { get; set; } // UUIDv7 for time-ordered generation
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public List<ChatMessage> History { get; set; } = [];
    public string InitialPrompt { get; set; } = string.Empty;
}
```

- **Id**: A UUIDv7, which is time-sortable and will be used as the filename.
- **Name**: A user-friendly name for the session, defaulting to the creation timestamp.
- **CreatedAt/LastModifiedAt**: Timestamps for session management.
- **History**: The list of `ChatMessage` objects for the conversation.
- **InitialPrompt**: The first user message, truncated to 50 characters for display in the session list.

### 2.2. SessionManager (Service)

A new service, `SessionManager`, will be responsible for the lifecycle of chat sessions.

**File:** `src/Mogzi.TUI/Services/SessionManager.cs`

```csharp
namespace Mogzi.TUI.Services;

public class SessionManager
{
    private readonly string _sessionsPath;
    private readonly HistoryManager _historyManager;
    private readonly ILogger<SessionManager> _logger;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private Session _currentSession;

    public SessionManager(HistoryManager historyManager, ILogger<SessionManager> logger)
    {
        // ... constructor logic ...
    }

    public async Task CreateNewSessionAsync() { /* ... */ }
    public async Task LoadSessionAsync(string sessionId) { /* ... */ }
    public async Task SaveCurrentSessionAsync() { /* ... */ }
    public async Task ClearCurrentSessionAsync() { /* ... */ }
    public async Task<List<Session>> ListSessionsAsync() { /* ... */ }
    public async Task RenameSessionAsync(string newName) { /* ... */ }
    private async Task HandleCorruptedSessionAsync(string filePath) { /* ... */ }
}
```

- **Responsibilities**:
    - Creating new sessions.
    - Loading existing sessions from disk.
    - Persisting the current session to disk continuously with atomic file operations.
    - Listing all available sessions.
    - Handling corrupted session files.
    - Supporting custom session naming.
    - Ensuring thread-safety and cross-process concurrency control.

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

1.  The `SlashCommandProcessor` detects the `/session rename <new_name>` command.
2.  It calls `await _sessionManager.RenameSessionAsync(newName)`.
3.  `SessionManager` acquires the file lock.
4.  It updates the `Name` property of the current session.
5.  It calls `SaveCurrentSessionAsync()` to persist the change.
6.  It releases the file lock.
7.  The UI component displays a confirmation message to the user.

## 4. Storage

-   **Location**: `~/.mogzi/chats/` (The path will be resolved cross-platform).
-   **Format**: JSON. Each session will be a single `.json` file.
-   **Filename**: The `Id` of the session (a UUIDv7) followed by `.json`. Example: `018f5a8c-6b6c-7f0b-89a3-318c408f5b5b.json`.

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

## 7. Performance Considerations

-   **Asynchronous I/O**: All file operations will use asynchronous methods (`ReadAllTextAsync`, `WriteAllTextAsync`) to avoid blocking the UI thread.
-   **Minimal Serialization**: Only changed fields will be updated before serialization to minimize the amount of data that needs to be processed.
-   **Efficient JSON**: System.Text.Json with source generation will be used for efficient serialization and deserialization.
-   **Prioritizing Reliability**: While performance optimizations will be implemented, the primary focus will be on ensuring data integrity through atomic file operations, even if this introduces some performance overhead.
