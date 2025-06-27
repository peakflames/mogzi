using MaxBot.Domain;
using System;
using System.IO;

namespace UI.Tests;

public class MockWorkingDirectoryProvider : IWorkingDirectoryProvider, IDisposable
{
    private readonly string _tempDirectory;

    public MockWorkingDirectoryProvider()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDirectory);
    }

    public string GetCurrentDirectory() => _tempDirectory;

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
        catch (IOException)
        {
            // Ignore exceptions during cleanup
        }
    }
}
