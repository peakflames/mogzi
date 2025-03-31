
using System.ComponentModel;
using System.IO;
using FluentResults;

namespace MaxBot.Tools;

public class FileSystemTools
{

    private readonly Action<string>? llmResponseDetailsCallback = null;

    public FileSystemTools(Action<string>? llmResponseDetailsCallback = null)
    {
        this.llmResponseDetailsCallback = llmResponseDetailsCallback;
    }

    [Description("Request to list files and directories within the specified directory. If recursive is true, it will list all files and directories recursively. If recursive is false or not provided, it will only list the top-level contents. Do not use this tool to confirm the existence of files you may have created, as the user will let you know if the files were created successfully or not.")]
    public string ListFiles(
        [Description("The path of the directory to list contents for (relative to the current working directory)")]
        string path,
        [Description("(optional) Whether to list files recursively. Use true for recursive listing, false or omit for top-level only.")]
        bool recursive = false)
    {
        llmResponseDetailsCallback?.Invoke($"Listing files in '{path}'{(recursive ? "recursively" : "")}.");
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), path);

        var result = string.Empty;
        if (!Directory.Exists(filePath))
        {
            var msg = $"ERROR: Failed to list files. The directory '{filePath}' does not exist.";
            llmResponseDetailsCallback?.Invoke(msg);
            return msg;
        }

        string[]? files;

        if (recursive)
        {
            files = Directory.GetFiles(filePath, "*", SearchOption.AllDirectories);
        }
        else
        {
            files = Directory.GetFiles(filePath);
        }

        llmResponseDetailsCallback?.Invoke($"listed {files.Length} files for '{filePath}'.");

        result = string.Join("\n", files);
        return result;
    }


    [Description("Request to write content to a file at the specified path. If the file exists, it will be overwritten with the provided content. If the file doesn't exist, it will be created. This tool will automatically create any directories needed to write the file. Returns a string message indicating success or failure.")]
    public string WriteFile(
        [Description("The path of the file to write (relative to the current working directory)")]
        string path,
        [Description("The content to write to the file. ALWAYS provide the COMPLETE intended content of the file, without any truncation or omissions. You MUST include ALL parts of the file, even if they haven't been modified.")]
        string content)
    {
        llmResponseDetailsCallback?.Invoke($"Writing to file '{path}'.");
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), path);

        var result = string.Empty;

        if (!Directory.Exists(Path.GetDirectoryName(filePath)))
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            }
            catch (Exception ex)
            {
                var msg = $"ERROR: Failed to create directory. {ex.Message}";
                llmResponseDetailsCallback?.Invoke(msg);
                return msg;
            }
        }

        File.WriteAllText(filePath, content);

        return $"success";
    }

    [Description("Request to read the contents of a file at the specified path. Use this when you need to examine the contents of an existing file you do not know the contents of, for example to analyze code, review text files, or extract information from configuration files. Automatically extracts raw text from PDF and DOCX files. May not be suitable for other types of binary files, as it returns the raw content as a string")]
    public string ReadFile(
        [Description("The path of the file to read (relative to the current working directory)")]
        string path)
    {
        llmResponseDetailsCallback?.Invoke($"Reading file '{path}'.");
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), path);

        if (File.Exists(filePath))
        {
            var result = File.ReadAllText(filePath);
            return result;
        }

        var msg = $"ERROR: Failed to read file. The file '{filePath}' does not exist.";
        llmResponseDetailsCallback?.Invoke(msg);
        return msg;
    }
}
        