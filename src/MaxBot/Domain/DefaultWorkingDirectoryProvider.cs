namespace MaxBot.Domain;

public class DefaultWorkingDirectoryProvider : IWorkingDirectoryProvider
{
    public string GetCurrentDirectory()
    {
        return Directory.GetCurrentDirectory();
    }
}
