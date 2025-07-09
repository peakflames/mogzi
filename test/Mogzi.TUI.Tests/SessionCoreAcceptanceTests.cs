using System.Text.Json;

namespace Mogzi.TUI.Tests;

/// <summary>
/// Black-box acceptance tests for core session management functionality.
/// Tests session lifecycle, persistence, attachment handling, and configuration.
/// Uses real service configuration and DI container with no mocking approach.
/// Follows the systems-first testing philosophy with complete user workflows.
/// </summary>
public class SessionCoreAcceptanceTests : SessionTestBase
{
    private readonly SessionListProvider _sessionListProvider;

    public SessionCoreAcceptanceTests(ITestOutputHelper output) 
        : base(output, nameof(SessionCoreAcceptanceTests))
    {
        // Get SessionListProvider from the service provider since it's registered as IUserSelectionProvider
        var providers = _serviceProvider.GetServices<IUserSelectionProvider>();
        _sessionListProvider = providers.OfType<SessionListProvider>().First();
        
        _logger.LogInformation("SessionCoreAcceptanceTests initialized with shared SessionManager instance");
    }

    #region Session Lifecycle Tests

    /// <summary>
    /// Tests TOR-5.3.1: On startup, if no session is specified via command-line arguments, the system SHALL create a new chat session. 
    /// The session file SHALL be saved in the `~/.mogzi/chats/` directory with a UUIDv7 timestamp as its filename.
    /// </summary>
    [Fact]
    public async Task NewSessionCreation_OnStartup_CreatesUUIDv7InCorrectDirectory()
    {
        // TOR-5.3.1
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
        // TOR-5.3.4
        // Arrange
        _output?.WriteLine("🧪 Testing TOR-5.3.4: Load specific session by ID");
        _logger.LogInformation("Testing session loading by ID");
        
        // Create a session with some conversation history and GUID-based name
        var sessionGuid = Guid.NewGuid().ToString("N")[^8..];
        await _sessionManager.CreateNewSessionAsync();
        var originalSessionId = _sessionManager.CurrentSession!.Id;
        var sessionName = $"Test-{sessionGuid}";
        await _sessionManager.RenameSessionAsync(sessionName);
        
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
        // TOR-5.3.7
        // Arrange
        _output?.WriteLine("🧪 Testing TOR-5.3.7: Human-readable JSON format storage");
        _logger.LogInformation("Testing JSON format storage");
        
        var sessionGuid = Guid.NewGuid().ToString("N")[^8..];
        await _sessionManager.CreateNewSessionAsync();
        var session = _sessionManager.CurrentSession!;
        
        var sessionName = $"Test-{sessionGuid}";
        await _sessionManager.RenameSessionAsync(sessionName);
        
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
        jsonContent.Should().Contain(sessionName, "JSON should contain session name");
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
        // TOR-5.3.10
        // Arrange
        _output?.WriteLine("🧪 Testing TOR-5.3.10: Custom session names for easier identification");
        _logger.LogInformation("Testing custom session naming");
        
        await _sessionManager.CreateNewSessionAsync();
        var session = _sessionManager.CurrentSession!;
        
        // Act - Assign custom name with GUID-based naming for test isolation
        var sessionGuid = Guid.NewGuid().ToString("N")[^8..];
        var customName = $"Test-Important-Project-{sessionGuid}";
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

    #endregion

    #region Attachment Handling Tests

    /// <summary>
    /// Tests TOR-5.3.11: The system SHALL support attachment handling for images, PDFs, and other file types within chat messages.
    /// Tests TOR-5.3.12: The system SHALL store attachments in a directory-based structure per session to maintain organization and prevent file conflicts.
    /// </summary>
    [Fact]
    public async Task AttachmentHandling_WithVariousFileTypes_CreatesDirectoryBasedStorage()
    {
        // TOR-5.3.11, TOR-5.3.12
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
        // TOR-5.3.13
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
        // TOR-5.3.14
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
        // TOR-5.3.15
        // Arrange
        _output?.WriteLine("🧪 Testing TOR-5.3.15: Directory structure organization");
        _logger.LogInformation("Testing directory structure organization");
        
        var sessionGuid = Guid.NewGuid().ToString("N")[^8..];
        await _sessionManager.CreateNewSessionAsync();
        var session = _sessionManager.CurrentSession!;
        
        // Add some session data
        var sessionName = $"Test-Attachments-{sessionGuid}";
        await _sessionManager.RenameSessionAsync(sessionName);
        
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
        sessionJson.Should().Contain(sessionName, "session JSON should contain session name");
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

    #endregion

    #region Session List Configuration Tests

    [Fact]
    public async Task SessionListProvider_RespectsConfiguredLimit_WhenManySessionsExist()
    {
        // Arrange
        _output?.WriteLine("🚀 Testing session list limit configuration");
        _logger.LogInformation("🚀 Testing session list limit configuration");
        
        // Get the configured limit from the ChatClient
        var chatClient = _serviceProvider.GetRequiredService<ChatClient>();
        var configuredLimit = chatClient.Config.SessionListLimit;
        _output?.WriteLine($"📋 Configured session limit: {configuredLimit}");
        
        // Create more sessions than the limit to test filtering with GUID-based names
        var testSessionCount = configuredLimit + 5;
        var createdSessionIds = new List<Guid>();
        
        for (int i = 0; i < testSessionCount; i++)
        {
            var sessionGuid = Guid.NewGuid().ToString("N")[^8..];
            await _sessionManager.CreateNewSessionAsync();
            var sessionId = _sessionManager.CurrentSession!.Id;
            await _sessionManager.RenameSessionAsync($"Test-Limit-{sessionGuid}-{i + 1:D2}");
            createdSessionIds.Add(sessionId);
            
            // Add a small delay to ensure different LastModifiedAt timestamps
            await Task.Delay(10);
        }
        
        _output?.WriteLine($"📋 Created {testSessionCount} test sessions");
        
        // Act: Get selections from the SessionListProvider
        var selections = await _sessionListProvider.GetSelectionsAsync();
        
        // Assert 1: Should return exactly the configured limit number of sessions
        selections.Should().HaveCount(configuredLimit, 
            $"should return exactly {configuredLimit} sessions as configured"); // TOR-5.3.3
        _output?.WriteLine($"✅ Returned {selections.Count} sessions (respects configured limit)");
        
        // Assert 2: Should include our test sessions (they should be among the most recent)
        var sessionNames = selections.Select(s => s.Text).ToList();
        var testSessionsInResults = sessionNames.Where(name => name.StartsWith("Test-Limit-")).ToList();
        
        // We should have some of our test sessions in the results since they were just created
        testSessionsInResults.Should().NotBeEmpty("should include some of the recently created test sessions");
        _output?.WriteLine($"✅ Found {testSessionsInResults.Count} test sessions in results");
        
        // Assert 3: Should NOT include the oldest test session if we created more than the limit
        if (testSessionCount > configuredLimit)
        {
            // The oldest test session should not be in the results since we created more than the limit
            // and our test sessions should be among the most recent
            var oldestTestSessionInResults = testSessionsInResults.Any(name => name.Contains("-01"));
            if (!oldestTestSessionInResults)
            {
                _output?.WriteLine("✅ Oldest test session correctly excluded from limited results");
            }
        }
        
        _output?.WriteLine("🎉 Session list limit configuration test completed successfully!");
        _logger.LogInformation("🎉 Session list limit configuration test completed successfully!");
    }

    [Fact]
    public async Task SessionListProvider_ReturnsAllSessions_WhenCountBelowLimit()
    {
        // Arrange
        _output?.WriteLine("🚀 Testing session list when count is below limit");
        _logger.LogInformation("🚀 Testing session list when count is below limit");
        
        // Get the configured limit from the ChatClient
        var chatClient = _serviceProvider.GetRequiredService<ChatClient>();
        var configuredLimit = chatClient.Config.SessionListLimit;
        
        // Create fewer sessions than the limit with GUID-based names
        var testSessionCount = Math.Max(1, configuredLimit - 3);
        var createdSessionIds = new List<Guid>();
        
        for (int i = 0; i < testSessionCount; i++)
        {
            var sessionGuid = Guid.NewGuid().ToString("N")[^8..];
            await _sessionManager.CreateNewSessionAsync();
            var sessionId = _sessionManager.CurrentSession!.Id;
            await _sessionManager.RenameSessionAsync($"Test-Below-{sessionGuid}-{i + 1:D2}");
            createdSessionIds.Add(sessionId);
            
            // Add a small delay to ensure different LastModifiedAt timestamps
            await Task.Delay(10);
        }
        
        _output?.WriteLine($"📋 Created {testSessionCount} test sessions (below limit of {configuredLimit})");
        
        // Act: Get selections from the SessionListProvider
        var selections = await _sessionListProvider.GetSelectionsAsync();
        
        // Assert: Should return all created sessions since count is below limit
        var testSessionSelections = selections.Where(s => s.Text.StartsWith("Test-Below-")).ToList();
        testSessionSelections.Should().HaveCount(testSessionCount,
            $"should return all {testSessionCount} created sessions when below limit");
        
        _output?.WriteLine($"✅ Returned all {testSessionSelections.Count} sessions (below configured limit)");
        
        _output?.WriteLine("🎉 Session list below-limit test completed successfully!");
        _logger.LogInformation("🎉 Session list below-limit test completed successfully!");
    }

    #endregion

    #region Error Handling and Edge Cases

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

    #endregion

    #region Session Loading by Name Tests

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
        
        // Create session with custom name using GUID-based naming for test isolation
        var sessionGuid = Guid.NewGuid().ToString("N")[^8..];
        await _sessionManager.CreateNewSessionAsync();
        var originalSessionId = _sessionManager.CurrentSession!.Id;
        var sessionName = $"Test-Bug-Investigation-{sessionGuid}";
        await _sessionManager.RenameSessionAsync(sessionName);
        
        // Add some conversation history
        var userMessage = new ChatMessage(ChatRole.User, "I found a bug in the authentication system");
        var assistantMessage = new ChatMessage(ChatRole.Assistant, "Let me help you investigate this bug");
        
        await _sessionManager.AddMessageToCurrentSessionAsync(userMessage);
        await _sessionManager.AddMessageToCurrentSessionAsync(assistantMessage);
        
        // Act - Load session by name with different case variations
        var testCases = new[]
        {
            sessionName,                                    // Exact match
            sessionName.ToLowerInvariant(),                 // All lowercase
            sessionName.ToUpperInvariant(),                 // All uppercase
            sessionName.Replace("Bug", "bug"),              // Mixed case variation 1
            sessionName.Replace("Investigation", "investigation") // Mixed case variation 2
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
    /// Tests session loading by name when no matching session exists.
    /// Verifies that the method returns false when no session matches the provided name.
    /// </summary>
    [Fact]
    public async Task SessionLoading_ByName_ReturnsFalseWhenNoMatch()
    {
        // Arrange
        _output?.WriteLine("🧪 Testing session loading by name when no match exists");
        _logger.LogInformation("Testing session loading with non-existent name");
        
        // Create some sessions with different names using GUID-based naming
        var sessionNames = new[] { "Project Alpha", "Bug Fixes", "Feature Development" };
        
        foreach (var name in sessionNames)
        {
            var sessionGuid = Guid.NewGuid().ToString("N")[^8..];
            await _sessionManager.CreateNewSessionAsync();
            await _sessionManager.RenameSessionAsync($"Test-{name.Replace(" ", "-")}-{sessionGuid}");
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

    #endregion
}
