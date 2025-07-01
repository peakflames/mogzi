namespace MaxBot.TUI.Services;

/// <summary>
/// Parses tool responses and extracts information for enhanced display.
/// </summary>
public class ToolResponseParser
{
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
            RawResponse = response,
            Status = ToolExecutionStatus.Success
        };

        // Try to parse XML response
        if (response.StartsWith("<tool_response"))
        {
            ParseXmlResponse(info, response);
        }
        else
        {
            // Fallback for non-XML responses
            info.Description = "Tool executed";
            info.Summary = response.Length > 100 ? response[..97] + "..." : response;
        }

        return info;
    }

    /// <summary>
    /// Extracts file change information from tool responses to generate diffs.
    /// </summary>
    /// <param name="toolName">The name of the tool</param>
    /// <param name="response">The tool response</param>
    /// <param name="originalContent">Original file content (if available)</param>
    /// <param name="newContent">New file content (if available)</param>
    /// <param name="filePath">File path for the diff</param>
    /// <returns>A unified diff if file changes are detected</returns>
    public UnifiedDiff? ExtractFileDiff(string toolName, string response, string? originalContent, string? newContent, string? filePath)
    {
        // Only generate diffs for file modification tools
        if (!IsFileModificationTool(toolName) || string.IsNullOrEmpty(filePath))
        {
            return null;
        }

        // If we have both original and new content, generate a diff
        if (originalContent != null && newContent != null && originalContent != newContent)
        {
            return UnifiedDiffGenerator.GenerateDiff(
                originalContent, 
                newContent, 
                $"a/{Path.GetFileName(filePath)}", 
                $"b/{Path.GetFileName(filePath)}");
        }

        // For new files, show as all additions
        if (originalContent == null && newContent != null)
        {
            return UnifiedDiffGenerator.GenerateDiff(
                "", 
                newContent, 
                "/dev/null", 
                $"b/{Path.GetFileName(filePath)}");
        }

        return null;
    }

    private static UnifiedDiff CreateSimpleDiff(string originalContent, string newContent, string originalPath, string modifiedPath)
    {
        return UnifiedDiffGenerator.GenerateDiff(originalContent, newContent, originalPath, modifiedPath);
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
                    info.Description = $"Modified {Path.GetFileName(absolutePath)}";
                }
            }

            // Extract notes for summary
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

    private static bool IsFileModificationTool(string toolName)
    {
        var fileTools = new[]
        {
            "write_file", "writefile", "write_to_file",
            "edit_file", "editfile", "edit",
            "create_file", "createfile"
        };

        return fileTools.Contains(toolName.ToLowerInvariant());
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
