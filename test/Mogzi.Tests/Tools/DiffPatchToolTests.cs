// Copyright (c) 2024 Taylor Southwick. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.

using FluentAssertions;
using Mogzi.Domain;
using Mogzi.Tools;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mogzi.Tests.Tools;

public class DiffPatchToolTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly MaxbotConfiguration _config;
    private readonly DiffPatchTools _diffPatchTools;
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider;

    public DiffPatchToolTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "MaxBotDiffPatchToolTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _config = new MaxbotConfiguration { ToolApprovals = "all" };
        _workingDirectoryProvider = new MockWorkingDirectoryProvider(_testDirectory);
        _diffPatchTools = new DiffPatchTools(_config, null, _workingDirectoryProvider);
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
    public void ApplyCodePatch_WithValidPatch_ShouldApplyChanges()
    {
        // Arrange
        var testFile = "test.txt";
        var initialContent = "Hello, old world!";
        File.WriteAllText(Path.Combine(_testDirectory, testFile), initialContent);
        var patch = @"--- a/test.txt
+++ b/test.txt
@@ -1 +1 @@
-Hello, old world!
+Hello, new world!
";

        // Act
        var result = _diffPatchTools.ApplyCodePatch(testFile, patch);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"apply_code_patch\">");
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("<content_on_disk>Hello, new world!</content_on_disk>");
        File.ReadAllText(Path.Combine(_testDirectory, testFile)).Should().Be("Hello, new world!");
    }

    [Fact]
    public void GenerateCodePatch_WithChanges_ShouldCreatePatch()
    {
        // Arrange
        var testFile = "test.txt";
        var initialContent = "line1\nline2\nline3";
        var modifiedContent = "line1\nline2a\nline3";
        File.WriteAllText(Path.Combine(_testDirectory, testFile), initialContent);

        // Act
        var result = _diffPatchTools.GenerateCodePatch(testFile, modifiedContent);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"generate_code_patch\">");
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("<patch>");
        result.Should().Contain("-line2");
        result.Should().Contain("+line2a");
    }

    [Fact]
    public void PreviewPatchApplication_WithValidPatch_ShouldShowPreview()
    {
        // Arrange
        var testFile = "test.txt";
        var initialContent = "one\ntwo\nthree";
        File.WriteAllText(Path.Combine(_testDirectory, testFile), initialContent);
        var patch = @"--- a/test.txt
+++ b/test.txt
@@ -1,3 +1,3 @@
 one
-two
+zwei
 three
";

        // Act
        var result = _diffPatchTools.PreviewPatchApplication(testFile, patch);

        // Assert
        result.Should().Contain("<tool_response tool_name=\"preview_patch_application\">");
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("<preview_content>");
        result.Should().Contain("one");
        result.Should().Contain("zwei");
        result.Should().Contain("three");
        result.Should().Contain("</preview_content>");
        File.ReadAllText(Path.Combine(_testDirectory, testFile)).Should().Be(initialContent); // File should be unchanged
    }

    [Fact]
    public void ApplyCodePatch_WithReadOnlyMode_ShouldReturnError()
    {
        // Arrange
        var readOnlyConfig = new MaxbotConfiguration { ToolApprovals = "readonly" };
        var readOnlyTools = new DiffPatchTools(readOnlyConfig, null, _workingDirectoryProvider);
        var testFile = "test.txt";
        var initialContent = "Hello, world!";
        File.WriteAllText(Path.Combine(_testDirectory, testFile), initialContent);
        var patch = @"--- a/test.txt
+++ b/test.txt
@@ -1 +1 @@
-Hello, world!
+Hello, universe!
";

        // Act
        var result = readOnlyTools.ApplyCodePatch(testFile, patch);

        // Assert
        result.Should().Contain("status=\"FAILED\"");
        result.Should().Contain("Tool is in read-only mode");
        File.ReadAllText(Path.Combine(_testDirectory, testFile)).Should().Be(initialContent); // File should be unchanged
    }

    [Fact]
    public void ApplyCodePatch_WithNonExistentFile_ShouldReturnError()
    {
        // Arrange
        var patch = @"--- a/nonexistent.txt
+++ b/nonexistent.txt
@@ -1 +1 @@
-old content
+new content
";

        // Act
        var result = _diffPatchTools.ApplyCodePatch("nonexistent.txt", patch);

        // Assert
        result.Should().Contain("status=\"FAILED\"");
        result.Should().Contain("File not found");
    }

    [Fact]
    public void ApplyCodePatch_WithInvalidPatch_ShouldReturnError()
    {
        // Arrange
        var testFile = "test.txt";
        var initialContent = "Hello, world!";
        File.WriteAllText(Path.Combine(_testDirectory, testFile), initialContent);
        var invalidPatch = "This is not a valid patch format";

        // Act
        var result = _diffPatchTools.ApplyCodePatch(testFile, invalidPatch);

        // Assert
        result.Should().Contain("status=\"FAILED\"");
        result.Should().Contain("Invalid patch format");
    }

    [Fact]
    public void ApplyCodePatch_WithMultipleHunks_ShouldApplyAll()
    {
        // Arrange
        var testFile = "test.txt";
        var initialContent = "line1\nline2\nline3\nline4\nline5";
        File.WriteAllText(Path.Combine(_testDirectory, testFile), initialContent);
        var patch = @"--- a/test.txt
+++ b/test.txt
@@ -1,2 +1,2 @@
-line1
+LINE1
 line2
@@ -4,2 +4,2 @@
 line4
-line5
+LINE5
";

        // Act
        var result = _diffPatchTools.ApplyCodePatch(testFile, patch);

        // Assert
        result.Should().Contain("status=\"SUCCESS\"");
        var finalContent = File.ReadAllText(Path.Combine(_testDirectory, testFile));
        var expectedContent = "LINE1" + Environment.NewLine + "line2" + Environment.NewLine + "line3" + Environment.NewLine + "line4" + Environment.NewLine + "LINE5";
        finalContent.Should().Be(expectedContent);
    }

    [Fact]
    public void ApplyCodePatch_WithFuzzyMatching_ShouldSucceed()
    {
        // Arrange
        var testFile = "test.txt";
        var initialContent = "line1\nline2\nline3\nline4"; // Content with extra line
        File.WriteAllText(Path.Combine(_testDirectory, testFile), initialContent);
        var patch = @"--- a/test.txt
+++ b/test.txt
@@ -1,3 +1,3 @@
 line1
-line2
+modified_line2
 line3
";

        // Act
        var result = _diffPatchTools.ApplyCodePatch(testFile, patch, useFuzzyMatching: true);

        // Assert
        result.Should().Contain("status=\"SUCCESS\"");
        // Note: This patch succeeds with regular matching, so fuzzy matching isn't needed
        // The test verifies that fuzzy matching mode doesn't break normal operation
    }

    [Fact]
    public void ApplyCodePatch_WithoutFuzzyMatching_ShouldFailOnMismatch()
    {
        // Arrange
        var testFile = "test.txt";
        var initialContent = "line1\nline2_modified\nline3"; // Content that doesn't exactly match patch
        File.WriteAllText(Path.Combine(_testDirectory, testFile), initialContent);
        var patch = @"--- a/test.txt
+++ b/test.txt
@@ -1,3 +1,3 @@
 line1
-line2
+modified_line2
 line3
";

        // Act
        var result = _diffPatchTools.ApplyCodePatch(testFile, patch, useFuzzyMatching: false);

        // Assert
        result.Should().Contain("status=\"FAILED\"");
    }

    [Fact]
    public void ApplyCodePatch_WithPathTraversalAttempt_ShouldReturnError()
    {
        // Arrange
        var patch = @"--- a/../../../etc/passwd
+++ b/../../../etc/passwd
@@ -1 +1 @@
-old content
+new content
";

        // Act
        var result = _diffPatchTools.ApplyCodePatch("../../../etc/passwd", patch);

        // Assert
        result.Should().Contain("status=\"FAILED\"");
        result.Should().Contain("Path is outside working directory");
    }

    [Fact]
    public void GenerateCodePatch_WithNonExistentFile_ShouldReturnError()
    {
        // Act
        var result = _diffPatchTools.GenerateCodePatch("nonexistent.txt", "new content");

        // Assert
        result.Should().Contain("status=\"FAILED\"");
        result.Should().Contain("File not found");
    }

    [Fact]
    public void GenerateCodePatch_WithNoChanges_ShouldCreateEmptyPatch()
    {
        // Arrange
        var testFile = "test.txt";
        var content = "unchanged content";
        File.WriteAllText(Path.Combine(_testDirectory, testFile), content);

        // Act
        var result = _diffPatchTools.GenerateCodePatch(testFile, content);

        // Assert
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("Hunks: 0");
    }

    [Fact]
    public void PreviewPatchApplication_WithNonExistentFile_ShouldReturnError()
    {
        // Arrange
        var patch = @"--- a/nonexistent.txt
+++ b/nonexistent.txt
@@ -1 +1 @@
-old content
+new content
";

        // Act
        var result = _diffPatchTools.PreviewPatchApplication("nonexistent.txt", patch);

        // Assert
        result.Should().Contain("status=\"FAILED\"");
        result.Should().Contain("File not found");
    }

    [Fact]
    public void PreviewPatchApplication_WithInvalidPatch_ShouldReturnError()
    {
        // Arrange
        var testFile = "test.txt";
        var initialContent = "Hello, world!";
        File.WriteAllText(Path.Combine(_testDirectory, testFile), initialContent);
        var invalidPatch = "This is not a valid patch format";

        // Act
        var result = _diffPatchTools.PreviewPatchApplication(testFile, invalidPatch);

        // Assert
        result.Should().Contain("status=\"FAILED\"");
        result.Should().Contain("Invalid patch format");
    }

    [Fact]
    public void PreviewPatchApplication_WithConflictingPatch_ShouldShowFailure()
    {
        // Arrange
        var testFile = "test.txt";
        var initialContent = "line1\nline2\nline3";
        File.WriteAllText(Path.Combine(_testDirectory, testFile), initialContent);
        var conflictingPatch = @"--- a/test.txt
+++ b/test.txt
@@ -1,3 +1,3 @@
 line1
-different_line
+modified_line
 line3
";

        // Act
        var result = _diffPatchTools.PreviewPatchApplication(testFile, conflictingPatch);

        // Assert
        result.Should().Contain("status=\"FAILED\"");
        result.Should().Contain("✗ Patch cannot be applied");
    }

    [Fact]
    public void ApplyCodePatch_WithAdditionOnly_ShouldAddLines()
    {
        // Arrange
        var testFile = "test.txt";
        var initialContent = "line1\nline3";
        File.WriteAllText(Path.Combine(_testDirectory, testFile), initialContent);
        var patch = @"--- a/test.txt
+++ b/test.txt
@@ -1,2 +1,3 @@
 line1
+line2
 line3
";

        // Act
        var result = _diffPatchTools.ApplyCodePatch(testFile, patch);

        // Assert
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("Lines added: 1");
        result.Should().Contain("Lines removed: 0");
        var expectedContent = "line1" + Environment.NewLine + "line2" + Environment.NewLine + "line3";
        File.ReadAllText(Path.Combine(_testDirectory, testFile)).Should().Be(expectedContent);
    }

    [Fact]
    public void ApplyCodePatch_WithRemovalOnly_ShouldRemoveLines()
    {
        // Arrange
        var testFile = "test.txt";
        var initialContent = "line1\nline2\nline3";
        File.WriteAllText(Path.Combine(_testDirectory, testFile), initialContent);
        var patch = @"--- a/test.txt
+++ b/test.txt
@@ -1,3 +1,2 @@
 line1
-line2
 line3
";

        // Act
        var result = _diffPatchTools.ApplyCodePatch(testFile, patch);

        // Assert
        result.Should().Contain("status=\"SUCCESS\"");
        result.Should().Contain("Lines added: 0");
        result.Should().Contain("Lines removed: 1");
        var expectedContent = "line1" + Environment.NewLine + "line3";
        File.ReadAllText(Path.Combine(_testDirectory, testFile)).Should().Be(expectedContent);
    }
}
