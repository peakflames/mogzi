using FluentResults;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using MaxBot.Domain;

namespace MaxBot;

public partial class ChatClient
{
    public IChatClient ChatClientMEAI { get; init; }
    public MaxbotConfiguration Config { get; init; }
    public string SystemPrompt { get; init; }

    private ChatClient(IChatClient chatClient, MaxbotConfiguration config)
    {
        ChatClientMEAI = chatClient;
        Config = config;

        // Detect the current operating system, we need to handle Windows, MacOS, and Linux differently
        var operatingSystem = Environment.OSVersion.Platform;
        var defaultShell = operatingSystem switch {
            PlatformID.Win32NT => "powershell",
            PlatformID.MacOSX => "zsh",
            _ => "bash"
        };

        SystemPrompt = Promptinator.SystemPrompt(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), operatingSystem.ToString(), defaultShell);
    }

    
    public static Result<ChatClient> Create(string configFilePath)
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
            configRoot = JsonSerializer.Deserialize<MaxbotConfigurationRoot>(
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


        // Find the default profile or use the first one if no default is specified
        var profile = maxbotConfig.Profiles.FirstOrDefault(p => p.Default) ?? maxbotConfig.Profiles.FirstOrDefault();
        if (profile is null)
        {
            return Result.Fail($"No profiles found in the configuration.");
        }

        // Find the corresponding API provider
        var apiProvider = maxbotConfig.ApiProviders.FirstOrDefault(p => p.Name == profile.ApiProvider);
        if (apiProvider is null)
        {
            return Result.Fail($"API provider '{profile.ApiProvider}' specified in profile '{profile.Name}' not found.");
        }

        string apiKey = apiProvider.ApiKey;
        string baseUrl = apiProvider.BaseUrl;
        string modelId = profile.ModelId;

        var chatClient = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { 
                Endpoint = new(baseUrl)
            })
            .AsChatClient(modelId);

        return new ChatClient(chatClient, maxbotConfig);
    }
}
