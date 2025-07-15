namespace Mogzi;

public partial class ChatClient
{
    public IChatClient ChatClientMEAI { get; init; }
    public ApplicationConfiguration Config { get; init; }
    public Profile ActiveProfile { get; init; }
    public ApiProvider ActiveApiProvider { get; init; }

    // System prompt is now a computed property that regenerates each time it's accessed
    public string SystemPrompt => Promptinator.GetSystemPrompt(
        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        OperatingSystem.ToString(),
        DefaultShell,
        Username,
        Hostname,
        WorkingDirectoryProvider.GetCurrentDirectory(),
        Config,
        Mode);

    public PlatformID OperatingSystem { get; init; }
    public string DefaultShell { get; init; }
    public string Username { get; init; }
    public string Hostname { get; init; }
    private string Mode { get; init; }
    private IWorkingDirectoryProvider WorkingDirectoryProvider { get; init; }

    public ChatOptions ChatOptions { get; init; }
    private SystemTools SystemTools { get; init; }
    private DiffPatchTools DiffPatchTools { get; init; }
    private ReadTextFileTool ReadTextFileTool { get; init; }
    private ReadImageFileTool ReadImageFileTool { get; init; }
    // private ReadPdfFileTool ReadPdfFileTool { get; init; } // Not Native AOT yet
    private WriteFileTool WriteFileTool { get; init; }
    private EditTool EditTool { get; init; }
    private LSTool LSTool { get; init; }
    private GrepTool GrepTool { get; init; }
    private ShellTool ShellTool { get; init; }

    private ChatClient(IChatClient chatClient, IWorkingDirectoryProvider workingDirectoryProvider, ApplicationConfiguration config, Profile activeProfile, ApiProvider activeApiProvider, string mode, Action<string, ConsoleColor>? llmResponseDetailsCallback = null)
    {
        ChatClientMEAI = chatClient;
        Config = config;
        ActiveProfile = activeProfile;
        ActiveApiProvider = activeApiProvider;
        Mode = mode;

        // Detect the current operating system, we need to handle Windows, MacOS, and Linux differently
        OperatingSystem = Environment.OSVersion.Platform;
        DefaultShell = OperatingSystem switch
        {
            PlatformID.Win32NT => "powershell",
            PlatformID.MacOSX => "zsh",
            _ => "bash"
        };

        // Get username
        Username = Environment.UserName;

        // Get hostname
        Hostname = System.Net.Dns.GetHostName();

        // Initialize working directory provider
        WorkingDirectoryProvider = workingDirectoryProvider;

        // SystemPrompt is now a computed property, so we don't need to set it here

        SystemTools = new SystemTools(config, llmResponseDetailsCallback);
        DiffPatchTools = new DiffPatchTools(config, llmResponseDetailsCallback, workingDirectoryProvider);
        ReadTextFileTool = new ReadTextFileTool(config, llmResponseDetailsCallback, workingDirectoryProvider);
        ReadImageFileTool = new ReadImageFileTool(config, llmResponseDetailsCallback, workingDirectoryProvider);
        // ReadPdfFileTool = new ReadPdfFileTool(config, llmResponseDetailsCallback, workingDirectoryProvider); // Not Native AOT yet
        WriteFileTool = new WriteFileTool(config, llmResponseDetailsCallback, workingDirectoryProvider);
        EditTool = new EditTool(config, llmResponseDetailsCallback, workingDirectoryProvider);
        LSTool = new LSTool(config, llmResponseDetailsCallback, workingDirectoryProvider);
        GrepTool = new GrepTool(config, llmResponseDetailsCallback, workingDirectoryProvider);
        ShellTool = new ShellTool(config, llmResponseDetailsCallback, workingDirectoryProvider);

        var allTools = new List<AITool>();
        allTools.AddRange(SystemTools.GetTools().Cast<AITool>());
        allTools.AddRange(DiffPatchTools.GetTools().Cast<AITool>());
        allTools.Add(ReadTextFileTool.GetTool());
        allTools.Add(ReadImageFileTool.GetTool());
        // allTools.Add(ReadPdfFileTool.GetTool()); // Not Native AOT yet
        allTools.Add(WriteFileTool.GetTool());
        allTools.Add(EditTool.GetTool());
        allTools.Add(LSTool.GetTool());
        allTools.Add(GrepTool.GetTool());
        allTools.Add(ShellTool.GetTool());

        ChatOptions = new ChatOptions
        {
            Tools = allTools
        };
    }


    public static Result<ChatClient> Create(IWorkingDirectoryProvider workingDirectoryProvider, string? configFilePath = null, string? profileName = null, string? toolApprovals = null, string? mode = "oneshot", Action<string, ConsoleColor>? llmResponseDetailsCallback = null, bool debug = false)
    {
        var result = Create(null, workingDirectoryProvider, configFilePath, profileName, toolApprovals, mode, llmResponseDetailsCallback, debug);
        return result;
    }


    public static Result<ChatClient> Create(IChatClient? chatClient, IWorkingDirectoryProvider workingDirectoryProvider, string? configFilePath = null, string? profileName = null, string? toolApprovals = null, string? mode = "oneshot", Action<string, ConsoleColor>? llmResponseDetailsCallback = null, bool debug = false)
    {
        configFilePath ??= Utils.ConfigurationLocator.FindConfigPath();
        if (configFilePath is null)
        {
            return Result.Fail("Could not find mogzi.config.json in the current directory or home directory.");
        }

        string jsonContent;
        try
        {
            jsonContent = File.ReadAllText(configFilePath);
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to read config file at '{configFilePath}': {ex.Message}");
        }

        ApplicationConfigurationRoot? configRoot;
        try
        {
            configRoot = JsonSerializer.Deserialize(
                jsonContent,
                ApplicationConfigurationContext.Default.ApplicationConfigurationRoot);
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to parse config file: {ex.Message}");
        }

        var rootConfig = configRoot?.RootConfig;
        if (rootConfig is null)
        {
            return Result.Fail($"While reading the config '{configFilePath}', was not able to find the 'mogziConfig' section.");
        }

        if (!string.IsNullOrEmpty(toolApprovals))
        {
            rootConfig.ToolApprovals = toolApprovals;
        }

        // Set debug flag if specified
        rootConfig.Debug = debug;


        // Find the specified profile, or default profile, or first profile
        Profile? profile;
        if (!string.IsNullOrEmpty(profileName))
        {
            profile = rootConfig.Profiles.FirstOrDefault(p => p.Name == profileName);
            if (profile is null)
            {
                return Result.Fail($"Profile '{profileName}' not found in configuration.");
            }
        }
        else
        {
            profile = rootConfig.Profiles.FirstOrDefault(p => p.Default) ?? rootConfig.Profiles.FirstOrDefault();
            if (profile is null)
            {
                return Result.Fail($"No profiles found in the configuration.");
            }
        }

        // Find the corresponding API provider
        var apiProvider = rootConfig.ApiProviders.FirstOrDefault(p => p.Name == profile.ApiProvider);
        if (apiProvider is null)
        {
            return Result.Fail($"API provider '{profile.ApiProvider}' specified in profile '{profile.Name}' not found.");
        }

        var apiKey = apiProvider.ApiKey;
        var baseUrl = apiProvider.BaseUrl;
        var modelId = profile.ModelId;

        if (chatClient == null)
        {
            // Create custom HttpClient with Mogzi's default headers
            var customHttpClient = new HttpClient();
            customHttpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/peakflames/mogzi");
            customHttpClient.DefaultRequestHeaders.Add("X-Title", "Mogzi");

            chatClient = new OpenAIClient(
                    new ApiKeyCredential(apiKey),
                    new OpenAIClientOptions
                    {
                        Endpoint = new(baseUrl),
                        RetryPolicy = new ClientRetryPolicy(3),
                        NetworkTimeout = TimeSpan.FromSeconds(600),
                        UserAgentApplicationId = "mogzi",
                        Transport = new HttpClientPipelineTransport(customHttpClient)
                    }
                )
                .GetChatClient(modelId)
                .AsIChatClient();

            chatClient = ChatClientBuilderChatClientExtensions.AsBuilder(chatClient)
                .ConfigureOptions(options => options.MaxOutputTokens = 16000)
                .ConfigureOptions(options => options.Temperature = 0.0f)
                .UseFunctionInvocation()
                .Build();
        }


        return new ChatClient(chatClient, workingDirectoryProvider, rootConfig, profile, apiProvider, mode ?? "oneshot", llmResponseDetailsCallback);
    }

}
