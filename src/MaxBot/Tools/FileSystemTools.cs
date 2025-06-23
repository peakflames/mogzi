using Microsoft.Extensions.AI;
using MaxBot.Domain;

namespace MaxBot.Tools;

public partial class FileSystemTools
{
    private readonly MaxbotConfiguration _config;
    private readonly Action<string>? _llmResponseDetailsCallback = null;
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider;

    // Directories to exclude from recursive listing to prevent enormous data returns
    private static readonly HashSet<string> BlacklistedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "node_modules",
        ".git",
        "venv",
        ".venv",
        "__pycache__",
        "bin",
        "obj",
        ".vs",
        "dist",
        "build",
        ".idea",
        "target",
        "vendor",
        ".next",
        ".nuxt",
        "coverage",
        ".nyc_output",
        ".cache",
        ".parcel-cache",
        ".webpack",
        ".rollup.cache"
    };

    public FileSystemTools(MaxbotConfiguration config, Action<string>? llmResponseDetailsCallback = null, IWorkingDirectoryProvider? workingDirectoryProvider = null)
    {
        _config = config;
        _llmResponseDetailsCallback = llmResponseDetailsCallback;
        _workingDirectoryProvider = workingDirectoryProvider ?? new DefaultWorkingDirectoryProvider();
    }

    public List<AIFunction> GetTools()
    {
        return
        [
            AIFunctionFactory.Create(
                ListFiles,
                new AIFunctionFactoryOptions
                {
                    Name = "list_files",
                    Description = "A read-only tool to list files and directories within the specified directory. If recursive is true, it will list all files and directories recursively. If recursive is false or not provided, it will only list the top-level contents. This tool can be useful to confirm the existence of files you may have created as part of a prudent verification strategy."
                }),
            AIFunctionFactory.Create(
                WriteFile,
                new AIFunctionFactoryOptions
                {
                    Name = "write_file",
                    Description = "Request to write content to a file at the specified path. If the file exists, it will be overwritten with the provided content. If the file doesn't exist, it will be created. This tool will automatically create any directories needed to write the file. Returns a string message indicating status, absolute_path, and the full, updated content of the file."
                }),
            AIFunctionFactory.Create(
                ReadFile,
                new AIFunctionFactoryOptions
                {
                    Name = "read_file",
                    Description = "A read-only tool to read the contents of a file at the specified path. Use this when you need to examine the contents of an existing file you do not know the contents of, for example to analyze code, review text files, or extract information from configuration files. Automatically extracts raw text from PDF and DOCX files. May not be suitable for other types of binary files, as it returns the raw content as a string"
                }),
            AIFunctionFactory.Create(
                SearchFiles,
                new AIFunctionFactoryOptions
                {
                    Name = "search_files",
                    Description = "A read-only tool to perform a regex search across files in a specified directory, providing context-rich results. This tool searches for patterns or specific content across multiple files, displaying each match with encapsulating context."
                })
        ];
    }
}
