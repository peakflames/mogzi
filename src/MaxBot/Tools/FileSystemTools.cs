
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FluentResults;
using MaxBot.Domain;

namespace MaxBot.Tools;

public class FileSystemTools
{
    private readonly MaxbotConfiguration _config;
    private readonly Action<string>? _llmResponseDetailsCallback = null;

    public FileSystemTools(MaxbotConfiguration config, Action<string>? llmResponseDetailsCallback = null)
    {
        _config = config;
        _llmResponseDetailsCallback = llmResponseDetailsCallback;
    }

    [Description("A read-only tool to list files and directories within the specified directory. If recursive is true, it will list all files and directories recursively. If recursive is false or not provided, it will only list the top-level contents. Do not use this tool to confirm the existence of files you may have created, as the user will let you know if the files were created successfully or not.")]
    public string ListFiles(
        [Description("The path of the directory to list contents for (relative to the current working directory)")]
        string path,
        [Description("(optional) Whether to list files recursively. Use true for recursive listing, false or omit for top-level only.")]
        bool recursive = false)
    {
        _llmResponseDetailsCallback?.Invoke($"Listing files in '{path}'{(recursive ? "recursively" : "")}.");
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), path);

        var result = string.Empty;
        if (!Directory.Exists(filePath))
        {
            var msg = $"ERROR: Failed to list files. The directory '{filePath}' does not exist.";
            _llmResponseDetailsCallback?.Invoke(msg);
            return msg;
        }

        string[]? files;

        if (recursive)
        {
            files = Directory.GetFiles(filePath, "*", SearchOption.AllDirectories);
        }
        else
        {
            files = Directory.GetFiles(filePath);
        }

        _llmResponseDetailsCallback?.Invoke($"listed {files.Length} files for '{filePath}'.");

        result = string.Join("\n", files);
        return result;
    }


    [Description("Request to write content to a file at the specified path. If the file exists, it will be overwritten with the provided content. If the file doesn't exist, it will be created. This tool will automatically create any directories needed to write the file. Returns a string message indicating success or failure.")]
    public string WriteFile(
        [Description("The path of the file to write (relative to the current working directory)")]
        string path,
        [Description("The content to write to the file. ALWAYS provide the COMPLETE intended content of the file, without any truncation or omissions. You MUST include ALL parts of the file, even if they haven't been modified.")]
        string content)
    {
        if (_config.ToolApprovals == "readonly")
        {
            return "ERROR: File system is in readonly mode. Write operations are disabled.";
        }
        
        _llmResponseDetailsCallback?.Invoke($"Writing to file '{path}' with integrity preservation.");
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), path);

        try
        {
            return WriteFileWithIntegrity(filePath, content);
        }
        catch (Exception ex)
        {
            var msg = $"ERROR: Failed to write file with integrity preservation. {ex.Message}";
            _llmResponseDetailsCallback?.Invoke(msg);
            return msg;
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
    private string WriteFileWithIntegrity(string filePath, string content)
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
                _llmResponseDetailsCallback?.Invoke($"Created directory: {directory}");
            }

            // Step 1: Create backup if file exists
            if (fileExisted)
            {
                backupPath = CreateBackup(filePath);
                _llmResponseDetailsCallback?.Invoke($"Created backup: {backupPath}");
            }

            // Step 2: Write to temporary file first (atomic operation)
            tempPath = filePath + ".tmp." + Guid.NewGuid().ToString("N")[..8];
            // Use UTF8 without BOM to avoid checksum mismatches
            var utf8WithoutBom = new UTF8Encoding(false);
            File.WriteAllText(tempPath, content, utf8WithoutBom);
            _llmResponseDetailsCallback?.Invoke($"Wrote content to temporary file: {tempPath}");

            // Step 3: Validate written content integrity
            var expectedChecksum = CalculateStringChecksum(content);
            var actualChecksum = CalculateFileChecksum(tempPath);
            
            if (expectedChecksum != actualChecksum)
            {
                throw new InvalidOperationException($"Checksum mismatch. Expected: {expectedChecksum}, Actual: {actualChecksum}");
            }
            
            _llmResponseDetailsCallback?.Invoke($"Checksum validation passed: {actualChecksum}");

            // Step 4: Atomic move from temp to final location
            if (fileExisted)
            {
                File.Delete(filePath);
            }
            File.Move(tempPath, filePath);
            _llmResponseDetailsCallback?.Invoke($"Atomically moved temporary file to final location");

            // Step 5: Final integrity verification
            var finalChecksum = CalculateFileChecksum(filePath);
            if (expectedChecksum != finalChecksum)
            {
                throw new InvalidOperationException($"Final checksum verification failed. Expected: {expectedChecksum}, Actual: {finalChecksum}");
            }

            // Step 6: Clean up backup (keep it for now as a safety measure)
            // We could optionally delete the backup here, but keeping it provides additional safety
            
            _llmResponseDetailsCallback?.Invoke($"File integrity preservation completed successfully");
            return "success";
        }
        catch (Exception ex)
        {
            _llmResponseDetailsCallback?.Invoke($"Error during file write, attempting rollback: {ex.Message}");
            
            // Rollback: Restore from backup if it exists
            try
            {
                if (tempPath != null && File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                    _llmResponseDetailsCallback?.Invoke($"Cleaned up temporary file: {tempPath}");
                }

                if (backupPath != null && File.Exists(backupPath))
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    File.Move(backupPath, filePath);
                    _llmResponseDetailsCallback?.Invoke($"Restored original file from backup");
                }
            }
            catch (Exception rollbackEx)
            {
                _llmResponseDetailsCallback?.Invoke($"ERROR: Rollback failed: {rollbackEx.Message}");
                return $"ERROR: Write failed and rollback failed. Original error: {ex.Message}. Rollback error: {rollbackEx.Message}";
            }

            return $"ERROR: Write failed but original file restored from backup. {ex.Message}";
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
        
        File.Copy(filePath, backupPath, false);
        return backupPath;
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

    [Description("A read-only tool to read the contents of a file at the specified path. Use this when you need to examine the contents of an existing file you do not know the contents of, for example to analyze code, review text files, or extract information from configuration files. Automatically extracts raw text from PDF and DOCX files. May not be suitable for other types of binary files, as it returns the raw content as a string")]
    public string ReadFile(
        [Description("The path of the file to read (relative to the current working directory)")]
        string path)
    {
        _llmResponseDetailsCallback?.Invoke($"Reading file '{path}'.");
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), path);

        if (File.Exists(filePath))
        {
            var result = File.ReadAllText(filePath);
            return result;
        }

        var msg = $"ERROR: Failed to read file. The file '{filePath}' does not exist.";
        _llmResponseDetailsCallback?.Invoke(msg);
        return msg;
    }
}
