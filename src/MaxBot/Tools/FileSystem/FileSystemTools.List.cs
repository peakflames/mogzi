using System.ComponentModel;

namespace MaxBot.Tools;

public partial class FileSystemTools
{
    public string ListFiles(
        [Description("The path of the directory to list contents for (relative to the current working directory)")]
        string path,
        [Description("(optional) Whether to list files recursively. Use true for recursive listing, false or omit for top-level only.")]
        bool recursive = false)
    {
        _llmResponseDetailsCallback?.Invoke($"Listing files in '{path}'{(recursive ? " recursively" : "")}.", ConsoleColor.DarkGray);
        
        var filePath = Path.Combine(_workingDirectoryProvider.GetCurrentDirectory(), path);

        if (!Directory.Exists(filePath))
        {
            var errorMsg = $"The directory '{filePath}' does not exist.";
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"ERROR: Failed to list files. {errorMsg}", ConsoleColor.Red);
            }
            return FormatXmlResponseForDirectoryListing("FAILED", path, filePath, 0, errorMsg, null);
        }

        try
        {
            // Get directories and files with blacklist filtering for recursive mode
            string[] directories;
            string[] files;
            
            if (recursive)
            {
                var (filteredDirectories, filteredFiles) = GetFilteredDirectoriesAndFiles(filePath);
                directories = filteredDirectories.ToArray();
                files = filteredFiles.ToArray();
            }
            else
            {
                directories = Directory.GetDirectories(filePath);
                files = Directory.GetFiles(filePath);
            }
            
            var totalItems = directories.Length + files.Length;
            
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"Listed {directories.Length} directories and {files.Length} files for '{filePath}'.", ConsoleColor.DarkGray);
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
                _llmResponseDetailsCallback?.Invoke($"ERROR: {errorMsg}", ConsoleColor.Red);
            }
            return FormatXmlResponseForDirectoryListing("FAILED", path, filePath, 0, errorMsg, null);
        }
    }
    
    /// <summary>
    /// Recursively gets directories and files while respecting the blacklist
    /// </summary>
    private (List<string> directories, List<string> files) GetFilteredDirectoriesAndFiles(string rootPath)
    {
        var allDirectories = new List<string>();
        var allFiles = new List<string>();
        var directoriesToProcess = new Queue<string>();
        
        directoriesToProcess.Enqueue(rootPath);
        
        while (directoriesToProcess.Count > 0)
        {
            var currentDir = directoriesToProcess.Dequeue();
            
            try
            {
                // Get immediate subdirectories
                var subdirectories = Directory.GetDirectories(currentDir);
                foreach (var subdir in subdirectories)
                {
                    var dirName = Path.GetFileName(subdir);
                    
                    // Add to results (we include blacklisted dirs in the listing, just don't recurse into them)
                    allDirectories.Add(subdir);
                    
                    // Only recurse if not blacklisted
                    if (!BlacklistedDirectories.Contains(dirName))
                    {
                        directoriesToProcess.Enqueue(subdir);
                    }
                }
                
                // Get files in current directory
                var filesInDir = Directory.GetFiles(currentDir);
                allFiles.AddRange(filesInDir);
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories we can't access
                continue;
            }
            catch (DirectoryNotFoundException)
            {
                // Skip directories that no longer exist
                continue;
            }
        }
        
        return (allDirectories, allFiles);
    }
}
