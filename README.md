# Multi-purpuse Autonomous eXpert (Max) 😻

An Autonomous AI agent right in your terminal or CI/CD pipelinee and open to multiple API providers.

Perform engineering research, full coding tasks, and multiple workflow with either natural language or shell scripts 👈

## Features ✨

- Support both `oneshot` and `Interactive Chat` interface with streaming AI responses
- Currently supports OpenAI-compatible APIs like Requesty.ai, Openrouter.ai, Supernova, Cerebras, Deepseek, more providers coming soon.
   - Excellent LLM results with Google Gemini, OpenAI GPT, and Anthropic models.

- Profile-based configuration for easy switching between providers and models
- Cross-platform support (Windows, MacOS, Linux)
- Control file system access via tool appproval (`readonly` or `all`)
- Chat history persistence to continue conversations across sessions
- Session management to list and load previous chat sessions
- MCP-Support coming soon

### Examples 💡

```bash
# oneshot request
max "Which is the tallest pokemon?"

# start full chat using your 'Sonnet' profile
max --chat -p Sonnet

# translate a README.md to portugese
cat README.md | max "Translate to portugese"

# list all saved chat sessions
max --list-sessions

# load a previous chat session
max --chat -l 20250621_230940
```

## Prerequisites ✅

Before running the application, ensure you have:

- Access to at least one supported API provider
- Configuration file set up with your API providers and profiles

## Setup 🛠️

1. Download the latest release following the steps below.

   For Windows:

   - Download loads max.exe and moves it to your WindowsApp directory
   ```sh
   Start-BitsTransfer -Source https://github.com/peakflames/maxbot/releases/latest/download/max-win-x64.exe -Destination max.exe; move -Force max.exe $env:USERPROFILE\\AppData\\Local\\Microsoft\\WindowsApps
   ```

    For MacOS (sudo):

    - Download loads max and moves it to your `/usr/local/bin`
  
    ```sh
    sudo curl -L -o max https://github.com/peakflames/maxbot/releases/latest/download/max-osx-x64 && sudo chmod +x max && sudo mv -f max /usr/local/bin
    ```

    For Linux (sudo)

    - Download loads max and moves it to your `/usr/local/bin`

    ```sh
    sudo curl -L -o max https://github.com/peakflames/maxbot/releases/latest/download/max-linux-x64 && sudo chmod +x max && sudo mv -f max /usr/local/bin
    ```

2. In your home directory, create a configuration file (`maxbot.config.json`) with your API provider details:

   ```json
   {
       "maxbotConfig": {
           "apiProviders": [
               {
                    "name": "MyCompanyProvider",
                    "type": "OpenAI-Compatible",
                    "apiKey": "example-key",
                    "baseUrl": "https://litellm.mycompany.com"
                },
                {
                    "name": "RequestyAI",
                    "type": "OpenAI-Compatible",
                    "apiKey": "example-key",
                    "baseUrl": "https://router.requesty.ai/v1"
                },
                {
                    "name": "Deepseek",
                    "type": "OpenAI-Compatible",
                    "apiKey": "example-key",
                    "baseUrl": "https://api.deepseek.com"
                }
           ],
           "profiles": [
               {
                   "default": true,
                   "name": "Default",
                   "apiProvider": "MyCompanyProvider",
                   "modelId": "03-mini"
               },
               {
                   "name": "Sonnet",
                   "apiProvider": "RequestyAI",
                   "modelId": "vertex/anthropic/claude-3-7-sonnet"
               },
               {
                    "name": "Gemini",
                    "apiProvider": "RequestyAI",
                    "modelId": "google/gemini-2.5-pro"
                },
                {
                    "name": "V3",
                    "apiProvider": "Deepseek",
                    "modelId": "deepseek-chat"
                }
           ]
       }
   }
   ```

## Usage 📄

```bash
max [prompt] [options]
```

To start a chat session, use the `--chat` option:

```bash
max --chat [options]
```

### Chat Interface 💬

- Start typing your messages after the prompt
- AI responses will stream in real-time with green text
- Exit the chat by typing `exit`, `quit`, or pressing Enter with no message
- Chat history is automatically saved to disk for future sessions
- Access previous chat sessions using the `--list-sessions` and `--chat -l <session_id>` commands

#### Slash Commands

While in chat mode, you can use the following slash commands for quick actions:

- `/status` - Display current configuration (similar to `--status` flag)
- `/tool-approval [readonly|all]` - Change tool approval setting on the fly
- `/sessions` - List available chat sessions (similar to `--list-sessions` flag)
- `/load-session [ID]` - Load a specific chat session without exiting chat mode
- `/help` - Display available slash commands

Example:
```
% /tool-approval all
Tool approval setting updated to 'all'

% /help
Available Slash Commands:
  /status                 - Display current configuration
  /tool-approval [mode]   - Set tool approval mode (readonly|all)
  /sessions               - List available chat sessions
  /load-session [ID]      - Load a specific chat session
  /help                   - Display this help message
```

### Configuration ⚙️

The application uses a JSON configuration file with the following structure:

- **apiProviders**: List of available API providers
  - **name**: Unique identifier for the provider
  - **type**: Provider type (OpenAI-Compatible at this time, more to come)
  - **apiKey**: Your API key for the provider
  - **baseUrl**: Base URL for the API (for OpenAI-Compatible providers)

- **profiles**: List of available profiles
  - **default**: Whether this is the default profile (true/false)
  - **name**: Profile name
  - **apiProvider**: Name of the API provider to use (must match a provider name)
  - **modelId**: Model ID to use for chat completion

### Tool Approval 🔒

MaxBot includes a tool approval feature to provide control over file system operations. This setting can be configured in your `maxbot.config.json` file or overridden at runtime with a command-line argument.

**Configuration:**

To set the default tool approval mode, add the `tool_approvals` property to your `maxbot.config.json`:

```json
{
    "maxbotConfig": {
        "tool_approvals": "readonly",
        "apiProviders": [ ... ],
        "profiles": [ ... ]
    }
}
```

**Command-Line Override:**

You can override the configuration file setting using the `--tool-approvals` or `-ta` argument:

```bash
max "Create a new file" -ta all
```

**Modes:**

*   `readonly`: (Default) MaxBot will ask for your permission before performing any write operations (e.g., creating or modifying files).
*   `all`: MaxBot is pre-approved to perform any file system operation without asking for confirmation.

### Session Management 💾

MaxBot automatically saves your chat history to disk, allowing you to continue conversations across multiple sessions. Each chat session is stored in a timestamped directory in your user profile.

**Listing Sessions:**

To view all your saved chat sessions:

```bash
max --list-sessions
```

This command displays a card-like view of each session, including:
- Session ID (timestamp)
- Creation date
- Last updated date
- Number of messages
- First user prompt (to help identify the conversation)

Example output:
```
┌─────────────────────────────────────────────────────────────┐
│ Session: 20250621_230940                                    │
├─────────────────────────────────────────────────────────────┤
│ Created:      2025-06-21 23:09:40                           │
│ Last Updated: 2025-06-21 23:15:22                           │
│ Messages:     8 entries                                     │
├─────────────────────────────────────────────────────────────┤
│ First Prompt: "How can I implement a binary search tree     │
│               in C#?"                                       │
└─────────────────────────────────────────────────────────────┘
```

**Loading a Session:**

To continue a previous conversation, use the `--chat` option with the `-l` or `--load` flag followed by the session ID:

```bash
max --chat -l 20250621_230940
```

This will:
1. Load the entire conversation history
2. Display the previous messages so you can see the context
3. Allow you to continue the conversation from where you left off

All chat sessions are stored in the `.maxbot/chats` directory in your user profile. Each session is a separate directory containing a `chatHistory.json` file with the conversation data.

**Session Storage Location:**

- Windows: `C:\Users\<username>\.maxbot\chats\`
- macOS: `/Users/<username>/.maxbot/chats/`
- Linux: `/home/<username>/.maxbot/chats/`

## Available Assistant Tools 🛠️

MaxBot is equipped with a powerful set of tools to interact with your local system. Here is a summary of the currently implemented and upcoming tools:

| Tool Name           | Status | Notes                                                              | Safety/Security Features                                                                                                                            |
| ------------------- | ------ | ------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| `list_files`        | ✅ Implemented | Lists files and directories, supports recursive listing.           | Read-only operation. Constrained to the working directory.                                                                                          |
| `read_file`         | ✅ Implemented | Reads the content of a specified file. Supports PDF and DOCX.      | Read-only operation. Constrained to the working directory.                                                                                          |
| `write_file`        | ✅ Implemented | Creates a new file or overwrites an existing one.                  | - Requires `--tool-approvals all`.<br>- Constrained to the working directory.<br>- Uses atomic writes with backups and checksums.<br>- Respects read-only file attributes. |
| `apply_code_patch`  | ✅ Implemented | Applies Git-style unified diff patches for precise code changes.   | - Requires `--tool-approvals all`.<br>- Constrained to the working directory.<br>- Uses fuzzy matching for robust patch application.                |
| `generate_code_patch` | ✅ Implemented | Creates unified diff patches showing changes between content.       | Read-only operation. Generates patches without modifying files.                                                                                     |
| `preview_patch_application` | ✅ Implemented | Previews what changes a patch would make without applying them.     | Read-only operation. Safe preview of potential changes.                                                                                             |
| `execute_command`   | ✅ Implemented | Executes shell commands.                                           | - Requires `--tool-approvals all` by default.<br>- Cross-platform aware (uses `cmd`, `zsh`, `bash` appropriately).                                     |
| `search_files`      | ✅ Implemented | Regex-based search across files for enhanced code analysis.        | Read-only operation. Constrained to the working directory. Supports pattern matching across file contents.                                          |
| `attempt_completion` | ✅ Implemented | Signals task completion and presents results to the user.          | Read-only operation. Provides structured completion feedback and optional demonstration commands.                                                    |
| `list_code_definition_names` | 📋 Planned | Lists code definitions (classes, functions, methods) in source files. | Read-only operation. Constrained to the working directory. Provides code structure analysis.                                                        |
| `mcp_tools`         | 📋 Planned | Model Context Protocol support for external integrations.          | Configurable approval requirements. Enables integration with external services and APIs.                                                            |

## Contributing 🤝

Contributions are welcome! Please read the [developer guidelines](.clinerules/developer_guidelines.md) for more information on how to build the project and run tests.

## Copyright ©️

Copyright (c) 2025 Todd Schavey

This work is licensed under the Creative Commons Attribution-NonCommercial 4.0 International License.
To view a copy of this license, visit http://creativecommons.org/licenses/by-nc/4.0/ or send a letter
to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.

## Who is Max?

He's the best damn orange cat on the plant. 🐈
