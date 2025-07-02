using Mogzi.Domain;

namespace Mogzi.Tests.Tools;

public class MockWorkingDirectoryProvider : IWorkingDirectoryProvider
{
    private readonly string _path;

    public MockWorkingDirectoryProvider(string path)
    {
        _path = path;
    }

    public string GetCurrentDirectory()
    {
        return _path;
    }
}
