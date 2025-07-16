namespace Mogzi.TUI.Services;

/// <summary>
/// Parses tool responses and extracts information for enhanced display.
/// </summary>
public class ToolResponseParser(ILogger<ToolResponseParser>? logger = null)
{
    private readonly ILogger<ToolResponseParser>? _logger = logger;

    /// <summary>
    /// Parses a tool response and extracts display information.
    /// </summary>
    /// <param name="toolName">The name of the tool that was executed</param>
    /// <param name="response">The tool response content</param>
    /// <returns>Parsed tool response information</returns>
    public ToolResponseInfo ParseToolResponse(string toolName, string response)
    {
        var info = new ToolResponseInfo
        {
            ToolName = toolName,
            RawResponse = response ?? "",
            Status = ToolExecutionStatus.Success
        };

        // Try to parse XML response
        if (response?.StartsWith("<tool_response") == true)
        {
            ParseXmlResponse(info, response);
        }
        else
        {
            // Fallback for non-XML responses
            info.Description = "Tool executed";
            info.Summary = response?.Length > 100 ? response[..97] + "..." : response ?? "";
        }

        return info;
    }

    /// <summary>
    /// Extracts file change information from tool responses to generate appropriate display content.
    /// </summary>
    /// <param name="toolName">The name of the tool</param>
    /// <param name="response">The tool response</param>
    /// <param name="originalContent">Original file content (if available)</param>
    /// <param name="newContent">New file content (if available)</param>
    /// <param name="filePath">File path for the diff</param>
    /// <returns>A unified diff if file changes are detected</returns>
    public UnifiedDiff? ExtractFileDiff(string toolName, string response, string? originalContent, string? newContent, string? filePath)
    {
        _logger?.LogTrace("ExtractFileDiff called - ToolName: '{ToolName}', FilePath: '{FilePath}', HasOriginal: {HasOriginal}, HasNew: {HasNew}",
            toolName, filePath, originalContent != null, newContent != null);

        // Handle different tool types appropriately
        var normalizedToolName = toolName.ToLowerInvariant();

        // For WriteFileTool - don't generate diffs, content will be shown directly
        if (IsWriteFileTool(normalizedToolName))
        {
            _logger?.LogTrace("WriteFileTool detected - skipping diff generation");
            return null;
        }

        // For EditTool - generate diff to show old vs new replacement
        if (IsEditTool(normalizedToolName))
        {
            _logger?.LogTrace("EditTool detected - generating replacement diff");
            return GenerateEditToolDiff(originalContent, newContent, filePath);
        }

        // For DiffPatchTools - extract diff from response or generate if needed
        if (IsDiffPatchTool(normalizedToolName))
        {
            _logger?.LogTrace("DiffPatchTool detected - extracting or generating diff");
            return ExtractOrGeneratePatchDiff(response, originalContent, newContent, filePath);
        }

        _logger?.LogTrace("Unknown tool type - no diff generated");
        return null;
    }

    private void ParseXmlResponse(ToolResponseInfo info, string xmlResponse)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xmlResponse);

            var root = doc.DocumentElement;
            if (root == null)
            {
                return;
            }

            // Extract tool name from XML if available
            var toolNameAttr = root.GetAttribute("tool_name");
            if (!string.IsNullOrEmpty(toolNameAttr))
            {
                info.ToolName = toolNameAttr;
            }

            // Extract status
            if (root.SelectSingleNode("result") is XmlElement resultNode)
            {
                var status = resultNode.GetAttribute("status");
                info.Status = status?.ToUpperInvariant() switch
                {
                    "SUCCESS" => ToolExecutionStatus.Success,
                    "FAILED" => ToolExecutionStatus.Failed,
                    _ => ToolExecutionStatus.Success
                };

                // Extract file path for file operations
                var absolutePath = resultNode.GetAttribute("absolute_path");
                if (!string.IsNullOrEmpty(absolutePath))
                {
                    info.FilePath = absolutePath;
                    info.Description = GetToolSpecificDescription(info.ToolName, absolutePath, xmlResponse);
                }
                else
                {
                    // For tools without file paths (like shell commands), generate description anyway
                    var normalizedToolName = info.ToolName.ToLowerInvariant();
                    if (normalizedToolName is "execute_command" or "shell" or "run_shell_command")
                    {
                        info.Description = ExtractShellCommandDescription(xmlResponse);
                    }
                }
            }

            // Handle attempt_completion tool specially
            if (info.ToolName.Equals("attempt_completion", StringComparison.OrdinalIgnoreCase))
            {
                var completionMessageNode = root.SelectSingleNode("completion_message");
                if (completionMessageNode != null)
                {
                    var completionMessage = completionMessageNode.InnerText?.Trim();
                    if (!string.IsNullOrEmpty(completionMessage))
                    {
                        info.Summary = completionMessage;
                        info.Description = "Task Completed";
                    }
                }
            }
            else
            {
                // Extract notes for summary for other tools
                var notesNode = root.SelectSingleNode("notes");
                if (notesNode != null)
                {
                    var notes = notesNode.InnerText?.Trim();
                    if (!string.IsNullOrEmpty(notes))
                    {
                        info.Summary = notes;

                        // Extract first line as description if not already set
                        if (string.IsNullOrEmpty(info.Description))
                        {
                            var firstLine = notes.Split('\n').FirstOrDefault()?.Trim();
                            if (!string.IsNullOrEmpty(firstLine))
                            {
                                info.Description = firstLine;
                            }
                        }
                    }
                }
            }

            // Extract error information
            var errorNode = root.SelectSingleNode("error");
            if (errorNode != null)
            {
                info.ErrorMessage = errorNode.InnerText?.Trim();
                info.Status = ToolExecutionStatus.Failed;
            }

            // Extract content for diff generation
            var contentNode = root.SelectSingleNode("content_on_disk");
            if (contentNode != null)
            {
                info.NewContent = contentNode.InnerText;
            }
        }
        catch (XmlException)
        {
            // If XML parsing fails, treat as plain text
            info.Summary = xmlResponse.Length > 200 ? xmlResponse[..197] + "..." : xmlResponse;
        }
    }

    private static bool IsWriteFileTool(string normalizedToolName)
    {
        return normalizedToolName is "write_file" or "writefile" or "write_to_file";
    }

    private static bool IsEditTool(string normalizedToolName)
    {
        return normalizedToolName is "replace_in_file" or "edit_file" or "editfile" or "edit";
    }

    private static bool IsDiffPatchTool(string normalizedToolName)
    {
        return normalizedToolName is "apply_code_patch" or "generate_code_patch" or "preview_patch_application";
    }

    /// <summary>
    /// Gets an appropriate description based on the tool type and file path.
    /// This fixes the issue where read-only tools were showing "Modified" incorrectly.
    /// </summary>
    /// <param name="toolName">The name of the tool</param>
    /// <param name="filePath">The file path being operated on</param>
    /// <param name="xmlResponse">The full XML response for extracting additional info like commands</param>
    /// <returns>An appropriate description for the tool operation</returns>
    private static string GetToolSpecificDescription(string toolName, string filePath, string? xmlResponse = null)
    {
        var fileName = Path.GetFileName(filePath);
        var normalizedToolName = toolName.ToLowerInvariant();

        return normalizedToolName switch
        {
            // Read-only tools
            "read_file" or "readfile" or "read_text_file" or "read_image_file" or "read_pdf_file" => $"Read {fileName}",

            // Write/modification tools
            "write_file" or "writefile" or "write_to_file" => $"Created {fileName}",
            "replace_in_file" or "edit_file" or "editfile" or "edit" => $"Modified {fileName}",

            // Directory/listing tools
            "ls" or "list" or "list_files" => $"Listed {fileName}",

            // Search tools
            "grep" or "search" => $"Searched {fileName}",

            // Shell/command tools
            "execute_command" or "shell" or "run_shell_command" => ExtractShellCommandDescription(xmlResponse),

            // Diff/patch tools
            "apply_code_patch" or "generate_code_patch" or "preview_patch_application" => $"Patched {fileName}",

            // Default fallback
            _ => $"Processed {fileName}"
        };
    }

    /// <summary>
    /// Extracts the actual command from shell tool XML response for better UX.
    /// </summary>
    /// <param name="xmlResponse">The XML response containing command information</param>
    /// <returns>A description showing the actual command that was executed</returns>
    private static string ExtractShellCommandDescription(string? xmlResponse)
    {
        if (string.IsNullOrEmpty(xmlResponse))
        {
            return "Executed command";
        }

        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xmlResponse);

            // Look for command in various possible locations
            var commandNode = doc.SelectSingleNode("//command") ??
                             doc.SelectSingleNode("//executed_command") ??
                             doc.SelectSingleNode("//notes");

            if (commandNode != null)
            {
                var commandText = commandNode.InnerText?.Trim();
                if (!string.IsNullOrEmpty(commandText))
                {
                    // For notes, try to extract command from common patterns
                    if (commandNode.Name == "notes")
                    {
                        // Look for patterns like "Successfully executed command: xyz" or "Command: xyz"
                        var match = System.Text.RegularExpressions.Regex.Match(commandText,
                            @"(?:Successfully executed command|Command|Executed):\s*(.+?)(?:\n|$)",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                        if (match.Success)
                        {
                            commandText = match.Groups[1].Value.Trim();
                        }
                    }

                    // Truncate long commands for display
                    if (commandText.Length > 50)
                    {
                        commandText = commandText[..47] + "...";
                    }

                    return $"Executed: {commandText}";
                }
            }
        }
        catch (XmlException)
        {
            // If XML parsing fails, fall back to default
        }

        return "Executed command";
    }

    private UnifiedDiff? GenerateEditToolDiff(string? originalContent, string? newContent, string? filePath)
    {
        if (string.IsNullOrEmpty(filePath) || newContent == null || originalContent == null)
        {
            return null;
        }

        try
        {
            // Enable logging in UnifiedDiffGenerator for debugging
            UnifiedDiffGenerator.SetLogger(_logger);

            var diff = UnifiedDiffGenerator.GenerateDiff(
                originalContent,
                newContent,
                $"a/{Path.GetFileName(filePath)}",
                $"b/{Path.GetFileName(filePath)}");

            return diff;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error generating EditTool diff");
            return null;
        }
    }

    private UnifiedDiff? ExtractOrGeneratePatchDiff(string response, string? originalContent, string? newContent, string? filePath)
    {
        // First try to extract diff from the response XML
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(response);

            // Look for patch content in the response
            var patchNode = doc.SelectSingleNode("//patch");
            if (patchNode != null)
            {
                var patchContent = patchNode.InnerText?.Trim();
                if (!string.IsNullOrEmpty(patchContent))
                {
                    _logger?.LogTrace("Found patch content in response, parsing...");
                    return ParseUnifiedDiffFromResponse(patchContent);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogTrace(ex, "Could not extract patch from response XML");
        }

        // Fallback: generate diff if we have content
        if (originalContent != null && newContent != null && !string.IsNullOrEmpty(filePath))
        {
            _logger?.LogTrace("Generating fallback diff for DiffPatchTool");
            return UnifiedDiffGenerator.GenerateDiff(
                originalContent,
                newContent,
                $"a/{Path.GetFileName(filePath)}",
                $"b/{Path.GetFileName(filePath)}");
        }

        _logger?.LogTrace("No diff generated for DiffPatchTool");
        return null;
    }

    private UnifiedDiff? ParseUnifiedDiffFromResponse(string patchContent)
    {
        try
        {
            var lines = patchContent.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
            var hunks = new List<DiffHunk>();
            var currentHunkLines = new List<DiffLine>();
            string? originalFile = null;
            string? modifiedFile = null;
            var originalStart = 0;
            var modifiedStart = 0;
            var originalLength = 0;
            var modifiedLength = 0;

            foreach (var line in lines)
            {
                if (line.StartsWith("--- "))
                {
                    originalFile = line[4..].Split('\t')[0];
                    continue;
                }
                if (line.StartsWith("+++ "))
                {
                    modifiedFile = line[4..].Split('\t')[0];
                    continue;
                }
                if (line.StartsWith("@@ "))
                {
                    if (currentHunkLines.Count > 0)
                    {
                        hunks.Add(new DiffHunk
                        {
                            Lines = currentHunkLines,
                            OriginalStart = originalStart,
                            OriginalLength = originalLength,
                            ModifiedStart = modifiedStart,
                            ModifiedLength = modifiedLength
                        });
                        currentHunkLines = [];
                    }

                    var match = System.Text.RegularExpressions.Regex.Match(line, @"^@@ -(\d+)(,(\d+))? \+(\d+)(,(\d+))? @@");
                    if (!match.Success)
                    {
                        _logger?.LogTrace("Failed to parse hunk header: {Line}", line);
                        return null;
                    }

                    originalStart = int.Parse(match.Groups[1].Value);
                    originalLength = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 1;
                    modifiedStart = int.Parse(match.Groups[4].Value);
                    modifiedLength = match.Groups[6].Success ? int.Parse(match.Groups[6].Value) : 1;
                    continue;
                }

                if (line.StartsWith("+"))
                {
                    currentHunkLines.Add(new DiffLine { Type = DiffLineType.Added, Content = line[1..] });
                }
                else if (line.StartsWith("-"))
                {
                    currentHunkLines.Add(new DiffLine { Type = DiffLineType.Removed, Content = line[1..] });
                }
                else if (line.StartsWith(" "))
                {
                    currentHunkLines.Add(new DiffLine { Type = DiffLineType.Context, Content = line[1..] });
                }
            }

            if (currentHunkLines.Count > 0)
            {
                hunks.Add(new DiffHunk
                {
                    Lines = currentHunkLines,
                    OriginalStart = originalStart,
                    OriginalLength = originalLength,
                    ModifiedStart = modifiedStart,
                    ModifiedLength = modifiedLength
                });
            }

            if (originalFile == null || modifiedFile == null)
            {
                _logger?.LogTrace("Patch did not contain file headers");
                return null;
            }

            return new UnifiedDiff { OriginalFile = originalFile, ModifiedFile = modifiedFile, Hunks = hunks };
        }
        catch (Exception ex)
        {
            _logger?.LogTrace(ex, "Error parsing unified diff from response");
            return null;
        }
    }
}

/// <summary>
/// Contains parsed information from a tool response.
/// </summary>
public class ToolResponseInfo
{
    /// <summary>The name of the tool that was executed</summary>
    public string ToolName { get; set; } = string.Empty;

    /// <summary>The execution status</summary>
    public ToolExecutionStatus Status { get; set; }

    /// <summary>A brief description of what the tool did</summary>
    public string? Description { get; set; }

    /// <summary>A summary of the tool's output</summary>
    public string? Summary { get; set; }

    /// <summary>Error message if the tool failed</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>File path if the tool operated on a file</summary>
    public string? FilePath { get; set; }

    /// <summary>New file content for diff generation</summary>
    public string? NewContent { get; set; }

    /// <summary>The raw tool response</summary>
    public string RawResponse { get; set; } = string.Empty;
}
