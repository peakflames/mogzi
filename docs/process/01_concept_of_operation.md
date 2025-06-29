# MaxBot CLI Concept of Operations

## Essential User Features

**Core Chat Interface:**
- Terminal-based chat with AI models via Microsoft.Extensions.AI
- Real-time streaming responses with visual feedback
- Single-line input with cursor positioning support

**File Integration:**
- File path autocomplete using `@path/to/file` syntax (planned)
- Context loading from configuration files
- AI tool integration for file operations

**Tool System:**
- AI can execute file operations, code patches, and system commands
- Tool execution with progress indicators
- Integrated tool approval system (readonly mode by default)

**Command System:**
- Slash commands for app control (`/help`, `/clear`, `/exit`)
- Command history navigation with Ctrl+P/N
- History persistence across sessions

**Session Management:**
- Chat history storage and retrieval
- State management for UI components
- Session statistics tracking

**Customization:**
- Configuration via maxbot.config.json
- Flexible working directory management
- Logging and debug capabilities

**Developer Features:**
- Built with .NET 9 and C# for cross-platform support
- Spectre.Console for rich terminal UI
- Dependency injection architecture
- Comprehensive error handling and logging
