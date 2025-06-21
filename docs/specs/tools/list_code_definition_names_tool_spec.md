# list_code_definition_names Tool Specification

## Purpose
List definition names (classes, functions, methods, etc.) used in source code files at the top level of a specified directory. This tool provides insights into codebase structure and important constructs.

## Parameters
- `path` (required): The directory path to analyze for source code definitions, relative to the current working directory

## Usage
```xml
<list_code_definition_names>
<path>src</path>
</list_code_definition_names>
```

## Capabilities
- Analyzes source code files in the specified directory
- Extracts top-level definitions like classes, functions, methods
- Provides overview of code structure and organization
- Works with multiple programming languages
- Shows definition types and names
- Helps understand codebase architecture

## Supported Definition Types
- **Classes** - Class definitions and declarations
- **Functions** - Function and method definitions
- **Interfaces** - Interface declarations (TypeScript, Java, etc.)
- **Types** - Type definitions and aliases
- **Constants** - Top-level constant declarations
- **Variables** - Global variable declarations
- **Modules** - Module definitions
- **Enums** - Enumeration definitions

## Best Practices
- Use to get high-level overview of code structure
- Analyze before making architectural changes
- Understand relationships between code components
- Use multiple times for different directories to understand full structure
- Combine with other tools for comprehensive analysis

## Common Use Cases
- Understanding codebase architecture before modifications
- Getting overview of available functions and classes
- Analyzing code organization patterns
- Finding entry points and main components
- Understanding module structure
- Preparing for refactoring tasks
- Documenting code structure

## Output Format
- Lists definition names grouped by file
- Shows definition types (class, function, etc.)
- Provides file paths for context
- May include parameter information for functions
- Organized for easy scanning and understanding

## Language Support
Works with various programming languages including:
- JavaScript/TypeScript
- Python
- Java
- C#
- C/C++
- Go
- Rust
- And many others

## Error Handling
- Returns error if directory doesn't exist
- Handles files that can't be parsed gracefully
- May skip binary or non-source files
- Provides meaningful error messages for parsing issues

## Integration with Other Tools
- Often used after `list_files` to understand code structure
- Frequently combined with `read_file` to examine specific definitions
- Used with `search_files` to find usage patterns
- Precedes detailed code analysis and modifications

## Examples

### Analyzing Main Source Directory
```xml
<list_code_definition_names>
<path>src</path>
</list_code_definition_names>
```

### Examining Test Structure
```xml
<list_code_definition_names>
<path>test</path>
</list_code_definition_names>
```

### Understanding Utility Functions
```xml
<list_code_definition_names>
<path>src/utils</path>
</list_code_definition_names>
```

## Limitations
- Only analyzes top-level directory (not recursive)
- May not capture all definition types in all languages
- Focuses on top-level definitions, not nested ones
- Parsing accuracy depends on language and code complexity

## Performance Considerations
- Generally fast as it only analyzes top-level directory
- Performance depends on number and size of source files
- May be slower with very large files or complex codebases
- Efficient for getting quick structural overview

## Use in Workflow
1. **Initial Analysis** - Get overview of codebase structure
2. **Targeted Reading** - Use results to guide which files to read
3. **Architecture Understanding** - Understand component relationships
4. **Modification Planning** - Plan changes based on existing structure
