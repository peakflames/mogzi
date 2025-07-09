using FluentAssertions;
using Mogzi.Domain;
using Mogzi.Tools;

namespace Mogzi.Tests.Tools;

public class GrepToolTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ApplicationConfiguration _config;
    private readonly GrepTool _grepTool;
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider;

    public GrepToolTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "MogziGrepToolTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _config = new ApplicationConfiguration { ToolApprovals = "auto" };
        _workingDirectoryProvider = new MockWorkingDirectoryProvider(_testDirectory);
        _grepTool = new GrepTool(_config, null, _workingDirectoryProvider);
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
    public async Task SearchFileContent_WithValidRegex_ShouldReturnMatchingResults()
    {
        // Arrange
        var searchDir = "search_test";
        var searchDirPath = Path.Combine(_testDirectory, searchDir);
        Directory.CreateDirectory(searchDirPath);

        await File.WriteAllTextAsync(Path.Combine(searchDirPath, "file1.txt"), "Hello World");
        await File.WriteAllTextAsync(Path.Combine(searchDirPath, "file2.log"), "Hello Universe");
        await File.WriteAllTextAsync(Path.Combine(searchDirPath, "file3.txt"), "No match here");

        // Act
        var result = await _grepTool.SearchFileContent("Hello\\s\\w+", searchDirPath);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"search_file_content\">"); // TOR-3.1
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("Found 2 match(es)");
        result.Should().Contain("<content_on_disk>");
        result.Should().Contain("file1.txt");
        result.Should().Contain("file2.log");
        result.Should().NotContain("file3.txt");
    }

    [Fact]
    public async Task SearchFileContent_WithNoMatches_ShouldReturnEmptySuccess()
    {
        // Arrange
        var searchDir = "search_no_match";
        var searchDirPath = Path.Combine(_testDirectory, searchDir);
        Directory.CreateDirectory(searchDirPath);
        await File.WriteAllTextAsync(Path.Combine(searchDirPath, "file1.txt"), "some content");

        // Act
        var result = await _grepTool.SearchFileContent("non_existent_pattern", searchDirPath);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"search_file_content\">"); // TOR-3.1
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("No matches found");
    }

    [Fact]
    public async Task SearchFileContent_WithFilePattern_ShouldFilterResults()
    {
        // Arrange
        var searchDir = "search_pattern";
        var searchDirPath = Path.Combine(_testDirectory, searchDir);
        Directory.CreateDirectory(searchDirPath);

        await File.WriteAllTextAsync(Path.Combine(searchDirPath, "file1.txt"), "Hello World");
        await File.WriteAllTextAsync(Path.Combine(searchDirPath, "file2.log"), "Hello Universe");

        // Act
        var result = await _grepTool.SearchFileContent("Hello", searchDirPath, "*.txt");

        // Assert
        result.Should().Contain("<tool_response tool_name=\"search_file_content\">"); // TOR-3.1
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("Found 1 match(es)");
        result.Should().Contain("file1.txt");
        result.Should().NotContain("file2.log");
    }
    
    [Fact]
    public async Task SearchFileContent_Recursive_ShouldFindMatchesInSubdirectories()
    {
        // Arrange
        var searchDir = "search_recursive";
        var searchDirPath = Path.Combine(_testDirectory, searchDir);
        var subDir = Path.Combine(searchDirPath, "sub");
        Directory.CreateDirectory(subDir);

        await File.WriteAllTextAsync(Path.Combine(searchDirPath, "root.txt"), "Root match");
        await File.WriteAllTextAsync(Path.Combine(subDir, "nested.txt"), "Nested match");

        // Act
        var result = await _grepTool.SearchFileContent("match", searchDirPath);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"search_file_content\">"); // TOR-3.1
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("Found 2 match(es)");
        result.Should().Contain("root.txt");
        result.Should().Contain(Path.Combine("sub", "nested.txt"));
    }

    [Fact]
    public async Task SearchFileContent_WithInvalidRegex_ShouldReturnError()
    {
        // Arrange
        var searchDir = "search_invalid_regex";
        var searchDirPath = Path.Combine(_testDirectory, searchDir);
        Directory.CreateDirectory(searchDirPath);
        await File.WriteAllTextAsync(Path.Combine(searchDirPath, "file1.txt"), "content");

        // Act
        var result = await _grepTool.SearchFileContent("[invalid", searchDirPath);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"search_file_content\">"); // TOR-3.1
        result.Should().Contain("status=\"FAILED\"");
        result.Should().Contain("<error>");
        result.Should().Contain("Invalid regular expression pattern");
    }

    [Fact]
    public async Task SearchFileContent_WithNonExistentDirectory_ShouldReturnError()
    {
        // Act
        var result = await _grepTool.SearchFileContent(".*", "/non_existent_dir");

        // Assert
        result.Should().Contain("<tool_response tool_name=\"search_file_content\">"); // TOR-7.2
        result.Should().Contain("status=\"FAILED\"");
        result.Should().Contain("<error>");
        result.Should().Contain("Search path must be within the root directory");
    }
}
