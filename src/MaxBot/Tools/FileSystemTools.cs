
using System.ComponentModel;
using System.IO;
using FluentResults;

namespace MaxBot.Tools;

public static class FileSystemTools
{
    [Description("Request to write content to a file at the specified path. If the file exists, it will be overwritten with the provided content. If the file doesn't exist, it will be created. This tool will automatically create any directories needed to write the file. Returns a string message indicating success or failure.")]
    public static string WriteFile(
        [Description("The path of the file to write (relative to the current working directory)")]
        string path,
        [Description("The content to write to the file. ALWAYS provide the COMPLETE intended content of the file, without any truncation or omissions. You MUST include ALL parts of the file, even if they haven't been modified.")]
        string content)
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), path);

        if (!Directory.Exists(Path.GetDirectoryName(filePath)))
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            }
            catch (Exception ex)
            {
                return $"ERROR: Failed to create directory. {ex.Message}";
            }
        }

        File.WriteAllText(filePath, content);
        return $"success";
    }

    [Description("Request to read the contents of a file at the specified path. Use this when you need to examine the contents of an existing file you do not know the contents of, for example to analyze code, review text files, or extract information from configuration files. Automatically extracts raw text from PDF and DOCX files. May not be suitable for other types of binary files, as it returns the raw content as a string")]
    public static string ReadFile(
        [Description("The path of the file to read (relative to the current working directory)")]
        string path)
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), path);
        if (File.Exists(filePath))
        {
            return File.ReadAllText(filePath);
        }

        return $"ERROR: Failed to read file. The file '{filePath}' does not exist.";
    }
}
        