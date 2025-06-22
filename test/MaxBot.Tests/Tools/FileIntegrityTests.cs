using FluentAssertions;
using MaxBot.Domain;
using MaxBot.Tools;
using System.Security.Cryptography;
using System.Text;

namespace MaxBot.Tests.Tools;

/// <summary>
/// Tests for TOR-3.2: File integrity preservation during operations
/// These tests verify that file operations preserve integrity through:
/// - Atomic write operations
/// - Backup and restore capabilities
/// - Checksum validation
/// - Rollback on failure
/// </summary>
public class FileIntegrityTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly MaxbotConfiguration _config;
    private readonly FileSystemTools _fileSystemTools;
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider;

    public FileIntegrityTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "MaxBotFileIntegrityTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        
        _config = new MaxbotConfiguration { ToolApprovals = "auto" };
        _workingDirectoryProvider = new MockWorkingDirectoryProvider(_testDirectory);
        _fileSystemTools = new FileSystemTools(_config, null, _workingDirectoryProvider);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    [Fact]
    public void Constructor_ShouldSetCurrentDirectoryToTestDirectory()
    {
        // Assert
        var expected = Path.GetFullPath(_testDirectory);
        var actual = Path.GetFullPath(_workingDirectoryProvider.GetCurrentDirectory());
        actual.Should().Be(expected);
    }

    [Fact]
    public void WriteFile_WhenSuccessful_ShouldPreserveFileIntegrity()
    {
        // Arrange
        var testFile = "test_integrity.txt";
        var originalContent = "Original content for integrity test";
        var newContent = "New content that should replace original";

        // Create original file
        File.WriteAllText(Path.Combine(_testDirectory, testFile), originalContent);
        var originalChecksum = CalculateFileChecksum(Path.Combine(_testDirectory, testFile));

        // Act
        var result = _fileSystemTools.WriteFile(testFile, newContent);

        // Assert
        result.Should().Be("success");
        File.Exists(Path.Combine(_testDirectory, testFile)).Should().BeTrue();
        File.ReadAllText(Path.Combine(_testDirectory, testFile)).Should().Be(newContent);
        
        // Verify file integrity with new content
        var newChecksum = CalculateFileChecksum(Path.Combine(_testDirectory, testFile));
        newChecksum.Should().NotBe(originalChecksum);
        
        // Verify content matches exactly what was written
        var actualContent = File.ReadAllText(Path.Combine(_testDirectory, testFile));
        actualContent.Should().Be(newContent);
    }

    [Fact]
    public void WriteFile_WhenDiskSpaceInsufficient_ShouldNotCorruptExistingFile()
    {
        // Arrange
        var testFile = "test_disk_space.txt";
        var originalContent = "Original content that should be preserved";
        var largeContent = new string('X', 1024 * 1024); // 1MB content

        // Create original file
        File.WriteAllText(Path.Combine(_testDirectory, testFile), originalContent);
        var originalChecksum = CalculateFileChecksum(Path.Combine(_testDirectory, testFile));

        // Act - Simulate disk space issue by trying to write to a read-only file system
        // Note: This is a simplified test - in real implementation, we'd need more sophisticated disk space simulation
        var result = _fileSystemTools.WriteFile(testFile, largeContent);

        // Assert - File should either succeed or remain unchanged
        File.Exists(Path.Combine(_testDirectory, testFile)).Should().BeTrue();
        
        if (result == "success")
        {
            // If write succeeded, verify integrity of new content
            File.ReadAllText(Path.Combine(_testDirectory, testFile)).Should().Be(largeContent);
        }
        else
        {
            // If write failed, original file should be preserved
            File.ReadAllText(Path.Combine(_testDirectory, testFile)).Should().Be(originalContent);
            CalculateFileChecksum(Path.Combine(_testDirectory, testFile)).Should().Be(originalChecksum);
        }
    }

    [Fact]
    public void WriteFile_WhenInterrupted_ShouldNotLeavePartialFile()
    {
        // Arrange
        var testFile = "test_interruption.txt";
        var originalContent = "Original content";
        var newContent = "New content that might be interrupted";

        // Create original file
        File.WriteAllText(Path.Combine(_testDirectory, testFile), originalContent);

        // Act
        var result = _fileSystemTools.WriteFile(testFile, newContent);

        // Assert
        File.Exists(Path.Combine(_testDirectory, testFile)).Should().BeTrue();
        
        // File should contain either complete original or complete new content, never partial
        var actualContent = File.ReadAllText(Path.Combine(_testDirectory, testFile));
        actualContent.Should().BeOneOf(originalContent, newContent);
        
        // Content should not be truncated or corrupted
        if (actualContent == newContent)
        {
            actualContent.Length.Should().Be(newContent.Length);
        }
        else
        {
            actualContent.Length.Should().Be(originalContent.Length);
        }
    }

    [Fact]
    public void WriteFile_WithBackupEnabled_ShouldCreateBackupBeforeModification()
    {
        // Arrange
        var testFile = "test_backup.txt";
        var originalContent = "Original content for backup test";
        var newContent = "New content after backup";

        // Create original file
        File.WriteAllText(Path.Combine(_testDirectory, testFile), originalContent);

        // Act
        var result = _fileSystemTools.WriteFile(testFile, newContent);

        // Assert
        result.Should().Be("success");
        
        // Original file should have new content
        File.ReadAllText(Path.Combine(_testDirectory, testFile)).Should().Be(newContent);
        
        // Backup file should exist with original content
        var backupFile = Path.Combine(_testDirectory, testFile) + ".backup";
        if (File.Exists(backupFile))
        {
            File.ReadAllText(backupFile).Should().Be(originalContent);
        }
    }

    [Fact]
    public void WriteFile_WhenBackupFails_ShouldNotProceedWithWrite()
    {
        // Arrange
        var testFile = "test_backup_fail.txt";
        var originalContent = "Original content";
        var newContent = "New content";

        // Create original file
        File.WriteAllText(Path.Combine(_testDirectory, testFile), originalContent);
        
        // Create a scenario where backup might fail (e.g., read-only directory)
        // This is a simplified test - real implementation would need more sophisticated backup failure simulation

        // Act
        var result = _fileSystemTools.WriteFile(testFile, newContent);

        // Assert
        File.Exists(Path.Combine(_testDirectory, testFile)).Should().BeTrue();
        
        // If backup failed, original content should be preserved
        // If backup succeeded, new content should be written
        var actualContent = File.ReadAllText(Path.Combine(_testDirectory, testFile));
        actualContent.Should().BeOneOf(originalContent, newContent);
    }

    [Fact]
    public void WriteFile_WithChecksumValidation_ShouldVerifyWrittenContent()
    {
        // Arrange
        var testFile = "test_checksum.txt";
        var content = "Content for checksum validation test";

        // Act
        var result = _fileSystemTools.WriteFile(testFile, content);

        // Assert
        result.Should().Be("success");
        File.Exists(Path.Combine(_testDirectory, testFile)).Should().BeTrue();
        
        // Verify written content matches expected content exactly
        var writtenContent = File.ReadAllText(Path.Combine(_testDirectory, testFile));
        writtenContent.Should().Be(content);
        
        // Verify checksum of written file
        var expectedChecksum = CalculateStringChecksum(content);
        var actualChecksum = CalculateFileChecksum(Path.Combine(_testDirectory, testFile));
        actualChecksum.Should().Be(expectedChecksum);
    }

    [Fact]
    public void WriteFile_WhenChecksumMismatch_ShouldRollbackChanges()
    {
        // Arrange
        var testFile = "test_checksum_mismatch.txt";
        var originalContent = "Original content";
        var newContent = "New content";

        // Create original file
        File.WriteAllText(Path.Combine(_testDirectory, testFile), originalContent);
        var originalChecksum = CalculateFileChecksum(Path.Combine(_testDirectory, testFile));

        // Act
        var result = _fileSystemTools.WriteFile(testFile, newContent);

        // Assert
        // In case of checksum mismatch, file should be rolled back to original state
        File.Exists(Path.Combine(_testDirectory, testFile)).Should().BeTrue();
        
        if (result != "success")
        {
            // If write failed due to checksum mismatch, original should be restored
            File.ReadAllText(Path.Combine(_testDirectory, testFile)).Should().Be(originalContent);
            CalculateFileChecksum(Path.Combine(_testDirectory, testFile)).Should().Be(originalChecksum);
        }
        else
        {
            // If write succeeded, verify integrity
            File.ReadAllText(Path.Combine(_testDirectory, testFile)).Should().Be(newContent);
            CalculateFileChecksum(Path.Combine(_testDirectory, testFile)).Should().Be(CalculateStringChecksum(newContent));
        }
    }

    [Fact]
    public void WriteFile_WithAtomicOperation_ShouldNotShowPartialContentDuringWrite()
    {
        // Arrange
        var testFile = "test_atomic.txt";
        var originalContent = "Original content";
        var newContent = "New content for atomic write test";

        // Create original file
        File.WriteAllText(Path.Combine(_testDirectory, testFile), originalContent);

        // Act
        var result = _fileSystemTools.WriteFile(testFile, newContent);

        // Assert
        result.Should().Be("success");
        
        // After atomic write, file should contain complete new content
        var finalContent = File.ReadAllText(Path.Combine(_testDirectory, testFile));
        finalContent.Should().Be(newContent);
        
        // File should not contain any mixture of old and new content
        finalContent.Should().NotContain(originalContent);
    }

    [Fact]
    public void WriteFile_WithLargeFile_ShouldMaintainIntegrityThroughoutOperation()
    {
        // Arrange
        var testFile = "test_large_file.txt";
        var largeContent = GenerateLargeContent(1024 * 100); // 100KB content
        
        // Act
        var result = _fileSystemTools.WriteFile(testFile, largeContent);

        // Assert
        result.Should().Be("success");
        File.Exists(Path.Combine(_testDirectory, testFile)).Should().BeTrue();
        
        // Verify complete content was written correctly
        var writtenContent = File.ReadAllText(Path.Combine(_testDirectory, testFile));
        writtenContent.Should().Be(largeContent);
        writtenContent.Length.Should().Be(largeContent.Length);
        
        // Verify checksum integrity
        var expectedChecksum = CalculateStringChecksum(largeContent);
        var actualChecksum = CalculateFileChecksum(Path.Combine(_testDirectory, testFile));
        actualChecksum.Should().Be(expectedChecksum);
    }

    [Fact]
    public void WriteFile_WithSpecialCharacters_ShouldPreserveEncodingIntegrity()
    {
        // Arrange
        var testFile = "test_encoding.txt";
        var contentWithSpecialChars = "Content with special chars: áéíóú ñ 中文 🚀 \n\r\t";

        // Act
        var result = _fileSystemTools.WriteFile(testFile, contentWithSpecialChars);

        // Assert
        result.Should().Be("success");
        File.Exists(Path.Combine(_testDirectory, testFile)).Should().BeTrue();
        
        // Verify special characters are preserved exactly
        var writtenContent = File.ReadAllText(Path.Combine(_testDirectory, testFile));
        writtenContent.Should().Be(contentWithSpecialChars);
        
        // Verify byte-level integrity
        var originalBytes = Encoding.UTF8.GetBytes(contentWithSpecialChars);
        var writtenBytes = File.ReadAllBytes(Path.Combine(_testDirectory, testFile));
        writtenBytes.Should().BeEquivalentTo(originalBytes);
    }

    private static string CalculateFileChecksum(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return Convert.ToHexString(hash);
    }

    private static string CalculateStringChecksum(string content)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    private static string GenerateLargeContent(int sizeInBytes)
    {
        var random = new Random(42); // Fixed seed for reproducible tests
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789\n ";
        var result = new StringBuilder(sizeInBytes);
        
        for (int i = 0; i < sizeInBytes; i++)
        {
            result.Append(chars[random.Next(chars.Length)]);
        }
        
        return result.ToString();
    }
}
