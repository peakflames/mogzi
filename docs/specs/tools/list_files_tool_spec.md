# list_files Tool Specification

## Purpose
List files and directories within a specified directory. This tool is essential for understanding project structure and exploring directory contents.

## Parameters
- `path` (required): The directory path to list contents for, relative to the current working directory
- `recursive` (optional): Whether to list files recursively (true for recursive, false or omit for top-level only)

## Usage
```xml
<list_files>
<path>src</path>
<recursive>true</recursive>
</list_files>
```

## Capabilities
- Lists files and directories in specified path
- Supports recursive directory traversal
- Shows file and directory names with paths
- Provides overview of project structure
- Works with any directory accessible from current working directory

## Best Practices
- Use `recursive=true` for complete project structure analysis
- Use `recursive=false` for top-level exploration of large directories
- Essential for understanding project organization before making changes
- Don't use to confirm file creation (user will provide feedback)
- Use before other file operations to understand context

## When to Use Recursive=true
- Analyzing complete project structure
- Understanding codebase organization
- Finding all files of a specific type
- Getting comprehensive view of nested directories
- Initial project exploration

## When to Use Recursive=false
- Exploring large directories like Desktop or Documents
- Getting quick overview of top-level structure
- When you only need immediate subdirectories
- Avoiding overwhelming output in deep directory trees

## Common Use Cases
- Initial project exploration and understanding
- Finding specific files or directories
- Understanding project organization patterns
- Locating configuration files
- Discovering test directories and structure
- Exploring source code organization
- Finding documentation directories

## Output Format
- Shows directory structure with proper indentation
- Distinguishes between files and directories
- Provides relative paths from specified directory
- May include file sizes and timestamps depending on system

## Error Handling
- Returns error if directory doesn't exist
- Returns error if insufficient permissions to read directory
- May return empty result for empty directories

## Integration with Other Tools
- Often used before `read_file` to find files to examine
- Frequently combined with `search_files` for comprehensive analysis
- Used with `list_code_definition_names` to understand code structure
- Precedes most file operations to understand context

## Examples

### Project Structure Analysis
```xml
<list_files>
<path>.</path>
<recursive>true</recursive>
</list_files>
```

### Top-level Exploration
```xml
<list_files>
<path>src</path>
<recursive>false</recursive>
</list_files>
```

### Specific Directory Investigation
```xml
<list_files>
<path>test</path>
<recursive>true</recursive>
</list_files>
```

## Performance Considerations
- Recursive listing of large directories may take time
- Consider using non-recursive for initial exploration
- May produce large output for deeply nested structures
- Use specific subdirectories when possible to limit scope
