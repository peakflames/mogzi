using FluentAssertions;
using Mogzi.Domain;
using Mogzi.Tools;

namespace Mogzi.Tests.Tools;

public class EditToolTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ApplicationConfiguration _config;
    private readonly EditTool _editTool;
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider;

    public EditToolTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "MogziEditToolTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _config = new ApplicationConfiguration { ToolApprovals = "auto" };
        _workingDirectoryProvider = new MockWorkingDirectoryProvider(_testDirectory);
        _editTool = new EditTool(_config, null, _workingDirectoryProvider);
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
    public async Task Replace_WithValidMatch_ShouldPerformReplacement()
    {
        // Arrange
        var testFile = "replace_test.txt";
        var initialContent = "Hello, old world!";
        var absolutePath = Path.Combine(_testDirectory, testFile);
        await File.WriteAllTextAsync(absolutePath, initialContent);
        var oldString = "old world";
        var newString = "new world";

        // Act
        var result = await _editTool.Replace(absolutePath, oldString, newString);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"replace_in_file\">"); // TOR-3.1
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("sha256_checksum="); // TOR-3.2
        result.Should().Contain("<content_on_disk>");
        result.Should().Contain("Hello, new world!");
        (await File.ReadAllTextAsync(absolutePath)).Should().Be("Hello, new world!");
    }

    [Fact]
    public async Task Replace_WithNoMatch_ShouldReturnError()
    {
        // Arrange
        var testFile = "replace_no_match.txt";
        var initialContent = "Hello, world!";
        var absolutePath = Path.Combine(_testDirectory, testFile);
        await File.WriteAllTextAsync(absolutePath, initialContent);
        var oldString = "non-existent content";
        var newString = "replacement";

        // Act
        var result = await _editTool.Replace(absolutePath, oldString, newString);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"replace_in_file\">"); // TOR-3.1
        result.Should().Contain("status=\"FAILED\"");
        result.Should().Contain("could not find the string to replace");
        (await File.ReadAllTextAsync(absolutePath)).Should().Be(initialContent); // File should be unchanged
    }

    [Fact]
    public async Task Replace_WithMultipleOccurrences_ShouldPerformAllReplacements()
    {
        // Arrange
        var testFile = "replace_multiple.txt";
        var initialContent = "one two three one";
        var absolutePath = Path.Combine(_testDirectory, testFile);
        await File.WriteAllTextAsync(absolutePath, initialContent);
        var oldString = "one";
        var newString = "1";

        // Act
        var result = await _editTool.Replace(absolutePath, oldString, newString, expected_replacements: 2);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"replace_in_file\">"); // TOR-3.1
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("sha256_checksum="); // TOR-3.2
        result.Should().Contain("<content_on_disk>");
        result.Should().Contain("1 two three 1");
        (await File.ReadAllTextAsync(absolutePath)).Should().Be("1 two three 1");
    }

    [Fact]
    public async Task Replace_WithNonExistentFile_ShouldReturnError()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "non_existent_file.txt");
        var oldString = "a";
        var newString = "b";

        // Act
        var result = await _editTool.Replace(testFile, oldString, newString);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"replace_in_file\">"); // TOR-3.1
        result.Should().Contain("status=\"FAILED\"");
        result.Should().Contain("File not found");
    }

    [Fact]
    public async Task Replace_OutsideWorkingDirectory_ShouldReturnError()
    {
        // Arrange
        var outsideDir = Path.Combine(Path.GetTempPath(), "MogziEditToolTests_Outside", Guid.NewGuid().ToString());
        Directory.CreateDirectory(outsideDir);
        var outsideFile = Path.Combine(outsideDir, "outside_replace_test.txt");
        await File.WriteAllTextAsync(outsideFile, "test");
        var oldString = "test";
        var newString = "new_test";

        // Act
        var result = await _editTool.Replace(outsideFile, oldString, newString);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"replace_in_file\">"); // TOR-7.2
        result.Should().Contain("status=\"FAILED\"");
        result.Should().Contain("File path must be within the root directory");
        (await File.ReadAllTextAsync(outsideFile)).Should().Be("test"); // Unchanged
        Directory.Delete(outsideDir, true);
    }

    [Fact]
    public async Task Replace_ToReadOnlyFile_ShouldReturnError()
    {
        // Arrange
        var testFile = "readonly_replace_test.txt";
        var testFilePath = Path.Combine(_testDirectory, testFile);
        await File.WriteAllTextAsync(testFilePath, "Initial content.");
        var fileInfo = new FileInfo(testFilePath);
        fileInfo.IsReadOnly = true;
        var oldString = "Initial";
        var newString = "New";

        // Act
        var result = await _editTool.Replace(testFilePath, oldString, newString);

        // Assert
        result.Should().Contain("is read-only"); // TOR-3.3
        (await File.ReadAllTextAsync(testFilePath)).Should().Be("Initial content."); // Unchanged

        // Cleanup
        fileInfo.IsReadOnly = false;
    }

    [Fact]
    public async Task Replace_WithReadOnlyToolApproval_ShouldReturnError()
    {
        // Arrange
        var testFile = "readonly_approval_test.txt";
        var absolutePath = Path.Combine(_testDirectory, testFile);
        await File.WriteAllTextAsync(absolutePath, "initial content");
        var oldString = "initial";
        var newString = "changed";
        _config.ToolApprovals = "readonly";

        // Act
        var result = await _editTool.Replace(absolutePath, oldString, newString);

        // Assert
        result.Should().Contain("Tool approvals are set to readonly mode"); // TOR-7.1
        (await File.ReadAllTextAsync(absolutePath)).Should().Be("initial content");
    }
}
