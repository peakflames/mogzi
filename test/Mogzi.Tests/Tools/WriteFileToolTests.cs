using FluentAssertions;
using Mogzi.Domain;
using Mogzi.Tools;

namespace Mogzi.Tests.Tools;

public class WriteFileToolTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ApplicationConfiguration _config;
    private readonly WriteFileTool _writeFileTool;
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider;

    public WriteFileToolTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "MogziWriteFileToolTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _config = new ApplicationConfiguration { ToolApprovals = "auto" };
        _workingDirectoryProvider = new MockWorkingDirectoryProvider(_testDirectory);
        _writeFileTool = new WriteFileTool(_config, null, _workingDirectoryProvider);
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
    public async Task WriteFile_ToNewFile_ShouldCreateFileWithContent()
    {
        // Arrange
        var testFile = "write_test.txt";
        var content = "This is a new file.";
        var absolutePath = Path.Combine(_testDirectory, testFile);

        // Act
        var result = await _writeFileTool.WriteFile(absolutePath, content);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"write_file\">"); // TOR-3.1
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("sha256_checksum="); // TOR-3.2
        result.Should().Contain("<content_on_disk>");
        result.Should().Contain(content);
        File.Exists(absolutePath).Should().BeTrue();
        (await File.ReadAllTextAsync(absolutePath)).Should().Be(content);
    }

    [Fact]
    public async Task WriteFile_ToExistingFile_ShouldOverwriteContent()
    {
        // Arrange
        var testFile = "overwrite_test.txt";
        var absolutePath = Path.Combine(_testDirectory, testFile);
        await File.WriteAllTextAsync(absolutePath, "Initial content.");
        var newContent = "This content should overwrite the original.";

        // Act
        var result = await _writeFileTool.WriteFile(absolutePath, newContent);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"write_file\">"); // TOR-3.1
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("sha256_checksum="); // TOR-3.2
        result.Should().Contain("<content_on_disk>");
        result.Should().Contain(newContent);
        (await File.ReadAllTextAsync(absolutePath)).Should().Be(newContent);
    }

    [Fact]
    public async Task WriteFile_ToPathWithMissingDirectories_ShouldCreateThem()
    {
        // Arrange
        var testFile = Path.Combine("new", "nested", "dir", "test.txt");
        var content = "File in a nested directory.";
        var absolutePath = Path.Combine(_testDirectory, testFile);

        // Act
        var result = await _writeFileTool.WriteFile(absolutePath, content);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"write_file\">"); // TOR-3.1
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("sha256_checksum="); // TOR-3.2
        result.Should().Contain("<content_on_disk>");
        result.Should().Contain(content);
        File.Exists(absolutePath).Should().BeTrue();
        (await File.ReadAllTextAsync(absolutePath)).Should().Be(content);
    }

    [Fact]
    public async Task WriteFile_OutsideWorkingDirectory_ShouldReturnError()
    {
        // Arrange
        var outsideDir = Path.Combine(Path.GetTempPath(), "MogziWriteFileToolTests_Outside", Guid.NewGuid().ToString());
        Directory.CreateDirectory(outsideDir);
        var outsideFile = Path.Combine(outsideDir, "outside_write_test.txt");

        // Act
        var result = await _writeFileTool.WriteFile(outsideFile, "test");

        // Assert
        result.Should().Contain("<tool_response tool_name=\"write_file\">"); // TOR-7.2
        result.Should().Contain("status=\"FAILED\"");
        result.Should().Contain("File path must be within the root directory");
        File.Exists(outsideFile).Should().BeFalse();
        Directory.Delete(outsideDir, true);
    }

    [Fact]
    public async Task WriteFile_ToReadOnlyFile_ShouldReturnError()
    {
        // Arrange
        var testFile = "readonly_write_test.txt";
        var testFilePath = Path.Combine(_testDirectory, testFile);
        await File.WriteAllTextAsync(testFilePath, "Initial content.");
        var fileInfo = new FileInfo(testFilePath);
        fileInfo.IsReadOnly = true;

        // Act
        var result = await _writeFileTool.WriteFile(testFilePath, "This should fail.");

        // Assert
        result.Should().Contain("is read-only"); // TOR-3.3
        (await File.ReadAllTextAsync(testFilePath)).Should().Be("Initial content."); // Unchanged

        // Cleanup
        fileInfo.IsReadOnly = false;
    }

    [Fact]
    public async Task WriteFile_WithReadOnlyToolApproval_ShouldReturnError()
    {
        // Arrange
        var testFile = "readonly_approval_test.txt";
        var absolutePath = Path.Combine(_testDirectory, testFile);
        var content = "This should not be written.";
        _config.ToolApprovals = "readonly";

        // Act
        var result = await _writeFileTool.WriteFile(absolutePath, content);

        // Assert
        result.Should().Contain("Tool approvals are set to readonly mode"); // TOR-7.1
        File.Exists(absolutePath).Should().BeFalse();
    }
}
