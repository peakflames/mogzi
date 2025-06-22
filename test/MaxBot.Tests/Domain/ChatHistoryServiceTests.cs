using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MaxBot.Domain;
using Microsoft.Extensions.AI;
using Xunit;

namespace MaxBot.Tests.Domain;

public class ChatHistoryServiceTests : IDisposable
{
    private readonly ChatHistoryService _chatHistoryService;
    private readonly string _testSessionPath;

    public ChatHistoryServiceTests()
    {
        _chatHistoryService = new ChatHistoryService();
        _testSessionPath = Path.Combine(_chatHistoryService.GetBasePath(), "test_session");
        
        // Create test directory
        Directory.CreateDirectory(_testSessionPath);
    }

    [Fact]
    public void CreateChatSession_ShouldCreateDirectoryWithTimestamp()
    {
        // Act
        string sessionPath = _chatHistoryService.CreateChatSession();
        
        // Assert
        Assert.True(Directory.Exists(sessionPath));
        
        // Cleanup
        try
        {
            Directory.Delete(sessionPath);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public async Task SaveAndLoadChatHistory_ShouldFilterSystemMessagesAndInjectSystemPrompt()
    {
        // Arrange
        var chatHistory = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "You are a helpful assistant."),
            new ChatMessage(ChatRole.User, "Hello, how are you?"),
            new ChatMessage(ChatRole.Assistant, "I'm doing well, thank you for asking!")
        };
        
        string newSystemPrompt = "You are an AI assistant named Max.";
        
        // Act
        await _chatHistoryService.SaveChatHistoryAsync(_testSessionPath, chatHistory);
        var loadedHistory = await _chatHistoryService.LoadChatHistoryAsync(_testSessionPath, newSystemPrompt);
        
        // Assert
        Assert.NotNull(loadedHistory);
        Assert.Equal(3, loadedHistory!.Count); // Should have 3 messages: system + user + assistant
        
        // First message should be the new system prompt
        Assert.Equal(ChatRole.System, loadedHistory[0].Role);
        Assert.Equal(newSystemPrompt, loadedHistory[0].Text);
        
        // Second message should be the user message
        Assert.Equal(ChatRole.User, loadedHistory[1].Role);
        Assert.Equal("Hello, how are you?", loadedHistory[1].Text);
        
        // Third message should be the assistant message
        Assert.Equal(ChatRole.Assistant, loadedHistory[2].Role);
        Assert.Equal("I'm doing well, thank you for asking!", loadedHistory[2].Text);
    }
    
    [Fact]
    public async Task SaveChatHistory_ShouldFilterOutSystemMessages()
    {
        // Arrange
        var chatHistory = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "You are a helpful assistant."),
            new ChatMessage(ChatRole.User, "Hello, how are you?"),
            new ChatMessage(ChatRole.Assistant, "I'm doing well, thank you for asking!")
        };
        
        // Act
        await _chatHistoryService.SaveChatHistoryAsync(_testSessionPath, chatHistory);
        
        // Read the file directly to check its contents
        string filePath = Path.Combine(_testSessionPath, "chatHistory.json");
        string jsonContent = await File.ReadAllTextAsync(filePath);
        
        // Assert
        Assert.DoesNotContain("You are a helpful assistant", jsonContent);
        Assert.Contains("Hello, how are you?", jsonContent);
        
        // Check for the assistant's response, accounting for possible JSON escaping
        bool containsAssistantResponse = 
            jsonContent.Contains("I'm doing well, thank you for asking!") || 
            jsonContent.Contains("I\\u0027m doing well, thank you for asking!") ||
            jsonContent.Contains("I\u0027m doing well, thank you for asking!");
        
        Assert.True(containsAssistantResponse, "Assistant response not found in JSON");
    }

    [Fact]
    public async Task LoadChatHistory_WithNonExistentFile_ShouldReturnNull()
    {
        // Arrange
        string nonExistentPath = Path.Combine(_chatHistoryService.GetBasePath(), "non_existent");
        
        // Act
        var result = await _chatHistoryService.LoadChatHistoryAsync(nonExistentPath);
        
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetChatSessions_ShouldReturnOrderedSessions()
    {
        // Arrange
        string session1 = Path.Combine(_chatHistoryService.GetBasePath(), "20250101_120000");
        string session2 = Path.Combine(_chatHistoryService.GetBasePath(), "20250101_130000");
        
        Directory.CreateDirectory(session1);
        Directory.CreateDirectory(session2);
        
        // Act
        var sessions = _chatHistoryService.GetChatSessions();
        
        // Assert
        Assert.Contains(session1, sessions);
        Assert.Contains(session2, sessions);
        
        // Most recent first
        Assert.True(sessions.IndexOf(session2) < sessions.IndexOf(session1));
        
        // Cleanup
        try
        {
            Directory.Delete(session1);
            Directory.Delete(session2);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public void GetMostRecentChatSession_ShouldReturnMostRecentSession()
    {
        // Arrange - First clean up the test session to avoid interference
        try
        {
            if (Directory.Exists(_testSessionPath))
            {
                Directory.Delete(_testSessionPath, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
        
        // Clean up any existing test directories
        var basePath = _chatHistoryService.GetBasePath();
        foreach (var dir in Directory.GetDirectories(basePath))
        {
            if (Path.GetFileName(dir).StartsWith("test_"))
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
        
        // Create test directories with specific timestamps
        string session1 = Path.Combine(basePath, "test_20250101_120000");
        string session2 = Path.Combine(basePath, "test_20250101_130000");
        
        Directory.CreateDirectory(session1);
        Directory.CreateDirectory(session2);
        
        try
        {
            // Act
            var sessions = _chatHistoryService.GetChatSessions();
            
            // Filter to only include our test sessions
            var testSessions = sessions.Where(s => Path.GetFileName(s).StartsWith("test_")).ToList();
            
            // Assert - We should have our two test sessions
            Assert.Equal(2, testSessions.Count);
            
            // The sessions should be ordered with most recent first
            Assert.Equal(session2, testSessions[0]);
            Assert.Equal(session1, testSessions[1]);
            
            // The most recent session should be session2
            var mostRecent = testSessions[0];
            Assert.Contains("test_20250101_130000", mostRecent);
        }
        finally
        {
            // Cleanup
            try
            {
                Directory.Delete(session1, true);
                Directory.Delete(session2, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    public void Dispose()
    {
        // Clean up test directory
        try
        {
            if (Directory.Exists(_testSessionPath))
            {
                Directory.Delete(_testSessionPath, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
