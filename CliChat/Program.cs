using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;

const string API_KEY = "SOME_API_KEY";

IChatClient client = new OpenAIClient(
    new ApiKeyCredential(API_KEY),
    new OpenAIClientOptions { 
        Endpoint = new("SOME_ENDPOINT_URL")
    })
    .AsChatClient("claude-3-7-sonnet");


List<ChatMessage> conversation =
[
    new(ChatRole.System, "You are a helpful AI assistant"),
    new(ChatRole.User, "Hello! How can you help me today?"),
];

await foreach (var update in client.GetStreamingResponseAsync(conversation))
{
    Console.Write(update);
}


Console.WriteLine();
