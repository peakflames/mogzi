# MaxBot Built-in Tools - Complete Specification Index

This directory contains comprehensive specifications for all MaxBot built-in tools, organized into individual documents for easy reference and maintenance.

## File System Tools

### [read_file_tool_spec.md](read_file_tool_spec.md)
Read the contents of files at specified paths. Essential for examining existing files before modifications and extracting information from various file types.

### [write_to_file_tool_spec.md](write_to_file_tool_spec.md)
Create new files or completely overwrite existing files with provided content. Used for initial file creation and major restructuring.

### [replace_in_file_tool_spec.md](replace_in_file_tool_spec.md)
Make targeted edits to specific parts of existing files using SEARCH/REPLACE blocks. The preferred tool for precise modifications without rewriting entire files.

### [list_files_tool_spec.md](list_files_tool_spec.md)
List files and directories within specified directories. Essential for understanding project structure and exploring directory contents.

### [search_files_tool_spec.md](search_files_tool_spec.md)
Perform regex searches across files in directories with context-rich results. Essential for finding patterns, specific content, or code implementations across multiple files.

### [list_code_definition_names_tool_spec.md](list_code_definition_names_tool_spec.md)
List definition names (classes, functions, methods, etc.) in source code files. Provides insights into codebase structure and important constructs.

## System Interaction Tools

### [execute_command_tool_spec.md](execute_command_tool_spec.md)
Execute CLI commands on the system. Enables running system operations, build commands, tests, and other command-line utilities.

### [browser_action_tool_spec.md](browser_action_tool_spec.md)
Interact with websites through a Puppeteer-controlled browser. Enables web development testing, verification, and interaction with web applications.

## Communication Tools

### [ask_followup_question_tool_spec.md](ask_followup_question_tool_spec.md)
Ask users questions to gather additional information needed to complete tasks. Enables interactive problem-solving and clarification.

### [attempt_completion_tool_spec.md](attempt_completion_tool_spec.md)
Present the result of completed work to users. Marks the end of tasks and provides summaries of accomplishments.

## Extended Functionality Tools

### [mcp_tools_spec.md](mcp_tools_spec.md)
Model Context Protocol tools that enable communication with locally running MCP servers. Extends functionality by connecting to external services, APIs, and specialized resources.

## Workflow Management Tools

### [workflow_management_tools_spec.md](workflow_management_tools_spec.md)
Tools for organizing tasks, managing conversations, and handling different operational modes. Includes new_task and plan_mode_respond tools.

## Tool Categories Summary

### **Core File Operations**
- `read_file` - Read file contents
- `write_to_file` - Create/overwrite files
- `replace_in_file` - Targeted file edits

### **Project Exploration**
- `list_files` - Directory structure
- `search_files` - Content search across files
- `list_code_definition_names` - Code structure analysis

### **System Integration**
- `execute_command` - CLI command execution
- `browser_action` - Web browser automation

### **User Interaction**
- `ask_followup_question` - Gather user input
- `attempt_completion` - Present final results

### **Extended Capabilities**
- `use_mcp_tool` - External MCP server tools
- `access_mcp_resource` - External MCP resources
- `load_mcp_documentation` - MCP server creation docs

### **Workflow Management**
- `new_task` - Task context management
- `plan_mode_respond` - Planning mode communication

## Usage Principles

1. **One Tool Per Message** - Use only one tool at a time and wait for confirmation
2. **Iterative Approach** - Each tool use should be informed by previous results
3. **Error Handling** - Always check tool results and handle failures appropriately
4. **Safety First** - Some tools require explicit approval for potentially impactful operations

## Getting Started

For new users, start with these essential tools:
1. `list_files` - Understand project structure
2. `read_file` - Examine existing files
3. `search_files` - Find specific content or patterns
4. `replace_in_file` or `write_to_file` - Make changes
5. `attempt_completion` - Present results

Each specification document contains detailed information about parameters, usage examples, best practices, common use cases, and integration patterns with other tools.
