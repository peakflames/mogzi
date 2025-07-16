# Interactive Command Handling Test

This document demonstrates the enhanced ShellTool that can now handle interactive commands without blocking the application.

## Key Improvements Made

### 1. **Cancellation Support**
- Added `CancellationToken` parameter to `RunShellCommand`
- Proper process tree termination on cancellation
- Graceful handling of cancelled commands

### 2. **Real-Time Output Streaming**
- Non-blocking execution with event-driven output handling
- Real-time stdout/stderr streaming to UI
- Debug mode shows live command output

### 3. **Interactive Command Detection & Handling**
- Detects common interactive commands (npx create-*, npm create, etc.)
- Automatically provides default responses to common prompts
- 30-second timeout for interactive prompts

### 4. **Improved Process Management**
- Proper process group handling on Unix systems
- Enhanced process tree termination (SIGTERM → SIGKILL progression)
- Windows and Unix-specific cleanup strategies

## Test Commands

### Previously Blocking Commands (Now Fixed)
```bash
# These commands would previously block the application:
npx create-react-app my-app
npm create vite@latest my-vue-app
yarn create next-app my-next-app

# Interactive git commands:
git clone https://github.com/user/repo.git
git pull origin main

# Docker interactive commands:
docker run -it ubuntu bash
docker exec -it container_name bash
```

### How It Works

1. **Command Detection**: The system detects potentially interactive commands using pattern matching
2. **Stdin Redirection**: Enables stdin redirection for interactive input
3. **Default Responses**: Automatically sends default responses (like Enter) for common prompts
4. **Timeout Handling**: Commands that hang waiting for input are terminated after 30 seconds
5. **Cancellation**: Users can cancel long-running commands with Ctrl+C

### Example Usage in Mogzi

When an AI model calls:
```csharp
await shellTool.RunShellCommand(
    "npx create-react-app my-app", 
    "Create a new React application",
    null, // directory
    cancellationToken
);
```

The system will:
1. Detect this as an interactive command
2. Start the process with stdin redirection enabled
3. Monitor for prompts and provide default responses
4. Stream output in real-time to the UI
5. Allow cancellation if needed
6. Return results without blocking

## Technical Implementation Details

### Process Management
- Uses `ProcessStartInfo` with `RedirectStandardInput = true`
- Implements proper process group management for child processes
- Cross-platform process termination (Windows: taskkill, Unix: kill signals)

### Interactive Handling
- Pattern-based detection of interactive commands
- Automatic response to common prompts
- Configurable timeout for interactive sessions

### Error Handling
- Graceful handling of process execution errors
- Proper cleanup of resources on cancellation
- Detailed error reporting in debug mode

## Benefits

1. **No More Blocking**: Interactive commands no longer freeze the application
2. **Better UX**: Real-time output feedback during command execution
3. **Cancellation**: Users can interrupt long-running commands
4. **Robustness**: Improved error handling and resource cleanup
5. **Cross-Platform**: Works consistently on Windows, macOS, and Linux

This implementation is based on the approach used by Gemini CLI and provides a robust solution for handling interactive terminal commands in AI assistant applications.
