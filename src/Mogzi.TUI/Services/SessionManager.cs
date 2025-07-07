

namespace Mogzi.TUI.Services;

/// <summary>
/// Service responsible for managing chat session lifecycle including creation, loading, saving, and listing sessions.
/// Implements directory-based storage with attachment support and atomic file operations.
/// </summary>
public class SessionManager : IDisposable
{
    private readonly string _sessionsPath;
    private readonly ILogger<SessionManager> _logger;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    /// <summary>
    /// Gets the current active session.
    /// </summary>
    public Session? CurrentSession { get; private set; }

    /// <summary>
    /// Initializes a new instance of the SessionManager.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information.</param>
    public SessionManager(ILogger<SessionManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize sessions directory path
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _sessionsPath = Path.Combine(homeDirectory, ".mogzi", "chats");

        // Ensure sessions directory exists
        _ = Directory.CreateDirectory(_sessionsPath);

        _logger.LogDebug("SessionManager initialized with sessions path: {SessionsPath}", _sessionsPath);
    }

    /// <summary>
    /// Gets the session directory path for a given session ID.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>The full path to the session directory.</returns>
    private string GetSessionDirectoryPath(Guid sessionId)
    {
        return Path.Combine(_sessionsPath, sessionId.ToString());
    }

    /// <summary>
    /// Gets the session.json file path for a given session ID.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>The full path to the session.json file.</returns>
    private string GetSessionFilePath(Guid sessionId)
    {
        return Path.Combine(GetSessionDirectoryPath(sessionId), "session.json");
    }

    /// <summary>
    /// Gets the attachments directory path for a given session ID.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>The full path to the attachments directory.</returns>
    private string GetAttachmentsDirectoryPath(Guid sessionId)
    {
        return Path.Combine(GetSessionDirectoryPath(sessionId), "attachments");
    }

    /// <summary>
    /// Creates a new chat session with a UUIDv7 identifier.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CreateNewSessionAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            var sessionId = Guid.CreateVersion7();
            var now = DateTime.UtcNow;

            CurrentSession = new Session
            {
                Id = sessionId,
                Name = now.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                CreatedAt = now,
                LastModifiedAt = now,
                History = [],
                InitialPrompt = string.Empty
            };

            await SaveCurrentSessionInternalAsync();

            _logger.LogInformation("Created new session with ID: {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create new session");
            throw;
        }
        finally
        {
            _ = _fileLock.Release();
        }
    }

    /// <summary>
    /// Loads an existing session by its identifier.
    /// </summary>
    /// <param name="sessionId">The session identifier (UUIDv7).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LoadSessionAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
        }

        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            throw new ArgumentException("Invalid session ID format", nameof(sessionId));
        }

        await _fileLock.WaitAsync();
        try
        {
            var sessionFilePath = GetSessionFilePath(sessionGuid);

            if (!File.Exists(sessionFilePath))
            {
                _logger.LogWarning("Session file not found: {SessionFilePath}", sessionFilePath);
                throw new FileNotFoundException($"Session file not found: {sessionId}");
            }

            var jsonContent = await File.ReadAllTextAsync(sessionFilePath);
            CurrentSession = JsonSerializer.Deserialize(jsonContent, SessionContext.Default.Session);

            if (CurrentSession == null)
            {
                _logger.LogError("Failed to deserialize session from file: {SessionFilePath}", sessionFilePath);
                await HandleCorruptedSessionAsync(sessionFilePath);

                // Create a new session without acquiring the lock again (we're already in the lock)
                var newSessionId = Guid.CreateVersion7();
                var now = DateTime.UtcNow;

                CurrentSession = new Session
                {
                    Id = newSessionId,
                    Name = now.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    CreatedAt = now,
                    LastModifiedAt = now,
                    History = [],
                    InitialPrompt = string.Empty
                };

                await SaveCurrentSessionInternalAsync();
                _logger.LogInformation("Created new session with ID: {newSessionId}", newSessionId);
                return;
            }

            _logger.LogInformation("Loaded session: {SessionId} with {MessageCount} messages",
                CurrentSession.Id, CurrentSession.History.Count);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Corrupted session file detected: {SessionId}", sessionId);
            var sessionFilePath = GetSessionFilePath(sessionGuid);
            await HandleCorruptedSessionAsync(sessionFilePath);

            // Create a new session without acquiring the lock again (we're already in the lock)
            var newSessionId = Guid.CreateVersion7();
            var now = DateTime.UtcNow;

            CurrentSession = new Session
            {
                Id = newSessionId,
                Name = now.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                CreatedAt = now,
                LastModifiedAt = now,
                History = [],
                InitialPrompt = string.Empty
            };

            await SaveCurrentSessionInternalAsync();
            _logger.LogInformation("Created new session with ID: {newSessionId}", newSessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load session: {SessionId}", sessionId);
            throw;
        }
        finally
        {
            _ = _fileLock.Release();
        }
    }

    /// <summary>
    /// Saves the current session to disk using atomic file operations.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SaveCurrentSessionAsync()
    {
        if (CurrentSession == null)
        {
            _logger.LogWarning("Attempted to save null session");
            return;
        }

        await _fileLock.WaitAsync();
        try
        {
            CurrentSession.LastModifiedAt = DateTime.UtcNow;
            await SaveCurrentSessionInternalAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save current session: {SessionId}", CurrentSession?.Id);
            throw;
        }
        finally
        {
            _ = _fileLock.Release();
        }
    }

    /// <summary>
    /// Clears the current session's history while preserving the session metadata.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ClearCurrentSessionAsync()
    {
        if (CurrentSession == null)
        {
            _logger.LogWarning("Attempted to clear null session");
            return;
        }

        await _fileLock.WaitAsync();
        try
        {
            CurrentSession.History.Clear();
            CurrentSession.InitialPrompt = string.Empty;
            CurrentSession.LastModifiedAt = DateTime.UtcNow;

            await SaveCurrentSessionInternalAsync();

            _logger.LogInformation("Cleared session history: {SessionId}", CurrentSession.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear session: {SessionId}", CurrentSession?.Id);
            throw;
        }
        finally
        {
            _ = _fileLock.Release();
        }
    }

    /// <summary>
    /// Lists all available sessions with their metadata.
    /// </summary>
    /// <returns>A list of session metadata objects.</returns>
    public async Task<List<Session>> ListSessionsAsync()
    {
        var sessions = new List<Session>();

        try
        {
            var sessionDirectories = Directory.GetDirectories(_sessionsPath);

            foreach (var sessionDir in sessionDirectories)
            {
                try
                {
                    var sessionFilePath = Path.Combine(sessionDir, "session.json");
                    if (!File.Exists(sessionFilePath))
                    {
                        continue;
                    }

                    var jsonContent = await File.ReadAllTextAsync(sessionFilePath);
                    var session = JsonSerializer.Deserialize(jsonContent, SessionContext.Default.Session);

                    if (session != null)
                    {
                        sessions.Add(session);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Skipping corrupted session directory: {SessionDir}", sessionDir);
                    // Continue processing other directories
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading session directory: {SessionDir}", sessionDir);
                    // Continue processing other directories
                }
            }

            // Sort by last modified date (most recent first)
            sessions.Sort((a, b) => b.LastModifiedAt.CompareTo(a.LastModifiedAt));

            _logger.LogDebug("Listed {SessionCount} sessions", sessions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list sessions from directory: {SessionsPath}", _sessionsPath);
            throw;
        }

        return sessions;
    }

    /// <summary>
    /// Renames the current session.
    /// </summary>
    /// <param name="newName">The new name for the session.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RenameSessionAsync(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("Session name cannot be empty", nameof(newName));
        }

        if (CurrentSession == null)
        {
            _logger.LogWarning("Attempted to rename null session");
            return;
        }

        await _fileLock.WaitAsync();
        try
        {
            var oldName = CurrentSession.Name;
            CurrentSession.Name = newName.Trim();
            CurrentSession.LastModifiedAt = DateTime.UtcNow;

            await SaveCurrentSessionInternalAsync();

            _logger.LogInformation("Renamed session {SessionId} from '{OldName}' to '{NewName}'",
                CurrentSession.Id, oldName, newName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rename session: {SessionId}", CurrentSession?.Id);
            throw;
        }
        finally
        {
            _ = _fileLock.Release();
        }
    }

    /// <summary>
    /// Adds a message to the current session and saves it.
    /// </summary>
    /// <param name="message">The chat message to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AddMessageToCurrentSessionAsync(ChatMessage message)
    {
        if (CurrentSession == null)
        {
            _logger.LogWarning("Attempted to add message to null session, creating new session");
            await CreateNewSessionAsync();
        }

        if (CurrentSession != null)
        {
            var messageIndex = CurrentSession.History.Count;
            var attachmentsDirectory = GetAttachmentsDirectoryPath(CurrentSession.Id);
            var serializableMessage = SerializableChatMessage.FromChatMessage(message, messageIndex, attachmentsDirectory);
            CurrentSession.History.Add(serializableMessage);

            // Set initial prompt if this is the first user message
            if (string.IsNullOrEmpty(CurrentSession.InitialPrompt) &&
                message.Role == ChatRole.User &&
                !string.IsNullOrWhiteSpace(message.Text))
            {
                CurrentSession.InitialPrompt = message.Text.Length > 50
                    ? message.Text[..50] + "..."
                    : message.Text;
            }

            await SaveCurrentSessionAsync();
        }
    }

    /// <summary>
    /// Internal method to save the current session with atomic file operations.
    /// Must be called within a file lock.
    /// </summary>
    private async Task SaveCurrentSessionInternalAsync()
    {
        if (CurrentSession == null)
        {
            return;
        }

        var sessionDirectoryPath = GetSessionDirectoryPath(CurrentSession.Id);
        var sessionFilePath = GetSessionFilePath(CurrentSession.Id);
        var tempFilePath = Path.Combine(sessionDirectoryPath, "session.tmp");

        try
        {
            // Ensure session directory exists
            _ = Directory.CreateDirectory(sessionDirectoryPath);

            // Serialize to JSON
            var jsonContent = JsonSerializer.Serialize(CurrentSession, SessionContext.Default.Session);

            // Write to temporary file first (atomic operation step 1)
            await File.WriteAllTextAsync(tempFilePath, jsonContent);

            // Replace original file with temporary file (atomic operation step 2)
            if (File.Exists(sessionFilePath))
            {
                File.Delete(sessionFilePath);
            }
            File.Move(tempFilePath, sessionFilePath);

            _logger.LogDebug("Saved session: {SessionId}", CurrentSession.Id);
        }
        catch (Exception)
        {
            // Clean up temporary file if it exists
            if (File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to clean up temporary file: {TempFilePath}", tempFilePath);
                }
            }
            throw;
        }
    }

    /// <summary>
    /// Handles corrupted session files by renaming them and logging the error.
    /// </summary>
    /// <param name="filePath">The path to the corrupted session file.</param>
    private async Task HandleCorruptedSessionAsync(string filePath)
    {
        try
        {
            var corruptedPath = Path.ChangeExtension(filePath, ".corrupted");

            // If a .corrupted file already exists, append a timestamp
            if (File.Exists(corruptedPath))
            {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                corruptedPath = Path.ChangeExtension(filePath, $".corrupted.{timestamp}");
            }

            File.Move(filePath, corruptedPath);

            _logger.LogError("Corrupted session file moved to: {CorruptedPath}", corruptedPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle corrupted session file: {FilePath}", filePath);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Converts the current session's history to a list of ChatMessage objects with attachments restored.
    /// </summary>
    /// <returns>A list of ChatMessage objects with attachments.</returns>
    public List<ChatMessage> GetCurrentSessionMessages()
    {
        if (CurrentSession == null)
        {
            return [];
        }

        var messages = new List<ChatMessage>();
        var attachmentsDirectory = GetAttachmentsDirectoryPath(CurrentSession.Id);

        foreach (var serializableMessage in CurrentSession.History)
        {
            var chatMessage = serializableMessage.ToChatMessage(attachmentsDirectory);
            messages.Add(chatMessage);
        }

        return messages;
    }

    /// <summary>
    /// Disposes of resources used by the SessionManager.
    /// </summary>
    public void Dispose()
    {
        _fileLock?.Dispose();
    }
}
