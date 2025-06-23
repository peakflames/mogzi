
using System.IO;
using System.Security.Cryptography;
using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using FluentResults;
using MaxBot.Domain;

namespace MaxBot.Tools;

public class FileSystemTools
{
    private readonly MaxbotConfiguration _config;
    private readonly Action<string>? _llmResponseDetailsCallback = null;
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider;

    public FileSystemTools(MaxbotConfiguration config, Action<string>? llmResponseDetailsCallback = null, IWorkingDirectoryProvider? workingDirectoryProvider = null)
    {
        _config = config;
        _llmResponseDetailsCallback = llmResponseDetailsCallback;
        _workingDirectoryProvider = workingDirectoryProvider ?? new DefaultWorkingDirectoryProvider();
    }

    public List<AIFunction> GetTools()
    {
        return
        [
            AIFunctionFactory.Create(
                ListFiles,
                new AIFunctionFactoryOptions
                {
                    Name = "list_files",
                    Description = "A read-only tool to list files and directories within the specified directory. If recursive is true, it will list all files and directories recursively. If recursive is false or not provided, it will only list the top-level contents. This tool can be useful to confirm the existence of files you may have created as part of a prudent verification strategy."
                }),
            AIFunctionFactory.Create(
                WriteFile,
                new AIFunctionFactoryOptions
                {
                    Name = "write_file",
                    Description = "Request to write content to a file at the specified path. If the file exists, it will be overwritten with the provided content. If the file doesn't exist, it will be created. This tool will automatically create any directories needed to write the file. Returns a string message indicating status, absolute_path, and the full, updated content of the file."
                }),
            AIFunctionFactory.Create(
                ReadFile,
                new AIFunctionFactoryOptions
                {
                    Name = "read_file",
                    Description = "A read-only tool to read the contents of a file at the specified path. Use this when you need to examine the contents of an existing file you do not know the contents of, for example to analyze code, review text files, or extract information from configuration files. Automatically extracts raw text from PDF and DOCX files. May not be suitable for other types of binary files, as it returns the raw content as a string"
                }),
            AIFunctionFactory.Create(
                ReplaceInFile,
                new AIFunctionFactoryOptions
                {
                    Name = "replace_in_file",
                    Description = "Request to replace sections of content in an existing file using SEARCH/REPLACE blocks that define exact changes to specific parts of the file. This tool should be used when you need to make targeted changes to specific parts of a file. Returns a string message indicating status, absolute_path, and the full, updated content of the file read from disk."
                })
        ];
    }

    public string ListFiles(
        [Description("The path of the directory to list contents for (relative to the current working directory)")]
        string path,
        [Description("(optional) Whether to list files recursively. Use true for recursive listing, false or omit for top-level only.")]
        bool recursive = false)
    {
        if (_config.Debug)
        {
            _llmResponseDetailsCallback?.Invoke($"Listing files in '{path}'{(recursive ? " recursively" : "")}.");
        }
        var filePath = Path.Combine(_workingDirectoryProvider.GetCurrentDirectory(), path);

        if (!Directory.Exists(filePath))
        {
            var errorMsg = $"The directory '{filePath}' does not exist.";
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"ERROR: Failed to list files. {errorMsg}");
            }
            return FormatXmlResponseForDirectoryListing("FAILED", path, filePath, 0, errorMsg, null);
        }

        try
        {
            // Get directories
            string[] directories;
            if (recursive)
            {
                directories = Directory.GetDirectories(filePath, "*", SearchOption.AllDirectories);
            }
            else
            {
                directories = Directory.GetDirectories(filePath);
            }
            
            // Get files
            string[] files;
            if (recursive)
            {
                files = Directory.GetFiles(filePath, "*", SearchOption.AllDirectories);
            }
            else
            {
                files = Directory.GetFiles(filePath);
            }
            
            var totalItems = directories.Length + files.Length;
            
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"Listed {directories.Length} directories and {files.Length} files for '{filePath}'.");
            }

            // Format directory contents with file sizes and timestamps
            var directoryContents = FormatDirectoryContentsWithDetails(filePath, directories, files, recursive);
            
            return FormatXmlResponseForDirectoryListing("SUCCESS", path, filePath, totalItems, null, directoryContents);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Failed to list files and directories. {ex.Message}";
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"ERROR: {errorMsg}");
            }
            return FormatXmlResponseForDirectoryListing("FAILED", path, filePath, 0, errorMsg, null);
        }
    }

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
        
        if (_config.Debug)
        {
            _llmResponseDetailsCallback?.Invoke($"Writing to file '{path}' with integrity preservation.");
        }

        if (!IsPathInWorkingDirectory(filePath))
        {
            return FormatXmlResponseForFileChange("FAILED", path, filePath, null, "Path is outside the working directory", null);
        }

        try
        {
            var response = WriteFileWithIntegrity(filePath, path, content, "file_write");
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke(response);
            }
            return response;
        }
        catch (Exception ex)
        {
            var msg = $"Failed to write file with integrity preservation. {ex.Message}";
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"ERROR: {msg}");
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

    public string ReadFile(
        [Description("The path of the file to read (relative to the current working directory)")]
        string path)
    {
        if (_config.Debug)
        {
            _llmResponseDetailsCallback?.Invoke($"Reading file '{path}'.");
        }
        var filePath = Path.Combine(_workingDirectoryProvider.GetCurrentDirectory(), path);

        if (!IsPathInWorkingDirectory(filePath))
        {
            return FormatXmlResponseForFileRead("FAILED", path, filePath, null, null, null, "Path is outside the working directory", null);
        }

        if (!File.Exists(filePath))
        {
            var errorMsg = $"File not found: {filePath}";
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"ERROR: {errorMsg}");
            }
            return FormatXmlResponseForFileRead("FAILED", path, filePath, null, null, null, errorMsg, null);
        }

        try
        {
            var content = File.ReadAllText(filePath);
            var fileInfo = new FileInfo(filePath);
            var checksum = CalculateStringChecksum(content);
            
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"Successfully read file '{path}' ({FormatFileSize(fileInfo.Length)}).");
            }
            
            return FormatXmlResponseForFileRead("SUCCESS", path, filePath, fileInfo.Length, fileInfo.LastWriteTime, checksum, null, content);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Failed to read file. {ex.Message}";
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"ERROR: {errorMsg}");
            }
            return FormatXmlResponseForFileRead("FAILED", path, filePath, null, null, null, errorMsg, null);
        }
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

        if (_config.Debug)
        {
            _llmResponseDetailsCallback?.Invoke($"Replacing content in file '{path}'.");
        }

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
                _llmResponseDetailsCallback?.Invoke($"ERROR: {msg}");
            }
            return FormatXmlResponseForFileChange("FAILED", path, filePath, null, msg, null, "replace_in_file");
        }
    }

    private bool IsPathInWorkingDirectory(string path)
    {
        var workingDirectory = Path.GetFullPath(_workingDirectoryProvider.GetCurrentDirectory());
        var fullPath = Path.GetFullPath(path);

        // Ensure workingDirectory has a trailing slash for correct comparison
        if (!workingDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            workingDirectory += Path.DirectorySeparatorChar;
        }

        // For the check to be safe, we also need to ensure the file path
        // doesn't use parent directory traversal trickery like ".." to escape.
        // Path.GetFullPath should resolve this, making the check below sufficient.
        return fullPath.StartsWith(workingDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private string FormatXmlResponseForFileChange(string status, string relativePath, string absolutePath, string? checksum, string? errorMessage, string? contentOnDisk, string toolName = "file_write")
    {
        var response = new StringBuilder();
        response.AppendLine($"<tool_response tool_name=\"{toolName}\">");
        response.AppendLine("    <notes>");
        response.AppendLine($"    Target relative path is `{relativePath}`");
        response.AppendLine("    </notes>");
        
        if (status == "SUCCESS")
        {
            response.AppendLine($"    <result status=\"{status}\" absolute_path=\"{absolutePath}\" sha256_checksum=\"{checksum}\" />");
        }
        else
        {
            response.AppendLine($"    <result status=\"{status}\" absolute_path=\"{absolutePath}\" />");
        }
        
        if (!string.IsNullOrEmpty(errorMessage))
        {
            response.AppendLine("    <error>");
            response.AppendLine($"        {errorMessage}");
            response.AppendLine("    </error>");
        }
        
        if (!string.IsNullOrEmpty(contentOnDisk))
        {
            response.AppendLine("    <content_on_disk>");
            response.AppendLine(contentOnDisk);
            response.AppendLine("    </content_on_disk>");
        }
        
        response.AppendLine("</tool_response>");
        return response.ToString();
    }

    private string FormatXmlResponseForDirectoryListing(string status, string relativePath, string absolutePath, int itemCount, string? errorMessage, string? directoryContents)
    {
        var response = new StringBuilder();
        response.AppendLine("<tool_response tool_name=\"list_files\">");
        response.AppendLine("    <notes>");
        response.AppendLine($"    Listed contents of directory `{relativePath}`");
        if (status == "SUCCESS")
        {
            response.AppendLine($"    Found {itemCount} items");
        }
        response.AppendLine("    </notes>");
        
        if (status == "SUCCESS")
        {
            response.AppendLine($"    <result status=\"{status}\" directory_path=\"{absolutePath}\" item_count=\"{itemCount}\" />");
        }
        else
        {
            response.AppendLine($"    <result status=\"{status}\" directory_path=\"{absolutePath}\" />");
        }
        
        if (!string.IsNullOrEmpty(errorMessage))
        {
            response.AppendLine("    <error>");
            response.AppendLine($"        {errorMessage}");
            response.AppendLine("    </error>");
        }
        
        if (!string.IsNullOrEmpty(directoryContents))
        {
            response.AppendLine("    <directory_contents>");
            response.AppendLine(directoryContents);
            response.AppendLine("    </directory_contents>");
        }
        
        response.AppendLine("</tool_response>");
        return response.ToString();
    }

    private string FormatDirectoryContentsWithDetails(string basePath, string[] directories, string[] files, bool recursive)
    {
        var contents = new StringBuilder();
        var allItems = new List<(string path, bool isDirectory, long size, DateTime lastModified)>();

        // Add directories
        foreach (var dir in directories)
        {
            var dirInfo = new DirectoryInfo(dir);
            allItems.Add((dir, true, 0, dirInfo.LastWriteTime));
        }

        // Add files with size information
        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            allItems.Add((file, false, fileInfo.Length, fileInfo.LastWriteTime));
        }

        // Sort by path for consistent output
        allItems.Sort((a, b) => string.Compare(a.path, b.path, StringComparison.OrdinalIgnoreCase));

        foreach (var item in allItems)
        {
            var relativePath = Path.GetRelativePath(basePath, item.path);
            // Normalize path separators to forward slashes for consistent output
            relativePath = relativePath.Replace('\\', '/');
            var sizeStr = item.isDirectory ? "<DIR>" : FormatFileSize(item.size);
            var timeStr = item.lastModified.ToString("yyyy-MM-dd HH:mm:ss");
            
            contents.AppendLine($"{timeStr}  {sizeStr,10}  {relativePath}{(item.isDirectory ? "/" : "")}");
        }

        return contents.ToString().TrimEnd();
    }

    private string FormatXmlResponseForFileRead(string status, string relativePath, string absolutePath, long? fileSize, DateTime? lastModified, string? checksum, string? errorMessage, string? fileContent)
    {
        var response = new StringBuilder();
        response.AppendLine("<tool_response tool_name=\"read_file\">");
        response.AppendLine("    <notes>");
        response.AppendLine($"    Target relative path is `{relativePath}`");
        if (status == "SUCCESS" && fileSize.HasValue && lastModified.HasValue)
        {
            response.AppendLine($"    File size: {FormatFileSize(fileSize.Value)}, Last modified: {lastModified.Value:yyyy-MM-dd HH:mm:ss}");
        }
        response.AppendLine("    </notes>");
        
        if (status == "SUCCESS")
        {
            response.AppendLine($"    <result status=\"{status}\" absolute_path=\"{absolutePath}\" file_size=\"{fileSize}\" last_modified=\"{lastModified:yyyy-MM-dd HH:mm:ss}\" sha256_checksum=\"{checksum}\" />");
        }
        else
        {
            response.AppendLine($"    <result status=\"{status}\" absolute_path=\"{absolutePath}\" />");
        }
        
        if (!string.IsNullOrEmpty(errorMessage))
        {
            response.AppendLine("    <error>");
            response.AppendLine($"        {errorMessage}");
            response.AppendLine("    </error>");
        }
        
        if (status == "SUCCESS")
        {
            response.AppendLine("    <file_content>");
            if (!string.IsNullOrEmpty(fileContent))
            {
                response.AppendLine(fileContent);
            }
            response.AppendLine("    </file_content>");
        }
        
        response.AppendLine("</tool_response>");
        return response.ToString();
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes == 0) return "0 B";
        
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        double size = bytes;
        
        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }
        
        return $"{size:F1} {suffixes[suffixIndex]}";
    }
}
