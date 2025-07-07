

using System.Text.Json;

namespace Mogzi.TUI.Tests;

/// <summary>
/// Requirement-based acceptance tests for session management functionality.
/// Tests the implementation of TOR-5.3.11 through TOR-5.3.15 (attachment handling requirements).
/// Uses real service configuration with no-mocking approach for black-box testing.
/// Focuses on directory-based session storage with attachment support.
/// </summary>
public class SessionManagementAcceptanceTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SessionManager _sessionManager;
    private readonly ILogger<SessionManagementAcceptanceTests> _logger;
    private readonly ITestOutputHelper? _output;
    private readonly string _testSessionsPath;
    private bool _disposed = false;

    public SessionManagementAcceptanceTests(ITestOutputHelper output)
    {
        _output = output;
        
        // Create a temporary directory for test sessions
        _testSessionsPath = Path.Combine(Path.GetTempPath(), "mogzi-test-sessions", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testSessionsPath);
        
        // Build service collection with real dependencies
        var services = new ServiceCollection();
        
        // Add logging for test visibility
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Create SessionManager with test logger
        var logger = _serviceProvider.GetRequiredService<ILogger<SessionManager>>();
        _sessionManager = new SessionManager(logger);
        
        _logger = _serviceProvider.GetRequiredService<ILogger<SessionManagementAcceptanceTests>>();
        
        _logger.LogInformation("SessionManagementAcceptanceTests initialized");
    }

    /// <summary>
    /// Tests TOR-5.3.11: The system SHALL support attachment handling for images, PDFs, and other file types within chat messages.
    /// Tests TOR-5.3.12: The system SHALL store attachments in a directory-based structure per session to maintain organization and prevent file conflicts.
    /// </summary>
    [Fact]
    public async Task AttachmentHandling_WithVariousFileTypes_CreatesDirectoryBasedStorage()
    {
        // Arrange
        _output?.WriteLine("🧪 Testing TOR-5.3.11 & TOR-5.3.12: Attachment handling with directory-based storage");
        _logger.LogInformation("Testing attachment handling with various file types");
        
        await _sessionManager.CreateNewSessionAsync();
        var session = _sessionManager.CurrentSession;
        Assert.NotNull(session);
        
        // Create test attachments of different types
        var imageBytes = Encoding.UTF8.GetBytes("fake-png-content");
        var pdfBytes = Encoding.UTF8.GetBytes("fake-pdf-content");
        var textBytes = Encoding.UTF8.GetBytes("Hello, this is a text file!");
        
        // Create ChatMessages with different attachment types
        var imageMessage = new ChatMessage(ChatRole.User, "Here's an image");
        imageMessage.Contents.Add(new TextContent("Here's an image"));
        imageMessage.Contents.Add(new DataContent(imageBytes, "image/png"));
        
        var pdfMessage = new ChatMessage(ChatRole.User, "Here's a PDF");
        pdfMessage.Contents.Add(new TextContent("Here's a PDF"));
        pdfMessage.Contents.Add(new DataContent(pdfBytes, "application/pdf"));
        
        var textMessage = new ChatMessage(ChatRole.User, "Here's a text file");
        textMessage.Contents.Add(new TextContent("Here's a text file"));
        textMessage.Contents.Add(new DataContent(textBytes, "text/plain"));
        
        // Act - Add messages with attachments
        await _sessionManager.AddMessageToCurrentSessionAsync(imageMessage);
        await _sessionManager.AddMessageToCurrentSessionAsync(pdfMessage);
        await _sessionManager.AddMessageToCurrentSessionAsync(textMessage);
        
        // Assert - Verify directory structure exists
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var sessionDir = Path.Combine(homeDirectory, ".mogzi", "chats", session.Id.ToString());
        var attachmentsDir = Path.Combine(sessionDir, "attachments");
        
        Directory.Exists(sessionDir).Should().BeTrue("session directory should exist"); // TOR-5.3.11, TOR-5.3.12
        Directory.Exists(attachmentsDir).Should().BeTrue("attachments directory should exist"); // TOR-5.3.11, TOR-5.3.12
        
        // Verify session.json exists
        var sessionFile = Path.Combine(sessionDir, "session.json");
        File.Exists(sessionFile).Should().BeTrue("session.json file should exist");
        
        // Verify attachment files exist
        var attachmentFiles = Directory.GetFiles(attachmentsDir);
        attachmentFiles.Should().HaveCount(3, "should have 3 attachment files");
        
        // Verify file naming pattern: {msg-index}-{content-index}-{hash}.{ext}
        var pngFiles = attachmentFiles.Where(f => f.EndsWith(".png")).ToArray();
        var pdfFiles = attachmentFiles.Where(f => f.EndsWith(".pdf")).ToArray();
        var txtFiles = attachmentFiles.Where(f => f.EndsWith(".txt")).ToArray();
        
        pngFiles.Should().HaveCount(1, "should have one PNG file");
        pdfFiles.Should().HaveCount(1, "should have one PDF file");
        txtFiles.Should().HaveCount(1, "should have one TXT file");
        
        // Verify files start with message index
        Path.GetFileName(pngFiles[0]).Should().StartWith("0-", "PNG should be from message index 0");
        Path.GetFileName(pdfFiles[0]).Should().StartWith("1-", "PDF should be from message index 1");
        Path.GetFileName(txtFiles[0]).Should().StartWith("2-", "TXT should be from message index 2");
        
        _output?.WriteLine("✅ TOR-5.3.11 & TOR-5.3.12: Directory-based attachment storage verified");
        _logger.LogInformation("Attachment handling test completed successfully");
    }

    /// <summary>
    /// Tests TOR-5.3.13: The system SHALL preserve attachment metadata including original filename, content type, and message association within the session data.
    /// </summary>
    [Fact]
    public async Task AttachmentMetadata_PreservesOriginalInformation_InSessionData()
    {
        // Arrange
        _output?.WriteLine("🧪 Testing TOR-5.3.13: Attachment metadata preservation");
        _logger.LogInformation("Testing attachment metadata preservation");
        
        await _sessionManager.CreateNewSessionAsync();
        var originalSessionId = _sessionManager.CurrentSession!.Id;
        
        var documentContent = Encoding.UTF8.GetBytes("Important document content");
        
        // Create a message with attachment
        var message = new ChatMessage(ChatRole.User, "Here's the document you requested");
        message.Contents.Add(new DataContent(documentContent, "application/pdf"));
        
        // Act - Add message and save session
        await _sessionManager.AddMessageToCurrentSessionAsync(message);
        
        // Reload session to verify persistence
        await _sessionManager.LoadSessionAsync(originalSessionId.ToString());
        var reloadedSession = _sessionManager.CurrentSession;
        
        // Assert - Verify metadata preservation
        reloadedSession.Should().NotBeNull("session should be loaded");
        reloadedSession!.History.Should().HaveCount(1, "should have one message");
        
        var reloadedMessage = reloadedSession.History[0];
        reloadedMessage.Content.Should().Be("Here's the document you requested", "message content should be preserved");
        reloadedMessage.Attachments.Should().HaveCount(1, "should have one attachment");
        
        var reloadedMetadata = reloadedMessage.Attachments[0];
        reloadedMetadata.OriginalFileName.Should().Be("attachment.pdf", "original filename should be preserved"); // TOR-5.3.13
        reloadedMetadata.MediaType.Should().Be("application/pdf", "media type should be preserved"); // TOR-5.3.13
        reloadedMetadata.MessageIndex.Should().Be(0, "message index should be preserved"); // TOR-5.3.13
        reloadedMetadata.SizeBytes.Should().Be(documentContent.Length, "file size should be preserved"); // TOR-5.3.13
        reloadedMetadata.ContentHash.Should().NotBeNullOrEmpty("content hash should be preserved"); // TOR-5.3.13
        reloadedMetadata.StoredFileName.Should().NotBeNullOrEmpty("stored filename should be preserved"); // TOR-5.3.13
        reloadedMetadata.StoredFileName.Should().EndWith(".pdf", "stored filename should have correct extension"); // TOR-5.3.13
        
        _output?.WriteLine("✅ TOR-5.3.13: Attachment metadata preservation verified");
        _logger.LogInformation("Attachment metadata preservation test completed successfully");
    }

    /// <summary>
    /// Tests TOR-5.3.14: The system SHALL use content-based hashing for attachment filenames to prevent duplicates and ensure data integrity.
    /// </summary>
    [Fact]
    public async Task ContentBasedHashing_PreventsDuplicates_EnsuresDataIntegrity()
    {
        // Arrange
        _output?.WriteLine("🧪 Testing TOR-5.3.14: Content-based hashing for deduplication");
        _logger.LogInformation("Testing content-based hashing and deduplication");
        
        await _sessionManager.CreateNewSessionAsync();
        var session = _sessionManager.CurrentSession!;
        
        var identicalContent = Encoding.UTF8.GetBytes("Identical content for deduplication test");
        
        // Act - Add the same content multiple times in different messages
        var message1 = new ChatMessage(ChatRole.User, "First message with file");
        message1.Contents.Add(new TextContent("First message with file"));
        message1.Contents.Add(new DataContent(identicalContent, "text/plain"));
        
        var message2 = new ChatMessage(ChatRole.User, "Second message with same file");
        message2.Contents.Add(new TextContent("Second message with same file"));
        message2.Contents.Add(new DataContent(identicalContent, "text/plain"));
        
        var message3 = new ChatMessage(ChatRole.User, "Third message with same file");
        message3.Contents.Add(new TextContent("Third message with same file"));
        message3.Contents.Add(new DataContent(identicalContent, "text/plain"));
        
        await _sessionManager.AddMessageToCurrentSessionAsync(message1);
        await _sessionManager.AddMessageToCurrentSessionAsync(message2);
        await _sessionManager.AddMessageToCurrentSessionAsync(message3);
        
        // Assert - Verify same content hash for identical content
        var reloadedSession = _sessionManager.CurrentSession!;
        reloadedSession.History.Should().HaveCount(3, "should have three messages");
        
        var attachment1 = reloadedSession.History[0].Attachments[0];
        var attachment2 = reloadedSession.History[1].Attachments[0];
        var attachment3 = reloadedSession.History[2].Attachments[0];
        
        attachment1.ContentHash.Should().Be(attachment2.ContentHash, "identical content should have same hash");
        attachment2.ContentHash.Should().Be(attachment3.ContentHash, "identical content should have same hash");
        
        // Verify hash is computed correctly (first 16 characters of SHA256)
        using var sha256 = SHA256.Create();
        var expectedFullHash = Convert.ToHexString(sha256.ComputeHash(identicalContent));
        var expectedShortHash = expectedFullHash[..16];
        
        attachment1.ContentHash.Should().Be(expectedShortHash, "hash should match first 16 chars of SHA256");
        
        // Verify content integrity by checking stored files
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var attachmentsDir = Path.Combine(homeDirectory, ".mogzi", "chats", session.Id.ToString(), "attachments");
        
        var file1Path = Path.Combine(attachmentsDir, attachment1.StoredFileName);
        var file2Path = Path.Combine(attachmentsDir, attachment2.StoredFileName);
        var file3Path = Path.Combine(attachmentsDir, attachment3.StoredFileName);
        
        File.Exists(file1Path).Should().BeTrue("first attachment file should exist");
        File.Exists(file2Path).Should().BeTrue("second attachment file should exist");
        File.Exists(file3Path).Should().BeTrue("third attachment file should exist");
        
        var content1 = await File.ReadAllBytesAsync(file1Path);
        var content2 = await File.ReadAllBytesAsync(file2Path);
        var content3 = await File.ReadAllBytesAsync(file3Path);
        
        content1.Should().BeEquivalentTo(identicalContent, "first file should contain original content");
        content2.Should().BeEquivalentTo(identicalContent, "second file should contain original content");
        content3.Should().BeEquivalentTo(identicalContent, "third file should contain original content");
        
        _output?.WriteLine("✅ TOR-5.3.14: Content-based hashing and deduplication verified");
        _logger.LogInformation("Content-based hashing test completed successfully");
    }

    /// <summary>
    /// Tests TOR-5.3.15: The system SHALL organize session storage using a directory structure with session metadata and attachments separated for efficient access.
    /// </summary>
    [Fact]
    public async Task DirectoryStructure_OrganizesSessionData_ForEfficientAccess()
    {
        // Arrange
        _output?.WriteLine("🧪 Testing TOR-5.3.15: Directory structure organization");
        _logger.LogInformation("Testing directory structure organization");
        
        await _sessionManager.CreateNewSessionAsync();
        var session = _sessionManager.CurrentSession!;
        
        // Add some session data
        await _sessionManager.RenameSessionAsync("Test Session with Attachments");
        
        var attachmentContent = Encoding.UTF8.GetBytes("Test file content for directory structure validation");
        
        // Add message with attachment
        var userMessage = new ChatMessage(ChatRole.User, "Hello, here's a file");
        userMessage.Contents.Add(new TextContent("Hello, here's a file"));
        userMessage.Contents.Add(new DataContent(attachmentContent, "text/plain"));
        
        var assistantMessage = new ChatMessage(ChatRole.Assistant, "I received your file, thank you!");
        assistantMessage.Contents.Add(new TextContent("I received your file, thank you!"));
        
        await _sessionManager.AddMessageToCurrentSessionAsync(userMessage);
        await _sessionManager.AddMessageToCurrentSessionAsync(assistantMessage);
        
        // Act & Assert - Verify directory structure
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var sessionDir = Path.Combine(homeDirectory, ".mogzi", "chats", session.Id.ToString());
        var sessionFile = Path.Combine(sessionDir, "session.json");
        var attachmentsDir = Path.Combine(sessionDir, "attachments");
        
        // Verify directory structure exists
        Directory.Exists(sessionDir).Should().BeTrue("session directory should exist");
        Directory.Exists(attachmentsDir).Should().BeTrue("attachments subdirectory should exist");
        File.Exists(sessionFile).Should().BeTrue("session.json file should exist");
        
        // Verify attachment files exist in attachments directory
        var attachmentFiles = Directory.GetFiles(attachmentsDir);
        attachmentFiles.Should().HaveCount(1, "should have one attachment file");
        
        var attachmentFile = attachmentFiles[0];
        File.Exists(attachmentFile).Should().BeTrue("attachment file should exist in attachments directory");
        
        // Verify session.json contains metadata but not attachment content
        var sessionJson = await File.ReadAllTextAsync(sessionFile);
        sessionJson.Should().Contain("Test Session with Attachments", "session JSON should contain session name");
        sessionJson.Should().Contain("attachment.txt", "session JSON should contain original filename");
        sessionJson.Should().Contain("text/plain", "session JSON should contain media type");
        sessionJson.Should().NotContain("Test file content for directory structure validation", 
            "session JSON should not contain actual file content");
        
        // Verify attachment file contains actual content
        var savedContent = await File.ReadAllBytesAsync(attachmentFile);
        savedContent.Should().BeEquivalentTo(attachmentContent, "attachment file should contain original content");
        
        // Verify efficient access - session metadata is separate from attachment data
        var sessionFileSize = new FileInfo(sessionFile).Length;
        var attachmentFileSize = new FileInfo(attachmentFile).Length;
        
        sessionFileSize.Should().BeLessThan(4096, "session file should be relatively small (metadata only)");
        attachmentFileSize.Should().Be(attachmentContent.Length, "attachment file should match content size");
        
        _output?.WriteLine("✅ TOR-5.3.15: Directory structure organization verified");
        _logger.LogInformation("Directory structure organization test completed successfully");
    }

    /// <summary>
    /// Tests SerializableChatMessage.FromChatMessage with attachment handling.
    /// Verifies that Microsoft.Extensions.AI ChatMessage with Contents is properly converted.
    /// </summary>
    [Fact]
    public async Task SerializableChatMessage_FromChatMessage_HandlesAttachments()
    {
        // Arrange
        _output?.WriteLine("🧪 Testing SerializableChatMessage.FromChatMessage with attachments");
        _logger.LogInformation("Testing ChatMessage conversion with attachments");
        
        await _sessionManager.CreateNewSessionAsync();
        var session = _sessionManager.CurrentSession!;
        
        // Create a ChatMessage with mixed content types
        var imageBytes = Encoding.UTF8.GetBytes("fake-image-data");
        var pdfBytes = Encoding.UTF8.GetBytes("fake-pdf-data");
        
        var chatMessage = new ChatMessage(ChatRole.User, "Here are some files for you");
        chatMessage.Contents.Add(new DataContent(imageBytes, "image/png"));
        chatMessage.Contents.Add(new DataContent(pdfBytes, "application/pdf"));
        
        // Act - Add message to session (this triggers SerializableChatMessage.FromChatMessage internally)
        await _sessionManager.AddMessageToCurrentSessionAsync(chatMessage);
        
        // Assert - Verify conversion
        var reloadedSession = _sessionManager.CurrentSession!;
        reloadedSession.History.Should().HaveCount(1, "should have one message");
        
        var serializable = reloadedSession.History[0];
        serializable.Role.Should().Be("user", "role should be converted to lowercase");
        serializable.Content.Should().Be("Here are some files for you", "text content should be extracted");
        serializable.Attachments.Should().HaveCount(2, "should have 2 attachments (image and PDF)");
        
        var imageAttachment = serializable.Attachments.FirstOrDefault(a => a.MediaType == "image/png");
        var pdfAttachment = serializable.Attachments.FirstOrDefault(a => a.MediaType == "application/pdf");
        
        imageAttachment.Should().NotBeNull("should have image attachment");
        pdfAttachment.Should().NotBeNull("should have PDF attachment");
        
        imageAttachment!.MessageIndex.Should().Be(0, "image attachment should have correct message index");
        pdfAttachment!.MessageIndex.Should().Be(0, "PDF attachment should have correct message index");
        
        imageAttachment.ContentIndex.Should().Be(1, "image should be content index 1 (after text)");
        pdfAttachment.ContentIndex.Should().Be(2, "PDF should be content index 2 (after image)");
        
        imageAttachment.OriginalFileName.Should().Be("attachment.png", "image should have PNG filename");
        pdfAttachment.OriginalFileName.Should().Be("attachment.pdf", "PDF should have PDF filename");
        
        _output?.WriteLine("✅ SerializableChatMessage.FromChatMessage with attachments verified");
        _logger.LogInformation("ChatMessage conversion test completed successfully");
    }

    /// <summary>
    /// Tests complete session lifecycle with attachments.
    /// Verifies create, save, load, and attachment retrieval workflow.
    /// </summary>
    [Fact]
    public async Task SessionLifecycle_WithAttachments_WorksEndToEnd()
    {
        // Arrange
        _output?.WriteLine("🧪 Testing complete session lifecycle with attachments");
        _logger.LogInformation("Testing end-to-end session lifecycle with attachments");
        
        // Create session and add messages with attachments
        await _sessionManager.CreateNewSessionAsync();
        var originalSessionId = _sessionManager.CurrentSession!.Id;
        
        var fileContent = Encoding.UTF8.GetBytes("Important document content for lifecycle test");
        
        var message = new ChatMessage(ChatRole.User, "Here's the document for the lifecycle test");
        message.Contents.Add(new DataContent(fileContent, "text/plain"));
        
        await _sessionManager.AddMessageToCurrentSessionAsync(message);
        
        // Act - Load session in new SessionManager instance (simulates app restart)
        var newLogger = _serviceProvider.GetRequiredService<ILogger<SessionManager>>();
        var newSessionManager = new SessionManager(newLogger);
        await newSessionManager.LoadSessionAsync(originalSessionId.ToString());
        
        // Assert - Verify session loaded correctly
        var loadedSession = newSessionManager.CurrentSession;
        loadedSession.Should().NotBeNull("session should be loaded");
        loadedSession!.Id.Should().Be(originalSessionId, "loaded session should have same ID");
        loadedSession.History.Should().HaveCount(1, "loaded session should have one message");
        
        var loadedMessage = loadedSession.History[0];
        loadedMessage.Content.Should().Be("Here's the document for the lifecycle test", "message content should be preserved");
        loadedMessage.Attachments.Should().HaveCount(1, "message should have one attachment");
        
        var loadedMetadata = loadedMessage.Attachments[0];
        loadedMetadata.OriginalFileName.Should().Be("attachment.txt", "original filename should be preserved");
        loadedMetadata.MediaType.Should().Be("text/plain", "media type should be preserved");
        
        // Verify attachment content can be retrieved using ToChatMessage
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var attachmentsDirectory = Path.Combine(homeDirectory, ".mogzi", "chats", originalSessionId.ToString(), "attachments");
        
        var restoredMessage = loadedMessage.ToChatMessage(attachmentsDirectory);
        restoredMessage.Contents.Should().HaveCount(2, "restored message should have text and data content");
        
        var dataContent = restoredMessage.Contents.OfType<DataContent>().FirstOrDefault();
        dataContent.Should().NotBeNull("should have data content");
        dataContent!.Data.ToArray().Should().BeEquivalentTo(fileContent, "retrieved content should match original");
        
        _output?.WriteLine("✅ Complete session lifecycle with attachments verified");
        _logger.LogInformation("Session lifecycle test completed successfully");
    }

    /// <summary>
    /// Tests error handling for corrupted session files.
    /// Verifies proper exception handling when session files are corrupted.
    /// </summary>
    [Fact]
    public async Task SessionLoading_WithCorruptedFile_HandlesGracefully()
    {
        // Arrange
        _output?.WriteLine("🧪 Testing error handling for corrupted session files");
        _logger.LogInformation("Testing error handling for corrupted sessions");

        _output?.WriteLine("📝 Step 1: Creating a valid session first...");
        // Create a valid session first
        await _sessionManager.CreateNewSessionAsync();
        var sessionId = _sessionManager.CurrentSession!.Id;
        _output?.WriteLine($"📝 Step 1 Complete: Created session {sessionId}");

        _output?.WriteLine("📝 Step 2: Corrupting the session file...");
        // Corrupt the session file
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var sessionFile = Path.Combine(homeDirectory, ".mogzi", "chats", sessionId.ToString(), "session.json");
        await File.WriteAllTextAsync(sessionFile, "{ invalid json content");
        _output?.WriteLine($"📝 Step 2 Complete: Corrupted file at {sessionFile}");

        _output?.WriteLine("📝 Step 3: Attempting to load corrupted session...");
        // Act & Assert - Verify graceful handling of corrupted file
        var startTime = DateTimeOffset.UtcNow;
        await _sessionManager.LoadSessionAsync(sessionId.ToString());
        var loadTime = DateTimeOffset.UtcNow - startTime;
        _output?.WriteLine($"📝 Step 3 Complete: Load operation took {loadTime.TotalMilliseconds}ms");

        _output?.WriteLine("📝 Step 4: Verifying new session was created...");
        // Should create a new session instead of crashing
        var currentSession = _sessionManager.CurrentSession;
        currentSession.Should().NotBeNull("should have created a new session");
        currentSession!.Id.Should().NotBe(sessionId, "should be a different session ID");
        _output?.WriteLine($"📝 Step 4 Complete: New session {currentSession.Id} created");

        _output?.WriteLine("📝 Step 5: Verifying corrupted file was renamed...");
        // Verify corrupted file was renamed
        var corruptedFile = Path.ChangeExtension(sessionFile, ".corrupted");
        File.Exists(corruptedFile).Should().BeTrue("corrupted file should be renamed");
        _output?.WriteLine($"📝 Step 5 Complete: Corrupted file renamed to {corruptedFile}");

        _output?.WriteLine("✅ Error handling for corrupted sessions verified");
        _logger.LogInformation("Corrupted session error handling test completed successfully");
    }

    /// <summary>
    /// Tests TOR-5.3.1: On startup, if no session is specified via command-line arguments, the system SHALL create a new chat session. 
    /// The session file SHALL be saved in the `~/.mogzi/chats/` directory with a UUIDv7 timestamp as its filename.
    /// </summary>
    [Fact]
    public async Task NewSessionCreation_OnStartup_CreatesUUIDv7InCorrectDirectory()
    {
        // Arrange
        _output?.WriteLine("🧪 Testing TOR-5.3.1: New session creation with UUIDv7 in ~/.mogzi/chats/");
        _logger.LogInformation("Testing new session creation");
        
        // Act - Create new session (simulates startup behavior)
        await _sessionManager.CreateNewSessionAsync();
        var session = _sessionManager.CurrentSession;
        
        // Assert - Verify session creation
        session.Should().NotBeNull("session should be created");
        
        // Verify UUIDv7 format (starts with timestamp)
        var sessionIdString = session!.Id.ToString();
        sessionIdString.Should().MatchRegex(@"^[0-9a-f]{8}-[0-9a-f]{4}-7[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$", 
            "session ID should be valid UUIDv7 format");
        
        // Verify directory structure
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var expectedSessionDir = Path.Combine(homeDirectory, ".mogzi", "chats", sessionIdString);
        var expectedSessionFile = Path.Combine(expectedSessionDir, "session.json");
        
        Directory.Exists(expectedSessionDir).Should().BeTrue("session directory should exist in ~/.mogzi/chats/"); // TOR-5.3.1
        File.Exists(expectedSessionFile).Should().BeTrue("session.json should exist"); // TOR-5.3.1
        
        // Verify UUIDv7 timestamp ordering (created recently)
        var creationTime = session.CreatedAt;
        var timeDifference = DateTimeOffset.UtcNow - creationTime;
        timeDifference.Should().BeLessThan(TimeSpan.FromSeconds(5), "session should be created recently");
        
        _output?.WriteLine("✅ TOR-5.3.1: New session creation with UUIDv7 verified");
        _logger.LogInformation("New session creation test completed successfully");
    }

    /// <summary>
    /// Tests TOR-5.3.4: The system SHALL allow a user to load a specific session by its name via a command-line argument, 
    /// which will resume the conversation from where it was left off.
    /// </summary>
    [Fact]
    public async Task SessionLoading_BySessionId_ResumesConversation()
    {
        // Arrange
        _output?.WriteLine("🧪 Testing TOR-5.3.4: Load specific session by ID");
        _logger.LogInformation("Testing session loading by ID");
        
        // Create a session with some conversation history
        await _sessionManager.CreateNewSessionAsync();
        var originalSessionId = _sessionManager.CurrentSession!.Id;
        
        var userMessage = new ChatMessage(ChatRole.User, "Hello, this is a test conversation");
        var assistantMessage = new ChatMessage(ChatRole.Assistant, "I understand, this is a test response");
        
        await _sessionManager.AddMessageToCurrentSessionAsync(userMessage);
        await _sessionManager.AddMessageToCurrentSessionAsync(assistantMessage);
        
        // Act - Load session by ID (simulates --session <session_id> command line argument)
        var newLogger = _serviceProvider.GetRequiredService<ILogger<SessionManager>>();
        var newSessionManager = new SessionManager(newLogger);
        await newSessionManager.LoadSessionAsync(originalSessionId.ToString());
        
        // Assert - Verify conversation resumed
        var loadedSession = newSessionManager.CurrentSession;
        loadedSession.Should().NotBeNull("session should be loaded");
        loadedSession!.Id.Should().Be(originalSessionId, "loaded session should have same ID");
        loadedSession.History.Should().HaveCount(2, "conversation should be resumed with all messages");
        
        loadedSession.History[0].Content.Should().Be("Hello, this is a test conversation", "first message should be preserved");
        loadedSession.History[0].Role.Should().Be("user", "first message role should be preserved");
        
        loadedSession.History[1].Content.Should().Be("I understand, this is a test response", "second message should be preserved");
        loadedSession.History[1].Role.Should().Be("assistant", "second message role should be preserved");
        
        _output?.WriteLine("✅ TOR-5.3.4: Session loading by ID verified");
        _logger.LogInformation("Session loading test completed successfully");
    }

    /// <summary>
    /// Tests TOR-5.3.7: The session history SHALL be stored in a human-readable JSON format to facilitate debugging and manual inspection.
    /// </summary>
    [Fact]
    public async Task SessionStorage_InJSONFormat_IsHumanReadable()
    {
        // Arrange
        _output?.WriteLine("🧪 Testing TOR-5.3.7: Human-readable JSON format storage");
        _logger.LogInformation("Testing JSON format storage");
        
        await _sessionManager.CreateNewSessionAsync();
        var session = _sessionManager.CurrentSession!;
        
        await _sessionManager.RenameSessionAsync("Test Session for JSON Verification");
        
        var message = new ChatMessage(ChatRole.User, "This is a test message for JSON format verification");
        message.Contents.Add(new TextContent("This is a test message for JSON format verification"));
        
        await _sessionManager.AddMessageToCurrentSessionAsync(message);
        
        // Act - Read the session file directly
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var sessionFile = Path.Combine(homeDirectory, ".mogzi", "chats", session.Id.ToString(), "session.json");
        var jsonContent = await File.ReadAllTextAsync(sessionFile);
        
        // Assert - Verify JSON format and human readability
        jsonContent.Should().NotBeNullOrEmpty("session file should contain JSON content");
        
        // Verify it's valid JSON by parsing it
        var sessionData = JsonSerializer.Deserialize<JsonElement>(jsonContent);
        sessionData.ValueKind.Should().Be(JsonValueKind.Object, "should be valid JSON object");
        
        // Verify human-readable structure (contains expected fields)
        jsonContent.Should().Contain("\"id\":", "JSON should contain session ID field");
        jsonContent.Should().Contain("\"name\":", "JSON should contain session name field");
        jsonContent.Should().Contain("\"createdAt\":", "JSON should contain creation timestamp");
        jsonContent.Should().Contain("\"lastModifiedAt\":", "JSON should contain last modified timestamp");
        jsonContent.Should().Contain("\"history\":", "JSON should contain message history");
        jsonContent.Should().Contain("Test Session for JSON Verification", "JSON should contain session name");
        jsonContent.Should().Contain("This is a test message for JSON format verification", "JSON should contain message content");
        
        // Verify formatting (should be indented for readability)
        jsonContent.Should().Contain("  ", "JSON should be indented for human readability");
        jsonContent.Should().Contain("\n", "JSON should have line breaks for readability");
        
        _output?.WriteLine("✅ TOR-5.3.7: Human-readable JSON format verified");
        _logger.LogInformation("JSON format storage test completed successfully");
    }

    /// <summary>
    /// Tests TOR-5.3.10: The system SHALL allow users to assign custom names to sessions for easier identification and management.
    /// </summary>
    [Fact]
    public async Task SessionNaming_CustomNames_EnablesEasierIdentification()
    {
        // Arrange
        _output?.WriteLine("🧪 Testing TOR-5.3.10: Custom session names for easier identification");
        _logger.LogInformation("Testing custom session naming");
        
        await _sessionManager.CreateNewSessionAsync();
        var session = _sessionManager.CurrentSession!;
        
        // Act - Assign custom name
        var customName = "My Important Project Discussion";
        await _sessionManager.RenameSessionAsync(customName);
        
        // Assert - Verify custom name is set
        var updatedSession = _sessionManager.CurrentSession!;
        updatedSession.Name.Should().Be(customName, "session should have custom name");
        
        // Verify name persists after reload
        var sessionId = updatedSession.Id;
        var newLogger = _serviceProvider.GetRequiredService<ILogger<SessionManager>>();
        var newSessionManager = new SessionManager(newLogger);
        await newSessionManager.LoadSessionAsync(sessionId.ToString());
        
        var reloadedSession = newSessionManager.CurrentSession!;
        reloadedSession.Name.Should().Be(customName, "custom name should persist after reload");
        
        // Verify name is stored in JSON file
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var sessionFile = Path.Combine(homeDirectory, ".mogzi", "chats", sessionId.ToString(), "session.json");
        var jsonContent = await File.ReadAllTextAsync(sessionFile);
        jsonContent.Should().Contain(customName, "custom name should be stored in JSON file");
        
        _output?.WriteLine("✅ TOR-5.3.10: Custom session naming verified");
        _logger.LogInformation("Custom session naming test completed successfully");
    }

    /// <summary>
    /// Tests session loading by user-friendly name with case-insensitive matching.
    /// Verifies that sessions can be loaded using their custom names instead of GUIDs.
    /// </summary>
    [Fact]
    public async Task SessionLoading_ByName_CaseInsensitiveMatching()
    {
        // Arrange
        _output?.WriteLine("🧪 Testing session loading by name with case-insensitive matching");
        _logger.LogInformation("Testing session loading by name");
        
        // Create session with custom name
        await _sessionManager.CreateNewSessionAsync();
        var originalSessionId = _sessionManager.CurrentSession!.Id;
        var sessionName = "Bug Investigation Session";
        await _sessionManager.RenameSessionAsync(sessionName);
        
        // Add some conversation history
        var userMessage = new ChatMessage(ChatRole.User, "I found a bug in the authentication system");
        var assistantMessage = new ChatMessage(ChatRole.Assistant, "Let me help you investigate this bug");
        
        await _sessionManager.AddMessageToCurrentSessionAsync(userMessage);
        await _sessionManager.AddMessageToCurrentSessionAsync(assistantMessage);
        
        // Act - Load session by name with different case variations
        var testCases = new[]
        {
            "Bug Investigation Session",     // Exact match
            "bug investigation session",     // All lowercase
            "BUG INVESTIGATION SESSION",     // All uppercase
            "Bug investigation Session",     // Mixed case
            "bug Investigation session"      // Different mixed case
        };
        
        foreach (var testName in testCases)
        {
            _output?.WriteLine($"📝 Testing case variation: '{testName}'");
            
            var newLogger = _serviceProvider.GetRequiredService<ILogger<SessionManager>>();
            var newSessionManager = new SessionManager(newLogger);
            
            // Assert - Verify session loads successfully by name
            var loadResult = await newSessionManager.TryLoadSessionByNameAsync(testName);
            loadResult.Should().BeTrue($"session should load with name variation: '{testName}'");
            
            var loadedSession = newSessionManager.CurrentSession;
            loadedSession.Should().NotBeNull($"session should be loaded for name: '{testName}'");
            loadedSession!.Id.Should().Be(originalSessionId, $"loaded session should have correct ID for name: '{testName}'");
            loadedSession.Name.Should().Be(sessionName, $"loaded session should have original name for variation: '{testName}'");
            loadedSession.History.Should().HaveCount(2, $"conversation should be preserved for name: '{testName}'");
        }
        
        _output?.WriteLine("✅ Session loading by name with case-insensitive matching verified");
        _logger.LogInformation("Session loading by name test completed successfully");
    }

    /// <summary>
    /// Tests session loading by name when multiple sessions have the same name.
    /// Verifies that the most recent session is loaded when there are name collisions.
    /// </summary>
    [Fact]
    public async Task SessionLoading_ByName_ReturnsLatestWhenMultipleMatches()
    {
        // Arrange
        _output?.WriteLine("🧪 Testing session loading by name with multiple matches (returns latest)");
        _logger.LogInformation("Testing session loading with name collisions");
        
        var sessionName = "Daily Standup Notes";
        var sessionIds = new List<Guid>();
        
        // Create multiple sessions with the same name at different times
        for (int i = 0; i < 3; i++)
        {
            await _sessionManager.CreateNewSessionAsync();
            var sessionId = _sessionManager.CurrentSession!.Id;
            sessionIds.Add(sessionId);
            
            await _sessionManager.RenameSessionAsync(sessionName);
            
            // Add unique content to identify each session
            var message = new ChatMessage(ChatRole.User, $"This is session number {i + 1}");
            await _sessionManager.AddMessageToCurrentSessionAsync(message);
            
            // Ensure different timestamps
            await Task.Delay(100);
        }
        
        // The last created session should be the most recent
        var expectedLatestSessionId = sessionIds.Last();
        
        // Act - Load session by name
        var newLogger = _serviceProvider.GetRequiredService<ILogger<SessionManager>>();
        var newSessionManager = new SessionManager(newLogger);
        var loadResult = await newSessionManager.TryLoadSessionByNameAsync(sessionName);
        
        // Assert - Verify most recent session is loaded
        loadResult.Should().BeTrue("session should load successfully by name");
        
        var loadedSession = newSessionManager.CurrentSession;
        loadedSession.Should().NotBeNull("session should be loaded");
        loadedSession!.Id.Should().Be(expectedLatestSessionId, "should load the most recent session with matching name");
        loadedSession.Name.Should().Be(sessionName, "loaded session should have correct name");
        loadedSession.History.Should().HaveCount(1, "loaded session should have one message");
        loadedSession.History[0].Content.Should().Be("This is session number 3", "should load the latest session content");
        
        _output?.WriteLine("✅ Session loading by name returns latest when multiple matches verified");
        _logger.LogInformation("Session loading with name collisions test completed successfully");
    }

    /// <summary>
    /// Tests session loading by name when no matching session exists.
    /// Verifies that the method returns false when no session matches the provided name.
    /// </summary>
    [Fact]
    public async Task SessionLoading_ByName_ReturnsFalseWhenNoMatch()
    {
        // Arrange
        _output?.WriteLine("🧪 Testing session loading by name when no match exists");
        _logger.LogInformation("Testing session loading with non-existent name");
        
        // Create some sessions with different names
        var sessionNames = new[] { "Project Alpha", "Bug Fixes", "Feature Development" };
        
        foreach (var name in sessionNames)
        {
            await _sessionManager.CreateNewSessionAsync();
            await _sessionManager.RenameSessionAsync(name);
        }
        
        // Act - Try to load session with non-existent name
        var newLogger = _serviceProvider.GetRequiredService<ILogger<SessionManager>>();
        var newSessionManager = new SessionManager(newLogger);
        var loadResult = await newSessionManager.TryLoadSessionByNameAsync("Non-Existent Session");
        
        // Assert - Verify load fails gracefully
        loadResult.Should().BeFalse("should return false when no session matches the name");
        newSessionManager.CurrentSession.Should().BeNull("current session should remain null when load fails");
        
        // Test with empty/null names
        var emptyNameResult = await newSessionManager.TryLoadSessionByNameAsync("");
        emptyNameResult.Should().BeFalse("should return false for empty name");
        
        var nullNameResult = await newSessionManager.TryLoadSessionByNameAsync(null!);
        nullNameResult.Should().BeFalse("should return false for null name");
        
        var whitespaceNameResult = await newSessionManager.TryLoadSessionByNameAsync("   ");
        whitespaceNameResult.Should().BeFalse("should return false for whitespace-only name");
        
        _output?.WriteLine("✅ Session loading by name returns false when no match verified");
        _logger.LogInformation("Session loading with non-existent name test completed successfully");
    }

    /// <summary>
    /// Tests end-to-end session loading workflow combining GUID and name-based loading.
    /// Simulates the complete CLI argument processing workflow.
    /// </summary>
    [Fact]
    public async Task SessionLoading_EndToEnd_SupportsGuidAndNameWorkflow()
    {
        // Arrange
        _output?.WriteLine("🧪 Testing end-to-end session loading workflow (GUID and name)");
        _logger.LogInformation("Testing complete session loading workflow");
        
        // Create session with custom name
        await _sessionManager.CreateNewSessionAsync();
        var sessionId = _sessionManager.CurrentSession!.Id;
        var sessionName = "Integration Test Session";
        await _sessionManager.RenameSessionAsync(sessionName);
        
        // Add conversation history
        var userMessage = new ChatMessage(ChatRole.User, "Testing the complete workflow");
        await _sessionManager.AddMessageToCurrentSessionAsync(userMessage);
        
        // Test Case 1: Load by GUID (existing behavior)
        _output?.WriteLine("📝 Test Case 1: Loading by GUID");
        var guidLogger = _serviceProvider.GetRequiredService<ILogger<SessionManager>>();
        var guidSessionManager = new SessionManager(guidLogger);
        
        // Simulate GUID parsing and loading
        var sessionIdString = sessionId.ToString();
        if (Guid.TryParse(sessionIdString, out _))
        {
            await guidSessionManager.LoadSessionAsync(sessionIdString);
        }
        
        guidSessionManager.CurrentSession.Should().NotBeNull("session should load by GUID");
        guidSessionManager.CurrentSession!.Id.Should().Be(sessionId, "loaded session should have correct ID");
        guidSessionManager.CurrentSession.Name.Should().Be(sessionName, "loaded session should have correct name");
        
        // Test Case 2: Load by name (new functionality)
        _output?.WriteLine("📝 Test Case 2: Loading by name");
        var nameLogger = _serviceProvider.GetRequiredService<ILogger<SessionManager>>();
        var nameSessionManager = new SessionManager(nameLogger);
        
        // Simulate name-based loading (when GUID parsing fails)
        var sessionIdentifier = sessionName;
        var sessionLoaded = false;
        
        if (Guid.TryParse(sessionIdentifier, out _))
        {
            // This would be the GUID path, but our identifier is a name
            sessionLoaded = false;
        }
        
        if (!sessionLoaded)
        {
            sessionLoaded = await nameSessionManager.TryLoadSessionByNameAsync(sessionIdentifier);
        }
        
        sessionLoaded.Should().BeTrue("session should load by name");
        nameSessionManager.CurrentSession.Should().NotBeNull("session should be loaded by name");
        nameSessionManager.CurrentSession!.Id.Should().Be(sessionId, "loaded session should have correct ID");
        nameSessionManager.CurrentSession.Name.Should().Be(sessionName, "loaded session should have correct name");
        
        // Test Case 3: Invalid identifier (neither GUID nor existing name)
        _output?.WriteLine("📝 Test Case 3: Invalid identifier");
        var invalidLogger = _serviceProvider.GetRequiredService<ILogger<SessionManager>>();
        var invalidSessionManager = new SessionManager(invalidLogger);
        
        var invalidIdentifier = "Non-Existent Session Name";
        var invalidSessionLoaded = false;
        
        if (Guid.TryParse(invalidIdentifier, out _))
        {
            // Would try GUID loading, but this isn't a GUID
            invalidSessionLoaded = false;
        }
        
        if (!invalidSessionLoaded)
        {
            invalidSessionLoaded = await invalidSessionManager.TryLoadSessionByNameAsync(invalidIdentifier);
        }
        
        invalidSessionLoaded.Should().BeFalse("should not load non-existent session");
        invalidSessionManager.CurrentSession.Should().BeNull("current session should remain null");
        
        _output?.WriteLine("✅ End-to-end session loading workflow verified");
        _logger.LogInformation("Complete session loading workflow test completed successfully");
    }

    /// <summary>
    /// Tests session persistence and data integrity across multiple operations.
    /// Verifies that session data remains consistent through create, modify, save, and load operations.
    /// </summary>
    [Fact]
    public async Task SessionPersistence_AcrossMultipleOperations_MaintainsDataIntegrity()
    {
        // Arrange
        _output?.WriteLine("🧪 Testing session persistence and data integrity");
        _logger.LogInformation("Testing session persistence across multiple operations");
        
        // Create session with initial data
        await _sessionManager.CreateNewSessionAsync();
        var originalSessionId = _sessionManager.CurrentSession!.Id;
        var originalCreatedAt = _sessionManager.CurrentSession.CreatedAt;
        
        await _sessionManager.RenameSessionAsync("Persistence Test Session");
        
        // Add multiple messages
        var messages = new[]
        {
            new ChatMessage(ChatRole.User, "First user message"),
            new ChatMessage(ChatRole.Assistant, "First assistant response"),
            new ChatMessage(ChatRole.User, "Second user message"),
            new ChatMessage(ChatRole.Assistant, "Second assistant response")
        };
        
        foreach (var msg in messages)
        {
            msg.Contents.Add(new TextContent(msg.Text ?? ""));
            await _sessionManager.AddMessageToCurrentSessionAsync(msg);
            
            // Small delay to ensure different timestamps
            await Task.Delay(10);
        }
        
        var lastModifiedAfterMessages = _sessionManager.CurrentSession.LastModifiedAt;
        
        // Act - Reload session multiple times
        for (int i = 0; i < 3; i++)
        {
            var newLogger = _serviceProvider.GetRequiredService<ILogger<SessionManager>>();
            var newSessionManager = new SessionManager(newLogger);
            await newSessionManager.LoadSessionAsync(originalSessionId.ToString());
            
            // Assert - Verify data integrity
            var reloadedSession = newSessionManager.CurrentSession!;
            reloadedSession.Id.Should().Be(originalSessionId, $"session ID should be preserved (iteration {i + 1})");
            reloadedSession.Name.Should().Be("Persistence Test Session", $"session name should be preserved (iteration {i + 1})");
            reloadedSession.CreatedAt.Should().Be(originalCreatedAt, $"creation time should be preserved (iteration {i + 1})");
            reloadedSession.LastModifiedAt.Should().Be(lastModifiedAfterMessages, $"last modified time should be preserved (iteration {i + 1})");
            reloadedSession.History.Should().HaveCount(4, $"message count should be preserved (iteration {i + 1})");
            
            // Verify message content integrity
            for (int j = 0; j < messages.Length; j++)
            {
                reloadedSession.History[j].Content.Should().Be(messages[j].Text, 
                    $"message {j} content should be preserved (iteration {i + 1})");
                reloadedSession.History[j].Role.Should().Be(messages[j].Role.ToString().ToLowerInvariant(), 
                    $"message {j} role should be preserved (iteration {i + 1})");
            }
        }
        
        _output?.WriteLine("✅ Session persistence and data integrity verified");
        _logger.LogInformation("Session persistence test completed successfully");
    }

    /// <summary>
    /// Tests session file structure and organization for efficient access.
    /// Verifies that the directory structure supports efficient session management operations.
    /// </summary>
    [Fact]
    public async Task SessionFileStructure_SupportsEfficientAccess_AndOrganization()
    {
        // Arrange
        _output?.WriteLine("🧪 Testing session file structure for efficient access");
        _logger.LogInformation("Testing session file structure and organization");
        
        // Create multiple sessions to test organization
        var sessionIds = new List<Guid>();
        
        for (int i = 0; i < 3; i++)
        {
            await _sessionManager.CreateNewSessionAsync();
            var sessionId = _sessionManager.CurrentSession!.Id;
            sessionIds.Add(sessionId);
            
            await _sessionManager.RenameSessionAsync($"Test Session {i + 1}");
            
            var message = new ChatMessage(ChatRole.User, $"Message for session {i + 1}");
            message.Contents.Add(new TextContent($"Message for session {i + 1}"));
            await _sessionManager.AddMessageToCurrentSessionAsync(message);
        }
        
        // Act & Assert - Verify directory structure
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var chatsDirectory = Path.Combine(homeDirectory, ".mogzi", "chats");
        
        Directory.Exists(chatsDirectory).Should().BeTrue("chats directory should exist");
        
        // Verify each session has its own directory
        foreach (var sessionId in sessionIds)
        {
            var sessionDir = Path.Combine(chatsDirectory, sessionId.ToString());
            var sessionFile = Path.Combine(sessionDir, "session.json");
            
            Directory.Exists(sessionDir).Should().BeTrue($"session directory should exist for {sessionId}");
            File.Exists(sessionFile).Should().BeTrue($"session.json should exist for {sessionId}");
            
            // Verify session file is reasonably sized (not empty, not huge)
            var fileInfo = new FileInfo(sessionFile);
            fileInfo.Length.Should().BeGreaterThan(0, "session file should not be empty");
            fileInfo.Length.Should().BeLessThan(10240, "session file should be reasonably sized for metadata");
        }
        
        // Verify sessions can be efficiently accessed by ID
        foreach (var sessionId in sessionIds)
        {
            var newLogger = _serviceProvider.GetRequiredService<ILogger<SessionManager>>();
            var newSessionManager = new SessionManager(newLogger);
            
            var startTime = DateTimeOffset.UtcNow;
            await newSessionManager.LoadSessionAsync(sessionId.ToString());
            var loadTime = DateTimeOffset.UtcNow - startTime;
            
            loadTime.Should().BeLessThan(TimeSpan.FromSeconds(1), "session loading should be efficient");
            newSessionManager.CurrentSession.Should().NotBeNull($"session {sessionId} should load successfully");
        }
        
        _output?.WriteLine("✅ Session file structure and efficient access verified");
        _logger.LogInformation("Session file structure test completed successfully");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        
        try
        {
            _sessionManager?.Dispose();
            
            if (_serviceProvider is IDisposable disposableProvider)
            {
                disposableProvider.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error during test cleanup");
        }
        
        GC.SuppressFinalize(this);
    }
}
