# search_files Tool Specification

## Purpose
Perform regex searches across files in a directory with context-rich results. This tool is essential for finding patterns, specific content, or code implementations across multiple files.

## Parameters
- `path` (required): The directory to search in (recursively searched)
- `regex` (required): The regular expression pattern to search for (uses Rust regex syntax)
- `file_pattern` (optional): Glob pattern to filter files (e.g., '*.ts' for TypeScript files)

## Usage
```xml
<search_files>
<path>src</path>
<regex>function\s+\w+\(</regex>
<file_pattern>*.js</file_pattern>
</search_files>
```

## Capabilities
- Searches across multiple files recursively
- Uses powerful Rust regex syntax
- Provides context lines around matches
- Filters by file patterns (glob syntax)
- Returns line numbers and file paths
- Shows surrounding code for better understanding

## Regex Syntax
Uses Rust regex syntax which includes:
- `\d` - digits
- `\w` - word characters
- `\s` - whitespace
- `+` - one or more
- `*` - zero or more
- `?` - zero or one
- `()` - grouping
- `|` - alternation
- `^` - start of line
- `$` - end of line

## File Pattern Examples
- `*.js` - JavaScript files only
- `*.{ts,tsx}` - TypeScript files
- `*.py` - Python files
- `*.md` - Markdown files
- `test*.js` - Test files starting with "test"
- `*config*` - Files containing "config" in name

## Best Practices
- Craft regex patterns carefully to balance specificity and flexibility
- Use file patterns to limit search scope when appropriate
- Analyze surrounding context provided in results
- Combine with other tools for comprehensive analysis
- Start with broader patterns and refine as needed

## Common Use Cases
- Finding function definitions across codebase
- Locating TODO comments and technical debt
- Searching for specific API usage patterns
- Finding configuration references
- Locating error handling patterns
- Discovering code duplication
- Finding import/export statements
- Searching for specific variable or class usage

## Search Pattern Examples

### Function Definitions
```xml
<regex>function\s+\w+\s*\(</regex>
```

### TODO Comments
```xml
<regex>TODO|FIXME|HACK</regex>
```

### Import Statements
```xml
<regex>import\s+.*from\s+['"]</regex>
```

### Class Definitions
```xml
<regex>class\s+\w+</regex>
```

### API Endpoints
```xml
<regex>\/api\/\w+</regex>
```

## Output Format
- Shows file path and line number for each match
- Provides context lines before and after match
- Highlights the matching pattern
- Groups results by file for easy navigation

## Error Handling
- Returns error for invalid regex patterns
- Returns empty results if no matches found
- May timeout on very large directories
- Handles permission errors gracefully

## Performance Considerations
- Recursive search may be slow on large codebases
- Use file patterns to limit scope when possible
- Complex regex patterns may impact performance
- Consider searching specific subdirectories for faster results

## Integration with Other Tools
- Often used before `read_file` to examine specific matches
- Frequently combined with `replace_in_file` for targeted changes
- Used with `list_files` to understand search scope
- Precedes code analysis and refactoring tasks

## Advanced Examples

### Finding Async Functions
```xml
<search_files>
<path>src</path>
<regex>async\s+function|function.*async</regex>
<file_pattern>*.js</file_pattern>
</search_files>
```

### Locating Error Handling
```xml
<search_files>
<path>src</path>
<regex>try\s*\{|catch\s*\(|throw\s+</regex>
</search_files>
```

### Finding Configuration Usage
```xml
<search_files>
<path>.</path>
<regex>config\.|\.config|CONFIG_</regex>
<file_pattern>*.{js,ts,json}</file_pattern>
</search_files>
