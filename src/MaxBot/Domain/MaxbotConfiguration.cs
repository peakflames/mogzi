using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MaxBot.Domain;

[JsonSerializable(typeof(MaxbotConfigurationRoot))]
public partial class MaxbotConfigurationContext : JsonSerializerContext
{
}

public class MaxbotConfigurationRoot
{
    [JsonPropertyName("maxbotConfig")]
    public MaxbotConfiguration? MaxbotConfig { get; set; }
}

public class MaxbotConfiguration
{
    [JsonPropertyName("apiProviders")]
    public List<ApiProvider> ApiProviders { get; set; } = new();
    
    [JsonPropertyName("profiles")]
    public List<Profile> Profiles { get; set; } = new();
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
