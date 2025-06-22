using FluentAssertions;
using MaxBot.Domain;
using MaxBot.Tools;

namespace MaxBot.Tests.Tools;

/// <summary>
/// Tests for TOR-3.1: Safe file system interaction.
/// These tests verify the basic safe operations of file system tools, including:
/// - Reading from existing and non-existing files.
/// - Writing new files and overwriting existing ones.
/// - Performing targeted replacements in files.
/// </summary>
public class FileSystemToolTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly MaxbotConfiguration _config;
    private readonly FileSystemTools _fileSystemTools;

    public FileSystemToolTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "MaxBotFileSystemToolTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        
        _config = new MaxbotConfiguration { ToolApprovals = "auto" };
        _fileSystemTools = new FileSystemTools(_config);
        
        Environment.CurrentDirectory = _testDirectory;
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

    // ReadFile Tests

    [Fact]
    public void ReadFile_WithExistingFile_ShouldReturnContent()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "read_test.txt");
        var content = "Hello, World!";
        File.WriteAllText(testFile, content);

        // Act
        var result = _fileSystemTools.ReadFile(testFile);

        // Assert
        result.Should().Be(content);
    }

    [Fact]
    public void ReadFile_WithNonExistentFile_ShouldReturnError()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "non_existent_file.txt");

        // Act
        var result = _fileSystemTools.ReadFile(testFile);

        // Assert
        result.Should().Contain("ERROR: File not found");
    }

    // WriteFile Tests

    [Fact]
    public void WriteFile_ToNewFile_ShouldCreateFileWithContent()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "write_test.txt");
        var content = "This is a new file.";

        // Act
        var result = _fileSystemTools.WriteFile(testFile, content);

        // Assert
        result.Should().Be("success");
        File.Exists(testFile).Should().BeTrue();
        File.ReadAllText(testFile).Should().Be(content);
    }

    [Fact]
    public void WriteFile_ToExistingFile_ShouldOverwriteContent()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "overwrite_test.txt");
        File.WriteAllText(testFile, "Initial content.");
        var newContent = "This content should overwrite the original.";

        // Act
        var result = _fileSystemTools.WriteFile(testFile, newContent);

        // Assert
        result.Should().Be("success");
        File.ReadAllText(testFile).Should().Be(newContent);
    }

    [Fact]
    public void WriteFile_ToPathWithMissingDirectories_ShouldCreateThem()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "new", "nested", "dir", "test.txt");
        var content = "File in a nested directory.";

        // Act
        var result = _fileSystemTools.WriteFile(testFile, content);

        // Assert
        result.Should().Be("success");
        File.Exists(testFile).Should().BeTrue();
        File.ReadAllText(testFile).Should().Be(content);
    }

    // ReplaceInFile Tests

    [Fact]
    public void ReplaceInFile_WithValidMatch_ShouldPerformReplacement()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "replace_test.txt");
        var initialContent = "Hello, old world!";
        File.WriteAllText(testFile, initialContent);
        var diff = "------- SEARCH\nold world\n=======\nnew world\n+++++++ REPLACE";

        // Act
        var result = _fileSystemTools.ReplaceInFile(testFile, diff);

        // Assert
        result.Should().Be("success");
        File.ReadAllText(testFile).Should().Be("Hello, new world!");
    }

    [Fact]
    public void ReplaceInFile_WithNoMatch_ShouldReturnError()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "replace_no_match.txt");
        var initialContent = "Hello, world!";
        File.WriteAllText(testFile, initialContent);
        var diff = "------- SEARCH\nnon-existent content\n=======\nreplacement\n+++++++ REPLACE";

        // Act
        var result = _fileSystemTools.ReplaceInFile(testFile, diff);

        // Assert
        result.Should().Contain("ERROR: Search block not found");
        File.ReadAllText(testFile).Should().Be(initialContent); // File should be unchanged
    }

    [Fact]
    public void ReplaceInFile_WithMultipleBlocks_ShouldPerformAllReplacements()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "replace_multiple.txt");
        var initialContent = "one two three";
        File.WriteAllText(testFile, initialContent);
        var diff = "------- SEARCH\none\n=======\n1\n+++++++ REPLACE\n" +
                   "------- SEARCH\nthree\n=======\n3\n+++++++ REPLACE";

        // Act
        var result = _fileSystemTools.ReplaceInFile(testFile, diff);

        // Assert
        result.Should().Be("success");
        File.ReadAllText(testFile).Should().Be("1 two 3");
    }

    [Fact]
    public void ReplaceInFile_WithNonExistentFile_ShouldReturnError()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "non_existent_file.txt");
        var diff = "------- SEARCH\na\n=======\nb\n+++++++ REPLACE";

        // Act
        var result = _fileSystemTools.ReplaceInFile(testFile, diff);

        // Assert
        result.Should().Contain("ERROR: File not found");
    }
}
