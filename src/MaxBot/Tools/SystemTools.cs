using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MaxBot.Domain;
using Microsoft.Extensions.AI;

namespace MaxBot.Tools;

public class SystemTools
{
    private readonly MaxbotConfiguration _config;
    private readonly Action<string>? _llmResponseDetailsCallback = null;

    public SystemTools(MaxbotConfiguration config, Action<string>? llmResponseDetailsCallback = null)
    {
        _config = config;
        _llmResponseDetailsCallback = llmResponseDetailsCallback;
    }

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
                })
        ];
    }

    public async Task<string> ExecuteCommand(
        [Description("The CLI command to execute.")] string command,
        [Description("A boolean indicating if the command requires explicit user approval.")] bool requiresApproval = true)
    {
        if (requiresApproval && _config.ToolApprovals.Equals("readonly", StringComparison.OrdinalIgnoreCase))
        {
            var msg = "Execution of this command requires approval. Please run with --tool-approvals all or use the /tool-approval slash command to grant permission.";
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke(msg);
            }
            return msg;
        }

        if (_config.Debug)
        {
            _llmResponseDetailsCallback?.Invoke($"Executing command: {command}");
        }

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

        process.Start();
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
            _llmResponseDetailsCallback?.Invoke(finalResult);
        }
        return finalResult;
    }
}
