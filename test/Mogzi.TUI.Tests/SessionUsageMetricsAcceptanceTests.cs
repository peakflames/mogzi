namespace Mogzi.TUI.Tests;

/// <summary>
/// Black-box acceptance tests for session usage metrics functionality.
/// Tests real-time token tracking, Cline-inspired display formatting, and session persistence.
/// Uses real service configuration and DI container with no mocking approach.
/// Follows the systems-first testing philosophy with complete user workflows.
/// </summary>
[Collection("Sequential_Session_Tests")]
public class SessionUsageMetricsAcceptanceTests : SessionTestBase
{
    private readonly ITuiMediator _mediator;
    private readonly FooterPanel _footerPanel;
    private readonly IRenderingUtilities _renderingUtilities;

    public SessionUsageMetricsAcceptanceTests(ITestOutputHelper output)
        : base(output, nameof(SessionUsageMetricsAcceptanceTests))
    {
        // Get required services for usage metrics testing
        _mediator = _serviceProvider.GetRequiredService<ITuiMediator>();
        _footerPanel = _serviceProvider.GetRequiredService<FooterPanel>();
        _renderingUtilities = _serviceProvider.GetRequiredService<IRenderingUtilities>();

        _logger.LogInformation("SessionUsageMetricsAcceptanceTests initialized with real service configuration");
    }

    #region Token Tracking Tests

    /// <summary>
    /// Tests TOR-5.4.1: The system SHALL track token usage metrics (input tokens, output tokens, request count) 
    /// for each session in real-time during AI interactions.
    /// </summary>
    [Fact]
    public async Task TokenTracking_DuringAIInteractions_UpdatesRealTime()
    {
        // TOR-5.4.1
        // Arrange
        _output?.WriteLine("🧪 Testing TOR-5.4.1: Real-time token usage tracking during AI interactions");
        _logger.LogInformation("Testing real-time token usage tracking");

        await _sessionManager.CreateNewSessionAsync();
        var session = _sessionManager.CurrentSession!;

        // Verify initial state - no usage metrics
        session.UsageMetrics.Should().BeNull("new session should have no usage metrics initially");

        // Act - Simulate AI interaction with usage details
        var mockUsageDetails = new UsageDetails
        {
            InputTokenCount = 150,
            OutputTokenCount = 75,
            TotalTokenCount = 225
        };

        // Simulate the usage tracking that happens during AI processing
        session.UsageMetrics ??= new SessionUsageMetrics();
        session.UsageMetrics.AddUsage(mockUsageDetails);
        await _sessionManager.SaveCurrentSessionAsync();

        // Assert - Verify metrics were tracked
        var updatedSession = _sessionManager.CurrentSession!;
        updatedSession.UsageMetrics.Should().NotBeNull("session should have usage metrics after AI interaction"); // TOR-5.4.1
        updatedSession.UsageMetrics!.InputTokens.Should().Be(150, "input tokens should be tracked"); // TOR-5.4.1
        updatedSession.UsageMetrics.OutputTokens.Should().Be(75, "output tokens should be tracked"); // TOR-5.4.1
        updatedSession.UsageMetrics.RequestCount.Should().Be(1, "request count should be incremented"); // TOR-5.4.1
        updatedSession.UsageMetrics.TotalTokens.Should().Be(225, "total tokens should be calculated correctly");

        // Simulate second AI interaction
        var secondUsageDetails = new UsageDetails
        {
            InputTokenCount = 200,
            OutputTokenCount = 100,
            TotalTokenCount = 300
        };

        updatedSession.UsageMetrics.AddUsage(secondUsageDetails);
        await _sessionManager.SaveCurrentSessionAsync();

        // Verify cumulative tracking
        var finalSession = _sessionManager.CurrentSession!;
        finalSession.UsageMetrics!.InputTokens.Should().Be(350, "input tokens should accumulate"); // TOR-5.4.1
        finalSession.UsageMetrics.OutputTokens.Should().Be(175, "output tokens should accumulate"); // TOR-5.4.1
        finalSession.UsageMetrics.RequestCount.Should().Be(2, "request count should increment"); // TOR-5.4.1
        finalSession.UsageMetrics.TotalTokens.Should().Be(525, "total tokens should accumulate");

        _output?.WriteLine("✅ TOR-5.4.1: Real-time token usage tracking verified");
        _logger.LogInformation("Real-time token usage tracking test completed successfully");
    }

    /// <summary>
    /// Tests TOR-5.4.2: The system SHALL persist session usage metrics as part of the session data 
    /// to ensure metrics survive application restarts and session reloading.
    /// </summary>
    [Fact]
    public async Task UsageMetricsPersistence_AcrossRestarts_SurvivesReloading()
    {
        // TOR-5.4.2
        // Arrange
        _output?.WriteLine("🧪 Testing TOR-5.4.2: Usage metrics persistence across application restarts");
        _logger.LogInformation("Testing usage metrics persistence");

        await _sessionManager.CreateNewSessionAsync();
        var originalSessionId = _sessionManager.CurrentSession!.Id;

        // Add usage metrics to session
        var session = _sessionManager.CurrentSession!;
        session.UsageMetrics = new SessionUsageMetrics();
        session.UsageMetrics.AddUsage(new UsageDetails
        {
            InputTokenCount = 1500,
            OutputTokenCount = 750,
            TotalTokenCount = 2250
        });
        session.UsageMetrics.AddUsage(new UsageDetails
        {
            InputTokenCount = 800,
            OutputTokenCount = 400,
            TotalTokenCount = 1200
        });

        await _sessionManager.SaveCurrentSessionAsync();

        // Act - Simulate application restart by loading session in new SessionManager
        var newLogger = _serviceProvider.GetRequiredService<ILogger<SessionManager>>();
        var newSessionManager = new SessionManager(newLogger);
        await newSessionManager.LoadSessionAsync(originalSessionId.ToString());

        // Assert - Verify metrics persisted correctly
        var reloadedSession = newSessionManager.CurrentSession;
        reloadedSession.Should().NotBeNull("session should be loaded after restart");
        reloadedSession!.UsageMetrics.Should().NotBeNull("usage metrics should persist across restarts"); // TOR-5.4.2

        var metrics = reloadedSession.UsageMetrics!;
        metrics.InputTokens.Should().Be(2300, "input tokens should persist"); // TOR-5.4.2
        metrics.OutputTokens.Should().Be(1150, "output tokens should persist"); // TOR-5.4.2
        metrics.RequestCount.Should().Be(2, "request count should persist"); // TOR-5.4.2
        metrics.TotalTokens.Should().Be(3450, "total tokens should be calculated correctly");
        metrics.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1), "last updated should be recent");

        // Verify metrics are stored in JSON file
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var sessionFile = Path.Combine(homeDirectory, ".mogzi", "chats", originalSessionId.ToString(), "session.json");
        var jsonContent = await File.ReadAllTextAsync(sessionFile);
        
        jsonContent.Should().Contain("usageMetrics", "JSON should contain usage metrics section"); // TOR-5.4.2
        jsonContent.Should().Contain("inputTokens", "JSON should contain input tokens field");
        jsonContent.Should().Contain("outputTokens", "JSON should contain output tokens field");
        jsonContent.Should().Contain("requestCount", "JSON should contain request count field");
        jsonContent.Should().Contain("2300", "JSON should contain correct input token count");
        jsonContent.Should().Contain("1150", "JSON should contain correct output token count");

        _output?.WriteLine("✅ TOR-5.4.2: Usage metrics persistence across restarts verified");
        _logger.LogInformation("Usage metrics persistence test completed successfully");
    }

    /// <summary>
    /// Tests TOR-5.4.6: The system SHALL maintain session-scoped usage metrics isolation, 
    /// ensuring each session tracks its own token usage independently.
    /// </summary>
    [Fact]
    public async Task SessionScopedIsolation_MultipleSessions_TrackIndependently()
    {
        // TOR-5.4.6
        // Arrange
        _output?.WriteLine("🧪 Testing TOR-5.4.6: Session-scoped usage metrics isolation");
        _logger.LogInformation("Testing session-scoped metrics isolation");

        // Create first session with usage metrics
        await _sessionManager.CreateNewSessionAsync();
        var session1Id = _sessionManager.CurrentSession!.Id;
        var session1 = _sessionManager.CurrentSession!;
        
        session1.UsageMetrics = new SessionUsageMetrics();
        session1.UsageMetrics.AddUsage(new UsageDetails
        {
            InputTokenCount = 1000,
            OutputTokenCount = 500,
            TotalTokenCount = 1500
        });
        await _sessionManager.SaveCurrentSessionAsync();

        // Create second session with different usage metrics
        await _sessionManager.CreateNewSessionAsync();
        var session2Id = _sessionManager.CurrentSession!.Id;
        var session2 = _sessionManager.CurrentSession!;
        
        session2.UsageMetrics = new SessionUsageMetrics();
        session2.UsageMetrics.AddUsage(new UsageDetails
        {
            InputTokenCount = 2000,
            OutputTokenCount = 1000,
            TotalTokenCount = 3000
        });
        await _sessionManager.SaveCurrentSessionAsync();

        // Act & Assert - Verify isolation by loading each session independently
        var newLogger1 = _serviceProvider.GetRequiredService<ILogger<SessionManager>>();
        var sessionManager1 = new SessionManager(newLogger1);
        await sessionManager1.LoadSessionAsync(session1Id.ToString());

        var newLogger2 = _serviceProvider.GetRequiredService<ILogger<SessionManager>>();
        var sessionManager2 = new SessionManager(newLogger2);
        await sessionManager2.LoadSessionAsync(session2Id.ToString());

        // Verify session 1 metrics are isolated
        var reloadedSession1 = sessionManager1.CurrentSession!;
        reloadedSession1.UsageMetrics.Should().NotBeNull("session 1 should have usage metrics");
        reloadedSession1.UsageMetrics!.InputTokens.Should().Be(1000, "session 1 input tokens should be isolated"); // TOR-5.4.6
        reloadedSession1.UsageMetrics.OutputTokens.Should().Be(500, "session 1 output tokens should be isolated"); // TOR-5.4.6
        reloadedSession1.UsageMetrics.RequestCount.Should().Be(1, "session 1 request count should be isolated"); // TOR-5.4.6

        // Verify session 2 metrics are isolated
        var reloadedSession2 = sessionManager2.CurrentSession!;
        reloadedSession2.UsageMetrics.Should().NotBeNull("session 2 should have usage metrics");
        reloadedSession2.UsageMetrics!.InputTokens.Should().Be(2000, "session 2 input tokens should be isolated"); // TOR-5.4.6
        reloadedSession2.UsageMetrics.OutputTokens.Should().Be(1000, "session 2 output tokens should be isolated"); // TOR-5.4.6
        reloadedSession2.UsageMetrics.RequestCount.Should().Be(1, "session 2 request count should be isolated"); // TOR-5.4.6

        // Verify sessions don't affect each other
        reloadedSession1.Id.Should().NotBe(reloadedSession2.Id, "sessions should have different IDs");
        reloadedSession1.UsageMetrics.InputTokens.Should().NotBe(reloadedSession2.UsageMetrics.InputTokens, 
            "sessions should have different input token counts");

        _output?.WriteLine("✅ TOR-5.4.6: Session-scoped usage metrics isolation verified");
        _logger.LogInformation("Session-scoped metrics isolation test completed successfully");
    }

    /// <summary>
    /// Tests TOR-5.4.8: The system SHALL update usage metrics immediately after each AI interaction 
    /// to provide real-time feedback to users.
    /// </summary>
    [Fact]
    public async Task ImmediateUpdates_AfterAIInteraction_ProvidesRealTimeFeedback()
    {
        // TOR-5.4.8
        // Arrange
        _output?.WriteLine("🧪 Testing TOR-5.4.8: Immediate usage metrics updates for real-time feedback");
        _logger.LogInformation("Testing immediate usage metrics updates");

        await _sessionManager.CreateNewSessionAsync();
        var session = _sessionManager.CurrentSession!;

        // Act - Simulate rapid AI interactions with immediate updates
        var interactions = new[]
        {
            new UsageDetails { InputTokenCount = 100, OutputTokenCount = 50, TotalTokenCount = 150 },
            new UsageDetails { InputTokenCount = 200, OutputTokenCount = 100, TotalTokenCount = 300 },
            new UsageDetails { InputTokenCount = 150, OutputTokenCount = 75, TotalTokenCount = 225 }
        };

        session.UsageMetrics = new SessionUsageMetrics();
        var timestamps = new List<DateTime>();

        foreach (var usage in interactions)
        {
            var beforeUpdate = DateTime.UtcNow;
            session.UsageMetrics.AddUsage(usage);
            await _sessionManager.SaveCurrentSessionAsync();
            timestamps.Add(session.UsageMetrics.LastUpdated);

            // Assert - Verify immediate update
            session.UsageMetrics.LastUpdated.Should().BeOnOrAfter(beforeUpdate, 
                "usage metrics should be updated immediately after AI interaction"); // TOR-5.4.8
            
            // Small delay to ensure timestamp differences
            await Task.Delay(10);
        }

        // Verify final accumulated state
        session.UsageMetrics.InputTokens.Should().Be(450, "all input tokens should be accumulated immediately"); // TOR-5.4.8
        session.UsageMetrics.OutputTokens.Should().Be(225, "all output tokens should be accumulated immediately"); // TOR-5.4.8
        session.UsageMetrics.RequestCount.Should().Be(3, "all requests should be counted immediately"); // TOR-5.4.8

        // Verify timestamps show progression (each update is more recent than the previous)
        for (int i = 1; i < timestamps.Count; i++)
        {
            timestamps[i].Should().BeOnOrAfter(timestamps[i - 1], 
                $"timestamp {i} should be on or after timestamp {i - 1} for real-time updates");
        }

        _output?.WriteLine("✅ TOR-5.4.8: Immediate usage metrics updates verified");
        _logger.LogInformation("Immediate usage metrics updates test completed successfully");
    }

    #endregion

    #region Display Formatting Tests

    /// <summary>
    /// Tests TOR-5.4.3: The system SHALL display token usage metrics in the footer formatted 
    /// with smart number abbreviations (345, 1.9k, 15k, 1.9m).
    /// </summary>
    [Fact]
    public void TokenNumberFormatting_SmartAbbreviations_DisplaysCorrectly()
    {
        // TOR-5.4.3
        // Arrange
        _output?.WriteLine("🧪 Testing TOR-5.4.3: Smart number abbreviations for token display");
        _logger.LogInformation("Testing smart number abbreviations");

        var testCases = new[]
        {
            (tokens: 0L, expected: "0"),
            (tokens: 345L, expected: "345"),
            (tokens: 999L, expected: "999"),
            (tokens: 1000L, expected: "1.0k"),
            (tokens: 1500L, expected: "1.5k"),
            (tokens: 1900L, expected: "1.9k"),
            (tokens: 9999L, expected: "10.0k"),
            (tokens: 10000L, expected: "10k"),
            (tokens: 15000L, expected: "15k"),
            (tokens: 150000L, expected: "150k"),
            (tokens: 999999L, expected: "999k"),
            (tokens: 1000000L, expected: "1.0m"),
            (tokens: 1900000L, expected: "1.9m"),
            (tokens: 15000000L, expected: "15.0m"),
            (tokens: 1000000000L, expected: "1000.0m")
        };

        // Act & Assert - Test each formatting case
        foreach (var (tokens, expected) in testCases)
        {
            var result = _renderingUtilities.FormatTokenNumber(tokens);
            result.Should().Be(expected, $"tokens {tokens} should format as '{expected}'"); // TOR-5.4.3
            _output?.WriteLine($"✅ {tokens:N0} tokens → '{result}' (expected: '{expected}')");
        }

        _output?.WriteLine("✅ TOR-5.4.3: Smart number abbreviations verified");
        _logger.LogInformation("Smart number abbreviations test completed successfully");
    }

    /// <summary>
    /// Tests TOR-5.4.4: The system SHALL clearly distinguish token flow direction in the token usage display.
    /// </summary>
    [Fact]
    public void TokenFlowDirection_InDisplay_ClearlyDistinguished()
    {
        // TOR-5.4.4
        // Arrange
        _output?.WriteLine("🧪 Testing TOR-5.4.4: Clear token flow direction distinction");
        _logger.LogInformation("Testing token flow direction display");

        // Create session with usage metrics
        var session = new Session
        {
            Id = Guid.NewGuid(),
            Name = "Test Session",
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow,
            UsageMetrics = new SessionUsageMetrics()
        };

        session.UsageMetrics.AddUsage(new UsageDetails
        {
            InputTokenCount = 1900,
            OutputTokenCount = 345,
            TotalTokenCount = 2245
        });

        // Act - Format session token usage
        var result = _renderingUtilities.FormatSessionTokenUsage(session);

        // Assert - Verify directional indicators are present
        result.Should().Contain("↑", "display should contain upward arrow for input tokens"); // TOR-5.4.4
        result.Should().Contain("↓", "display should contain downward arrow for output tokens"); // TOR-5.4.4
        result.Should().Contain("1.9k", "display should show formatted input tokens");
        result.Should().Contain("345", "display should show formatted output tokens");
        result.Should().MatchRegex(@"Tokens:\s*↑\s*1\.9k\s*↓\s*345", "display should follow expected format pattern");

        // Test with zero tokens
        var emptySession = new Session
        {
            Id = Guid.NewGuid(),
            Name = "Empty Session",
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow,
            UsageMetrics = new SessionUsageMetrics()
        };

        var emptyResult = _renderingUtilities.FormatSessionTokenUsage(emptySession);
        emptyResult.Should().Be("Tokens: ↑ 0 ↓ 0", "empty session should show zero tokens"); // TOR-5.4.4

        _output?.WriteLine($"✅ Token usage display: '{result}'");
        _output?.WriteLine($"✅ Empty session display: '{emptyResult}'");
        _output?.WriteLine("✅ TOR-5.4.4: Token flow direction distinction verified");
        _logger.LogInformation("Token flow direction display test completed successfully");
    }

    /// <summary>
    /// Tests TOR-5.4.5: The system SHALL calculate and display context window utilization as a percentage.
    /// </summary>
    [Fact]
    public void ContextWindowUtilization_AsPercentage_CalculatedCorrectly()
    {
        // TOR-5.4.5
        // Arrange
        _output?.WriteLine("🧪 Testing TOR-5.4.5: Context window utilization percentage calculation");
        _logger.LogInformation("Testing context window utilization calculation");

        var appService = _serviceProvider.GetRequiredService<IAppService>();
        var historyManager = _serviceProvider.GetRequiredService<HistoryManager>();

        // Create test chat history with known token counts
        var testMessages = new List<ChatMessage>
        {
            new(ChatRole.User, "Short message"),
            new(ChatRole.Assistant, "This is a longer response that should consume more tokens in the context window calculation"),
            new(ChatRole.User, "Another user message for testing context window utilization")
        };

        // Act - Format context window usage
        var result = _renderingUtilities.FormatContextWindowUsage(appService, testMessages);

        // Assert - Verify percentage calculation and display
        result.Should().Contain("Context:", "display should start with Context label"); // TOR-5.4.5
        result.Should().Contain("/", "display should show current/max format");
        result.Should().Contain("(", "display should contain percentage in parentheses");
        result.Should().Contain("%)", "display should end percentage with %)");
        result.Should().MatchRegex(@"Context:\s*\d+.*?/\d+.*?\s*\(\d+%\)", "should match expected format pattern");

        // Test with empty history
        var emptyResult = _renderingUtilities.FormatContextWindowUsage(appService, new List<ChatMessage>());
        emptyResult.Should().Contain("Context:", "empty history should still show Context label");
        emptyResult.Should().Contain("(0%)", "empty history should show 0% utilization"); // TOR-5.4.5

        _output?.WriteLine($"✅ Context window display: '{result}'");
        _output?.WriteLine($"✅ Empty context display: '{emptyResult}'");
        _output?.WriteLine("✅ TOR-5.4.5: Context window utilization percentage verified");
        _logger.LogInformation("Context window utilization test completed successfully");
    }

    /// <summary>
    /// Tests TOR-5.4.7: The system SHALL gracefully handle missing or unavailable usage data 
    /// by displaying placeholder indicators ("--") without affecting application functionality.
    /// </summary>
    [Fact]
    public void GracefulDegradation_MissingUsageData_ShowsPlaceholders()
    {
        // TOR-5.4.7
        // Arrange
        _output?.WriteLine("🧪 Testing TOR-5.4.7: Graceful handling of missing usage data");
        _logger.LogInformation("Testing graceful degradation with missing usage data");

        // Test with null session
        var nullSessionResult = _renderingUtilities.FormatSessionTokenUsage(null);
        nullSessionResult.Should().Be("Tokens: --", "null session should show placeholder"); // TOR-5.4.7

        // Test with session without usage metrics
        var sessionWithoutMetrics = new Session
        {
            Id = Guid.NewGuid(),
            Name = "Session Without Metrics",
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow,
            UsageMetrics = null
        };

        var noMetricsResult = _renderingUtilities.FormatSessionTokenUsage(sessionWithoutMetrics);
        noMetricsResult.Should().Be("Tokens: --", "session without metrics should show placeholder"); // TOR-5.4.7

        // Test with session with zero usage
        var sessionWithZeroUsage = new Session
        {
            Id = Guid.NewGuid(),
            Name = "Session With Zero Usage",
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow,
            UsageMetrics = new SessionUsageMetrics()
        };

        var zeroUsageResult = _renderingUtilities.FormatSessionTokenUsage(sessionWithZeroUsage);
        zeroUsageResult.Should().Be("Tokens: ↑ 0 ↓ 0", "session with zero usage should show zero tokens"); // TOR-5.4.7

        // Test cache usage (not yet implemented)
        var cacheResult = _renderingUtilities.FormatCacheUsage(sessionWithoutMetrics);
        cacheResult.Should().Be("Cache: --", "cache usage should show placeholder when not implemented"); // TOR-5.4.7

        // Verify application functionality is not affected
        Action testAction = () =>
        {
            _renderingUtilities.FormatSessionTokenUsage(null);
            _renderingUtilities.FormatCacheUsage(null);
            _renderingUtilities.FormatContextWindowUsage(_serviceProvider.GetRequiredService<IAppService>(), new List<ChatMessage>());
        };

        testAction.Should().NotThrow("missing usage data should not throw exceptions"); // TOR-5.4.7

        _output?.WriteLine($"✅ Null session: '{nullSessionResult}'");
        _output?.WriteLine($"✅ No metrics: '{noMetricsResult}'");
        _output?.WriteLine($"✅ Zero usage: '{zeroUsageResult}'");
        _output?.WriteLine($"✅ Cache placeholder: '{cacheResult}'");
        _output?.WriteLine("✅ TOR-5.4.7: Graceful degradation with placeholders verified");
        _logger.LogInformation("Graceful degradation test completed successfully");
    }

    /// <summary>
    /// Tests TOR-5.4.10: The system SHALL display usage metrics in a consistent footer format: 
    /// "[Tokens: ↑ X ↓ Y] [Cache: --] [Context: A/B (C%)]" for optimal user experience.
    /// </summary>
    [Fact]
    public async Task FooterFormat_ConsistentDisplay_OptimalUserExperience()
    {
        // TOR-5.4.10
        // Arrange
        _output?.WriteLine("🧪 Testing TOR-5.4.10: Consistent footer format for optimal user experience");
        _logger.LogInformation("Testing consistent footer format");

        // Create session with usage metrics
        var session = new Session
        {
            Id = Guid.NewGuid(),
            Name = "Test Footer Session",
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow,
            UsageMetrics = new SessionUsageMetrics()
        };

        session.UsageMetrics.AddUsage(new UsageDetails
        {
            InputTokenCount = 1900,
            OutputTokenCount = 345,
            TotalTokenCount = 2245
        });

        // Note: Following no-mocking approach - using real session manager with test session
        // The session is already set in the real session manager from the test setup
        await _sessionManager.CreateNewSessionAsync();
        var currentSession = _sessionManager.CurrentSession!;
        
        // Update the current session with our test usage metrics
        currentSession.UsageMetrics = session.UsageMetrics;
        await _sessionManager.SaveCurrentSessionAsync();

        // Act - Get individual format components
        var tokenInfo = _renderingUtilities.FormatSessionTokenUsage(session);
        var cacheInfo = _renderingUtilities.FormatCacheUsage(session);
        var contextInfo = _renderingUtilities.FormatContextWindowUsage(
            _serviceProvider.GetRequiredService<IAppService>(),
            new List<ChatMessage> { new(ChatRole.User, "Test message") });

        // Assert - Verify consistent format components
        tokenInfo.Should().StartWith("Tokens:", "token info should start with 'Tokens:'"); // TOR-5.4.10
        tokenInfo.Should().Contain("↑", "token info should contain upward arrow");
        tokenInfo.Should().Contain("↓", "token info should contain downward arrow");
        tokenInfo.Should().MatchRegex(@"Tokens:\s*↑\s*\d+\.?\d*[km]?\s*↓\s*\d+\.?\d*[km]?", "token format should be consistent");

        cacheInfo.Should().Be("Cache: --", "cache info should show placeholder consistently"); // TOR-5.4.10

        contextInfo.Should().Contain("Context:", "context info should contain 'Context:'"); // TOR-5.4.10
        contextInfo.Should().Contain("/", "context info should contain separator");
        contextInfo.Should().Contain("(", "context info should contain opening parenthesis");
        contextInfo.Should().Contain("%)", "context info should contain closing percentage");

        // Verify complete footer format when combined
        var completeFooter = $"[{tokenInfo}] [{cacheInfo}] [{contextInfo}]";
        completeFooter.Should().MatchRegex(@"\[Tokens:\s*↑.*?↓.*?\]\s*\[Cache:\s*--\]\s*\[.*?Context:.*?/.*?\s*\(\d+%\).*?\]",
            "complete footer should match expected format pattern"); // TOR-5.4.10

        _output?.WriteLine($"✅ Token info: '{tokenInfo}'");
        _output?.WriteLine($"✅ Cache info: '{cacheInfo}'");
        _output?.WriteLine($"✅ Context info: '{contextInfo}'");
        _output?.WriteLine($"✅ Complete footer: '{completeFooter}'");
        _output?.WriteLine("✅ TOR-5.4.10: Consistent footer format verified");
        _logger.LogInformation("Consistent footer format test completed successfully");
    }

    #endregion

    #region Future Enhancement Tests

    /// <summary>
    /// Tests TOR-5.4.9: The system SHALL include cache token tracking fields in the usage metrics structure 
    /// to support future cache token functionality when available.
    /// </summary>
    [Fact]
    public void CacheTokenFields_InUsageMetrics_SupportFutureFunctionality()
    {
        // TOR-5.4.9
        // Arrange
        _output?.WriteLine("🧪 Testing TOR-5.4.9: Cache token fields for future functionality");
        _logger.LogInformation("Testing cache token fields in usage metrics");

        var usageMetrics = new SessionUsageMetrics();

        // Act & Assert - Verify cache token fields exist
        usageMetrics.Should().NotBeNull("usage metrics should be instantiable");
        
        // Verify cache token properties exist (using reflection to check structure)
        var cacheReadProperty = typeof(SessionUsageMetrics).GetProperty("CacheReadTokens");
        var cacheWriteProperty = typeof(SessionUsageMetrics).GetProperty("CacheWriteTokens");

        cacheReadProperty.Should().NotBeNull("CacheReadTokens property should exist for future functionality"); // TOR-5.4.9
        cacheWriteProperty.Should().NotBeNull("CacheWriteTokens property should exist for future functionality"); // TOR-5.4.9

        cacheReadProperty!.PropertyType.Should().Be(typeof(long), "CacheReadTokens should be long type");
        cacheWriteProperty!.PropertyType.Should().Be(typeof(long), "CacheWriteTokens should be long type");

        // Verify initial values
        usageMetrics.CacheReadTokens.Should().Be(0, "cache read tokens should initialize to zero"); // TOR-5.4.9
        usageMetrics.CacheWriteTokens.Should().Be(0, "cache write tokens should initialize to zero"); // TOR-5.4.9

        // Verify fields can be set (for future use)
        usageMetrics.CacheReadTokens = 100;
        usageMetrics.CacheWriteTokens = 50;

        usageMetrics.CacheReadTokens.Should().Be(100, "cache read tokens should be settable");
        usageMetrics.CacheWriteTokens.Should().Be(50, "cache write tokens should be settable");

        // Verify Reset() method handles cache tokens
        usageMetrics.Reset();
        usageMetrics.CacheReadTokens.Should().Be(0, "Reset() should clear cache read tokens"); // TOR-5.4.9
        usageMetrics.CacheWriteTokens.Should().Be(0, "Reset() should clear cache write tokens"); // TOR-5.4.9

        _output?.WriteLine("✅ TOR-5.4.9: Cache token fields for future functionality verified");
        _logger.LogInformation("Cache token fields test completed successfully");
    }

    #endregion

    #region Integration Tests

    /// <summary>
    /// Integration test verifying complete session usage metrics workflow from creation to display.
    /// Tests the end-to-end functionality including persistence, formatting, and display.
    /// </summary>
    [Fact]
    public async Task CompleteUsageMetricsWorkflow_EndToEnd_WorksCorrectly()
    {
        // Arrange
        _output?.WriteLine("🧪 Testing complete session usage metrics workflow end-to-end");
        _logger.LogInformation("Testing complete usage metrics workflow");

        // Create new session
        await _sessionManager.CreateNewSessionAsync();
        var session = _sessionManager.CurrentSession!;
        var sessionId = session.Id;

        // Simulate multiple AI interactions with varying token usage
        var interactions = new[]
        {
            new UsageDetails { InputTokenCount = 500, OutputTokenCount = 250, TotalTokenCount = 750 },
            new UsageDetails { InputTokenCount = 1200, OutputTokenCount = 600, TotalTokenCount = 1800 },
            new UsageDetails { InputTokenCount = 800, OutputTokenCount = 400, TotalTokenCount = 1200 }
        };

        session.UsageMetrics = new SessionUsageMetrics();

        // Act - Process each interaction
        foreach (var usage in interactions)
        {
            session.UsageMetrics.AddUsage(usage);
            await _sessionManager.SaveCurrentSessionAsync();
        }

        // Verify intermediate state
        session.UsageMetrics.InputTokens.Should().Be(2500, "input tokens should accumulate correctly");
        session.UsageMetrics.OutputTokens.Should().Be(1250, "output tokens should accumulate correctly");
        session.UsageMetrics.RequestCount.Should().Be(3, "request count should be correct");

        // Test session reload
        var newLogger = _serviceProvider.GetRequiredService<ILogger<SessionManager>>();
        var newSessionManager = new SessionManager(newLogger);
        await newSessionManager.LoadSessionAsync(sessionId.ToString());

        var reloadedSession = newSessionManager.CurrentSession!;
        reloadedSession.UsageMetrics.Should().NotBeNull("reloaded session should have usage metrics");
        reloadedSession.UsageMetrics!.InputTokens.Should().Be(2500, "reloaded input tokens should match");
        reloadedSession.UsageMetrics.OutputTokens.Should().Be(1250, "reloaded output tokens should match");

        // Test display formatting
        var tokenDisplay = _renderingUtilities.FormatSessionTokenUsage(reloadedSession);
        tokenDisplay.Should().Contain("2.5k", "should format input tokens with abbreviation");
        tokenDisplay.Should().Contain("1.2k", "should format output tokens with abbreviation");
        tokenDisplay.Should().Contain("↑", "should show input direction");
        tokenDisplay.Should().Contain("↓", "should show output direction");

        // Test cache display (placeholder)
        var cacheDisplay = _renderingUtilities.FormatCacheUsage(reloadedSession);
        cacheDisplay.Should().Be("Cache: --", "cache should show placeholder");

        // Test context window display
        var testMessages = new List<ChatMessage>
        {
            new(ChatRole.User, "Test message for context calculation")
        };
        var contextDisplay = _renderingUtilities.FormatContextWindowUsage(
            _serviceProvider.GetRequiredService<IAppService>(), testMessages);
        contextDisplay.Should().Contain("Context:", "should show context label");
        contextDisplay.Should().Contain("%)", "should show percentage");

        _output?.WriteLine($"✅ Token display: '{tokenDisplay}'");
        _output?.WriteLine($"✅ Cache display: '{cacheDisplay}'");
        _output?.WriteLine($"✅ Context display: '{contextDisplay}'");
        _output?.WriteLine("✅ Complete usage metrics workflow verified");
        _logger.LogInformation("Complete usage metrics workflow test completed successfully");
    }

    #endregion
}
