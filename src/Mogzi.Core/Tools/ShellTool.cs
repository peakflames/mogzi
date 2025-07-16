namespace Mogzi.Tools;

public class ShellTool(ApplicationConfiguration config, Action<string, ConsoleColor>? llmResponseDetailsCallback = null, IWorkingDirectoryProvider? workingDirectoryProvider = null)
{
    private readonly ApplicationConfiguration _config = config;
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
        [Description("(OPTIONAL) Directory to run the command in, if not the project root directory. Must be relative to the project root directory and must already exist.")] string? directory = null,
        CancellationToken cancellationToken = default)
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
            var result = await ExecuteCommand(command, executionDirectory, cancellationToken);

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

    private async Task<ShellExecutionResult> ExecuteCommand(string command, string workingDirectory, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new ShellExecutionResult
            {
                Command = command,
                Stdout = "",
                Stderr = "Command was cancelled before it could start.",
                Output = "Command was cancelled before it could start.",
                ExitCode = -1,
                ProcessId = 0,
                WasCancelled = true
            };
        }

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
                RedirectStandardInput = true, // Enable stdin redirection for interactive commands
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            }
        };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var output = new StringBuilder();
        var lastOutputTime = DateTime.UtcNow;
        var outputLock = new object();
        var processExited = false;

        // Real-time output handling with periodic updates
        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null && !processExited)
            {
                var data = StripAnsiCodes(e.Data);
                lock (outputLock)
                {
                    _ = stdout.AppendLine(data);
                    _ = output.AppendLine(data);
                    lastOutputTime = DateTime.UtcNow;
                }

                // Provide real-time feedback for long-running commands
                if (_config.Debug)
                {
                    _llmResponseDetailsCallback?.Invoke($"STDOUT: {data}", ConsoleColor.Gray);
                }
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null && !processExited)
            {
                var data = StripAnsiCodes(e.Data);
                lock (outputLock)
                {
                    _ = stderr.AppendLine(data);
                    _ = output.AppendLine(data);
                    lastOutputTime = DateTime.UtcNow;
                }

                // Provide real-time feedback for errors
                if (_config.Debug)
                {
                    _llmResponseDetailsCallback?.Invoke($"STDERR: {data}", ConsoleColor.Yellow);
                }
            }
        };

        process.Exited += (sender, e) =>
        {
            processExited = true;
        };

        try
        {
            _ = process.Start();

            // For Unix systems, set up process group for better process management
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    // This helps with managing child processes
                    process.StartInfo.UseShellExecute = false;
                }
                catch
                {
                    // Ignore if we can't set process group
                }
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Handle interactive commands by providing default responses to common prompts
            _ = Task.Run(async () =>
            {
                try
                {
                    await HandleInteractivePrompts(process, command, cancellationToken);
                }
                catch
                {
                    // Ignore errors in interactive handling
                }
            }, cancellationToken);

            // Set up cancellation handling
            var cancellationRegistration = cancellationToken.Register(async () =>
            {
                if (!process.HasExited)
                {
                    try
                    {
                        await KillProcessTreeAsync(process);
                    }
                    catch (Exception ex)
                    {
                        if (_config.Debug)
                        {
                            _llmResponseDetailsCallback?.Invoke($"Error killing process: {ex.Message}", ConsoleColor.Red);
                        }
                    }
                }
            });

            try
            {
                // Wait for process to exit or cancellation
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Process was cancelled
                return new ShellExecutionResult
                {
                    Command = command,
                    Stdout = stdout.ToString().TrimEnd(),
                    Stderr = stderr.ToString().TrimEnd(),
                    Output = output.ToString().TrimEnd(),
                    ExitCode = -1,
                    ProcessId = process.Id,
                    WasCancelled = true
                };
            }
            finally
            {
                cancellationRegistration.Dispose();
            }

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
                ProcessId = process.Id,
                WasCancelled = false
            };
        }
        catch (Exception ex)
        {
            return new ShellExecutionResult
            {
                Command = command,
                Stdout = stdout.ToString().TrimEnd(),
                Stderr = $"Process execution error: {ex.Message}",
                Output = $"Process execution error: {ex.Message}",
                ExitCode = -1,
                ProcessId = process.Id,
                WasCancelled = cancellationToken.IsCancellationRequested
            };
        }
    }

    private async Task HandleInteractivePrompts(Process process, string command, CancellationToken cancellationToken)
    {
        // Detect common interactive commands and provide default responses
        var commandLower = command.ToLowerInvariant();

        // Common interactive commands that might need default responses
        var interactiveCommands = new[]
        {
            "npx create-", "npm create", "yarn create",
            "git clone", "git pull", "git push",
            "sudo ", "su ",
            "ssh ", "scp ",
            "docker run -it", "docker exec -it"
        };

        var isInteractive = interactiveCommands.Any(commandLower.Contains);

        if (!isInteractive)
        {
            return;
        }

        // For interactive commands, we'll monitor for common prompts and provide defaults
        var timeout = TimeSpan.FromSeconds(30); // Timeout for interactive prompts
        var startTime = DateTime.UtcNow;

        while (!process.HasExited && !cancellationToken.IsCancellationRequested &&
               DateTime.UtcNow - startTime < timeout)
        {
            await Task.Delay(1000, cancellationToken);

            // If the process seems to be waiting (no recent output), send default responses
            // This is a simple heuristic - in practice, you might want more sophisticated detection
            try
            {
                if (commandLower.Contains("npx create-") || commandLower.Contains("npm create"))
                {
                    // For create-* commands, send Enter to accept defaults
                    await process.StandardInput.WriteLineAsync("");
                    await Task.Delay(500, cancellationToken);
                }
            }
            catch
            {
                // Ignore errors when trying to send input
                break;
            }
        }
    }

    private async Task KillProcessTreeAsync(Process process)
    {
        if (process.HasExited)
        {
            return;
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // On Windows, use taskkill to kill the process tree
                var killProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = $"/pid {process.Id} /f /t",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                _ = killProcess.Start();
                await killProcess.WaitForExitAsync();
            }
            else
            {
                // On Unix systems, try to kill the process group first, then the process
                try
                {
                    // Send SIGTERM to process group
                    _ = Process.Start("kill", $"-TERM -{process.Id}");
                    await Task.Delay(200);

                    if (!process.HasExited)
                    {
                        // Send SIGKILL to process group
                        _ = Process.Start("kill", $"-KILL -{process.Id}");
                    }
                }
                catch
                {
                    // Fallback to killing just the main process
                    process.Kill(entireProcessTree: true);
                }
            }
        }
        catch
        {
            // Last resort - try the built-in Kill method
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // If all else fails, ignore the error
            }
        }
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
        public bool WasCancelled { get; set; }
    }
}
