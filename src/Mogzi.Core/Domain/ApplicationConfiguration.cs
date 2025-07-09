using System.Text.Json.Serialization;

namespace Mogzi.Domain;

[JsonSerializable(typeof(ApplicationConfigurationRoot))]
public partial class ApplicationConfigurationContext : JsonSerializerContext
{
}

public class ApplicationConfigurationRoot
{
    [JsonPropertyName("mogziConfig")]
    public ApplicationConfiguration? RootConfig { get; set; }
}

public class ApplicationConfiguration
{
    [JsonPropertyName("defaultMode")]
    public string DefaultMode { get; set; } = "oneshot";

    [JsonPropertyName("apiProviders")]
    public List<ApiProvider> ApiProviders { get; set; } = [];

    [JsonPropertyName("profiles")]
    public List<Profile> Profiles { get; set; } = [];

    [JsonPropertyName("tool_approvals")]
    public string ToolApprovals { get; set; } = "readonly";

    [JsonPropertyName("debug")]
    public bool Debug { get; set; } = false;

    [JsonPropertyName("sessionListLimit")]
    public int SessionListLimit { get; set; } = 10;
}

public class ApiProvider
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = string.Empty;

    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; } = string.Empty;
}

public class Profile
{
    [JsonPropertyName("default")]
    public bool Default { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("apiProvider")]
    public string ApiProvider { get; set; } = string.Empty;

    [JsonPropertyName("modelId")]
    public string ModelId { get; set; } = string.Empty;
}
