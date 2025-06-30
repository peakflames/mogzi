using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace MaxBot.Tools;

public class SystemTools(MaxbotConfiguration config, Action<string, ConsoleColor>? llmResponseDetailsCallback = null)
{
    private readonly MaxbotConfiguration _config = config;
    private readonly Action<string, ConsoleColor>? _llmResponseDetailsCallback = llmResponseDetailsCallback;

    public List<AIFunction> GetTools()
    {
        return
        [
            AIFunctionFactory.Create(
                ExecuteCommand,
                new AIFunctionFactoryOptions
                {
                    Name = "execute_command",
                    Description = "Executes a CLI command on the user's system."
                }),
            AIFunctionFactory.Create(
                AttemptCompletion,
                new AIFunctionFactoryOptions
                {
                    Name = "attempt_completion",
                    Description = "Presents the result of completed work to the user. This tool marks the end of a task and provides a summary of what was accomplished, optionally with a command to demonstrate the results. Optionally you may provide a CLI command to showcase the result of your work. The user may respond with feedback if they are not satisfied with the result, which you can use to make improvements and try again. IMPORTANT. The tool will display the result text to the user, do not repeat it."
                })
        ];
    }

    public async Task<string> ExecuteCommand(
        [Description("The CLI command to execute. Command that result in or require interactivity are forbidden. Command must be compatible with the User's shell")] string command,
        [Description("A boolean indicating if the command requires explicit user approval.")] bool requiresApproval = true)
    {
        if (requiresApproval && _config.ToolApprovals.Equals("readonly", StringComparison.OrdinalIgnoreCase))
        {
            var msg = "Execution of this command requires approval. Please run with --tool-approvals all or use the /tool-approval slash command to grant permission.";
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke(msg, ConsoleColor.DarkGray);
            }
            return msg;
        }

        _llmResponseDetailsCallback?.Invoke($"Executing command: {command}", ConsoleColor.DarkGray);


        string fileName;
        string arguments;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            fileName = "cmd.exe";
            arguments = $"/C \"{command}\"";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            fileName = "/bin/zsh";
            arguments = $"-c \"{command}\"";
        }
        else // Fallback for Linux and other Unix-like systems
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
                CreateNoWindow = true
            }
        };

        _ = process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var result = "";
        if (!string.IsNullOrEmpty(output))
        {
            result += $"Output:\n{output}";
        }
        if (!string.IsNullOrEmpty(error))
        {
            result += $"Error:\n{error}";
        }

        var finalResult = string.IsNullOrEmpty(result) ? "Command executed successfully with no output." : result;
        if (_config.Debug)
        {
            _llmResponseDetailsCallback?.Invoke(finalResult, ConsoleColor.DarkGray);
        }
        return finalResult;
    }

    public string AttemptCompletion(
        [Description("The final result description - should be comprehensive and final")] string result)
    {
        _llmResponseDetailsCallback?.Invoke($"\n\n🎉 TASK COMPLETED 🎉\n\n{result}\n\n", ConsoleColor.Green);

        var response = new StringBuilder();
        _ = response.AppendLine("<tool_response tool_name=\"attempt_completion\" >");
        _ = response.AppendLine($"    <result status=\"SUCCESS\"  />");
        _ = response.AppendLine($"    <notes>Your message is now displayed to the User. DO NOT REPEAT as the User will see duplication of text</notes>");
        _ = response.AppendLine("</tool_response>");
        return response.ToString();
    }
}
