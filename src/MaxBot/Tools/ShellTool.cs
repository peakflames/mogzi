using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace MaxBot.Tools;

public class ShellTool(MaxbotConfiguration config, Action<string, ConsoleColor>? llmResponseDetailsCallback = null, IWorkingDirectoryProvider? workingDirectoryProvider = null)
{
    private readonly MaxbotConfiguration _config = config;
    private readonly Action<string, ConsoleColor>? _llmResponseDetailsCallback = llmResponseDetailsCallback;
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider = workingDirectoryProvider ?? new DefaultWorkingDirectoryProvider();
    private readonly HashSet<string> _whitelist = [];

    public AIFunction GetTool()
    {
        return AIFunctionFactory.Create(
            RunShellCommand,
            new AIFunctionFactoryOptions
            {
                Name = "run_shell_command",
                Description = "Execute shell commands"
            });
    }

    public async Task<string> RunShellCommand(
        [Description("Exact bash command to execute as `bash -c <command>`")] string command,
        [Description("Brief description of the command for the user. Be specific and concise. Ideally a single sentence. Can be up to 3 sentences for clarity. No line breaks.")] string? description = null,
        [Description("(OPTIONAL) Directory to run the command in, if not the project root directory. Must be relative to the project root directory and must already exist.")] string? directory = null)
    {
        try
        {
            // Validate parameters
            var validationError = ValidateToolParams(command, directory);
            if (validationError != null)
            {
                return CreateErrorResponse("run_shell_command", $"Command rejected: {command}. Reason: {validationError}");
            }

            // Check tool approvals
            var rootCommand = GetCommandRoot(command);
            if (rootCommand != null && !_whitelist.Contains(rootCommand))
            {
                if (_config.ToolApprovals.Equals("readonly", StringComparison.OrdinalIgnoreCase))
                {
                    var msg = "Execution of this command requires approval. Please run with --tool-approvals all or use the /tool-approval slash command to grant permission.";
                    if (_config.Debug)
                    {
                        _llmResponseDetailsCallback?.Invoke(msg, ConsoleColor.DarkGray);
                    }
                    return CreateErrorResponse("run_shell_command", msg);
                }
                else
                {
                    // Auto-approve for non-readonly mode
                    _ = _whitelist.Add(rootCommand);
                }
            }

            var workingDirectory = _workingDirectoryProvider.GetCurrentDirectory();
            var executionDirectory = directory != null
                ? Path.Combine(workingDirectory, directory)
                : workingDirectory;

            _llmResponseDetailsCallback?.Invoke($"Executing command: {command}{(directory != null ? $" [in {directory}]" : "")}{(description != null ? $" ({description.Replace("\n", " ")})" : "")}", ConsoleColor.DarkGray);

            // Execute the command
            var result = await ExecuteCommand(command, executionDirectory);

            if (_config.Debug)
            {
                var debugInfo = $"Command: {result.Command}, Exit Code: {result.ExitCode}, Stdout: {(string.IsNullOrEmpty(result.Stdout) ? "(empty)" : result.Stdout)}, Stderr: {(string.IsNullOrEmpty(result.Stderr) ? "(empty)" : result.Stderr)}";
                _llmResponseDetailsCallback?.Invoke(debugInfo, ConsoleColor.DarkGray);
            }

            return CreateSuccessResponse(command, directory, result);
        }
        catch (Exception ex)
        {
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"ERROR: Error executing command. {ex.Message}", ConsoleColor.Red);
            }
            return CreateErrorResponse("run_shell_command", $"Unexpected error: {ex.Message}");
        }
    }

    private string? ValidateToolParams(string command, string? directory)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return "Command cannot be empty.";
        }

        var rootCommand = GetCommandRoot(command);
        if (rootCommand == null)
        {
            return "Could not identify command root to obtain permission from user.";
        }

        if (directory != null)
        {
            if (Path.IsPathRooted(directory))
            {
                return "Directory cannot be absolute. Must be relative to the project root directory.";
            }

            var workingDirectory = _workingDirectoryProvider.GetCurrentDirectory();
            var fullDirectory = Path.Combine(workingDirectory, directory);

            if (!Directory.Exists(fullDirectory))
            {
                return "Directory must exist.";
            }

            // Ensure directory is within working directory
            if (!IsPathInWorkingDirectory(fullDirectory, workingDirectory))
            {
                return $"Directory must be within the project root directory: {directory}";
            }
        }

        return null;
    }

    private string? GetCommandRoot(string command)
    {
        return command
            .Trim() // remove leading and trailing whitespace
            .Replace("(", "").Replace(")", "").Replace("{", "").Replace("}", "") // remove all grouping operators
            .Split([' ', ';', '&', '|'], StringSplitOptions.RemoveEmptyEntries)[0] // split on any whitespace or separator or chaining operators and take first part
            ?.Split(['/', '\\']) // split on any path separators
            .LastOrDefault(); // take last part and return command root
    }

    private bool IsPathInWorkingDirectory(string absolutePath, string workingDirectory)
    {
        try
        {
            var normalizedAbsolutePath = Path.GetFullPath(absolutePath);
            var normalizedWorkingDirectory = Path.GetFullPath(workingDirectory);

            // Check if the path is exactly the working directory
            if (string.Equals(normalizedAbsolutePath, normalizedWorkingDirectory,
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                return true;
            }

            // Ensure working directory ends with directory separator for subdirectory comparison
            if (!normalizedWorkingDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
                !normalizedWorkingDirectory.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                normalizedWorkingDirectory += Path.DirectorySeparatorChar;
            }

            return normalizedAbsolutePath.StartsWith(normalizedWorkingDirectory,
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private async Task<ShellExecutionResult> ExecuteCommand(string command, string workingDirectory)
    {
        string fileName;
        string arguments;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            fileName = "cmd.exe";
            arguments = $"/c \"{command}\"";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            fileName = "/bin/zsh";
            arguments = $"-c \"{command}\"";
        }
        else // Linux and other Unix-like systems
        {
            fileName = "/bin/bash";
            arguments = $"-c \"{command}\"";
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            }
        };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var output = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                var data = StripAnsiCodes(e.Data);
                _ = stdout.AppendLine(data);
                _ = output.AppendLine(data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                var data = StripAnsiCodes(e.Data);
                _ = stderr.AppendLine(data);
                _ = output.AppendLine(data);
            }
        };

        _ = process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        var stdoutText = stdout.ToString().TrimEnd();
        var stderrText = stderr.ToString().TrimEnd();
        var outputText = output.ToString().TrimEnd();

        return new ShellExecutionResult
        {
            Command = command,
            Stdout = stdoutText,
            Stderr = stderrText,
            Output = outputText,
            ExitCode = process.ExitCode,
            ProcessId = process.Id
        };
    }

    private string StripAnsiCodes(string input)
    {
        // Simple ANSI escape sequence removal
        return System.Text.RegularExpressions.Regex.Replace(input, @"\x1B\[[0-?]*[ -/]*[@-~]", "");
    }

    private string CreateSuccessResponse(string command, string? directory, ShellExecutionResult result)
    {
        var notes = new StringBuilder();
        _ = notes.AppendLine($"Successfully executed command: {command}");
        if (directory != null)
        {
            _ = notes.AppendLine($"Directory: {directory}");
        }
        _ = notes.AppendLine($"Exit code: {result.ExitCode}");

        var llmContent = new StringBuilder();
        _ = llmContent.AppendLine($"Command: {result.Command}");
        _ = llmContent.AppendLine($"Directory: {directory ?? "(root)"}");
        _ = llmContent.AppendLine($"Stdout: {(string.IsNullOrEmpty(result.Stdout) ? "(empty)" : result.Stdout)}");
        _ = llmContent.AppendLine($"Stderr: {(string.IsNullOrEmpty(result.Stderr) ? "(empty)" : result.Stderr)}");
        _ = llmContent.AppendLine($"Exit Code: {result.ExitCode}");
        _ = llmContent.AppendLine($"Process Group PGID: {result.ProcessId}");

        var displayOutput = string.IsNullOrEmpty(result.Output) ?
            (result.ExitCode != 0 ? $"Command exited with code: {result.ExitCode}" : "") :
            result.Output;

        return $@"<tool_response tool_name=""run_shell_command"">
    <notes>{SecurityElement.Escape(notes.ToString().Trim())}</notes>
    <result status=""SUCCESS"" />
    <llm_content>{SecurityElement.Escape(llmContent.ToString().Trim())}</llm_content>
    <display_output>{SecurityElement.Escape(displayOutput)}</display_output>
</tool_response>";
    }

    private string CreateErrorResponse(string toolName, string error)
    {
        return $@"<tool_response tool_name=""{toolName}"">
    <result status=""FAILED"" />
    <error>{SecurityElement.Escape(error)}</error>
</tool_response>";
    }

    private class ShellExecutionResult
    {
        public string Command { get; set; } = "";
        public string Stdout { get; set; } = "";
        public string Stderr { get; set; } = "";
        public string Output { get; set; } = "";
        public int ExitCode { get; set; }
        public int ProcessId { get; set; }
    }
}
