namespace MaxBot.Prompts;

/// <summary>
/// Contains environment-specific system prompt information
/// </summary>
public static class EnvSystemPrompt
{
    public static string GetEnvPrompt(string currentDateTime, string userOperatingSystem, string userShell, string username, string hostname, string currentWorkingDirectory, string mode, string toolApprovals)
    {
        return $"""
<system_environment>
SYSTEM INFORMATION:
- The User's operating system is {userOperatingSystem}.
- The User's shell is {userShell}.
- The User's username is {username}.
- The User's hostname is {hostname}.
- The User's current working directory absolute path is '{currentWorkingDirectory}'.
- The User is active mode is '{mode}'.
- The User's Tool Approval Setting is '{toolApprovals.ToLower()}'. 
- The User's current date is {currentDateTime}.
</system_environment>
""";
    }

}
