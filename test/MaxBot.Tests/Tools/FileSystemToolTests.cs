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
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider;

    public FileSystemToolTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "MaxBotFileSystemToolTests", Guid.NewGuid().ToString());
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

    // ReadFile Tests

    [Fact]
    public void ReadFile_WithExistingFile_ShouldReturnContent()
    {
        // Arrange
        var testFile = "read_test.txt";
        var content = "Hello, World!";
        File.WriteAllText(Path.Combine(_testDirectory, testFile), content);

        // Act
        var result = _fileSystemTools.ReadFile(testFile);

        // Assert
        result.Should().Be(content);
    }

    [Fact]
    public void ReadFile_WithNonExistentFile_ShouldReturnError()
    {
        // Arrange
        var testFile = "non_existent_file.txt";

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
        var testFile = "write_test.txt";
        var content = "This is a new file.";

        // Act
        var result = _fileSystemTools.WriteFile(testFile, content);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"file_write\">");
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("sha256_checksum=");
        result.Should().Contain("<content_on_disk>");
        result.Should().Contain(content);
        File.Exists(Path.Combine(_testDirectory, testFile)).Should().BeTrue();
        File.ReadAllText(Path.Combine(_testDirectory, testFile)).Should().Be(content);
    }

    [Fact]
    public void WriteFile_ToExistingFile_ShouldOverwriteContent()
    {
        // Arrange
        var testFile = "overwrite_test.txt";
        File.WriteAllText(Path.Combine(_testDirectory, testFile), "Initial content.");
        var newContent = "This content should overwrite the original.";

        // Act
        var result = _fileSystemTools.WriteFile(testFile, newContent);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"file_write\">");
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("sha256_checksum=");
        result.Should().Contain("<content_on_disk>");
        result.Should().Contain(newContent);
        File.ReadAllText(Path.Combine(_testDirectory, testFile)).Should().Be(newContent);
    }

    [Fact]
    public void WriteFile_ToPathWithMissingDirectories_ShouldCreateThem()
    {
        // Arrange
        var testFile = Path.Combine("new", "nested", "dir", "test.txt");
        var content = "File in a nested directory.";

        // Act
        var result = _fileSystemTools.WriteFile(testFile, content);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"file_write\">");
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("sha256_checksum=");
        result.Should().Contain("<content_on_disk>");
        result.Should().Contain(content);
        File.Exists(Path.Combine(_testDirectory, testFile)).Should().BeTrue();
        File.ReadAllText(Path.Combine(_testDirectory, testFile)).Should().Be(content);
    }

    // ReplaceInFile Tests

    [Fact]
    public void ReplaceInFile_WithValidMatch_ShouldPerformReplacement()
    {
        // Arrange
        var testFile = "replace_test.txt";
        var initialContent = "Hello, old world!";
        File.WriteAllText(Path.Combine(_testDirectory, testFile), initialContent);
        var diff = "------- SEARCH\nold world\n=======\nnew world\n+++++++ REPLACE";

        // Act
        var result = _fileSystemTools.ReplaceInFile(testFile, diff);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"replace_in_file\">");
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("sha256_checksum=");
        result.Should().Contain("<content_on_disk>");
        result.Should().Contain("Hello, new world!");
        File.ReadAllText(Path.Combine(_testDirectory, testFile)).Should().Be("Hello, new world!");
    }

    [Fact]
    public void ReplaceInFile_WithNoMatch_ShouldReturnError()
    {
        // Arrange
        var testFile = "replace_no_match.txt";
        var initialContent = "Hello, world!";
        File.WriteAllText(Path.Combine(_testDirectory, testFile), initialContent);
        var diff = "------- SEARCH\nnon-existent content\n=======\nreplacement\n+++++++ REPLACE";

        // Act
        var result = _fileSystemTools.ReplaceInFile(testFile, diff);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"replace_in_file\">");
        result.Should().Contain("status=\"FAILED\"");
        result.Should().Contain("Search block not found");
        File.ReadAllText(Path.Combine(_testDirectory, testFile)).Should().Be(initialContent); // File should be unchanged
    }

    [Fact]
    public void ReplaceInFile_WithMultipleBlocks_ShouldPerformAllReplacements()
    {
        // Arrange
        var testFile = "replace_multiple.txt";
        var initialContent = "one two three";
        File.WriteAllText(Path.Combine(_testDirectory, testFile), initialContent);
        var diff = "------- SEARCH\none\n=======\n1\n+++++++ REPLACE\n" +
                   "------- SEARCH\nthree\n=======\n3\n+++++++ REPLACE";

        // Act
        var result = _fileSystemTools.ReplaceInFile(testFile, diff);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"replace_in_file\">");
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("sha256_checksum=");
        result.Should().Contain("<content_on_disk>");
        result.Should().Contain("1 two 3");
        File.ReadAllText(Path.Combine(_testDirectory, testFile)).Should().Be("1 two 3");
    }

    [Fact]
    public void ReplaceInFile_WithNonExistentFile_ShouldReturnError()
    {
        // Arrange
        var testFile = "non_existent_file.txt";
        var diff = "------- SEARCH\na\n=======\nb\n+++++++ REPLACE";

        // Act
        var result = _fileSystemTools.ReplaceInFile(testFile, diff);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"replace_in_file\">");
        result.Should().Contain("status=\"FAILED\"");
        result.Should().Contain("File not found");
    }

    // TOR-7.2: Working Directory Constraint Tests

    [Fact]
    public void ReadFile_OutsideWorkingDirectory_ShouldReturnError()
    {
        // Arrange
        var outsideDir = Path.Combine(Path.GetTempPath(), "MaxBotFileSystemToolTests_Outside", Guid.NewGuid().ToString());
        Directory.CreateDirectory(outsideDir);
        var outsideFile = Path.Combine(outsideDir, "outside_file.txt");
        File.WriteAllText(outsideFile, "test");

        // Act
        var result = _fileSystemTools.ReadFile(outsideFile);

        // Assert
        result.Should().Contain("ERROR: Path is outside the working directory");
        Directory.Delete(outsideDir, true);
    }

    [Fact]
    public void WriteFile_OutsideWorkingDirectory_ShouldReturnError()
    {
        // Arrange
        var outsideDir = Path.Combine(Path.GetTempPath(), "MaxBotFileSystemToolTests_Outside", Guid.NewGuid().ToString());
        Directory.CreateDirectory(outsideDir);
        var outsideFile = Path.Combine(outsideDir, "outside_write_test.txt");

        // Act
        var result = _fileSystemTools.WriteFile(outsideFile, "test");

        // Assert
        result.Should().Contain("<tool_response tool_name=\"file_write\">");
        result.Should().Contain("status=\"FAILED\"");
        result.Should().Contain("Path is outside the working directory");
        File.Exists(outsideFile).Should().BeFalse();
        Directory.Delete(outsideDir, true);
    }

    [Fact]
    public void ReplaceInFile_OutsideWorkingDirectory_ShouldReturnError()
    {
        // Arrange
        var outsideDir = Path.Combine(Path.GetTempPath(), "MaxBotFileSystemToolTests_Outside", Guid.NewGuid().ToString());
        Directory.CreateDirectory(outsideDir);
        var outsideFile = Path.Combine(outsideDir, "outside_replace_test.txt");
        File.WriteAllText(outsideFile, "test");
        var diff = "------- SEARCH\ntest\n=======\nnew_test\n+++++++ REPLACE";

        // Act
        var result = _fileSystemTools.ReplaceInFile(outsideFile, diff);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"replace_in_file\">");
        result.Should().Contain("status=\"FAILED\"");
        result.Should().Contain("Path is outside the working directory");
        File.ReadAllText(outsideFile).Should().Be("test"); // Unchanged
        Directory.Delete(outsideDir, true);
    }

    // ListFiles Tests

    [Fact]
    public void ListFiles_WithExistingDirectory_ShouldReturnXmlResponse()
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
        var result = _fileSystemTools.ListFiles(testSubDir);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"list_files\">");
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("item_count=\"2\"");
        result.Should().Contain("<directory_contents>");
        result.Should().Contain("file1.txt");
        result.Should().Contain("file2.txt");
        result.Should().Contain("Found 2 items");
    }

    [Fact]
    public void ListFiles_WithNonExistentDirectory_ShouldReturnXmlError()
    {
        // Arrange
        var nonExistentDir = "non_existent_directory";

        // Act
        var result = _fileSystemTools.ListFiles(nonExistentDir);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"list_files\">");
        result.Should().Contain("status=\"FAILED\"");
        result.Should().Contain("<error>");
        result.Should().Contain("does not exist");
    }

    [Fact]
    public void ListFiles_WithRecursiveOption_ShouldIncludeSubdirectories()
    {
        // Arrange
        var testSubDir = "recursive_test_dir";
        var testSubDirPath = Path.Combine(_testDirectory, testSubDir);
        Directory.CreateDirectory(testSubDirPath);
        
        var nestedDir = Path.Combine(testSubDirPath, "nested");
        Directory.CreateDirectory(nestedDir);
        
        var testFile1 = Path.Combine(testSubDirPath, "file1.txt");
        var testFile2 = Path.Combine(nestedDir, "file2.txt");
        File.WriteAllText(testFile1, "Content 1");
        File.WriteAllText(testFile2, "Content 2");

        // Act
        var result = _fileSystemTools.ListFiles(testSubDir, recursive: true);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"list_files\">");
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("item_count=\"3\""); // 1 directory + 2 files
        result.Should().Contain("<directory_contents>");
        result.Should().Contain("file1.txt");
        result.Should().Contain("nested/");
        result.Should().Contain("nested/file2.txt");
    }

    [Fact]
    public void ListFiles_WithEmptyDirectory_ShouldReturnEmptyListing()
    {
        // Arrange
        var emptyDir = "empty_test_dir";
        var emptyDirPath = Path.Combine(_testDirectory, emptyDir);
        Directory.CreateDirectory(emptyDirPath);

        // Act
        var result = _fileSystemTools.ListFiles(emptyDir);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"list_files\">");
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("item_count=\"0\"");
        result.Should().Contain("Found 0 items");
    }

    [Fact]
    public void ListFiles_ShouldIncludeFileSizesAndTimestamps()
    {
        // Arrange
        var testSubDir = "size_test_dir";
        var testSubDirPath = Path.Combine(_testDirectory, testSubDir);
        Directory.CreateDirectory(testSubDirPath);
        
        var testFile = Path.Combine(testSubDirPath, "test_file.txt");
        File.WriteAllText(testFile, "Hello World"); // 11 bytes

        // Act
        var result = _fileSystemTools.ListFiles(testSubDir);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"list_files\">");
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("<directory_contents>");
        result.Should().MatchRegex(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}"); // Timestamp format
        result.Should().Contain("11.0 B"); // File size
        result.Should().Contain("test_file.txt");
    }

    // TOR-3.3: File Permission Respect Tests

    [Fact]
    public void WriteFile_ToReadOnlyFile_ShouldReturnError()
    {
        // Arrange
        var testFile = "readonly_write_test.txt";
        var testFilePath = Path.Combine(_testDirectory, testFile);
        File.WriteAllText(testFilePath, "Initial content.");
        var fileInfo = new FileInfo(testFilePath);
        fileInfo.IsReadOnly = true;

        // Act
        var result = _fileSystemTools.WriteFile(testFile, "This should fail.");

        // Assert
        result.Should().Contain("is read-only and cannot be modified");
        File.ReadAllText(testFilePath).Should().Be("Initial content."); // Unchanged

        // Cleanup
        fileInfo.IsReadOnly = false;
    }

    [Fact]
    public void ReplaceInFile_ToReadOnlyFile_ShouldReturnError()
    {
        // Arrange
        var testFile = "readonly_replace_test.txt";
        var testFilePath = Path.Combine(_testDirectory, testFile);
        File.WriteAllText(testFilePath, "Initial content.");
        var fileInfo = new FileInfo(testFilePath);
        fileInfo.IsReadOnly = true;
        var diff = "------- SEARCH\nInitial\n=======\nNew\n+++++++ REPLACE";

        // Act
        var result = _fileSystemTools.ReplaceInFile(testFile, diff);

        // Assert
        result.Should().Contain("is read-only and cannot be modified");
        File.ReadAllText(testFilePath).Should().Be("Initial content."); // Unchanged

        // Cleanup
        fileInfo.IsReadOnly = false;
    }
}
