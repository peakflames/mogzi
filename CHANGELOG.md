# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.3] - 2025-06-XX

- TBD

## [1.3.2] - 2025-06-23

- **Completed Phase 1 Implementation**
- **Graceful Error Handling (TOR-8.2):** Implemented comprehensive `try-catch` blocks in the main application loop and tool execution to prevent crashes and provide user-friendly error messages for API failures and tool errors.
- **`execute_command` tool (TOR-4.2 & TOR-7.1):** Added a new tool to execute shell commands.
  - The tool is cross-platform, automatically using `cmd.exe` on Windows, `zsh` on macOS, and `bash` on Linux.
  - It respects the `tool_approvals` configuration, providing a security layer for potentially destructive operations.
- **File Permission Checks (TOR-3.3):** Enhanced `write_file` and `replace_in_file` to respect read-only file attributes, preventing errors when trying to modify locked files.
- **New Git-Style Diff Patch Tools:** Replaced `replace_in_file` with more robust Git-style unified diff patch tools:
  - **`apply_code_patch`:** Apply unified diff patches to files with fuzzy matching support for handling whitespace and formatting variations
  - **`generate_code_patch`:** Generate Git-compatible unified diff patches showing changes between original and modified content
  - **`preview_patch_application`:** Preview patch changes without applying them for safe validation
  - Enhanced file modification capabilities with atomic operations, SHA256 checksum validation, and comprehensive error reporting
  - Supports multiple hunks, context lines configuration, and detailed change statistics
- **Documentation Updates:** Comprehensive update of all tool specifications and system documentation:
  - Created detailed specification files for all three new diff patch tools with usage examples and best practices
  - Updated all existing tool specifications to reference new diff patch tools instead of `replace_in_file`
  - Updated requirements traceability matrix and project plan to reflect new tool implementation
  - Updated system prompt with diff patch tools best practices and workflow recommendations
- **Updated Developer Guidelines:**
  - Mandated a "Requirements-Driven Workflow" that requires creating skeleton tests before implementation.
  - Reinforced the need to assume cross-platform compatibility for all OS-level interactions.
  - Added a requirement to verify Native AOT compatibility for all new dependencies.
  - Added a guideline to test against the application's secure-by-default settings.
- Refactored the `Cli` project to use the Command Pattern, improving modularity and maintainability.
- Refactor system prompt to improve tool usage guidelines and verification

## [1.3.1] - 2025-06-22

- Add active profile name to the CLI interface prompt and update the emoji styling. 
- Display tool approvals setting in startup information to improve visibility of the current configuration.
- Added slash commands in chat mode:
  - `/status` - Display current configuration (similar to `--status` flag)
  - `/tool-approval [readonly|all]` - Change tool approval setting
  - `/sessions` - List available sessions (similar to `--list-sessions` flag)
  - `/load-session [ID]` - Load a specific chat session
  - `/help` - Display available slash commands
- Fixed bug where changing tool approval setting with `/tool-approval` command wasn't reflected in the system prompt
- Added `--debug` flag to enable detailed tool logging in the console
- Improved session listing to order sessions by last update date, with most recently used sessions appearing last
- Enhanced GitHub Actions workflow to support develop branch builds:
  - Added automatic build artifacts for all pushes to the develop branch
  - Added manual trigger for creating pre-releases from the develop branch
  - Pre-releases include version number with date and commit hash
  - Pre-releases are clearly marked as development builds

## [1.3.0] - 2025-06-21

- Enhanced session listing with card-like display showing session details, message count, and first user prompt
- Improved session loading to display previous chat history when loading a session
- Added ability to cancel API requests with Ctrl+C during chat interactions
- Refactored application architecture to move core logic from App.cs to AppService in the MaxBot project
- Implemented TOR-7.2: Added working directory constraints to all file system tools to enhance security.
- Create detailed tool specifications for all 12 core MaxBot tools
- Add requirements traceability matrix for verification tracking
- Establish 3-phase project implementation plan with priority-based approach
- Update developer guidelines with requirements-driven development standards
- Add workflow for generating AI assistant repository instructions
- Create comprehensive workflow for implementing next priority tasks with TOR verification
- Enhance workflow with iterative development, frequent validation, and comprehensive documentation
- Complete TOR-3.2 Implementation: Enhanced FileSystemTools.WriteFile with atomic operations, backup creation, checksum validation, and rollback capabilities
- Implemented TOR-3.1: Safe file system interaction, including `ReadFile`, `WriteFile`, and `ReplaceInFile` tools.
- Added comprehensive unit tests for file system tools to ensure safe and reliable operations.
- Updated project plan and trace matrix to reflect the completion of TOR-3.1.
- Corrected the test execution command in the developer guidelines.
- Updated `FileSystemTools` to use `AIFunctionFactoryOptions` to provide snake_case names for tools to the LLM, aligning with tool specifications.
- Added an architecture section with Mermaid diagrams to the developer guidelines.
- Implemented TOR-5.3: Chat history persistence to disk with session management
- Added ability to list and load previous chat sessions
- Added filtering of system messages when saving chat history
- Added injection of current system prompt when loading chat history
- Added comprehensive tests for chat history service
- Fixed list_files tool to properly list both files and directories as per specification

## [1.2.0] - 2025-06-21

__Added__:

- Command-line argument `--tool-approvals` (alias `-ta`) to override the tool approval setting from the configuration file.

__Changed__:

- The `FileSystemTools` now respect the `ToolApprovals` setting from the configuration.
- The system prompt now includes the active mode.

## [1.1.1] - 2025-06-20

__Changed__:

- The `--status` command now displays a detailed table of all available providers and profiles, in addition to the active configuration.

## [1.1.0] - 2025-06-20

__Added__:

- Ability to pipe standard input into the CLI as part of the prompt.
- Acceptance test script (`test/local_exe_acceptance.ps1`) to automate testing of the published executable.
- Cline workflow for generating releases (`.clinerules/workflows/generate_release.md`).

__Changed__:

- Updated developer guidelines and workflow rules.

## [0.3.0] - 2025-03-31

__Changed__:

- fix display of emojis and tweaked the system prompt
- tweaked the chat shell prompt

## [0.2.0] - 2025-03-30

__Added__:

- Support for AIFunctions (Tools)
  - ListFiles
  - WriteFile
  - ReadFile

__Changed__:

- Improved console output

## [0.1.0] - 2025-03-30

__Added__:

- Initial release
- Support both Oneshot and Interactive Chat interface with streaming AI responses
- Supports only OpenAI-compatible APIs
- Profile-based configuration for easy switching between providers and models
- Cross-platform support (Windows, MacOS, Linux)
- Support locating the configuration file in the current directory or the user's home directory (in that order).
