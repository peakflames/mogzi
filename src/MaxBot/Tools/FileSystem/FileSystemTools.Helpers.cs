using System.Text;
using System.Security;
using System.Security.Cryptography;

namespace MaxBot.Tools;

public partial class FileSystemTools
{
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
    
    private string FormatXmlResponseForSearch(string status, string relativePath, string regex, string? filePattern, int filesSearched, int totalMatches, string? errorMessage, string? searchResults)
    {
        var response = new StringBuilder();
        response.AppendLine("<tool_response tool_name=\"search_files\">");
        response.AppendLine("    <notes>");
        response.AppendLine($"    Searched in `{relativePath}` for regex `{regex}`" + (string.IsNullOrEmpty(filePattern) ? "" : $" with pattern `{filePattern}`") + ".");
        if (status == "SUCCESS")
        {
            response.AppendLine($"    Found {totalMatches} matches in {filesSearched} files.");
        }
        response.AppendLine("    </notes>");
        
        response.AppendLine($"    <result status=\"{status}\" files_searched=\"{filesSearched}\" total_matches=\"{totalMatches}\" />");
        
        if (!string.IsNullOrEmpty(errorMessage))
        {
            response.AppendLine("    <error>");
            response.AppendLine($"        {SecurityElement.Escape(errorMessage)}");
            response.AppendLine("    </error>");
        }
        
        if (!string.IsNullOrEmpty(searchResults))
        {
            response.AppendLine("    <search_results>");
            response.Append(searchResults);
            response.AppendLine("    </search_results>");
        }
        
        response.AppendLine("</tool_response>");
        return response.ToString();
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
}
