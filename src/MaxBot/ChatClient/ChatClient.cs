using FluentResults;
using System.Text.Json;
using System.ClientModel.Primitives;

namespace MaxBot;

public partial class ChatClient
{
    public IChatClient ChatClientMEAI { get; init; }
    public MaxbotConfiguration Config { get; init; }
    public Profile ActiveProfile { get; init; }
    public ApiProvider ActiveApiProvider { get; init; }

    // System prompt is now a computed property that regenerates each time it's accessed
    public string SystemPrompt => Promptinator.GetSystemPrompt(
        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        OperatingSystem.ToString(),
        DefaultShell,
        Username,
        Hostname,
        Directory.GetCurrentDirectory(),
        Config,
        Mode);

    public PlatformID OperatingSystem { get; init; }
    public string DefaultShell { get; init; }
    public string Username { get; init; }
    public string Hostname { get; init; }
    private string Mode { get; init; }

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

    private ChatClient(IChatClient chatClient, MaxbotConfiguration config, Profile activeProfile, ApiProvider activeApiProvider, string mode, Action<string, ConsoleColor>? llmResponseDetailsCallback = null)
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

        // SystemPrompt is now a computed property, so we don't need to set it here

        SystemTools = new SystemTools(config, llmResponseDetailsCallback);
        DiffPatchTools = new DiffPatchTools(config, llmResponseDetailsCallback);
        ReadTextFileTool = new ReadTextFileTool(config, llmResponseDetailsCallback);
        ReadImageFileTool = new ReadImageFileTool(config, llmResponseDetailsCallback);
        // ReadPdfFileTool = new ReadPdfFileTool(config, llmResponseDetailsCallback); // Not Native AOT yet
        WriteFileTool = new WriteFileTool(config, llmResponseDetailsCallback);
        EditTool = new EditTool(config, llmResponseDetailsCallback);
        LSTool = new LSTool(config, llmResponseDetailsCallback);
        GrepTool = new GrepTool(config, llmResponseDetailsCallback);
        ShellTool = new ShellTool(config, llmResponseDetailsCallback);

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


    public static Result<ChatClient> Create(string configFilePath, string? profileName = null, string? toolApprovals = null, string? mode = "oneshot", Action<string, ConsoleColor>? llmResponseDetailsCallback = null, bool debug = false)
    {
        var result = Create(null, configFilePath, profileName, toolApprovals, mode, llmResponseDetailsCallback, debug);
        return result;
    }


    public static Result<ChatClient> Create(IChatClient? chatClient, string configFilePath, string? profileName = null, string? toolApprovals = null, string? mode = "oneshot", Action<string, ConsoleColor>? llmResponseDetailsCallback = null, bool debug = false)
    {
        string jsonContent;
        try
        {
            jsonContent = File.ReadAllText(configFilePath);
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to read config file: {ex.Message}");
        }

        MaxbotConfigurationRoot? configRoot;
        try
        {
            configRoot = JsonSerializer.Deserialize(
                jsonContent,
                MaxbotConfigurationContext.Default.MaxbotConfigurationRoot);
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to parse config file: {ex.Message}");
        }

        var maxbotConfig = configRoot?.MaxbotConfig;
        if (maxbotConfig is null)
        {
            return Result.Fail($"While reading the config '{configFilePath}', was not able to find the 'maxbotConfig' section.");
        }

        if (!string.IsNullOrEmpty(toolApprovals))
        {
            maxbotConfig.ToolApprovals = toolApprovals;
        }

        // Set debug flag if specified
        maxbotConfig.Debug = debug;


        // Find the specified profile, or default profile, or first profile
        Profile? profile;
        if (!string.IsNullOrEmpty(profileName))
        {
            profile = maxbotConfig.Profiles.FirstOrDefault(p => p.Name == profileName);
            if (profile is null)
            {
                return Result.Fail($"Profile '{profileName}' not found in configuration.");
            }
        }
        else
        {
            profile = maxbotConfig.Profiles.FirstOrDefault(p => p.Default) ?? maxbotConfig.Profiles.FirstOrDefault();
            if (profile is null)
            {
                return Result.Fail($"No profiles found in the configuration.");
            }
        }

        // Find the corresponding API provider
        var apiProvider = maxbotConfig.ApiProviders.FirstOrDefault(p => p.Name == profile.ApiProvider);
        if (apiProvider is null)
        {
            return Result.Fail($"API provider '{profile.ApiProvider}' specified in profile '{profile.Name}' not found.");
        }

        var apiKey = apiProvider.ApiKey;
        var baseUrl = apiProvider.BaseUrl;
        var modelId = profile.ModelId;

        if (chatClient == null)
        {
            chatClient = new OpenAIClient(
                    new ApiKeyCredential(apiKey),
                    new OpenAIClientOptions
                    {
                        Endpoint = new(baseUrl),
                        RetryPolicy = new ClientRetryPolicy(3),
                        NetworkTimeout = TimeSpan.FromSeconds(600),
                        UserAgentApplicationId = "maxbot"
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


        return new ChatClient(chatClient, maxbotConfig, profile, apiProvider, mode ?? "oneshot", llmResponseDetailsCallback);
    }

}
