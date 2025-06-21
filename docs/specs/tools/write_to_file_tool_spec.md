# write_to_file Tool Specification

## Purpose
Create a new file or completely overwrite an existing file with provided content. This tool is used for initial file creation or when complete file replacement is needed.

## Parameters
- `path` (required): The file path relative to the current working directory
- `content` (required): The complete content to write to the file

## Usage
```xml
<write_to_file>
<path>src/config.json</path>
<content>
{
  "apiEndpoint": "https://api.example.com",
  "version": "1.0.0"
}
</content>
</write_to_file>
```

## Capabilities
- Creates new files with specified content
- Overwrites existing files completely
- Automatically creates any necessary parent directories
- Handles any text-based file format
- Preserves exact formatting and content as provided

## Best Practices
- Always provide the COMPLETE intended content without truncation
- Use for initial file creation or major restructuring
- Consider using `replace_in_file` for targeted edits instead
- Ensure content is properly formatted before writing
- Use when changes are so extensive that targeted edits would be complex

## When to Use
- Creating new files from scratch
- Complete file restructuring or reorganization
- Generating boilerplate or template files
- Replacing entire configuration files
- Creating documentation files
- When the majority of file content needs to change

## When NOT to Use
- Making small, targeted changes (use `replace_in_file` instead)
- Modifying just a few lines in a large file
- Adding single functions or small code blocks

## Error Handling
- Returns error if path is invalid
- Returns error if insufficient permissions to write
- May fail if disk space is insufficient

## Auto-formatting Considerations
- Editor may automatically format the file after writing
- Final file state may differ from input due to formatting rules
- Use the returned final_file_content as reference for subsequent edits

## Integration with Other Tools
- Often used after `read_file` to understand existing structure
- May be followed by `replace_in_file` for fine-tuning
- Commonly used with `list_files` to understand directory structure
