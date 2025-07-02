# Changelog

## [0.13.5] - 2025-07-02

- fix bug when running --list-profiles

## [0.13.4] - 2025-07-01

- Add new interactive slash command, `/tool-approvals`, that allows users to change the tool approval mode (`readonly` or `all`) for the current session.
  
## [0.13.3] - 2025-07-01

- Add **--list-profiles** command

## [0.13.2] - 2025-07-01

- Add **Enhanced Tool Display**: Tool execution results now display in bordered panels for improved visual separation
  - Write file operations show content in grey rounded border panels with filename headers
  - Replace operations show diffs in blue rounded border panels with filename headers
  - Intelligent filename extraction from tool descriptions for accurate panel headers
  - Borders persist in scrollback history for easy reference
  - Line numbers and syntax highlighting maintained within bordered containers

- Added **Modular System Prompt Design**:
  - Modular system prompt architecture with model-specific prompts for Claude, Gemini, and OpenAI
  - Automatic model family detection and absolute working directory path enforcement
  - Comprehensive design documentation for modular prompt system (04_08)

- Added Environment context automatically appended to user messages for fresh system state per interaction


## [0.13.1] - 2025-07-01

- Add .editorconfig and address all warnings
- Add .clinerules/developer_guidelines.md that is intentionally small.
- Add autocomplete system with file path and slash command support (#12)

## [0.13.0] - 2025-06-30

### 🎉 Initial Release

MaxBot is a powerful AI-powered CLI assistant that provides intelligent file operations, code editing, and system interaction capabilities through a rich terminal user interface.

### ✨ Core Features

**AI-Powered Chat Interface**
- Interactive chat mode with real-time streaming responses
- Oneshot mode for single command execution
- Dynamic system prompt generation with environment context
- Token counting and usage metrics with SharpToken integration
- Chat session persistence and history management

**Rich Terminal User Interface**
- Built with Spectre.Console for beautiful terminal rendering
- Flexible column-based layout with dynamic content updates
- Advanced keyboard handling with custom key bindings
- Scrollback terminal with efficient content management
- Real-time status updates during AI processing and tool execution

**Comprehensive Tool Suite**
- **File Operations**: Read, write, and edit text files with security validation
- **Directory Operations**: List directories with filtering and recursive options
- **Search Capabilities**: Grep-style text search across files with regex support
- **Code Editing**: In-place file editing with search/replace operations
- **Image Processing**: Read and process image files with base64 encoding
- **System Integration**: Execute shell commands with cross-platform support
- **Diff/Patch System**: Generate and apply unified diffs with fuzzy matching

### 🔧 Technical Architecture

**Modern .NET Implementation**
- Built on .NET 9 with C# for high performance
- Native AOT compilation support for fast startup and small binaries
- Source-generated JSON serialization for AOT compatibility
- FluentResults pattern for functional error handling

**AI Integration**
- Microsoft.Extensions.AI for standardized AI service integration
- OpenAI SDK with configurable endpoints and retry policies
- Function calling support with comprehensive tool registration
- Streaming response processing with IAsyncEnumerable patterns

**Security & Safety**
- Working directory enforcement for all file operations
- Comprehensive parameter validation and input sanitization
- Two-tier tool approval system (readonly/all modes)
- Cross-platform path validation with proper case sensitivity
- File permission checking before operations

### 🛠️ Advanced Capabilities

**Configuration Management**
- JSON-based configuration with profile support
- Multiple AI provider and model configurations
- Environment-specific settings and debug modes
- Automatic configuration discovery (current directory or home directory)

**Diff/Patch System**
- Unified diff generation and parsing
- Intelligent patch application with exact and fuzzy matching
- Multiple fuzzy matching strategies (line offset, whitespace normalization)
- Longest Common Subsequence algorithm for optimal matching
- Comprehensive patch result reporting with confidence scores

**Cross-Platform Support**
- Windows (PowerShell), macOS (Zsh), Linux (Bash) shell detection
- Platform-specific command execution and path handling
- Consistent security boundaries across all platforms
- Native performance on all supported operating systems

### 🔒 Security Features

**File System Security**
- All file operations restricted to working directory
- Absolute path requirement with validation
- Permission checking before file access
- Secure error messages to prevent information disclosure

**Tool Approval System**
- Configurable tool execution permissions
- Readonly mode blocks potentially destructive operations
- Runtime approval checking for each tool execution
- Debug mode for detailed operation logging

### 📋 Available Tools

**System Tools**
- `execute_command`: Cross-platform shell command execution
- `attempt_completion`: Task completion with result presentation

**File Management Tools**
- `read_text_file`: Secure file reading with range support (offset/limit)
- `write_file`: File creation and modification with validation
- `edit_file`: In-place file editing with search/replace operations
- `list_directory`: Directory listing with filtering options

**Search and Analysis Tools**
- `search_file_content`: Regex-based text search across files
- `read_image_file`: Image file processing with base64 encoding

**Code Development Tools**
- `generate_code_patch`: Create unified diffs for code changes
- `apply_code_patch`: Apply patches with fuzzy matching support
- `preview_patch_application`: Preview patch results before application

### 🚀 Performance Features

**Efficient Processing**
- Streaming AI responses for real-time interaction
- Minimal memory footprint with efficient content management
- Fast startup with AOT compilation
- Optimized terminal rendering with selective updates

**Smart Resource Management**
- Automatic cleanup of processes and streams
- Efficient chat history storage and retrieval
- Token optimization with dynamic prompt generation
- Memory-efficient handling of large files and responses

### 🔧 Configuration

**Profile-Based Setup**
- Multiple AI provider configurations (OpenAI, custom endpoints)
- Model-specific settings and parameters
- Default profile selection with override options
- Environment-specific configuration support

**Flexible Deployment**
- Configuration file discovery in current directory or home directory
- Command-line parameter overrides
- Debug mode for development and troubleshooting
- Cross-platform configuration compatibility

### 📦 Dependencies

**Core Libraries**
- Microsoft.Extensions.AI (9.6.0) - AI service abstraction
- Microsoft.Extensions.AI.OpenAI (9.6.0-preview) - OpenAI integration
- FluentResults (3.16.0) - Functional error handling
- Spectre.Console - Rich terminal UI rendering
- SharpToken (2.0.3) - Token counting and metrics

**Development Tools**
- .NET 9 SDK with AOT compilation support
- Source generators for JSON serialization
- Cross-platform development and deployment

### 🎯 Use Cases

**Software Development**
- Code review and analysis with AI assistance
- Automated file editing and refactoring
- Project exploration and documentation
- Diff generation and patch application

**System Administration**
- File system operations with safety constraints
- Cross-platform script execution
- System information gathering and analysis
- Automated task execution with AI guidance

**Content Management**
- Text file processing and analysis
- Bulk file operations with validation
- Search and replace across multiple files
- Image processing and analysis

### 🔄 Architecture Highlights

**Service-Oriented Design**
- Dependency injection with Microsoft.Extensions.DI
- Interface segregation for clean abstractions
- Factory patterns for complex object creation
- Centralized configuration and state management

**Event-Driven Processing**
- Asynchronous streaming with cancellation support
- Real-time UI updates during processing
- State management with proper synchronization
- Event-driven keyboard and user interaction handling

**Extensible Tool System**
- Plugin-like tool architecture with AIFunction factory
- Consistent tool interface and response formatting
- Comprehensive error handling and validation
- Security boundaries enforced at the tool level

This initial release establishes MaxBot as a powerful, secure, and user-friendly AI assistant for developers and system administrators, providing a solid foundation for future enhancements and capabilities.
