using FluentAssertions;
using Mogzi.Domain;
using Mogzi.Tools;

namespace Mogzi.Tests.Tools;

public class ReadTextFileToolTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ApplicationConfiguration _config;
    private readonly ReadTextFileTool _readTextFileTool;
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider;

    public ReadTextFileToolTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "MogziReadTextFileToolTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _config = new ApplicationConfiguration { ToolApprovals = "auto" };
        var mockProvider = new MockWorkingDirectoryProvider(_testDirectory);
        _workingDirectoryProvider = mockProvider;
        _readTextFileTool = new ReadTextFileTool(_config, null, mockProvider);
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

    private string Between(string str, string start, string end)
    {
        var startIndex = str.IndexOf(start) + start.Length;
        var endIndex = str.IndexOf(end, startIndex);
        return str.Substring(startIndex, endIndex - startIndex);
    }

    [Fact]
    public async Task ReadFile_WithExistingFile_ShouldReturnXmlResponse()
    {
        // Arrange
        var testFile = "read_test.txt";
        var content = "Hello, World!";
        var absolutePath = Path.Combine(_testDirectory, testFile);
        await File.WriteAllTextAsync(absolutePath, content);

        // Act
        var result = await _readTextFileTool.ReadTextFile(absolutePath);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"read_text_file\">"); // TOR-3.1
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("sha256_checksum="); // TOR-3.2
        result.Should().Contain("<content_on_disk>");
        result.Should().Contain(content);
    }

    [Fact]
    public async Task ReadFile_WithNonExistentFile_ShouldReturnXmlError()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "non_existent_file.txt");

        // Act
        var result = await _readTextFileTool.ReadTextFile(testFile);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"read_text_file\">"); // TOR-3.1
        result.Should().Contain("status=\"FAILED\"");
        result.Should().Contain("<error>");
        result.Should().Contain("File not found");
    }

    [Fact]
    public async Task ReadFile_ShouldIncludeValidChecksum()
    {
        // Arrange
        var testFile = "checksum_test.txt";
        var content = "Test content for checksum validation";
        var absolutePath = Path.Combine(_testDirectory, testFile);
        await File.WriteAllTextAsync(absolutePath, content);

        // Act
        var result = await _readTextFileTool.ReadTextFile(absolutePath);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"read_text_file\">"); // TOR-3.1
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().MatchRegex(@"sha256_checksum=""[a-f0-9]{64}"""); // TOR-3.2
        result.Should().Contain("<content_on_disk>");
        result.Should().Contain(content);
    }

    [Fact]
    public async Task ReadFile_WithLineRange_ShouldReturnPartialContent()
    {
        // Arrange
        var testFile = "range_test.txt";
        var content = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5";
        var absolutePath = Path.Combine(_testDirectory, testFile);
        await File.WriteAllTextAsync(absolutePath, content);

        // Act
        var result = await _readTextFileTool.ReadTextFile(absolutePath, offset: 1, limit: 2);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"read_text_file\">"); // TOR-3.1
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("<content_on_disk>Line 2\nLine 3</content_on_disk>");
    }

    [Fact]
    public async Task ReadFile_WithEmptyFile_ShouldReturnXmlResponse()
    {
        // Arrange
        var testFile = "empty_test.txt";
        var absolutePath = Path.Combine(_testDirectory, testFile);
        await File.WriteAllTextAsync(absolutePath, "");

        // Act
        var result = await _readTextFileTool.ReadTextFile(absolutePath);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"read_text_file\">"); // TOR-3.1
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("<content_on_disk></content_on_disk>");
        result.Should().MatchRegex(@"sha256_checksum=""[a-f0-9]{64}"""); // TOR-3.2
    }

    [Fact]
    public async Task ReadFile_WithLargeFile_ShouldDefaultToLimit()
    {
        // Arrange
        var testFile = "large_test.txt";
        var content = new string('A', 6000); // > 5000 limit
        var absolutePath = Path.Combine(_testDirectory, testFile);
        await File.WriteAllTextAsync(absolutePath, content);

        // Act
        var result = await _readTextFileTool.ReadTextFile(absolutePath);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"read_text_file\">"); // TOR-3.1
        result.Should().Contain("status=\"SUCCESS\"");
        var resultContent = Between(result, "<content_on_disk>", "</content_on_disk>");
        resultContent.Length.Should().Be(5000);
    }

    [Fact]
    public async Task ReadFile_OutsideWorkingDirectory_ShouldReturnXmlError()
    {
        // Arrange
        var outsideDir = Path.Combine(Path.GetTempPath(), "MogziReadTextFileToolTests_Outside", Guid.NewGuid().ToString());
        Directory.CreateDirectory(outsideDir);
        var outsideFile = Path.Combine(outsideDir, "outside_file.txt");
        await File.WriteAllTextAsync(outsideFile, "test");

        // Act
        var result = await _readTextFileTool.ReadTextFile(outsideFile);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"read_text_file\">"); // TOR-7.2
        result.Should().Contain("status=\"FAILED\"");
        result.Should().Contain("<error>");
        result.Should().Contain("File path must be within the root directory");
        Directory.Delete(outsideDir, true);
    }
}
