using System.ComponentModel;
using System.Security;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace Mogzi.Tools;

public class ReadImageFileTool(MaxbotConfiguration config, Action<string, ConsoleColor>? llmResponseDetailsCallback = null, IWorkingDirectoryProvider? workingDirectoryProvider = null)
{
    private readonly MaxbotConfiguration _config = config;
    private readonly Action<string, ConsoleColor>? _llmResponseDetailsCallback = llmResponseDetailsCallback;
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider = workingDirectoryProvider ?? new DefaultWorkingDirectoryProvider();

    public AIFunction GetTool()
    {
        return AIFunctionFactory.Create(
            ReadImageFile,
            new AIFunctionFactoryOptions
            {
                Name = "read_image_file",
                Description = "Reads an image file from the local filesystem and returns it as multimodal content for AI analysis. Supports PNG, JPG, JPEG, GIF, WEBP, SVG, and BMP formats. The image will be included in the conversation as visual content that can be analyzed by the AI model."
            });
    }

    public async Task<string> ReadImageFile(
        [Description("The absolute path to the image file to read (e.g., '/home/user/project/image.png'). Relative paths are not supported. You must provide an absolute path.")] string absolute_path)
    {
        _llmResponseDetailsCallback?.Invoke($"Reading image file '{absolute_path}'.", ConsoleColor.DarkGray);

        try
        {
            // Validate parameters
            var validationError = ValidateParameters(absolute_path);
            if (validationError != null)
            {
                return CreateErrorResponse("read_image_file", validationError);
            }

            var workingDirectory = _workingDirectoryProvider.GetCurrentDirectory();
            var absolutePath = Path.GetFullPath(absolute_path);

            // Security validation - ensure path is within working directory
            if (!IsPathInWorkingDirectory(absolutePath, workingDirectory))
            {
                return CreateErrorResponse("read_image_file", $"File path must be within the root directory ({workingDirectory}): {absolute_path}");
            }

            // Check if file exists
            if (!File.Exists(absolutePath))
            {
                return CreateErrorResponse("read_image_file", $"File not found: {absolute_path}");
            }

            // Validate file extension
            var fileExtension = Path.GetExtension(absolutePath).ToLowerInvariant();
            if (!IsImageFile(fileExtension))
            {
                return CreateErrorResponse("read_image_file", $"File is not a supported image format. Supported formats: PNG, JPG, JPEG, GIF, WEBP, SVG, BMP. File: {absolute_path}");
            }

            // Check if file is readable
            var fileInfo = new FileInfo(absolutePath);
            if (!HasReadPermission(fileInfo))
            {
                return CreateErrorResponse("read_image_file", $"File is not readable: {absolute_path}");
            }

            // Read image as binary data
            var imageBytes = await File.ReadAllBytesAsync(absolutePath);
            var mimeType = GetImageMimeType(fileExtension);
            var checksum = ComputeSha256(imageBytes);
            var fileName = Path.GetFileName(absolutePath);

            // Return success response with image information
            // The actual image content will be handled by the AI system through DataContent
            return CreateSuccessResponse(absolute_path, absolutePath, fileName, imageBytes.Length, mimeType, checksum);
        }
        catch (UnauthorizedAccessException)
        {
            return CreateErrorResponse("read_image_file", $"Access denied reading file: {absolute_path}");
        }
        catch (DirectoryNotFoundException)
        {
            return CreateErrorResponse("read_image_file", $"Directory not found for file: {absolute_path}");
        }
        catch (FileNotFoundException)
        {
            return CreateErrorResponse("read_image_file", $"File not found: {absolute_path}");
        }
        catch (IOException ex)
        {
            return CreateErrorResponse("read_image_file", $"I/O error reading file: {ex.Message}");
        }
        catch (Exception ex)
        {
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"ERROR: Error reading image file. {ex.Message}", ConsoleColor.Red);
            }
            return CreateErrorResponse("read_image_file", $"Unexpected error: {ex.Message}");
        }
    }

    private string? ValidateParameters(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "Path cannot be empty or whitespace";
        }

        // Check if path is absolute
        if (!Path.IsPathRooted(path))
        {
            return $"File path must be absolute, but was relative: {path}. You must provide an absolute path.";
        }

        // Check for invalid characters
        var invalidChars = Path.GetInvalidPathChars();
        if (path.Any(c => invalidChars.Contains(c)))
        {
            return "Path contains invalid characters";
        }

        return null;
    }

    private bool IsPathInWorkingDirectory(string absolutePath, string workingDirectory)
    {
        try
        {
            var normalizedAbsolutePath = Path.GetFullPath(absolutePath);
            var normalizedWorkingDirectory = Path.GetFullPath(workingDirectory);

            // Check if the path is exactly the working directory
            if (string.Equals(normalizedAbsolutePath, normalizedWorkingDirectory,
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                return true;
            }

            // Ensure working directory ends with directory separator for subdirectory comparison
            if (!normalizedWorkingDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
                !normalizedWorkingDirectory.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                normalizedWorkingDirectory += Path.DirectorySeparatorChar;
            }

            return normalizedAbsolutePath.StartsWith(normalizedWorkingDirectory,
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private bool HasReadPermission(FileInfo fileInfo)
    {
        try
        {
            // Try to open the file for reading to check permissions
            using var stream = fileInfo.OpenRead();
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    private string CreateSuccessResponse(string relativePath, string absolutePath, string fileName, int fileSize, string mimeType, string checksum)
    {
        return $@"<tool_response tool_name=""read_image_file"">
    <notes>Successfully read image file {relativePath}
File name: {fileName}
File size: {fileSize} bytes
MIME type: {mimeType}
The image has been loaded and is now available for AI analysis as visual content.</notes>
    <result status=""SUCCESS"" absolute_path=""{SecurityElement.Escape(absolutePath)}"" sha256_checksum=""{checksum}"" mime_type=""{mimeType}"" file_size=""{fileSize}"" />
    <image_info>Image file '{fileName}' loaded successfully and available for visual analysis.</image_info>
</tool_response>";
    }

    private string CreateErrorResponse(string toolName, string error)
    {
        return $@"<tool_response tool_name=""{toolName}"">
    <result status=""FAILED"" />
    <error>{SecurityElement.Escape(error)}</error>
</tool_response>";
    }

    private static bool IsImageFile(string fileExtension)
    {
        return fileExtension switch
        {
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".svg" or ".bmp" => true,
            _ => false
        };
    }

    private static string GetImageMimeType(string fileExtension)
    {
        return fileExtension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".bmp" => "image/bmp",
            _ => "application/octet-stream"
        };
    }

    private static string ComputeSha256(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
