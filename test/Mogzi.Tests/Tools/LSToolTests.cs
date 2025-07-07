using FluentAssertions;
using Mogzi.Domain;
using Mogzi.Tools;

namespace Mogzi.Tests.Tools;

public class LSToolTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ApplicationConfiguration _config;
    private readonly LSTool _lsTool;
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider;

    public LSToolTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "MogziLSToolTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _config = new ApplicationConfiguration { ToolApprovals = "auto" };
        _workingDirectoryProvider = new MockWorkingDirectoryProvider(_testDirectory);
        _lsTool = new LSTool(_config, null, _workingDirectoryProvider);
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
    public void ListDirectory_WithExistingDirectory_ShouldReturnXmlResponse()
    {
        // Arrange
        var testSubDir = "list_test_dir";
        var testSubDirPath = Path.Combine(_testDirectory, testSubDir);
        Directory.CreateDirectory(testSubDirPath);
        
        var testFile1 = Path.Combine(testSubDirPath, "file1.txt");
        var testFile2 = Path.Combine(testSubDirPath, "file2.txt");
        File.WriteAllText(testFile1, "Content 1");
        File.WriteAllText(testFile2, "Content 2");

        // Act
        var result = _lsTool.ListDirectory(testSubDirPath);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"list_directory\">"); // TOR-3.1
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("<content_on_disk>");
        result.Should().Contain("file1.txt");
        result.Should().Contain("file2.txt");
    }

    [Fact]
    public void ListDirectory_WithNonExistentDirectory_ShouldReturnXmlError()
    {
        // Arrange
        var nonExistentDir = Path.Combine(_testDirectory, "non_existent_directory");

        // Act
        var result = _lsTool.ListDirectory(nonExistentDir);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"list_directory\">"); // TOR-3.1
        result.Should().Contain("status=\"FAILED\"");
        result.Should().Contain("<error>");
        result.Should().Contain("Directory not found");
    }

    [Fact]
    public void ListDirectory_WithGitIgnore_ShouldRespectIt()
    {
        // Arrange
        var gitRepoDir = Path.Combine(_testDirectory, "git_repo");
        Directory.CreateDirectory(gitRepoDir);
        File.WriteAllText(Path.Combine(gitRepoDir, ".gitignore"), "*.log");
        File.WriteAllText(Path.Combine(gitRepoDir, "file1.txt"), "content");
        File.WriteAllText(Path.Combine(gitRepoDir, "file2.log"), "content");

        // Act
        var result = _lsTool.ListDirectory(gitRepoDir, respect_git_ignore: true);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"list_directory\">"); // TOR-3.1
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("file1.txt");
        result.Should().NotContain("file2.log");
    }

    [Fact]
    public void ListDirectory_WithEmptyDirectory_ShouldReturnEmptyListing()
    {
        // Arrange
        var emptyDir = "empty_test_dir";
        var emptyDirPath = Path.Combine(_testDirectory, emptyDir);
        Directory.CreateDirectory(emptyDirPath);

        // Act
        var result = _lsTool.ListDirectory(emptyDirPath);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"list_directory\">"); // TOR-3.1
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("(empty directory)");
    }

    [Fact]
    public void ListDirectory_ShouldIncludeFileSizes()
    {
        // Arrange
        var testSubDir = "size_test_dir";
        var testSubDirPath = Path.Combine(_testDirectory, testSubDir);
        Directory.CreateDirectory(testSubDirPath);
        
        var testFile = Path.Combine(testSubDirPath, "test_file.txt");
        File.WriteAllText(testFile, "Hello World"); // 11 bytes

        // Act
        var result = _lsTool.ListDirectory(testSubDirPath);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"list_directory\">"); // TOR-3.1
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("<content_on_disk>");
        result.Should().Contain("(11 B)"); // File size
        result.Should().Contain("test_file.txt");
    }

    [Fact]
    public void ListDirectory_OutsideWorkingDirectory_ShouldReturnError()
    {
        // Arrange
        var outsideDir = Path.Combine(Path.GetTempPath(), "MogziLSToolTests_Outside", Guid.NewGuid().ToString());
        Directory.CreateDirectory(outsideDir);

        // Act
        var result = _lsTool.ListDirectory(outsideDir);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"list_directory\">"); // TOR-7.2
        result.Should().Contain("status=\"FAILED\"");
        result.Should().Contain("Path must be within the root directory");
        Directory.Delete(outsideDir, true);
    }
}
