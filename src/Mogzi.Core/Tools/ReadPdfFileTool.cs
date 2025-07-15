using UglyToad.PdfPig;

namespace Mogzi.Tools;

public class ReadPdfFileTool(ApplicationConfiguration config, Action<string, ConsoleColor>? llmResponseDetailsCallback = null, IWorkingDirectoryProvider? workingDirectoryProvider = null)
{
    private readonly ApplicationConfiguration _config = config;
    private readonly Action<string, ConsoleColor>? _llmResponseDetailsCallback = llmResponseDetailsCallback;
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider = workingDirectoryProvider ?? new DefaultWorkingDirectoryProvider();

    public AIFunction GetTool()
    {
        return AIFunctionFactory.Create(
            ReadPdfFile,
            new AIFunctionFactoryOptions
            {
                Name = "read_pdf_file",
                Description = "Reads a PDF file from the local filesystem and extracts its text content. This tool can extract text from PDF documents and provides both the extracted text and metadata about the PDF file."
            });
    }

    public async Task<string> ReadPdfFile(
        [Description("The absolute path to the PDF file to read (e.g., '/home/user/project/document.pdf'). Relative paths are not supported. You must provide an absolute path.")] string absolute_path)
    {
        _llmResponseDetailsCallback?.Invoke($"Reading PDF file '{absolute_path}'.", ConsoleColor.DarkGray);

        try
        {
            // Validate parameters
            var validationError = ValidateParameters(absolute_path);
            if (validationError != null)
            {
                return CreateErrorResponse("read_pdf_file", validationError);
            }

            var workingDirectory = _workingDirectoryProvider.GetCurrentDirectory();
            var absolutePath = Path.GetFullPath(absolute_path);

            // Security validation - ensure path is within working directory
            if (!IsPathInWorkingDirectory(absolutePath, workingDirectory))
            {
                return CreateErrorResponse("read_pdf_file", $"File path must be within the root directory ({workingDirectory}): {absolute_path}");
            }

            // Check if file exists
            if (!File.Exists(absolutePath))
            {
                return CreateErrorResponse("read_pdf_file", $"File not found: {absolute_path}");
            }

            // Validate file extension
            var fileExtension = Path.GetExtension(absolutePath).ToLowerInvariant();
            if (fileExtension != ".pdf")
            {
                return CreateErrorResponse("read_pdf_file", $"File is not a PDF file. Expected .pdf extension but got: {fileExtension}. File: {absolute_path}");
            }

            // Check if file is readable
            var fileInfo = new FileInfo(absolutePath);
            if (!HasReadPermission(fileInfo))
            {
                return CreateErrorResponse("read_pdf_file", $"File is not readable: {absolute_path}");
            }

            // Read PDF as binary data to get basic information
            var pdfBytes = await File.ReadAllBytesAsync(absolutePath);
            var checksum = ComputeSha256(pdfBytes);
            var fileName = Path.GetFileName(absolutePath);

            // Basic PDF validation - check for PDF header
            if (!IsPdfFile(pdfBytes))
            {
                return CreateErrorResponse("read_pdf_file", $"File does not appear to be a valid PDF file: {absolute_path}");
            }

            // Extract text from PDF using UglyToad.PdfPig
            string extractedText;
            int pageCount;
            try
            {
                using var document = PdfDocument.Open(pdfBytes);
                pageCount = document.NumberOfPages;

                var textBuilder = new StringBuilder();

                for (var i = 1; i <= document.NumberOfPages; i++)
                {
                    var page = document.GetPage(i);
                    var pageText = page.Text;

                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        _ = textBuilder.AppendLine($"--- Page {i} ---");
                        _ = textBuilder.AppendLine(pageText.Trim());
                        _ = textBuilder.AppendLine();
                    }
                }

                extractedText = textBuilder.ToString().Trim();
            }
            catch (Exception ex)
            {
                _llmResponseDetailsCallback?.Invoke($"Warning: Could not extract text from PDF: {ex.Message}", ConsoleColor.Yellow);
                extractedText = "[Text extraction failed - PDF may be image-based or encrypted]";
                pageCount = 0;
            }

            // Return success response with PDF information and extracted text
            return CreateSuccessResponse(absolute_path, absolutePath, fileName, pdfBytes.Length, checksum, extractedText, pageCount);
        }
        catch (UnauthorizedAccessException)
        {
            return CreateErrorResponse("read_pdf_file", $"Access denied reading file: {absolute_path}");
        }
        catch (DirectoryNotFoundException)
        {
            return CreateErrorResponse("read_pdf_file", $"Directory not found for file: {absolute_path}");
        }
        catch (FileNotFoundException)
        {
            return CreateErrorResponse("read_pdf_file", $"File not found: {absolute_path}");
        }
        catch (IOException ex)
        {
            return CreateErrorResponse("read_pdf_file", $"I/O error reading file: {ex.Message}");
        }
        catch (Exception ex)
        {
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"ERROR: Error reading PDF file. {ex.Message}", ConsoleColor.Red);
            }
            return CreateErrorResponse("read_pdf_file", $"Unexpected error: {ex.Message}");
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

    private bool IsPdfFile(byte[] fileBytes)
    {
        // Check for PDF header: %PDF-
        if (fileBytes.Length < 5)
        {
            return false;
        }

        return fileBytes[0] == 0x25 && // %
               fileBytes[1] == 0x50 && // P
               fileBytes[2] == 0x44 && // D
               fileBytes[3] == 0x46 && // F
               fileBytes[4] == 0x2D;   // -
    }

    private string CreateSuccessResponse(string relativePath, string absolutePath, string fileName, int fileSize, string checksum, string extractedText, int pageCount)
    {
        var escapedText = SecurityElement.Escape(extractedText);
        var textPreview = extractedText.Length > 200 ? extractedText[..200] + "..." : extractedText;
        var escapedPreview = SecurityElement.Escape(textPreview);

        return $@"<tool_response tool_name=""read_pdf_file"">
    <notes>Successfully read PDF file {relativePath}
File name: {fileName}
File size: {fileSize} bytes
Pages: {pageCount}
MIME type: application/pdf
Text extracted: {extractedText.Length} characters
Preview: {escapedPreview}</notes>
    <result status=""SUCCESS"" absolute_path=""{SecurityElement.Escape(absolutePath)}"" sha256_checksum=""{checksum}"" mime_type=""application/pdf"" file_size=""{fileSize}"" page_count=""{pageCount}"" text_length=""{extractedText.Length}"" />
    <extracted_text>{escapedText}</extracted_text>
</tool_response>";
    }

    private string CreateErrorResponse(string toolName, string error)
    {
        return $@"<tool_response tool_name=""{toolName}"">
    <result status=""FAILED"" />
    <error>{SecurityElement.Escape(error)}</error>
</tool_response>";
    }

    private static string ComputeSha256(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
