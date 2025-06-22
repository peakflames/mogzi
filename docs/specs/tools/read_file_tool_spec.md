# read_file Tool Specification

## Purpose
Read the contents of a file at a specified path. This tool is essential for examining existing files before making modifications or extracting information from various file types.

## Parameters
- `path` (required): The file path relative to the current working directory

## Usage
```xml
<read_file>
<path>src/main.js</path>
</read_file>
```

## Capabilities
- Reads text files and returns content as string
- Automatically extracts raw text from PDF and DOCX files
- Works with configuration files, source code, documentation, etc.
- May not be suitable for binary files as it returns raw content

## Best Practices
- Use to examine existing files before making modifications
- Essential for understanding code structure before making changes
- Always read files before attempting to modify them with replace_in_file
- Use to gather context about project structure and patterns

## Common Use Cases
- Analyzing code before refactoring
- Reading configuration files to understand project setup
- Extracting information from documentation files
- Understanding project structure and dependencies
- Reviewing existing implementations before adding features
- Checking file contents to understand data formats

## Error Handling
- Returns error if file doesn't exist
- Returns error if file is not readable due to permissions
- May return garbled content for binary files

## Integration with Other Tools
- Often used before `replace_in_file` to understand current content
- Frequently combined with `search_files` for comprehensive code analysis
- Used with `list_files` to explore and then examine specific files
