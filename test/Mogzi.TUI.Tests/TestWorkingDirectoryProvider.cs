namespace Mogzi.TUI.Tests;

/// <summary>
/// Test-specific working directory provider that ensures tests run from the project root directory.
/// This fixes the issue where tests run from the test output directory and can't find project files.
/// </summary>
public class TestWorkingDirectoryProvider : IWorkingDirectoryProvider
{
    private readonly string _projectRootDirectory;

    public TestWorkingDirectoryProvider()
    {
        // Find the project root by looking for the .git directory or src directory
        _projectRootDirectory = FindProjectRoot();
    }

    public string GetCurrentDirectory()
    {
        return _projectRootDirectory;
    }

    /// <summary>
    /// Finds the project root directory by traversing up from the current directory
    /// until we find a directory containing .git or src folders.
    /// </summary>
    private static string FindProjectRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var directory = new DirectoryInfo(currentDir);

        // Traverse up the directory tree looking for project root indicators
        while (directory != null)
        {
            // Check for common project root indicators
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")) ||
                Directory.Exists(Path.Combine(directory.FullName, "src")) ||
                File.Exists(Path.Combine(directory.FullName, "README.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        // Fallback: if we can't find the project root, use the current directory
        // This shouldn't happen in normal test execution
        return currentDir;
    }
}
