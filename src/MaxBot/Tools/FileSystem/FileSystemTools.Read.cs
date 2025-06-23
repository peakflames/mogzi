using System.ComponentModel;

namespace MaxBot.Tools;

public partial class FileSystemTools
{
    public string ReadFile(
        [Description("The path of the file to read (relative to the current working directory)")]
        string path)
    {
        _llmResponseDetailsCallback?.Invoke($"Reading file '{path}'.", ConsoleColor.DarkGray);
        
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
                _llmResponseDetailsCallback?.Invoke($"ERROR: {errorMsg}", ConsoleColor.Red);
            }
            return FormatXmlResponseForFileRead("FAILED", path, filePath, null, null, null, errorMsg, null);
        }

        try
        {
            var content = File.ReadAllText(filePath);
            var fileInfo = new FileInfo(filePath);
            var checksum = CalculateStringChecksum(content);
            
            _llmResponseDetailsCallback?.Invoke($"Successfully read file '{path}' ({FormatFileSize(fileInfo.Length)}).", ConsoleColor.DarkGray);
            
            return FormatXmlResponseForFileRead("SUCCESS", path, filePath, fileInfo.Length, fileInfo.LastWriteTime, checksum, null, content);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Failed to read file. {ex.Message}";
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"ERROR: {errorMsg}", ConsoleColor.Red);
            }
            return FormatXmlResponseForFileRead("FAILED", path, filePath, null, null, null, errorMsg, null);
        }
    }
}
