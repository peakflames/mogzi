using System.ComponentModel;
using System.Text;

namespace MaxBot.Tools;

public partial class FileSystemTools
{
    public string WriteFile(
        [Description("The path of the file to write (relative to the current working directory)")]
        string path,
        [Description("The content to write to the file. ALWAYS provide the COMPLETE intended content of the file, without any truncation or omissions. You MUST include ALL parts of the file, even if they haven't been modified.")]
        string content)
    {
        var filePath = Path.Combine(_workingDirectoryProvider.GetCurrentDirectory(), path);
        
        if (_config.ToolApprovals == "readonly")
        {
            return FormatXmlResponseForFileChange("FAILED", path, filePath, null, "File system is in readonly mode. Write operations are disabled.", null);
        }

        if (File.Exists(filePath) && new FileInfo(filePath).IsReadOnly)
        {
            return FormatXmlResponseForFileChange("FAILED", path, filePath, null, $"File '{path}' is read-only and cannot be modified.", null);
        }
        
        _llmResponseDetailsCallback?.Invoke($"Writing to file '{path}' with integrity preservation.", ConsoleColor.DarkGray);
        

        if (!IsPathInWorkingDirectory(filePath))
        {
            return FormatXmlResponseForFileChange("FAILED", path, filePath, null, "Path is outside the working directory", null);
        }

        try
        {
            var response = WriteFileWithIntegrity(filePath, path, content, "file_write");
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke(response, ConsoleColor.DarkGray);
            }
            return response;
        }
        catch (Exception ex)
        {
            var msg = $"Failed to write file with integrity preservation. {ex.Message}";
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"ERROR: {msg}", ConsoleColor.Red);
            }
            return FormatXmlResponseForFileChange("FAILED", path, filePath, null, msg, null);
        }
    }

    /// <summary>
    /// Implements TOR-3.2: File integrity preservation during operations
    /// Features:
    /// - Atomic write operations using temporary files
    /// - Backup creation before modification
    /// - Checksum validation of written content
    /// - Rollback on failure
    /// </summary>
    private string WriteFileWithIntegrity(string filePath, string relativePath, string content, string toolName)
    {
        string? backupPath = null;
        string? tempPath = null;
        bool fileExisted = File.Exists(filePath);

        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Step 1: Create backup if file exists
            if (fileExisted)
            {
                backupPath = CreateBackup(filePath);
            }

            // Step 2: Write to temporary file first (atomic operation)
            tempPath = filePath + ".tmp." + Guid.NewGuid().ToString("N")[..8];
            // Use UTF8 without BOM to avoid checksum mismatches
            var utf8WithoutBom = new UTF8Encoding(false);
            File.WriteAllText(tempPath, content, utf8WithoutBom);

            // Step 3: Validate written content integrity
            var expectedChecksum = CalculateStringChecksum(content);
            var actualChecksum = CalculateFileChecksum(tempPath);

            if (expectedChecksum != actualChecksum)
            {
                throw new InvalidOperationException($"Checksum mismatch. Expected: {expectedChecksum}, Actual: {actualChecksum}");
            }

            // Step 4: Atomic move from temp to final location
            if (fileExisted)
            {
                File.Delete(filePath);
            }
            File.Move(tempPath, filePath);

            // Step 5: Final integrity verification
            var finalChecksum = CalculateFileChecksum(filePath);
            if (expectedChecksum != finalChecksum)
            {
                throw new InvalidOperationException($"Final checksum verification failed. Expected: {expectedChecksum}, Actual: {finalChecksum}");
            }

            // Step 6: Read content from disk for verification
            var contentOnDisk = File.ReadAllText(filePath);

            // Return success response with new XML format
            return FormatXmlResponseForFileChange("SUCCESS", relativePath, filePath, finalChecksum, null, contentOnDisk, toolName);
        }
        catch (Exception ex)
        {
            // Rollback: Restore from backup if it exists
            try
            {
                if (tempPath != null && File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                if (backupPath != null && File.Exists(backupPath))
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    File.Move(backupPath, filePath);
                }
            }
            catch (Exception rollbackEx)
            {
                return FormatXmlResponseForFileChange("FAILED", relativePath, filePath, null, 
                    $"Write failed and rollback failed. Original error: {ex.Message}. Rollback error: {rollbackEx.Message}", null, toolName);
            }

            return FormatXmlResponseForFileChange("FAILED", relativePath, filePath, null, 
                $"Write failed but original file restored from backup. {ex.Message}", null, toolName);
        }
        finally
        {
            // Clean up backup file if it exists
            if (backupPath != null && File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
        }
    }

    private string CreateBackup(string filePath)
    {
        var backupPath = filePath + ".backup";
        var counter = 1;
        
        // Ensure unique backup filename
        while (File.Exists(backupPath))
        {
            backupPath = $"{filePath}.backup.{counter}";
            counter++;
        }
        
        File.Copy(filePath, backupPath, true);
        return backupPath;
    }
    
    public string ReplaceInFile(
        [Description("The path of the file to modify (relative to the current working directory)")]
        string path,
        [Description("One or more SEARCH/REPLACE blocks")]
        string diff)
    {
        var filePath = Path.Combine(_workingDirectoryProvider.GetCurrentDirectory(), path);
        
        if (_config.ToolApprovals == "readonly")
        {
            return FormatXmlResponseForFileChange("FAILED", path, filePath, null, "File system is in readonly mode. Write operations are disabled.", null, "replace_in_file");
        }

        if (File.Exists(filePath) && new FileInfo(filePath).IsReadOnly)
        {
            return FormatXmlResponseForFileChange("FAILED", path, filePath, null, $"File '{path}' is read-only and cannot be modified.", null, "replace_in_file");
        }

        _llmResponseDetailsCallback?.Invoke($"Replacing content in file '{path}'.", ConsoleColor.DarkGray);

        if (!IsPathInWorkingDirectory(filePath))
        {
            return FormatXmlResponseForFileChange("FAILED", path, filePath, null, "Path is outside the working directory", null, "replace_in_file");
        }

        if (!File.Exists(filePath))
        {
            return FormatXmlResponseForFileChange("FAILED", path, filePath, null, $"File not found: {filePath}", null, "replace_in_file");
        }

        try
        {
            var originalContent = File.ReadAllText(filePath);
            var modifiedContent = originalContent;

            var searchBlocks = diff.Split(new[] { "------- SEARCH" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var block in searchBlocks)
            {
                var parts = block.Split(new[] { "=======" }, StringSplitOptions.None);
                if (parts.Length != 2)
                {
                    return FormatXmlResponseForFileChange("FAILED", path, filePath, null, "Invalid SEARCH/REPLACE block format.", null, "replace_in_file");
                }

                var search = parts[0].Trim('\r', '\n');
                var replace = parts[1].Split(new[] { "+++++++ REPLACE" }, StringSplitOptions.None)[0].Trim('\r', '\n');

                if (modifiedContent.Contains(search))
                {
                    modifiedContent = modifiedContent.Replace(search, replace);
                }
                else
                {
                    return FormatXmlResponseForFileChange("FAILED", path, filePath, null, "Search block not found", null, "replace_in_file");
                }
            }

            return WriteFileWithIntegrity(filePath, path, modifiedContent, "replace_in_file");
        }
        catch (Exception ex)
        {
            var msg = $"Failed to replace content in file. {ex.Message}";
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"ERROR: {msg}", ConsoleColor.Red);
            }
            return FormatXmlResponseForFileChange("FAILED", path, filePath, null, msg, null, "replace_in_file");
        }
    }
}
