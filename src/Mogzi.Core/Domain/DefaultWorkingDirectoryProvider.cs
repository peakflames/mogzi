namespace Mogzi.Domain;

public class DefaultWorkingDirectoryProvider : IWorkingDirectoryProvider
{
    public string GetCurrentDirectory()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        // Ensure we always return an absolute path
        return Path.IsPathRooted(currentDirectory)
            ? currentDirectory
            : Path.GetFullPath(currentDirectory);
    }
}
