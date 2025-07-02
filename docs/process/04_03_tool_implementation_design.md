# Mogzi Tool Implementation Design

## Tool Architecture Overview

Mogzi's tool system provides AI function calling capabilities through a comprehensive set of tools that enable file operations, system interactions, and code manipulation. The design emphasizes security, reliability, and extensibility while maintaining consistent patterns across all tool implementations.

## Base Tool Pattern

### Core Tool Structure

**Standard Tool Implementation:**
```csharp
public class ReadTextFileTool
{
    private readonly MaxbotConfiguration _config;
    private readonly Action<string, ConsoleColor>? _llmResponseDetailsCallback;
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider;

    public ReadTextFileTool(MaxbotConfiguration config, Action<string, ConsoleColor>? llmResponseDetailsCallback = null, IWorkingDirectoryProvider? workingDirectoryProvider = null)
    {
        _config = config;
        _llmResponseDetailsCallback = llmResponseDetailsCallback;
        _workingDirectoryProvider = workingDirectoryProvider ?? new DefaultWorkingDirectoryProvider();
    }

    public AIFunction GetTool()
    {
        return AIFunctionFactory.Create(
            ReadTextFile,
            new AIFunctionFactoryOptions
            {
                Name = "read_text_file",
                Description = "Reads and returns the content of a text file from the local filesystem. Supports reading specific line ranges for large files."
            });
    }
    
    public async Task<string> ReadTextFile(
        [Description("The absolute path to the text file to read. Relative paths are not supported.")] string absolute_path,
        [Description("Optional: The 0-based line number to start reading from. Requires 'limit' to be set.")] int? offset = null,
        [Description("Optional: Maximum number of lines to read. Use with 'offset' to paginate through large files.")] int? limit = null)
    {
        // Implementation with comprehensive validation and error handling
    }
}
```

**Design Principles:**
- **Dependency Injection**: All tools receive configuration and optional dependencies
- **Factory Pattern**: GetTool() method creates AIFunction instances
- **Async Operations**: All I/O operations are async for performance
- **Parameter Validation**: Comprehensive input validation with descriptive attributes
- **Error Handling**: Structured error responses with security considerations

### Tool Registration Pattern

**Centralized Tool Registration:**
```csharp
// Tool instantiation with dependency injection
SystemTools = new SystemTools(config, llmResponseDetailsCallback);
DiffPatchTools = new DiffPatchTools(config, llmResponseDetailsCallback);
ReadTextFileTool = new ReadTextFileTool(config, llmResponseDetailsCallback);
ReadImageFileTool = new ReadImageFileTool(config, llmResponseDetailsCallback);
WriteFileTool = new WriteFileTool(config, llmResponseDetailsCallback);
EditTool = new EditTool(config, llmResponseDetailsCallback);
LSTool = new LSTool(config, llmResponseDetailsCallback);
GrepTool = new GrepTool(config, llmResponseDetailsCallback);
ShellTool = new ShellTool(config, llmResponseDetailsCallback);

// Tool registration and ChatOptions configuration
var allTools = new List<AITool>();
allTools.AddRange(SystemTools.GetTools().Cast<AITool>());
allTools.AddRange(DiffPatchTools.GetTools().Cast<AITool>());
allTools.Add(ReadTextFileTool.GetTool());
allTools.Add(ReadImageFileTool.GetTool());
allTools.Add(WriteFileTool.GetTool());
allTools.Add(EditTool.GetTool());
allTools.Add(LSTool.GetTool());
allTools.Add(GrepTool.GetTool());
allTools.Add(ShellTool.GetTool());

ChatOptions = new ChatOptions { Tools = allTools };
```

## Security Implementation

### Parameter Validation

**Comprehensive Input Validation:**
```csharp
private string? ValidateParameters(string path, int? offset, int? limit)
{
    if (string.IsNullOrWhiteSpace(path))
        return "Path cannot be empty or whitespace";

    // Check if path is absolute
    if (!Path.IsPathRooted(path))
        return $"File path must be absolute, but was relative: {path}. You must provide an absolute path.";

    // Check for invalid characters
    var invalidChars = Path.GetInvalidPathChars();
    if (path.Any(c => invalidChars.Contains(c)))
        return "Path contains invalid characters";

    // Validate offset and limit parameters
    if (offset.HasValue && offset.Value < 0)
        return "Offset must be a non-negative number";

    if (limit.HasValue && limit.Value <= 0)
        return "Limit must be a positive number";

    if (offset.HasValue && !limit.HasValue)
        return "When offset is specified, limit must also be specified";

    return null;
}
```

### Working Directory Security

**Path Security Enforcement:**
```csharp
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
```

### Permission Validation

**File Access Permission Checking:**
```csharp
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

private bool HasWritePermission(FileInfo fileInfo)
{
    try
    {
        // For existing files, try to open for writing
        if (fileInfo.Exists)
        {
            using var stream = fileInfo.OpenWrite();
            return true;
        }
        
        // For new files, check directory write permissions
        var directory = fileInfo.Directory;
        if (directory?.Exists == true)
        {
            var testFile = Path.Combine(directory.FullName, Path.GetRandomFileName());
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        
        return false;
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
```

### Tool Approval System

**Configuration-Based Approval Checking:**
```csharp
private bool RequiresApproval(string operation)
{
    // Define operations that require approval
    var destructiveOperations = new[] { "write", "delete", "execute", "modify" };
    return destructiveOperations.Contains(operation.ToLowerInvariant());
}

private string CheckToolApproval(string operation)
{
    if (RequiresApproval(operation) && _config.ToolApprovals.Equals("readonly", StringComparison.OrdinalIgnoreCase))
    {
        var msg = "Execution of this command requires approval. Please run with --tool-approvals all or use the /tool-approval slash command to grant permission.";
        if (_config.Debug)
        {
            _llmResponseDetailsCallback?.Invoke(msg, ConsoleColor.DarkGray);
        }
        return msg;
    }
    
    return string.Empty; // Approved
}
```

## Tool Response Format

### Structured XML Responses

**Standard Response Format:**
```xml
<tool_response tool_name="read_file">
    <notes>Successfully read file path/to/file.txt</notes>
    <result status="SUCCESS" absolute_path="..." sha256_checksum="..." />
    <content_on_disk>file content here</content_on_disk>
</tool_response>
```

**Error Response Format:**
```xml
<tool_response tool_name="read_file">
    <notes>Failed to read file: File not found</notes>
    <result status="ERROR" error_type="FileNotFound" />
</tool_response>
```

### Response Generation Pattern

**Consistent Response Building:**
```csharp
private string BuildSuccessResponse(string toolName, string notes, string content, string? checksum = null)
{
    var result = checksum != null 
        ? $"<result status=\"SUCCESS\" sha256_checksum=\"{checksum}\" />"
        : "<result status=\"SUCCESS\" />";
        
    return $@"<tool_response tool_name=""{toolName}"">
    <notes>{notes}</notes>
    {result}
    <content_on_disk>{content}</content_on_disk>
</tool_response>";
}

private string BuildErrorResponse(string toolName, string notes, string errorType)
{
    return $@"<tool_response tool_name=""{toolName}"">
    <notes>{notes}</notes>
    <result status=""ERROR"" error_type=""{errorType}"" />
</tool_response>";
}
```

## Individual Tool Implementations

### File System Tools

**ReadTextFileTool:**
- **Purpose**: Read text files with optional pagination
- **Security**: Working directory validation, permission checking
- **Features**: Line range support, encoding detection, checksum generation
- **Error Handling**: File not found, permission denied, encoding issues

**WriteFileTool:**
- **Purpose**: Create or overwrite text files
- **Security**: Working directory validation, approval checking
- **Features**: Atomic writes, backup creation, encoding specification
- **Error Handling**: Permission denied, disk space, path validation

**EditTool:**
- **Purpose**: Apply targeted edits to existing files
- **Security**: Working directory validation, approval checking
- **Features**: Line-based editing, backup creation, validation
- **Error Handling**: File corruption, edit conflicts, permission issues

### System Tools

**LSTool:**
- **Purpose**: List directory contents with filtering
- **Security**: Working directory validation, permission checking
- **Features**: Recursive listing, pattern filtering, metadata inclusion
- **Error Handling**: Directory not found, permission denied

**GrepTool:**
- **Purpose**: Search file contents using regex patterns
- **Security**: Working directory validation, pattern validation
- **Features**: Multi-file search, context lines, case sensitivity
- **Error Handling**: Invalid regex, file access errors

**ShellTool:**
- **Purpose**: Execute shell commands with output capture
- **Security**: Command validation, approval checking, timeout enforcement
- **Features**: Cross-platform shell detection, output streaming
- **Error Handling**: Command not found, timeout, permission denied

### Image and Document Tools

**ReadImageFileTool:**
- **Purpose**: Extract text from images using OCR
- **Security**: Working directory validation, file type validation
- **Features**: Multiple image format support, OCR confidence scoring
- **Error Handling**: Unsupported format, OCR failures, file corruption

**ReadPdfFileTool:**
- **Purpose**: Extract text content from PDF files
- **Security**: Working directory validation, file size limits
- **Features**: Multi-page extraction, metadata extraction
- **Error Handling**: Corrupted PDF, password protection, extraction failures

## Cross-Platform Design

### Platform Detection

**Shell Command Execution:**
```csharp
private (string fileName, string arguments) GetShellCommand(string command)
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        return ("cmd.exe", $"/C \"{command}\"");
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
        return ("/bin/zsh", $"-c \"{command}\"");
    }
    else // Linux and Unix-like systems
    {
        return ("/bin/bash", $"-c \"{command}\"");
    }
}
```

### Path Handling Design

**Cross-Platform Path Operations:**
```csharp
private string NormalizePath(string path)
{
    // Use Path.GetFullPath for consistent path normalization
    var normalizedPath = Path.GetFullPath(path);
    
    // Handle case sensitivity based on platform
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        return normalizedPath.ToLowerInvariant();
    }
    
    return normalizedPath;
}

private bool PathsEqual(string path1, string path2)
{
    var comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
        ? StringComparison.OrdinalIgnoreCase 
        : StringComparison.Ordinal;
        
    return string.Equals(NormalizePath(path1), NormalizePath(path2), comparison);
}
```

## Error Handling Strategy

### Exception Management

**Structured Exception Handling:**
```csharp
public async Task<string> ExecuteToolOperation()
{
    try
    {
        // Validate parameters
        var validationError = ValidateParameters();
        if (!string.IsNullOrEmpty(validationError))
        {
            return BuildErrorResponse("validation_error", validationError);
        }

        // Check security constraints
        if (!IsPathInWorkingDirectory(path, workingDirectory))
        {
            return BuildErrorResponse("security_error", "Path outside working directory");
        }

        // Check tool approvals
        var approvalError = CheckToolApproval("write");
        if (!string.IsNullOrEmpty(approvalError))
        {
            return BuildErrorResponse("approval_required", approvalError);
        }

        // Execute operation
        var result = await PerformOperation();
        return BuildSuccessResponse("operation_completed", result);
    }
    catch (UnauthorizedAccessException)
    {
        return BuildErrorResponse("permission_denied", "Insufficient permissions");
    }
    catch (FileNotFoundException)
    {
        return BuildErrorResponse("file_not_found", "Specified file does not exist");
    }
    catch (DirectoryNotFoundException)
    {
        return BuildErrorResponse("directory_not_found", "Specified directory does not exist");
    }
    catch (IOException ex)
    {
        return BuildErrorResponse("io_error", $"I/O operation failed: {ex.Message}");
    }
    catch (Exception ex)
    {
        var errorMessage = _config.Debug ? ex.ToString() : "An unexpected error occurred";
        return BuildErrorResponse("unexpected_error", errorMessage);
    }
}
```

### Security Error Messages

**Information Disclosure Prevention:**
```csharp
private string GetSecureErrorMessage(Exception ex, string operation)
{
    // Provide generic error messages to prevent information disclosure
    return ex switch
    {
        UnauthorizedAccessException => "Access denied",
        FileNotFoundException => "File not found",
        DirectoryNotFoundException => "Directory not found",
        SecurityException => "Security violation",
        _ => _config.Debug ? ex.Message : "Operation failed"
    };
}
```


This tool implementation design ensures Mogzi's tools are secure, reliable, and maintainable while providing comprehensive functionality for AI-assisted development tasks. The consistent patterns and robust error handling make the system both user-friendly and developer-friendly.
