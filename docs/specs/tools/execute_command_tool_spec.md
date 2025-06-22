# execute_command Tool Specification

## Purpose
Execute CLI commands on the system. This tool enables running system operations, build commands, tests, and other command-line utilities to accomplish development tasks.

## Parameters
- `command` (required): The CLI command to execute, valid for the current operating system
- `requires_approval` (required): Boolean indicating whether the command requires explicit user approval

## Usage
```xml
<execute_command>
<command>npm run dev</command>
<requires_approval>false</requires_approval>
</execute_command>
```

## Approval Requirements
Set `requires_approval` to `true` for:
- Installing/uninstalling packages
- Deleting/overwriting files
- System configuration changes
- Network operations
- Commands with potential unintended side effects

Set `requires_approval` to `false` for:
- Reading files/directories
- Running development servers
- Building projects
- Running tests
- Non-destructive operations

## Capabilities
- Executes commands in the current working directory
- Supports interactive and long-running commands
- Commands run in user's VSCode terminal
- Each command runs in a new terminal instance
- Can chain commands using appropriate shell syntax

## Best Practices
- Provide clear explanation of what the command does
- Tailor commands to the user's operating system
- Prefer complex CLI commands over creating executable scripts
- Use proper command chaining syntax for the user's shell
- Consider if command should be executed in a specific directory

## Common Use Cases
- Building and compiling projects
- Running tests and test suites
- Starting development servers
- Installing dependencies
- Running linters and formatters
- Git operations
- File system operations
- Package management

## System Considerations
- Commands execute from current working directory
- Use `cd directory && command` to run in different directories
- Consider Windows vs Unix command differences
- Respect user's default shell and environment

## Error Handling
- Returns command output and exit codes
- May not stream output properly in some cases
- Assume success if no error is explicitly returned
- Use `ask_followup_question` if actual output is needed

## Integration with Other Tools
- Often used after `read_file` to understand project structure
- May be followed by `browser_action` to test results
- Commonly combined with file tools to verify command effects
- Used with `list_files` to check command outcomes

## Examples

### Development Commands
```xml
<execute_command>
<command>npm install</command>
<requires_approval>true</requires_approval>
</execute_command>
```

### Build Commands
```xml
<execute_command>
<command>dotnet build</command>
<requires_approval>false</requires_approval>
</execute_command>
```

### Test Commands
```xml
<execute_command>
<command>pytest tests/</command>
<requires_approval>false</requires_approval>
</execute_command>
```

### Directory Operations
```xml
<execute_command>
<command>mkdir -p src/components</command>
<requires_approval>false</requires_approval>
</execute_command>
