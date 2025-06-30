================================================
FILE: src/Libraries/Microsoft.Extensions.AI.OpenAI/README.md
================================================
# Microsoft.Extensions.AI.OpenAI

Provides an implementation of the `IChatClient` interface for the `OpenAI` package and OpenAI-compatible endpoints.

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.AI.OpenAI
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage Examples

### Chat

```csharp
using Microsoft.Extensions.AI;

IChatClient client =
    new OpenAI.Chat.ChatClient("gpt-4o-mini", Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
    .AsIChatClient();

Console.WriteLine(await client.GetResponseAsync("What is AI?"));
```

### Chat + Conversation History

```csharp
using Microsoft.Extensions.AI;

IChatClient client =
    new OpenAI.Chat.ChatClient("gpt-4o-mini", Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
    .AsIChatClient();

Console.WriteLine(await client.GetResponseAsync(
[
    new ChatMessage(ChatRole.System, "You are a helpful AI assistant"),
    new ChatMessage(ChatRole.User, "What is AI?"),
]));
```

### Chat streaming

```csharp
using Microsoft.Extensions.AI;

IChatClient client =
    new OpenAI.Chat.ChatClient("gpt-4o-mini", Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
    .AsIChatClient();

await foreach (var update in client.GetStreamingResponseAsync("What is AI?"))
{
    Console.Write(update);
}
```

### Tool calling

```csharp
using System.ComponentModel;
using Microsoft.Extensions.AI;

IChatClient openaiClient =
    new OpenAI.Chat.ChatClient("gpt-4o-mini", Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
    .AsIChatClient();

IChatClient client = new ChatClientBuilder(openaiClient)
    .UseFunctionInvocation()
    .Build();

ChatOptions chatOptions = new()
{
    Tools = [AIFunctionFactory.Create(GetWeather)]
};

await foreach (var message in client.GetStreamingResponseAsync("Do I need an umbrella?", chatOptions))
{
    Console.Write(message);
}

[Description("Gets the weather")]
static string GetWeather() => Random.Shared.NextDouble() > 0.5 ? "It's sunny" : "It's raining";
```

### Caching

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

IChatClient openaiClient =
    new OpenAI.Chat.ChatClient("gpt-4o-mini", Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
    .AsIChatClient();

IChatClient client = new ChatClientBuilder(openaiClient)
    .UseDistributedCache(cache)
    .Build();

for (int i = 0; i < 3; i++)
{
    await foreach (var message in client.GetStreamingResponseAsync("In less than 100 words, what is AI?"))
    {
        Console.Write(message);
    }

    Console.WriteLine();
    Console.WriteLine();
}
```

### Telemetry

```csharp
using Microsoft.Extensions.AI;
using OpenTelemetry.Trace;

// Configure OpenTelemetry exporter
var sourceName = Guid.NewGuid().ToString();
var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
    .AddSource(sourceName)
    .AddConsoleExporter()
    .Build();

IChatClient openaiClient =
    new OpenAI.Chat.ChatClient("gpt-4o-mini", Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
    .AsIChatClient();

IChatClient client = new ChatClientBuilder(openaiClient)
    .UseOpenTelemetry(sourceName: sourceName, configure: c => c.EnableSensitiveData = true)
    .Build();

Console.WriteLine(await client.GetResponseAsync("What is AI?"));
```

### Telemetry, Caching, and Tool Calling

```csharp
using System.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;

// Configure telemetry
var sourceName = Guid.NewGuid().ToString();
var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
    .AddSource(sourceName)
    .AddConsoleExporter()
    .Build();

// Configure caching
IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

// Configure tool calling
var chatOptions = new ChatOptions
{
    Tools = [AIFunctionFactory.Create(GetPersonAge)]
};

IChatClient openaiClient =
    new OpenAI.Chat.ChatClient("gpt-4o-mini", Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
    .AsIChatClient();

IChatClient client = new ChatClientBuilder(openaiClient)
    .UseDistributedCache(cache)
    .UseFunctionInvocation()
    .UseOpenTelemetry(sourceName: sourceName, configure: c => c.EnableSensitiveData = true)
    .Build();

for (int i = 0; i < 3; i++)
{
    Console.WriteLine(await client.GetResponseAsync("How much older is Alice than Bob?", chatOptions));
}

[Description("Gets the age of a person specified by name.")]
static int GetPersonAge(string personName) =>
    personName switch
    {
        "Alice" => 42,
        "Bob" => 35,
        _ => 26,
    };
```

### Text embedding generation

```csharp
using Microsoft.Extensions.AI;

IEmbeddingGenerator<string, Embedding<float>> generator =
    new OpenAI.Embeddings.EmbeddingClient("text-embedding-3-small", Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
    .AsIEmbeddingGenerator();

var embeddings = await generator.GenerateAsync("What is AI?");

Console.WriteLine(string.Join(", ", embeddings[0].Vector.ToArray()));
```

### Text embedding generation with caching

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

IEmbeddingGenerator<string, Embedding<float>> openAIGenerator =
    new OpenAI.Embeddings.EmbeddingClient("text-embedding-3-small", Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
    .AsIEmbeddingGenerator();

IEmbeddingGenerator<string, Embedding<float>> generator = new EmbeddingGeneratorBuilder<string, Embedding<float>>(openAIGenerator)
    .UseDistributedCache(cache)
    .Build();

foreach (var prompt in new[] { "What is AI?", "What is .NET?", "What is AI?" })
{
    var embeddings = await generator.GenerateAsync(prompt);

    Console.WriteLine(string.Join(", ", embeddings[0].Vector.ToArray()));
}
```

### Dependency Injection

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// App Setup
var builder = Host.CreateApplicationBuilder();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Trace));

builder.Services.AddChatClient(services =>
    new OpenAI.Chat.ChatClient("gpt-4o-mini", Environment.GetEnvironmentVariable("OPENAI_API_KEY")).AsIChatClient())
    .UseDistributedCache()
    .UseLogging();

var app = builder.Build();

// Elsewhere in the app
var chatClient = app.Services.GetRequiredService<IChatClient>();
Console.WriteLine(await chatClient.GetResponseAsync("What is AI?"));
```

### Minimal Web API

```csharp
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddChatClient(services =>
    new OpenAI.Chat.ChatClient("gpt-4o-mini", builder.Configuration["OPENAI_API_KEY"]).AsIChatClient());

builder.Services.AddEmbeddingGenerator(services =>
    new OpenAI.Embeddings.EmbeddingClient("text-embedding-3-small", builder.Configuration["OPENAI_API_KEY"]).AsIEmbeddingGenerator());

var app = builder.Build();

app.MapPost("/chat", async (IChatClient client, string message) =>
{
    var response = await client.GetResponseAsync(message);
    return response.Message;
});

app.MapPost("/embedding", async (IEmbeddingGenerator<string, Embedding<float>> client, string message) =>
{
    var response = await client.GenerateAsync(message);
    return response[0].Vector;
});

app.Run();
```

## Documentation

Learn how to create a conversational .NET console chat app using an OpenAI or Azure OpenAI model with the [Quickstart - Build an AI chat app with .NET](https://learn.microsoft.com/dotnet/ai/quickstarts/build-chat-app?pivots=openai) documentation.

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.OpenAI/Microsoft.Extensions.AI.OpenAI.csproj
================================================
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>Microsoft.Extensions.AI</RootNamespace>
    <Description>Implementation of generative AI abstractions for OpenAI-compatible endpoints.</Description>
    <Workstream>AI</Workstream>
  </PropertyGroup>

  <PropertyGroup>
    <Stage>preview</Stage>
    <EnablePackageValidation>false</EnablePackageValidation>
    <MinCodeCoverage>49</MinCodeCoverage>
    <MinMutationScore>0</MinMutationScore>
  </PropertyGroup>

  <PropertyGroup>
    <TargetFrameworks>$(TargetFrameworks);netstandard2.0</TargetFrameworks>
    <NoWarn>$(NoWarn);CA1063;CA1508;CA2227;SA1316;S1121;S3358;EA0002;OPENAI002</NoWarn>
    <NoWarn>$(NoWarn);MEAI001</NoWarn>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <DisableNETStandardCompatErrors>true</DisableNETStandardCompatErrors>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>

  <PropertyGroup>
    <InjectCompilerFeatureRequiredOnLegacy>true</InjectCompilerFeatureRequiredOnLegacy>
    <InjectExperimentalAttributeOnLegacy>true</InjectExperimentalAttributeOnLegacy>
    <InjectRequiredMemberOnLegacy>true</InjectRequiredMemberOnLegacy>
    <InjectSharedEmptyCollections>true</InjectSharedEmptyCollections>
    <InjectSharedServerSentEvents>true</InjectSharedServerSentEvents>
    <InjectStringHashOnLegacy>true</InjectStringHashOnLegacy>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="OpenAI" />
    <PackageReference Include="System.Memory.Data" />
    <PackageReference Include="System.Text.Json" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../Microsoft.Extensions.AI.Abstractions/Microsoft.Extensions.AI.Abstractions.csproj" />
  </ItemGroup>

</Project>



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.OpenAI/Microsoft.Extensions.AI.OpenAI.json
================================================



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.OpenAI/OpenAIAssistantChatClient.cs
================================================
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using OpenAI.Assistants;

#pragma warning disable CA1031 // Do not catch general exception types
#pragma warning disable SA1005 // Single line comments should begin with single space
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable S125 // Sections of code should not be commented out
#pragma warning disable S907 // "goto" statement should not be used
#pragma warning disable S1067 // Expressions should not be too complex
#pragma warning disable S1751 // Loops with at most one iteration should be refactored
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
#pragma warning disable S4456 // Parameter validation in yielding methods should be wrapped
#pragma warning disable S4457 // Parameter validation in "async"/"await" methods should be wrapped

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="IChatClient"/> for an Azure.AI.Agents.Persistent <see cref="AssistantClient"/>.</summary>
[Experimental("OPENAI001")]
internal sealed partial class OpenAIAssistantChatClient : IChatClient
{
    /// <summary>The underlying <see cref="AssistantClient" />.</summary>
    private readonly AssistantClient _client;

    /// <summary>Metadata for the client.</summary>
    private readonly ChatClientMetadata _metadata;

    /// <summary>The ID of the agent to use.</summary>
    private readonly string _assistantId;

    /// <summary>The thread ID to use if none is supplied in <see cref="ChatOptions.ConversationId"/>.</summary>
    private readonly string? _defaultThreadId;

    /// <summary>List of tools associated with the assistant.</summary>
    private IReadOnlyList<ToolDefinition>? _assistantTools;

    /// <summary>Initializes a new instance of the <see cref="OpenAIAssistantChatClient"/> class for the specified <see cref="AssistantClient"/>.</summary>
    public OpenAIAssistantChatClient(AssistantClient assistantClient, string assistantId, string? defaultThreadId)
    {
        _client = Throw.IfNull(assistantClient);
        _assistantId = Throw.IfNullOrWhitespace(assistantId);

        _defaultThreadId = defaultThreadId;

        // https://github.com/openai/openai-dotnet/issues/215
        // The endpoint isn't currently exposed, so use reflection to get at it, temporarily. Once packages
        // implement the abstractions directly rather than providing adapters on top of the public APIs,
        // the package can provide such implementations separate from what's exposed in the public API.
        Uri providerUrl = typeof(AssistantClient).GetField("_endpoint", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(assistantClient) as Uri ?? OpenAIClientExtensions.DefaultOpenAIEndpoint;

        _metadata = new("openai", providerUrl);
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType is null ? throw new ArgumentNullException(nameof(serviceType)) :
        serviceKey is not null ? null :
        serviceType == typeof(ChatClientMetadata) ? _metadata :
        serviceType == typeof(AssistantClient) ? _client :
        serviceType.IsInstanceOfType(this) ? this :
        null;

    /// <inheritdoc />
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        GetStreamingResponseAsync(messages, options, cancellationToken).ToChatResponseAsync(cancellationToken);

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        // Extract necessary state from messages and options.
        (RunCreationOptions runOptions, List<FunctionResultContent>? toolResults) = await CreateRunOptionsAsync(messages, options, cancellationToken).ConfigureAwait(false);

        // Get the thread ID.
        string? threadId = options?.ConversationId ?? _defaultThreadId;
        if (threadId is null && toolResults is not null)
        {
            Throw.ArgumentException(nameof(messages), "No thread ID was provided, but chat messages includes tool results.");
        }

        // Get any active run ID for this thread. This is necessary in case a thread has been left with an
        // active run, in which all attempts other than submitting tools will fail. We thus need to cancel
        // any active run on the thread.
        ThreadRun? threadRun = null;
        if (threadId is not null)
        {
            await foreach (var run in _client.GetRunsAsync(
                threadId,
                new RunCollectionOptions { Order = RunCollectionOrder.Descending, PageSizeLimit = 1 },
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                if (run.Status != RunStatus.Completed && run.Status != RunStatus.Cancelled && run.Status != RunStatus.Failed && run.Status != RunStatus.Expired)
                {
                    threadRun = run;
                }

                break;
            }
        }

        // Submit the request.
        IAsyncEnumerable<StreamingUpdate> updates;
        if (threadRun is not null &&
            ConvertFunctionResultsToToolOutput(toolResults, out List<ToolOutput>? toolOutputs) is { } toolRunId &&
            toolRunId == threadRun.Id)
        {
            // There's an active run and we have tool results to submit, so submit the results and continue streaming.
            // This is going to ignore any additional messages in the run options, as we are only submitting tool outputs,
            // but there doesn't appear to be a way to submit additional messages, and having such additional messages is rare.
            updates = _client.SubmitToolOutputsToRunStreamingAsync(threadRun.ThreadId, threadRun.Id, toolOutputs, cancellationToken);
        }
        else
        {
            if (threadId is null)
            {
                // No thread ID was provided, so create a new thread.
                ThreadCreationOptions threadCreationOptions = new();
                foreach (var message in runOptions.AdditionalMessages)
                {
                    threadCreationOptions.InitialMessages.Add(message);
                }

                runOptions.AdditionalMessages.Clear();

                var thread = await _client.CreateThreadAsync(threadCreationOptions, cancellationToken).ConfigureAwait(false);
                threadId = thread.Value.Id;
            }
            else if (threadRun is not null)
            {
                // There was an active run; we need to cancel it before starting a new run.
                _ = await _client.CancelRunAsync(threadId, threadRun.Id, cancellationToken).ConfigureAwait(false);
                threadRun = null;
            }

            // Now create a new run and stream the results.
            updates = _client.CreateRunStreamingAsync(
                threadId: threadId,
                _assistantId,
                runOptions,
                cancellationToken);
        }

        // Process each update.
        string? responseId = null;
        await foreach (var update in updates.ConfigureAwait(false))
        {
            switch (update)
            {
                case ThreadUpdate tu:
                    threadId ??= tu.Value.Id;
                    goto default;

                case RunUpdate ru:
                    threadId ??= ru.Value.ThreadId;
                    responseId ??= ru.Value.Id;

                    ChatResponseUpdate ruUpdate = new()
                    {
                        AuthorName = _assistantId,
                        ConversationId = threadId,
                        CreatedAt = ru.Value.CreatedAt,
                        MessageId = responseId,
                        ModelId = ru.Value.Model,
                        RawRepresentation = ru,
                        ResponseId = responseId,
                        Role = ChatRole.Assistant,
                    };

                    if (ru.Value.Usage is { } usage)
                    {
                        ruUpdate.Contents.Add(new UsageContent(new()
                        {
                            InputTokenCount = usage.InputTokenCount,
                            OutputTokenCount = usage.OutputTokenCount,
                            TotalTokenCount = usage.TotalTokenCount,
                        }));
                    }

                    if (ru is RequiredActionUpdate rau && rau.ToolCallId is string toolCallId && rau.FunctionName is string functionName)
                    {
                        ruUpdate.Contents.Add(
                            new FunctionCallContent(
                                JsonSerializer.Serialize([ru.Value.Id, toolCallId], AssistantJsonContext.Default.StringArray),
                                functionName,
                                JsonSerializer.Deserialize(rau.FunctionArguments, AssistantJsonContext.Default.IDictionaryStringObject)!));
                    }

                    yield return ruUpdate;
                    break;

                case MessageContentUpdate mcu:
                    yield return new(mcu.Role == MessageRole.User ? ChatRole.User : ChatRole.Assistant, mcu.Text)
                    {
                        AuthorName = _assistantId,
                        ConversationId = threadId,
                        MessageId = responseId,
                        RawRepresentation = mcu,
                        ResponseId = responseId,
                    };
                    break;

                default:
                    yield return new ChatResponseUpdate
                    {
                        AuthorName = _assistantId,
                        ConversationId = threadId,
                        MessageId = responseId,
                        RawRepresentation = update,
                        ResponseId = responseId,
                        Role = ChatRole.Assistant,
                    };
                    break;
            }
        }
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // nop
    }

    /// <summary>
    /// Creates the <see cref="RunCreationOptions"/> to use for the request and extracts any function result contents 
    /// that need to be submitted as tool results.
    /// </summary>
    private async ValueTask<(RunCreationOptions RunOptions, List<FunctionResultContent>? ToolResults)> CreateRunOptionsAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options, CancellationToken cancellationToken)
    {
        // Create the options instance to populate, either a fresh or using one the caller provides.
        RunCreationOptions runOptions =
            options?.RawRepresentationFactory?.Invoke(this) as RunCreationOptions ??
            new();

        // Populate the run options from the ChatOptions, if provided.
        if (options is not null)
        {
            runOptions.MaxOutputTokenCount ??= options.MaxOutputTokens;
            runOptions.ModelOverride ??= options.ModelId;
            runOptions.NucleusSamplingFactor ??= options.TopP;
            runOptions.Temperature ??= options.Temperature;
            runOptions.AllowParallelToolCalls ??= options.AllowMultipleToolCalls;

            if (options.Tools is { Count: > 0 } tools)
            {
                // If the caller has provided any tool overrides, we'll assume they don't want to use the assistant's tools.
                // But if they haven't, the only way we can provide our tools is via an override, whereas we'd really like to
                // just add them. To handle that, we'll get all of the assistant's tools and add them to the override list
                // along with our tools.
                if (runOptions.ToolsOverride.Count == 0)
                {
                    if (_assistantTools is null)
                    {
                        var assistant = await _client.GetAssistantAsync(_assistantId, cancellationToken).ConfigureAwait(false);
                        _assistantTools = assistant.Value.Tools;
                    }

                    foreach (var tool in _assistantTools)
                    {
                        runOptions.ToolsOverride.Add(tool);
                    }
                }

                // The caller can provide tools in the supplied ThreadAndRunOptions. Augment it with any supplied via ChatOptions.Tools.
                foreach (AITool tool in tools)
                {
                    switch (tool)
                    {
                        case AIFunction aiFunction:
                            bool? strict = aiFunction.AdditionalProperties.TryGetValue(OpenAIClientExtensions.StrictKey, out var strictValue) && strictValue is bool strictBool ?
                                strictBool :
                                null;

                            JsonElement jsonSchema = OpenAIClientExtensions.GetSchema(aiFunction, strict);

                            runOptions.ToolsOverride.Add(new FunctionToolDefinition(aiFunction.Name)
                            {
                                Description = aiFunction.Description,
                                Parameters = BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(jsonSchema, AssistantJsonContext.Default.JsonElement)),
                                StrictParameterSchemaEnabled = strict,
                            });
                            break;

                        case HostedCodeInterpreterTool:
                            runOptions.ToolsOverride.Add(new CodeInterpreterToolDefinition());
                            break;
                    }
                }
            }

            // Store the tool mode, if relevant.
            if (runOptions.ToolConstraint is null)
            {
                switch (options.ToolMode)
                {
                    case NoneChatToolMode:
                        runOptions.ToolConstraint = ToolConstraint.None;
                        break;

                    case AutoChatToolMode:
                        runOptions.ToolConstraint = ToolConstraint.Auto;
                        break;

                    case RequiredChatToolMode required when required.RequiredFunctionName is { } functionName:
                        runOptions.ToolConstraint = new ToolConstraint(ToolDefinition.CreateFunction(functionName));
                        break;

                    case RequiredChatToolMode required:
                        runOptions.ToolConstraint = ToolConstraint.Required;
                        break;
                }
            }

            // Store the response format, if relevant.
            if (runOptions.ResponseFormat is null)
            {
                switch (options.ResponseFormat)
                {
                    case ChatResponseFormatText:
                        runOptions.ResponseFormat = AssistantResponseFormat.CreateTextFormat();
                        break;

                    case ChatResponseFormatJson jsonFormat when OpenAIClientExtensions.StrictSchemaTransformCache.GetOrCreateTransformedSchema(jsonFormat) is { } jsonSchema:
                        runOptions.ResponseFormat = AssistantResponseFormat.CreateJsonSchemaFormat(
                            jsonFormat.SchemaName,
                            BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(jsonSchema, AssistantJsonContext.Default.JsonElement)),
                            jsonFormat.SchemaDescription);
                        break;

                    case ChatResponseFormatJson jsonFormat:
                        runOptions.ResponseFormat = AssistantResponseFormat.CreateJsonObjectFormat();
                        break;
                }
            }
        }

        // Configure system instructions.
        StringBuilder? instructions = null;
        void AppendSystemInstructions(string? toAppend)
        {
            if (!string.IsNullOrEmpty(toAppend))
            {
                if (instructions is null)
                {
                    instructions = new(toAppend);
                }
                else
                {
                    _ = instructions.AppendLine().AppendLine(toAppend);
                }
            }
        }

        AppendSystemInstructions(runOptions.AdditionalInstructions);
        AppendSystemInstructions(options?.Instructions);

        // Process ChatMessages.
        List<FunctionResultContent>? functionResults = null;
        foreach (var chatMessage in messages)
        {
            List<MessageContent> messageContents = [];

            // Assistants doesn't support system/developer messages directly. It does support transient per-request instructions,
            // so we can use the system/developer messages to build up a set of instructions that will be passed to the assistant
            // as part of this request. However, in doing so, on a subsequent request that information will be lost, as there's no
            // way to store per-thread instructions in the OpenAI Assistants API. We don't want to convert these to user messages,
            // however, as that would then expose the system/developer messages in a way that might make the model more likely
            // to include that information in its responses. System messages should ideally be instead done as instructions to
            // the assistant when the assistant is created.
            if (chatMessage.Role == ChatRole.System ||
                chatMessage.Role == OpenAIClientExtensions.ChatRoleDeveloper)
            {
                foreach (var textContent in chatMessage.Contents.OfType<TextContent>())
                {
                    AppendSystemInstructions(textContent.Text);
                }

                continue;
            }

            foreach (AIContent content in chatMessage.Contents)
            {
                switch (content)
                {
                    case TextContent text:
                        messageContents.Add(MessageContent.FromText(text.Text));
                        break;

                    case UriContent image when image.HasTopLevelMediaType("image"):
                        messageContents.Add(MessageContent.FromImageUri(image.Uri));
                        break;

                    // Assistants doesn't support data URIs.
                    //case DataContent image when image.HasTopLevelMediaType("image"):
                    //    messageContents.Add(MessageContent.FromImageUri(new Uri(image.Uri)));
                    //    break;

                    case FunctionResultContent result:
                        (functionResults ??= []).Add(result);
                        break;

                    case AIContent when content.RawRepresentation is MessageContent rawRep:
                        messageContents.Add(rawRep);
                        break;
                }
            }

            if (messageContents.Count > 0)
            {
                runOptions.AdditionalMessages.Add(new ThreadInitializationMessage(
                    chatMessage.Role == ChatRole.Assistant ? MessageRole.Assistant : MessageRole.User,
                    messageContents));
            }
        }

        runOptions.AdditionalInstructions = instructions?.ToString();

        return (runOptions, functionResults);
    }

    /// <summary>Convert <see cref="FunctionResultContent"/> instances to <see cref="ToolOutput"/> instances.</summary>
    /// <param name="toolResults">The tool results to process.</param>
    /// <param name="toolOutputs">The generated list of tool outputs, if any could be created.</param>
    /// <returns>The run ID associated with the corresponding function call requests.</returns>
    private static string? ConvertFunctionResultsToToolOutput(List<FunctionResultContent>? toolResults, out List<ToolOutput>? toolOutputs)
    {
        string? runId = null;
        toolOutputs = null;
        if (toolResults?.Count > 0)
        {
            foreach (var frc in toolResults)
            {
                // When creating the FunctionCallContext, we created it with a CallId == [runId, callId].
                // We need to extract the run ID and ensure that the ToolOutput we send back to Azure
                // is only the call ID.
                string[]? runAndCallIDs;
                try
                {
                    runAndCallIDs = JsonSerializer.Deserialize(frc.CallId, AssistantJsonContext.Default.StringArray);
                }
                catch
                {
                    continue;
                }

                if (runAndCallIDs is null ||
                    runAndCallIDs.Length != 2 ||
                    string.IsNullOrWhiteSpace(runAndCallIDs[0]) || // run ID
                    string.IsNullOrWhiteSpace(runAndCallIDs[1]) || // call ID
                    (runId is not null && runId != runAndCallIDs[0]))
                {
                    continue;
                }

                runId = runAndCallIDs[0];
                (toolOutputs ??= []).Add(new(runAndCallIDs[1], frc.Result?.ToString() ?? string.Empty));
            }
        }

        return runId;
    }

    [JsonSerializable(typeof(JsonElement))]
    [JsonSerializable(typeof(string[]))]
    [JsonSerializable(typeof(IDictionary<string, object>))]
    private sealed partial class AssistantJsonContext : JsonSerializerContext;
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.OpenAI/OpenAIChatClient.cs
================================================
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using OpenAI;
using OpenAI.Chat;

#pragma warning disable CA1308 // Normalize strings to uppercase
#pragma warning disable EA0011 // Consider removing unnecessary conditional access operator (?)
#pragma warning disable S1067 // Expressions should not be too complex
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
#pragma warning disable SA1204 // Static elements should appear before instance elements

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="IChatClient"/> for an OpenAI <see cref="OpenAIClient"/> or <see cref="ChatClient"/>.</summary>
internal sealed partial class OpenAIChatClient : IChatClient
{
    /// <summary>Metadata about the client.</summary>
    private readonly ChatClientMetadata _metadata;

    /// <summary>The underlying <see cref="ChatClient" />.</summary>
    private readonly ChatClient _chatClient;

    /// <summary>Initializes a new instance of the <see cref="OpenAIChatClient"/> class for the specified <see cref="ChatClient"/>.</summary>
    /// <param name="chatClient">The underlying client.</param>
    /// <exception cref="ArgumentNullException"><paramref name="chatClient"/> is <see langword="null"/>.</exception>
    public OpenAIChatClient(ChatClient chatClient)
    {
        _ = Throw.IfNull(chatClient);

        _chatClient = chatClient;

        // https://github.com/openai/openai-dotnet/issues/215
        // The endpoint and model aren't currently exposed, so use reflection to get at them, temporarily. Once packages
        // implement the abstractions directly rather than providing adapters on top of the public APIs,
        // the package can provide such implementations separate from what's exposed in the public API.
        Uri providerUrl = typeof(ChatClient).GetField("_endpoint", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(chatClient) as Uri ?? OpenAIClientExtensions.DefaultOpenAIEndpoint;
        string? model = typeof(ChatClient).GetField("_model", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(chatClient) as string;

        _metadata = new("openai", providerUrl, model);
    }

    /// <inheritdoc />
    object? IChatClient.GetService(Type serviceType, object? serviceKey)
    {
        _ = Throw.IfNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(ChatClientMetadata) ? _metadata :
            serviceType == typeof(ChatClient) ? _chatClient :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        var openAIChatMessages = ToOpenAIChatMessages(messages, options, AIJsonUtilities.DefaultOptions);
        var openAIOptions = ToOpenAIOptions(options);

        // Make the call to OpenAI.
        var response = await _chatClient.CompleteChatAsync(openAIChatMessages, openAIOptions, cancellationToken).ConfigureAwait(false);

        return FromOpenAIChatCompletion(response.Value, options, openAIOptions);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        var openAIChatMessages = ToOpenAIChatMessages(messages, options, AIJsonUtilities.DefaultOptions);
        var openAIOptions = ToOpenAIOptions(options);

        // Make the call to OpenAI.
        var chatCompletionUpdates = _chatClient.CompleteChatStreamingAsync(openAIChatMessages, openAIOptions, cancellationToken);

        return FromOpenAIStreamingChatCompletionAsync(chatCompletionUpdates, cancellationToken);
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the IChatClient interface.
    }

    /// <summary>Converts an Extensions chat message enumerable to an OpenAI chat message enumerable.</summary>
    private static IEnumerable<OpenAI.Chat.ChatMessage> ToOpenAIChatMessages(IEnumerable<ChatMessage> inputs, ChatOptions? chatOptions, JsonSerializerOptions jsonOptions)
    {
        // Maps all of the M.E.AI types to the corresponding OpenAI types.
        // Unrecognized or non-processable content is ignored.

        if (chatOptions?.Instructions is { } instructions && !string.IsNullOrWhiteSpace(instructions))
        {
            yield return new SystemChatMessage(instructions);
        }

        foreach (ChatMessage input in inputs)
        {
            if (input.Role == ChatRole.System ||
                input.Role == ChatRole.User ||
                input.Role == OpenAIClientExtensions.ChatRoleDeveloper)
            {
                var parts = ToOpenAIChatContent(input.Contents);
                yield return
                    input.Role == ChatRole.System ? new SystemChatMessage(parts) { ParticipantName = input.AuthorName } :
                    input.Role == OpenAIClientExtensions.ChatRoleDeveloper ? new DeveloperChatMessage(parts) { ParticipantName = input.AuthorName } :
                    new UserChatMessage(parts) { ParticipantName = input.AuthorName };
            }
            else if (input.Role == ChatRole.Tool)
            {
                foreach (AIContent item in input.Contents)
                {
                    if (item is FunctionResultContent resultContent)
                    {
                        string? result = resultContent.Result as string;
                        if (result is null && resultContent.Result is not null)
                        {
                            try
                            {
                                result = JsonSerializer.Serialize(resultContent.Result, jsonOptions.GetTypeInfo(typeof(object)));
                            }
                            catch (NotSupportedException)
                            {
                                // If the type can't be serialized, skip it.
                            }
                        }

                        yield return new ToolChatMessage(resultContent.CallId, result ?? string.Empty);
                    }
                }
            }
            else if (input.Role == ChatRole.Assistant)
            {
                List<ChatMessageContentPart>? contentParts = null;
                List<ChatToolCall>? toolCalls = null;
                string? refusal = null;
                foreach (var content in input.Contents)
                {
                    switch (content)
                    {
                        case ErrorContent ec when ec.ErrorCode == nameof(AssistantChatMessage.Refusal):
                            refusal = ec.Message;
                            break;

                        case FunctionCallContent fc:
                            (toolCalls ??= []).Add(
                                ChatToolCall.CreateFunctionToolCall(fc.CallId, fc.Name, new(JsonSerializer.SerializeToUtf8Bytes(
                                    fc.Arguments, jsonOptions.GetTypeInfo(typeof(IDictionary<string, object?>))))));
                            break;

                        default:
                            if (ToChatMessageContentPart(content) is { } part)
                            {
                                (contentParts ??= []).Add(part);
                            }

                            break;
                    }
                }

                AssistantChatMessage message;
                if (contentParts is not null)
                {
                    message = new(contentParts);
                    if (toolCalls is not null)
                    {
                        foreach (var toolCall in toolCalls)
                        {
                            message.ToolCalls.Add(toolCall);
                        }
                    }
                }
                else
                {
                    message = toolCalls is not null ?
                        new(toolCalls) :
                        new(ChatMessageContentPart.CreateTextPart(string.Empty));
                }

                message.ParticipantName = input.AuthorName;
                message.Refusal = refusal;

                yield return message;
            }
        }
    }

    /// <summary>Converts a list of <see cref="AIContent"/> to a list of <see cref="ChatMessageContentPart"/>.</summary>
    private static List<ChatMessageContentPart> ToOpenAIChatContent(IList<AIContent> contents)
    {
        List<ChatMessageContentPart> parts = [];

        foreach (var content in contents)
        {
            if (ToChatMessageContentPart(content) is { } part)
            {
                parts.Add(part);
            }
        }

        if (parts.Count == 0)
        {
            parts.Add(ChatMessageContentPart.CreateTextPart(string.Empty));
        }

        return parts;
    }

    private static ChatMessageContentPart? ToChatMessageContentPart(AIContent content)
    {
        switch (content)
        {
            case TextContent textContent:
                return ChatMessageContentPart.CreateTextPart(textContent.Text);

            case UriContent uriContent when uriContent.HasTopLevelMediaType("image"):
                return ChatMessageContentPart.CreateImagePart(uriContent.Uri, GetImageDetail(content));

            case DataContent dataContent when dataContent.HasTopLevelMediaType("image"):
                return ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(dataContent.Data), dataContent.MediaType, GetImageDetail(content));

            case DataContent dataContent when dataContent.HasTopLevelMediaType("audio"):
                var audioData = BinaryData.FromBytes(dataContent.Data);
                if (dataContent.MediaType.Equals("audio/mpeg", StringComparison.OrdinalIgnoreCase))
                {
                    return ChatMessageContentPart.CreateInputAudioPart(audioData, ChatInputAudioFormat.Mp3);
                }
                else if (dataContent.MediaType.Equals("audio/wav", StringComparison.OrdinalIgnoreCase))
                {
                    return ChatMessageContentPart.CreateInputAudioPart(audioData, ChatInputAudioFormat.Wav);
                }

                break;

            case DataContent dataContent when dataContent.MediaType.StartsWith("application/pdf", StringComparison.OrdinalIgnoreCase):
                return ChatMessageContentPart.CreateFilePart(BinaryData.FromBytes(dataContent.Data), dataContent.MediaType, $"{Guid.NewGuid():N}.pdf");

            case AIContent when content.RawRepresentation is ChatMessageContentPart rawContentPart:
                return rawContentPart;
        }

        return null;
    }

    private static ChatImageDetailLevel? GetImageDetail(AIContent content)
    {
        if (content.AdditionalProperties?.TryGetValue("detail", out object? value) is true)
        {
            return value switch
            {
                string detailString => new ChatImageDetailLevel(detailString),
                ChatImageDetailLevel detail => detail,
                _ => null
            };
        }

        return null;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> FromOpenAIStreamingChatCompletionAsync(
        IAsyncEnumerable<StreamingChatCompletionUpdate> updates,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Dictionary<int, FunctionCallInfo>? functionCallInfos = null;
        ChatRole? streamedRole = null;
        ChatFinishReason? finishReason = null;
        StringBuilder? refusal = null;
        string? responseId = null;
        DateTimeOffset? createdAt = null;
        string? modelId = null;

        // Process each update as it arrives
        await foreach (StreamingChatCompletionUpdate update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            // The role and finish reason may arrive during any update, but once they've arrived, the same value should be the same for all subsequent updates.
            streamedRole ??= update.Role is ChatMessageRole role ? FromOpenAIChatRole(role) : null;
            finishReason ??= update.FinishReason is OpenAI.Chat.ChatFinishReason reason ? FromOpenAIFinishReason(reason) : null;
            responseId ??= update.CompletionId;
            createdAt ??= update.CreatedAt;
            modelId ??= update.Model;

            // Create the response content object.
            ChatResponseUpdate responseUpdate = new()
            {
                ResponseId = update.CompletionId,
                MessageId = update.CompletionId, // There is no per-message ID, but there's only one message per response, so use the response ID
                CreatedAt = update.CreatedAt,
                FinishReason = finishReason,
                ModelId = modelId,
                RawRepresentation = update,
                Role = streamedRole,
            };

            // Transfer over content update items.
            if (update.ContentUpdate is { Count: > 0 })
            {
                foreach (ChatMessageContentPart contentPart in update.ContentUpdate)
                {
                    if (ToAIContent(contentPart) is AIContent aiContent)
                    {
                        responseUpdate.Contents.Add(aiContent);
                    }
                }
            }

            // Transfer over refusal updates.
            if (update.RefusalUpdate is not null)
            {
                _ = (refusal ??= new()).Append(update.RefusalUpdate);
            }

            // Transfer over tool call updates.
            if (update.ToolCallUpdates is { Count: > 0 } toolCallUpdates)
            {
                foreach (StreamingChatToolCallUpdate toolCallUpdate in toolCallUpdates)
                {
                    functionCallInfos ??= [];
                    if (!functionCallInfos.TryGetValue(toolCallUpdate.Index, out FunctionCallInfo? existing))
                    {
                        functionCallInfos[toolCallUpdate.Index] = existing = new();
                    }

                    existing.CallId ??= toolCallUpdate.ToolCallId;
                    existing.Name ??= toolCallUpdate.FunctionName;
                    if (toolCallUpdate.FunctionArgumentsUpdate is { } argUpdate && !argUpdate.ToMemory().IsEmpty)
                    {
                        _ = (existing.Arguments ??= new()).Append(argUpdate.ToString());
                    }
                }
            }

            // Transfer over usage updates.
            if (update.Usage is ChatTokenUsage tokenUsage)
            {
                var usageDetails = FromOpenAIUsage(tokenUsage);
                responseUpdate.Contents.Add(new UsageContent(usageDetails));
            }

            // Now yield the item.
            yield return responseUpdate;
        }

        // Now that we've received all updates, combine any for function calls into a single item to yield.
        if (functionCallInfos is not null)
        {
            ChatResponseUpdate responseUpdate = new()
            {
                ResponseId = responseId,
                MessageId = responseId, // There is no per-message ID, but there's only one message per response, so use the response ID
                CreatedAt = createdAt,
                FinishReason = finishReason,
                ModelId = modelId,
                Role = streamedRole,
            };

            foreach (var entry in functionCallInfos)
            {
                FunctionCallInfo fci = entry.Value;
                if (!string.IsNullOrWhiteSpace(fci.Name))
                {
                    var callContent = ParseCallContentFromJsonString(
                        fci.Arguments?.ToString() ?? string.Empty,
                        fci.CallId!,
                        fci.Name!);
                    responseUpdate.Contents.Add(callContent);
                }
            }

            // Refusals are about the model not following the schema for tool calls. As such, if we have any refusal,
            // add it to this function calling item.
            if (refusal is not null)
            {
                responseUpdate.Contents.Add(new ErrorContent(refusal.ToString()) { ErrorCode = "Refusal" });
            }

            yield return responseUpdate;
        }
    }

    private static ChatResponse FromOpenAIChatCompletion(ChatCompletion openAICompletion, ChatOptions? options, ChatCompletionOptions chatCompletionOptions)
    {
        _ = Throw.IfNull(openAICompletion);

        // Create the return message.
        ChatMessage returnMessage = new()
        {
            MessageId = openAICompletion.Id, // There's no per-message ID, so we use the same value as the response ID
            RawRepresentation = openAICompletion,
            Role = FromOpenAIChatRole(openAICompletion.Role),
        };

        // Populate its content from those in the OpenAI response content.
        foreach (ChatMessageContentPart contentPart in openAICompletion.Content)
        {
            if (ToAIContent(contentPart) is AIContent aiContent)
            {
                returnMessage.Contents.Add(aiContent);
            }
        }

        // Output audio is handled separately from message content parts.
        if (openAICompletion.OutputAudio is ChatOutputAudio audio)
        {
            string mimeType = chatCompletionOptions?.AudioOptions?.OutputAudioFormat.ToString()?.ToLowerInvariant() switch
            {
                "opus" => "audio/opus",
                "aac" => "audio/aac",
                "flac" => "audio/flac",
                "wav" => "audio/wav",
                "pcm" => "audio/pcm",
                "mp3" or _ => "audio/mpeg",
            };

            var dc = new DataContent(audio.AudioBytes.ToMemory(), mimeType);

            returnMessage.Contents.Add(dc);
        }

        // Also manufacture function calling content items from any tool calls in the response.
        if (options?.Tools is { Count: > 0 })
        {
            foreach (ChatToolCall toolCall in openAICompletion.ToolCalls)
            {
                if (!string.IsNullOrWhiteSpace(toolCall.FunctionName))
                {
                    var callContent = ParseCallContentFromBinaryData(toolCall.FunctionArguments, toolCall.Id, toolCall.FunctionName);
                    callContent.RawRepresentation = toolCall;

                    returnMessage.Contents.Add(callContent);
                }
            }
        }

        // And add error content for any refusals, which represent errors in generating output that conforms to a provided schema.
        if (openAICompletion.Refusal is string refusal)
        {
            returnMessage.Contents.Add(new ErrorContent(refusal) { ErrorCode = nameof(openAICompletion.Refusal) });
        }

        // Wrap the content in a ChatResponse to return.
        var response = new ChatResponse(returnMessage)
        {
            CreatedAt = openAICompletion.CreatedAt,
            FinishReason = FromOpenAIFinishReason(openAICompletion.FinishReason),
            ModelId = openAICompletion.Model,
            RawRepresentation = openAICompletion,
            ResponseId = openAICompletion.Id,
        };

        if (openAICompletion.Usage is ChatTokenUsage tokenUsage)
        {
            response.Usage = FromOpenAIUsage(tokenUsage);
        }

        return response;
    }

    /// <summary>Converts an extensions options instance to an OpenAI options instance.</summary>
    private ChatCompletionOptions ToOpenAIOptions(ChatOptions? options)
    {
        if (options is null)
        {
            return new ChatCompletionOptions();
        }

        if (options.RawRepresentationFactory?.Invoke(this) is not ChatCompletionOptions result)
        {
            result = new ChatCompletionOptions();
        }

        result.FrequencyPenalty ??= options.FrequencyPenalty;
        result.MaxOutputTokenCount ??= options.MaxOutputTokens;
        result.TopP ??= options.TopP;
        result.PresencePenalty ??= options.PresencePenalty;
        result.Temperature ??= options.Temperature;
        result.AllowParallelToolCalls ??= options.AllowMultipleToolCalls;
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.
        result.Seed ??= options.Seed;
#pragma warning restore OPENAI001

        if (options.StopSequences is { Count: > 0 } stopSequences)
        {
            foreach (string stopSequence in stopSequences)
            {
                result.StopSequences.Add(stopSequence);
            }
        }

        if (options.Tools is { Count: > 0 } tools)
        {
            foreach (AITool tool in tools)
            {
                if (tool is AIFunction af)
                {
                    result.Tools.Add(ToOpenAIChatTool(af));
                }
            }

            if (result.ToolChoice is null && result.Tools.Count > 0)
            {
                switch (options.ToolMode)
                {
                    case NoneChatToolMode:
                        result.ToolChoice = ChatToolChoice.CreateNoneChoice();
                        break;

                    case AutoChatToolMode:
                    case null:
                        result.ToolChoice = ChatToolChoice.CreateAutoChoice();
                        break;

                    case RequiredChatToolMode required:
                        result.ToolChoice = required.RequiredFunctionName is null ?
                            ChatToolChoice.CreateRequiredChoice() :
                            ChatToolChoice.CreateFunctionChoice(required.RequiredFunctionName);
                        break;
                }
            }
        }

        if (result.ResponseFormat is null)
        {
            if (options.ResponseFormat is ChatResponseFormatText)
            {
                result.ResponseFormat = OpenAI.Chat.ChatResponseFormat.CreateTextFormat();
            }
            else if (options.ResponseFormat is ChatResponseFormatJson jsonFormat)
            {
                result.ResponseFormat = OpenAIClientExtensions.StrictSchemaTransformCache.GetOrCreateTransformedSchema(jsonFormat) is { } jsonSchema ?
                    OpenAI.Chat.ChatResponseFormat.CreateJsonSchemaFormat(
                        jsonFormat.SchemaName ?? "json_schema",
                        BinaryData.FromBytes(
                            JsonSerializer.SerializeToUtf8Bytes(jsonSchema, ChatClientJsonContext.Default.JsonElement)),
                        jsonFormat.SchemaDescription) :
                    OpenAI.Chat.ChatResponseFormat.CreateJsonObjectFormat();
            }
        }

        return result;
    }

    /// <summary>Converts an Extensions function to an OpenAI chat tool.</summary>
    private static ChatTool ToOpenAIChatTool(AIFunction aiFunction)
    {
        bool? strict =
            aiFunction.AdditionalProperties.TryGetValue(OpenAIClientExtensions.StrictKey, out object? strictObj) &&
            strictObj is bool strictValue ?
            strictValue : null;

        // Perform transformations making the schema legal per OpenAI restrictions
        JsonElement jsonSchema = OpenAIClientExtensions.GetSchema(aiFunction, strict);

        // Map to an intermediate model so that redundant properties are skipped.
        var tool = JsonSerializer.Deserialize(jsonSchema, ChatClientJsonContext.Default.ChatToolJson)!;
        var functionParameters = BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(tool, ChatClientJsonContext.Default.ChatToolJson));
        return ChatTool.CreateFunctionTool(aiFunction.Name, aiFunction.Description, functionParameters, strict);
    }

    private static UsageDetails FromOpenAIUsage(ChatTokenUsage tokenUsage)
    {
        var destination = new UsageDetails
        {
            InputTokenCount = tokenUsage.InputTokenCount,
            OutputTokenCount = tokenUsage.OutputTokenCount,
            TotalTokenCount = tokenUsage.TotalTokenCount,
            AdditionalCounts = [],
        };

        var counts = destination.AdditionalCounts;

        if (tokenUsage.InputTokenDetails is ChatInputTokenUsageDetails inputDetails)
        {
            const string InputDetails = nameof(ChatTokenUsage.InputTokenDetails);
            counts.Add($"{InputDetails}.{nameof(ChatInputTokenUsageDetails.AudioTokenCount)}", inputDetails.AudioTokenCount);
            counts.Add($"{InputDetails}.{nameof(ChatInputTokenUsageDetails.CachedTokenCount)}", inputDetails.CachedTokenCount);
        }

        if (tokenUsage.OutputTokenDetails is ChatOutputTokenUsageDetails outputDetails)
        {
            const string OutputDetails = nameof(ChatTokenUsage.OutputTokenDetails);
            counts.Add($"{OutputDetails}.{nameof(ChatOutputTokenUsageDetails.ReasoningTokenCount)}", outputDetails.ReasoningTokenCount);
            counts.Add($"{OutputDetails}.{nameof(ChatOutputTokenUsageDetails.AudioTokenCount)}", outputDetails.AudioTokenCount);
            counts.Add($"{OutputDetails}.{nameof(ChatOutputTokenUsageDetails.AcceptedPredictionTokenCount)}", outputDetails.AcceptedPredictionTokenCount);
            counts.Add($"{OutputDetails}.{nameof(ChatOutputTokenUsageDetails.RejectedPredictionTokenCount)}", outputDetails.RejectedPredictionTokenCount);
        }

        return destination;
    }

    /// <summary>Converts an OpenAI role to an Extensions role.</summary>
    private static ChatRole FromOpenAIChatRole(ChatMessageRole role) =>
        role switch
        {
            ChatMessageRole.System => ChatRole.System,
            ChatMessageRole.User => ChatRole.User,
            ChatMessageRole.Assistant => ChatRole.Assistant,
            ChatMessageRole.Tool => ChatRole.Tool,
            ChatMessageRole.Developer => OpenAIClientExtensions.ChatRoleDeveloper,
            _ => new ChatRole(role.ToString()),
        };

    /// <summary>Creates an <see cref="AIContent"/> from a <see cref="ChatMessageContentPart"/>.</summary>
    /// <param name="contentPart">The content part to convert into a content.</param>
    /// <returns>The constructed <see cref="AIContent"/>, or <see langword="null"/> if the content part could not be converted.</returns>
    private static AIContent? ToAIContent(ChatMessageContentPart contentPart)
    {
        AIContent? aiContent = null;

        if (contentPart.Kind == ChatMessageContentPartKind.Text)
        {
            aiContent = new TextContent(contentPart.Text);
        }
        else if (contentPart.Kind == ChatMessageContentPartKind.Image)
        {
            aiContent =
                contentPart.ImageUri is not null ? new UriContent(contentPart.ImageUri, "image/*") :
                contentPart.ImageBytes is not null ? new DataContent(contentPart.ImageBytes.ToMemory(), contentPart.ImageBytesMediaType) :
                null;

            if (aiContent is not null && contentPart.ImageDetailLevel?.ToString() is string detail)
            {
                (aiContent.AdditionalProperties ??= [])[nameof(contentPart.ImageDetailLevel)] = detail;
            }
        }

        if (aiContent is not null)
        {
            if (contentPart.Refusal is string refusal)
            {
                (aiContent.AdditionalProperties ??= [])[nameof(contentPart.Refusal)] = refusal;
            }

            aiContent.RawRepresentation = contentPart;
        }

        return aiContent;
    }

    /// <summary>Converts an OpenAI finish reason to an Extensions finish reason.</summary>
    private static ChatFinishReason? FromOpenAIFinishReason(OpenAI.Chat.ChatFinishReason? finishReason) =>
        finishReason?.ToString() is not string s ? null :
        finishReason switch
        {
            OpenAI.Chat.ChatFinishReason.Stop => ChatFinishReason.Stop,
            OpenAI.Chat.ChatFinishReason.Length => ChatFinishReason.Length,
            OpenAI.Chat.ChatFinishReason.ContentFilter => ChatFinishReason.ContentFilter,
            OpenAI.Chat.ChatFinishReason.ToolCalls or OpenAI.Chat.ChatFinishReason.FunctionCall => ChatFinishReason.ToolCalls,
            _ => new ChatFinishReason(s),
        };

    private static FunctionCallContent ParseCallContentFromJsonString(string json, string callId, string name) =>
        FunctionCallContent.CreateFromParsedArguments(json, callId, name,
            argumentParser: static json => JsonSerializer.Deserialize(json, ChatClientJsonContext.Default.IDictionaryStringObject)!);

    private static FunctionCallContent ParseCallContentFromBinaryData(BinaryData ut8Json, string callId, string name) =>
        FunctionCallContent.CreateFromParsedArguments(ut8Json, callId, name,
            argumentParser: static json => JsonSerializer.Deserialize(json, ChatClientJsonContext.Default.IDictionaryStringObject)!);

    /// <summary>Used to create the JSON payload for an OpenAI chat tool description.</summary>
    private sealed class ChatToolJson
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "object";

        [JsonPropertyName("required")]
        public HashSet<string> Required { get; set; } = [];

        [JsonPropertyName("properties")]
        public Dictionary<string, JsonElement> Properties { get; set; } = [];

        [JsonPropertyName("additionalProperties")]
        public bool AdditionalProperties { get; set; }
    }

    /// <summary>POCO representing function calling info. Used to concatenation information for a single function call from across multiple streaming updates.</summary>
    private sealed class FunctionCallInfo
    {
        public string? CallId;
        public string? Name;
        public StringBuilder? Arguments;
    }

    /// <summary>Source-generated JSON type information.</summary>
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web,
        UseStringEnumConverter = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true)]
    [JsonSerializable(typeof(ChatToolJson))]
    [JsonSerializable(typeof(IDictionary<string, object?>))]
    [JsonSerializable(typeof(string[]))]
    private sealed partial class ChatClientJsonContext : JsonSerializerContext;
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.OpenAI/OpenAIClientExtensions.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using OpenAI;
using OpenAI.Assistants;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Embeddings;
using OpenAI.Responses;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for working with <see cref="OpenAIClient"/>s.</summary>
public static class OpenAIClientExtensions
{
    /// <summary>Key into AdditionalProperties used to store a strict option.</summary>
    internal const string StrictKey = "strictJsonSchema";

    /// <summary>Gets the default OpenAI endpoint.</summary>
    internal static Uri DefaultOpenAIEndpoint { get; } = new("https://api.openai.com/v1");

    /// <summary>Gets a <see cref="ChatRole"/> for "developer".</summary>
    internal static ChatRole ChatRoleDeveloper { get; } = new ChatRole("developer");

    /// <summary>
    /// Gets the JSON schema transformer cache conforming to OpenAI <b>strict</b> restrictions per https://platform.openai.com/docs/guides/structured-outputs?api-mode=responses#supported-schemas.
    /// </summary>
    internal static AIJsonSchemaTransformCache StrictSchemaTransformCache { get; } = new(new()
    {
        DisallowAdditionalProperties = true,
        ConvertBooleanSchemas = true,
        MoveDefaultKeywordToDescription = true,
        RequireAllProperties = true,
    });

    /// <summary>Gets an <see cref="IChatClient"/> for use with this <see cref="ChatClient"/>.</summary>
    /// <param name="chatClient">The client.</param>
    /// <returns>An <see cref="IChatClient"/> that can be used to converse via the <see cref="ChatClient"/>.</returns>
    public static IChatClient AsIChatClient(this ChatClient chatClient) =>
        new OpenAIChatClient(chatClient);

    /// <summary>Gets an <see cref="IChatClient"/> for use with this <see cref="OpenAIResponseClient"/>.</summary>
    /// <param name="responseClient">The client.</param>
    /// <returns>An <see cref="IChatClient"/> that can be used to converse via the <see cref="OpenAIResponseClient"/>.</returns>
    public static IChatClient AsIChatClient(this OpenAIResponseClient responseClient) =>
        new OpenAIResponseChatClient(responseClient);

    /// <summary>Gets an <see cref="IChatClient"/> for use with this <see cref="AssistantClient"/>.</summary>
    /// <param name="assistantClient">The <see cref="AssistantClient"/> instance to be accessed as an <see cref="IChatClient"/>.</param>
    /// <param name="assistantId">The unique identifier of the assistant with which to interact.</param>
    /// <param name="threadId">
    /// An optional existing thread identifier for the chat session. This serves as a default, and may be overridden per call to
    /// <see cref="IChatClient.GetResponseAsync"/> or <see cref="IChatClient.GetStreamingResponseAsync"/> via the <see cref="ChatOptions.ConversationId"/>
    /// property. If no thread ID is provided via either mechanism, a new thread will be created for the request.
    /// </param>
    /// <returns>An <see cref="IChatClient"/> instance configured to interact with the specified agent and thread.</returns>
    [Experimental("OPENAI001")]
    public static IChatClient AsIChatClient(this AssistantClient assistantClient, string assistantId, string? threadId = null) =>
        new OpenAIAssistantChatClient(assistantClient, assistantId, threadId);

    /// <summary>Gets an <see cref="ISpeechToTextClient"/> for use with this <see cref="AudioClient"/>.</summary>
    /// <param name="audioClient">The client.</param>
    /// <returns>An <see cref="ISpeechToTextClient"/> that can be used to transcribe audio via the <see cref="AudioClient"/>.</returns>
    [Experimental("MEAI001")]
    public static ISpeechToTextClient AsISpeechToTextClient(this AudioClient audioClient) =>
        new OpenAISpeechToTextClient(audioClient);

    /// <summary>Gets an <see cref="IEmbeddingGenerator{String, Single}"/> for use with this <see cref="EmbeddingClient"/>.</summary>
    /// <param name="embeddingClient">The client.</param>
    /// <param name="defaultModelDimensions">The number of dimensions to generate in each embedding.</param>
    /// <returns>An <see cref="IEmbeddingGenerator{String, Embedding}"/> that can be used to generate embeddings via the <see cref="EmbeddingClient"/>.</returns>
    public static IEmbeddingGenerator<string, Embedding<float>> AsIEmbeddingGenerator(this EmbeddingClient embeddingClient, int? defaultModelDimensions = null) =>
        new OpenAIEmbeddingGenerator(embeddingClient, defaultModelDimensions);

    /// <summary>Gets the JSON schema to use from the function.</summary>
    internal static JsonElement GetSchema(AIFunction function, bool? strict) =>
        strict is true ?
            StrictSchemaTransformCache.GetOrCreateTransformedSchema(function) :
            function.JsonSchema;
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.OpenAI/OpenAIEmbeddingGenerator.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using OpenAI;
using OpenAI.Embeddings;

#pragma warning disable S1067 // Expressions should not be too complex
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

namespace Microsoft.Extensions.AI;

/// <summary>An <see cref="IEmbeddingGenerator{String, Embedding}"/> for an OpenAI <see cref="EmbeddingClient"/>.</summary>
internal sealed class OpenAIEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    /// <summary>Default OpenAI endpoint.</summary>
    private const string DefaultOpenAIEndpoint = "https://api.openai.com/v1";

    /// <summary>Metadata about the embedding generator.</summary>
    private readonly EmbeddingGeneratorMetadata _metadata;

    /// <summary>The underlying <see cref="OpenAI.Chat.ChatClient" />.</summary>
    private readonly EmbeddingClient _embeddingClient;

    /// <summary>The number of dimensions produced by the generator.</summary>
    private readonly int? _dimensions;

    /// <summary>Initializes a new instance of the <see cref="OpenAIEmbeddingGenerator"/> class.</summary>
    /// <param name="embeddingClient">The underlying client.</param>
    /// <param name="defaultModelDimensions">The number of dimensions to generate in each embedding.</param>
    /// <exception cref="ArgumentNullException"><paramref name="embeddingClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="defaultModelDimensions"/> is not positive.</exception>
    public OpenAIEmbeddingGenerator(EmbeddingClient embeddingClient, int? defaultModelDimensions = null)
    {
        _ = Throw.IfNull(embeddingClient);
        if (defaultModelDimensions < 1)
        {
            Throw.ArgumentOutOfRangeException(nameof(defaultModelDimensions), "Value must be greater than 0.");
        }

        _embeddingClient = embeddingClient;
        _dimensions = defaultModelDimensions;

        // https://github.com/openai/openai-dotnet/issues/215
        // The endpoint and model aren't currently exposed, so use reflection to get at them, temporarily. Once packages
        // implement the abstractions directly rather than providing adapters on top of the public APIs,
        // the package can provide such implementations separate from what's exposed in the public API.
        string providerUrl = (typeof(EmbeddingClient).GetField("_endpoint", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(embeddingClient) as Uri)?.ToString() ??
            DefaultOpenAIEndpoint;

        FieldInfo? modelField = typeof(EmbeddingClient).GetField("_model", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        string? modelId = modelField?.GetValue(embeddingClient) as string;

        _metadata = CreateMetadata("openai", providerUrl, modelId, defaultModelDimensions);
    }

    /// <inheritdoc />
    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        OpenAI.Embeddings.EmbeddingGenerationOptions? openAIOptions = ToOpenAIOptions(options);

        var embeddings = (await _embeddingClient.GenerateEmbeddingsAsync(values, openAIOptions, cancellationToken).ConfigureAwait(false)).Value;

        return new(embeddings.Select(e =>
                new Embedding<float>(e.ToFloats())
                {
                    CreatedAt = DateTimeOffset.UtcNow,
                    ModelId = embeddings.Model,
                }))
        {
            Usage = new()
            {
                InputTokenCount = embeddings.Usage.InputTokenCount,
                TotalTokenCount = embeddings.Usage.TotalTokenCount
            },
        };
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the IEmbeddingGenerator interface.
    }

    /// <inheritdoc />
    object? IEmbeddingGenerator.GetService(Type serviceType, object? serviceKey)
    {
        _ = Throw.IfNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(EmbeddingGeneratorMetadata) ? _metadata :
            serviceType == typeof(EmbeddingClient) ? _embeddingClient :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <summary>Creates the <see cref="EmbeddingGeneratorMetadata"/> for this instance.</summary>
    private static EmbeddingGeneratorMetadata CreateMetadata(string providerName, string providerUrl, string? defaultModelId, int? defaultModelDimensions) =>
        new(providerName, Uri.TryCreate(providerUrl, UriKind.Absolute, out Uri? providerUri) ? providerUri : null, defaultModelId, defaultModelDimensions);

    /// <summary>Converts an extensions options instance to an OpenAI options instance.</summary>
    private OpenAI.Embeddings.EmbeddingGenerationOptions ToOpenAIOptions(EmbeddingGenerationOptions? options)
    {
        if (options?.RawRepresentationFactory?.Invoke(this) is not OpenAI.Embeddings.EmbeddingGenerationOptions result)
        {
            result = new OpenAI.Embeddings.EmbeddingGenerationOptions();
        }

        result.Dimensions ??= options?.Dimensions ?? _dimensions;
        return result;
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.OpenAI/OpenAIResponseChatClient.cs
================================================
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using OpenAI.Responses;

#pragma warning disable S907 // "goto" statement should not be used
#pragma warning disable S1067 // Expressions should not be too complex
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
#pragma warning disable S3604 // Member initializer values should not be redundant
#pragma warning disable SA1204 // Static elements should appear before instance elements

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="IChatClient"/> for an <see cref="OpenAIResponseClient"/>.</summary>
internal sealed partial class OpenAIResponseChatClient : IChatClient
{
    /// <summary>Metadata about the client.</summary>
    private readonly ChatClientMetadata _metadata;

    /// <summary>The underlying <see cref="OpenAIResponseClient" />.</summary>
    private readonly OpenAIResponseClient _responseClient;

    /// <summary>Initializes a new instance of the <see cref="OpenAIResponseChatClient"/> class for the specified <see cref="OpenAIResponseClient"/>.</summary>
    /// <param name="responseClient">The underlying client.</param>
    /// <exception cref="ArgumentNullException"><paramref name="responseClient"/> is <see langword="null"/>.</exception>
    public OpenAIResponseChatClient(OpenAIResponseClient responseClient)
    {
        _ = Throw.IfNull(responseClient);

        _responseClient = responseClient;

        // https://github.com/openai/openai-dotnet/issues/215
        // The endpoint and model aren't currently exposed, so use reflection to get at them, temporarily. Once packages
        // implement the abstractions directly rather than providing adapters on top of the public APIs,
        // the package can provide such implementations separate from what's exposed in the public API.
        Uri providerUrl = typeof(OpenAIResponseClient).GetField("_endpoint", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(responseClient) as Uri ?? OpenAIClientExtensions.DefaultOpenAIEndpoint;
        string? model = typeof(OpenAIResponseClient).GetField("_model", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(responseClient) as string;

        _metadata = new("openai", providerUrl, model);
    }

    /// <inheritdoc />
    object? IChatClient.GetService(Type serviceType, object? serviceKey)
    {
        _ = Throw.IfNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(ChatClientMetadata) ? _metadata :
            serviceType == typeof(OpenAIResponseClient) ? _responseClient :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        // Convert the inputs into what OpenAIResponseClient expects.
        var openAIResponseItems = ToOpenAIResponseItems(messages);
        var openAIOptions = ToOpenAIResponseCreationOptions(options);

        // Make the call to the OpenAIResponseClient.
        var openAIResponse = (await _responseClient.CreateResponseAsync(openAIResponseItems, openAIOptions, cancellationToken).ConfigureAwait(false)).Value;

        // Convert and return the results.
        ChatResponse response = new()
        {
            ConversationId = openAIOptions.StoredOutputEnabled is false ? null : openAIResponse.Id,
            CreatedAt = openAIResponse.CreatedAt,
            FinishReason = ToFinishReason(openAIResponse.IncompleteStatusDetails?.Reason),
            Messages = [new(ChatRole.Assistant, [])],
            ModelId = openAIResponse.Model,
            RawRepresentation = openAIResponse,
            ResponseId = openAIResponse.Id,
            Usage = ToUsageDetails(openAIResponse),
        };

        if (!string.IsNullOrEmpty(openAIResponse.EndUserId))
        {
            (response.AdditionalProperties ??= [])[nameof(openAIResponse.EndUserId)] = openAIResponse.EndUserId;
        }

        if (openAIResponse.Error is not null)
        {
            (response.AdditionalProperties ??= [])[nameof(openAIResponse.Error)] = openAIResponse.Error;
        }

        if (openAIResponse.OutputItems is not null)
        {
            ChatMessage message = response.Messages[0];
            Debug.Assert(message.Contents is List<AIContent>, "Expected a List<AIContent> for message contents.");

            foreach (ResponseItem outputItem in openAIResponse.OutputItems)
            {
                switch (outputItem)
                {
                    case MessageResponseItem messageItem:
                        message.MessageId = messageItem.Id;
                        message.RawRepresentation = messageItem;
                        message.Role = ToChatRole(messageItem.Role);
                        (message.AdditionalProperties ??= []).Add(nameof(messageItem.Id), messageItem.Id);
                        ((List<AIContent>)message.Contents).AddRange(ToAIContents(messageItem.Content));
                        break;

                    case FunctionCallResponseItem functionCall:
                        response.FinishReason ??= ChatFinishReason.ToolCalls;
                        var fcc = FunctionCallContent.CreateFromParsedArguments(
                            functionCall.FunctionArguments.ToMemory(),
                            functionCall.CallId,
                            functionCall.FunctionName,
                            static json => JsonSerializer.Deserialize(json.Span, ResponseClientJsonContext.Default.IDictionaryStringObject)!);
                        fcc.RawRepresentation = outputItem;
                        message.Contents.Add(fcc);
                        break;

                    default:
                        message.Contents.Add(new()
                        {
                            RawRepresentation = outputItem,
                        });
                        break;
                }
            }

            if (openAIResponse.Error is { } error)
            {
                message.Contents.Add(new ErrorContent(error.Message) { ErrorCode = error.Code });
            }
        }

        return response;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        // Convert the inputs into what OpenAIResponseClient expects.
        var openAIResponseItems = ToOpenAIResponseItems(messages);
        var openAIOptions = ToOpenAIResponseCreationOptions(options);

        // Make the call to the OpenAIResponseClient and process the streaming results.
        DateTimeOffset? createdAt = null;
        string? responseId = null;
        string? conversationId = null;
        string? modelId = null;
        string? lastMessageId = null;
        ChatRole? lastRole = null;
        Dictionary<int, MessageResponseItem> outputIndexToMessages = [];
        Dictionary<int, FunctionCallInfo>? functionCallInfos = null;
        await foreach (var streamingUpdate in _responseClient.CreateResponseStreamingAsync(openAIResponseItems, openAIOptions, cancellationToken).ConfigureAwait(false))
        {
            switch (streamingUpdate)
            {
                case StreamingResponseCreatedUpdate createdUpdate:
                    createdAt = createdUpdate.Response.CreatedAt;
                    responseId = createdUpdate.Response.Id;
                    conversationId = openAIOptions.StoredOutputEnabled is false ? null : responseId;
                    modelId = createdUpdate.Response.Model;
                    goto default;

                case StreamingResponseCompletedUpdate completedUpdate:
                    yield return new()
                    {
                        Contents = ToUsageDetails(completedUpdate.Response) is { } usage ? [new UsageContent(usage)] : [],
                        ConversationId = conversationId,
                        CreatedAt = createdAt,
                        FinishReason =
                            ToFinishReason(completedUpdate.Response?.IncompleteStatusDetails?.Reason) ??
                            (functionCallInfos is not null ? ChatFinishReason.ToolCalls : ChatFinishReason.Stop),
                        MessageId = lastMessageId,
                        ModelId = modelId,
                        RawRepresentation = streamingUpdate,
                        ResponseId = responseId,
                        Role = lastRole,
                    };
                    break;

                case StreamingResponseOutputItemAddedUpdate outputItemAddedUpdate:
                    switch (outputItemAddedUpdate.Item)
                    {
                        case MessageResponseItem mri:
                            outputIndexToMessages[outputItemAddedUpdate.OutputIndex] = mri;
                            break;

                        case FunctionCallResponseItem fcri:
                            (functionCallInfos ??= [])[outputItemAddedUpdate.OutputIndex] = new(fcri);
                            break;
                    }

                    goto default;

                case StreamingResponseOutputItemDoneUpdate outputItemDoneUpdate:
                    _ = outputIndexToMessages.Remove(outputItemDoneUpdate.OutputIndex);
                    goto default;

                case StreamingResponseOutputTextDeltaUpdate outputTextDeltaUpdate:
                    _ = outputIndexToMessages.TryGetValue(outputTextDeltaUpdate.OutputIndex, out MessageResponseItem? messageItem);
                    lastMessageId = messageItem?.Id;
                    lastRole = ToChatRole(messageItem?.Role);
                    yield return new ChatResponseUpdate(lastRole, outputTextDeltaUpdate.Delta)
                    {
                        ConversationId = conversationId,
                        CreatedAt = createdAt,
                        MessageId = lastMessageId,
                        ModelId = modelId,
                        RawRepresentation = streamingUpdate,
                        ResponseId = responseId,
                    };
                    break;

                case StreamingResponseFunctionCallArgumentsDeltaUpdate functionCallArgumentsDeltaUpdate:
                {
                    if (functionCallInfos?.TryGetValue(functionCallArgumentsDeltaUpdate.OutputIndex, out FunctionCallInfo? callInfo) is true)
                    {
                        _ = (callInfo.Arguments ??= new()).Append(functionCallArgumentsDeltaUpdate.Delta);
                    }

                    goto default;
                }

                case StreamingResponseFunctionCallArgumentsDoneUpdate functionCallOutputDoneUpdate:
                {
                    if (functionCallInfos?.TryGetValue(functionCallOutputDoneUpdate.OutputIndex, out FunctionCallInfo? callInfo) is true)
                    {
                        _ = functionCallInfos.Remove(functionCallOutputDoneUpdate.OutputIndex);

                        var fci = FunctionCallContent.CreateFromParsedArguments(
                            callInfo.Arguments?.ToString() ?? string.Empty,
                            callInfo.ResponseItem.CallId,
                            callInfo.ResponseItem.FunctionName,
                            static json => JsonSerializer.Deserialize(json, ResponseClientJsonContext.Default.IDictionaryStringObject)!);

                        lastMessageId = callInfo.ResponseItem.Id;
                        lastRole = ChatRole.Assistant;
                        yield return new ChatResponseUpdate(lastRole, [fci])
                        {
                            ConversationId = conversationId,
                            CreatedAt = createdAt,
                            MessageId = lastMessageId,
                            ModelId = modelId,
                            RawRepresentation = streamingUpdate,
                            ResponseId = responseId,
                        };

                        break;
                    }

                    goto default;
                }

                case StreamingResponseErrorUpdate errorUpdate:
                    yield return new ChatResponseUpdate
                    {
                        Contents =
                        [
                            new ErrorContent(errorUpdate.Message)
                            {
                                ErrorCode = errorUpdate.Code,
                                Details = errorUpdate.Param,
                            }
                        ],
                        ConversationId = conversationId,
                        CreatedAt = createdAt,
                        MessageId = lastMessageId,
                        ModelId = modelId,
                        RawRepresentation = streamingUpdate,
                        ResponseId = responseId,
                        Role = lastRole,
                    };
                    break;

                case StreamingResponseRefusalDoneUpdate refusalDone:
                    yield return new ChatResponseUpdate
                    {
                        Contents = [new ErrorContent(refusalDone.Refusal) { ErrorCode = nameof(ResponseContentPart.Refusal) }],
                        ConversationId = conversationId,
                        CreatedAt = createdAt,
                        MessageId = lastMessageId,
                        ModelId = modelId,
                        RawRepresentation = streamingUpdate,
                        ResponseId = responseId,
                        Role = lastRole,
                    };
                    break;

                default:
                    yield return new ChatResponseUpdate
                    {
                        ConversationId = conversationId,
                        CreatedAt = createdAt,
                        MessageId = lastMessageId,
                        ModelId = modelId,
                        RawRepresentation = streamingUpdate,
                        ResponseId = responseId,
                        Role = lastRole,
                    };
                    break;
            }
        }
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the IChatClient interface.
    }

    /// <summary>Creates a <see cref="ChatRole"/> from a <see cref="MessageRole"/>.</summary>
    private static ChatRole ToChatRole(MessageRole? role) =>
        role switch
        {
            MessageRole.System => ChatRole.System,
            MessageRole.Developer => OpenAIClientExtensions.ChatRoleDeveloper,
            MessageRole.User => ChatRole.User,
            _ => ChatRole.Assistant,
        };

    /// <summary>Creates a <see cref="ChatFinishReason"/> from a <see cref="ResponseIncompleteStatusReason"/>.</summary>
    private static ChatFinishReason? ToFinishReason(ResponseIncompleteStatusReason? statusReason) =>
        statusReason == ResponseIncompleteStatusReason.ContentFilter ? ChatFinishReason.ContentFilter :
        statusReason == ResponseIncompleteStatusReason.MaxOutputTokens ? ChatFinishReason.Length :
        null;

    /// <summary>Converts a <see cref="ChatOptions"/> to a <see cref="ResponseCreationOptions"/>.</summary>
    private ResponseCreationOptions ToOpenAIResponseCreationOptions(ChatOptions? options)
    {
        if (options is null)
        {
            return new ResponseCreationOptions();
        }

        if (options.RawRepresentationFactory?.Invoke(this) is not ResponseCreationOptions result)
        {
            result = new ResponseCreationOptions();
        }

        // Handle strongly-typed properties.
        result.MaxOutputTokenCount ??= options.MaxOutputTokens;
        result.PreviousResponseId ??= options.ConversationId;
        result.TopP ??= options.TopP;
        result.Temperature ??= options.Temperature;
        result.ParallelToolCallsEnabled ??= options.AllowMultipleToolCalls;
        if (options.Instructions is { } instructions)
        {
            result.Instructions = string.IsNullOrEmpty(result.Instructions) ?
                instructions :
                $"{result.Instructions}{Environment.NewLine}{instructions}";
        }

        // Populate tools if there are any.
        if (options.Tools is { Count: > 0 } tools)
        {
            foreach (AITool tool in tools)
            {
                switch (tool)
                {
                    case AIFunction aiFunction:
                        bool strict =
                            aiFunction.AdditionalProperties.TryGetValue(OpenAIClientExtensions.StrictKey, out object? strictObj) &&
                            strictObj is bool strictValue &&
                            strictValue;

                        JsonElement jsonSchema = OpenAIClientExtensions.GetSchema(aiFunction, strict);

                        var oaitool = JsonSerializer.Deserialize(jsonSchema, ResponseClientJsonContext.Default.ResponseToolJson)!;
                        var functionParameters = BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(oaitool, ResponseClientJsonContext.Default.ResponseToolJson));
                        result.Tools.Add(ResponseTool.CreateFunctionTool(aiFunction.Name, aiFunction.Description, functionParameters, strict));
                        break;

                    case HostedWebSearchTool:
                        WebSearchToolLocation? location = null;
                        if (tool.AdditionalProperties.TryGetValue(nameof(WebSearchToolLocation), out object? objLocation))
                        {
                            location = objLocation as WebSearchToolLocation;
                        }

                        WebSearchToolContextSize? size = null;
                        if (tool.AdditionalProperties.TryGetValue(nameof(WebSearchToolContextSize), out object? objSize) &&
                            objSize is WebSearchToolContextSize)
                        {
                            size = (WebSearchToolContextSize)objSize;
                        }

                        result.Tools.Add(ResponseTool.CreateWebSearchTool(location, size));
                        break;
                }
            }

            if (result.ToolChoice is null && result.Tools.Count > 0)
            {
                switch (options.ToolMode)
                {
                    case NoneChatToolMode:
                        result.ToolChoice = ResponseToolChoice.CreateNoneChoice();
                        break;

                    case AutoChatToolMode:
                    case null:
                        result.ToolChoice = ResponseToolChoice.CreateAutoChoice();
                        break;

                    case RequiredChatToolMode required:
                        result.ToolChoice = required.RequiredFunctionName is not null ?
                            ResponseToolChoice.CreateFunctionChoice(required.RequiredFunctionName) :
                            ResponseToolChoice.CreateRequiredChoice();
                        break;
                }
            }
        }

        if (result.TextOptions is null)
        {
            if (options.ResponseFormat is ChatResponseFormatText)
            {
                result.TextOptions = new()
                {
                    TextFormat = ResponseTextFormat.CreateTextFormat()
                };
            }
            else if (options.ResponseFormat is ChatResponseFormatJson jsonFormat)
            {
                result.TextOptions = new()
                {
                    TextFormat = OpenAIClientExtensions.StrictSchemaTransformCache.GetOrCreateTransformedSchema(jsonFormat) is { } jsonSchema ?
                        ResponseTextFormat.CreateJsonSchemaFormat(
                            jsonFormat.SchemaName ?? "json_schema",
                            BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(jsonSchema, ResponseClientJsonContext.Default.JsonElement)),
                            jsonFormat.SchemaDescription) :
                        ResponseTextFormat.CreateJsonObjectFormat(),
                };
            }
        }

        return result;
    }

    /// <summary>Convert a sequence of <see cref="ChatMessage"/>s to <see cref="ResponseItem"/>s.</summary>
    private static IEnumerable<ResponseItem> ToOpenAIResponseItems(
        IEnumerable<ChatMessage> inputs)
    {
        foreach (ChatMessage input in inputs)
        {
            if (input.Role == ChatRole.System ||
                input.Role == OpenAIClientExtensions.ChatRoleDeveloper)
            {
                string text = input.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    yield return input.Role == ChatRole.System ?
                        ResponseItem.CreateSystemMessageItem(text) :
                        ResponseItem.CreateDeveloperMessageItem(text);
                }

                continue;
            }

            if (input.Role == ChatRole.User)
            {
                yield return ResponseItem.CreateUserMessageItem(ToOpenAIResponsesContent(input.Contents));
                continue;
            }

            if (input.Role == ChatRole.Tool)
            {
                foreach (AIContent item in input.Contents)
                {
                    switch (item)
                    {
                        case FunctionResultContent resultContent:
                            string? result = resultContent.Result as string;
                            if (result is null && resultContent.Result is not null)
                            {
                                try
                                {
                                    result = JsonSerializer.Serialize(resultContent.Result, AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(object)));
                                }
                                catch (NotSupportedException)
                                {
                                    // If the type can't be serialized, skip it.
                                }
                            }

                            yield return ResponseItem.CreateFunctionCallOutputItem(resultContent.CallId, result ?? string.Empty);
                            break;
                    }
                }

                continue;
            }

            if (input.Role == ChatRole.Assistant)
            {
                foreach (AIContent item in input.Contents)
                {
                    switch (item)
                    {
                        case TextContent textContent:
                            yield return ResponseItem.CreateAssistantMessageItem(textContent.Text);
                            break;

                        case FunctionCallContent callContent:
                            yield return ResponseItem.CreateFunctionCallItem(
                                callContent.CallId,
                                callContent.Name,
                                BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(
                                    callContent.Arguments,
                                    AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(IDictionary<string, object?>)))));
                            break;

                        case AIContent when item.RawRepresentation is ResponseItem rawRep:
                            yield return rawRep;
                            break;
                    }
                }

                continue;
            }
        }
    }

    /// <summary>Extract usage details from an <see cref="OpenAIResponse"/>.</summary>
    private static UsageDetails? ToUsageDetails(OpenAIResponse? openAIResponse)
    {
        UsageDetails? ud = null;
        if (openAIResponse?.Usage is { } usage)
        {
            ud = new()
            {
                InputTokenCount = usage.InputTokenCount,
                OutputTokenCount = usage.OutputTokenCount,
                TotalTokenCount = usage.TotalTokenCount,
            };

            if (usage.OutputTokenDetails is { } outputDetails)
            {
                ud.AdditionalCounts ??= [];

                const string OutputDetails = nameof(usage.OutputTokenDetails);
                ud.AdditionalCounts.Add($"{OutputDetails}.{nameof(outputDetails.ReasoningTokenCount)}", outputDetails.ReasoningTokenCount);
            }
        }

        return ud;
    }

    /// <summary>Convert a sequence of <see cref="ResponseContentPart"/>s to a list of <see cref="AIContent"/>.</summary>
    private static List<AIContent> ToAIContents(IEnumerable<ResponseContentPart> contents)
    {
        List<AIContent> results = [];

        foreach (ResponseContentPart part in contents)
        {
            switch (part.Kind)
            {
                case ResponseContentPartKind.OutputText:
                    results.Add(new TextContent(part.Text)
                    {
                        RawRepresentation = part,
                    });
                    break;

                case ResponseContentPartKind.Refusal:
                    results.Add(new ErrorContent(part.Refusal)
                    {
                        ErrorCode = nameof(ResponseContentPartKind.Refusal),
                        RawRepresentation = part,
                    });
                    break;

                default:
                    results.Add(new()
                    {
                        RawRepresentation = part,
                    });
                    break;
            }
        }

        return results;
    }

    /// <summary>Convert a list of <see cref="AIContent"/>s to a list of <see cref="ResponseContentPart"/>.</summary>
    private static List<ResponseContentPart> ToOpenAIResponsesContent(IList<AIContent> contents)
    {
        List<ResponseContentPart> parts = [];
        foreach (var content in contents)
        {
            switch (content)
            {
                case TextContent textContent:
                    parts.Add(ResponseContentPart.CreateInputTextPart(textContent.Text));
                    break;

                case UriContent uriContent when uriContent.HasTopLevelMediaType("image"):
                    parts.Add(ResponseContentPart.CreateInputImagePart(uriContent.Uri));
                    break;

                case DataContent dataContent when dataContent.HasTopLevelMediaType("image"):
                    parts.Add(ResponseContentPart.CreateInputImagePart(BinaryData.FromBytes(dataContent.Data), dataContent.MediaType));
                    break;

                case DataContent dataContent when dataContent.MediaType.StartsWith("application/pdf", StringComparison.OrdinalIgnoreCase):
                    parts.Add(ResponseContentPart.CreateInputFilePart(null, $"{Guid.NewGuid():N}.pdf",
                        BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(dataContent.Uri, ResponseClientJsonContext.Default.String))));
                    break;

                case ErrorContent errorContent when errorContent.ErrorCode == nameof(ResponseContentPartKind.Refusal):
                    parts.Add(ResponseContentPart.CreateRefusalPart(errorContent.Message));
                    break;

                case AIContent when content.RawRepresentation is ResponseContentPart rawRep:
                    parts.Add(rawRep);
                    break;
            }
        }

        if (parts.Count == 0)
        {
            parts.Add(ResponseContentPart.CreateInputTextPart(string.Empty));
        }

        return parts;
    }

    /// <summary>Used to create the JSON payload for an OpenAI chat tool description.</summary>
    private sealed class ResponseToolJson
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "object";

        [JsonPropertyName("required")]
        public HashSet<string> Required { get; set; } = [];

        [JsonPropertyName("properties")]
        public Dictionary<string, JsonElement> Properties { get; set; } = [];

        [JsonPropertyName("additionalProperties")]
        public bool AdditionalProperties { get; set; }
    }

    /// <summary>POCO representing function calling info.</summary>
    /// <remarks>Used to concatenation information for a single function call from across multiple streaming updates.</remarks>
    private sealed class FunctionCallInfo(FunctionCallResponseItem item)
    {
        public readonly FunctionCallResponseItem ResponseItem = item;
        public StringBuilder? Arguments;
    }

    /// <summary>Source-generated JSON type information.</summary>
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web,
        UseStringEnumConverter = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true)]
    [JsonSerializable(typeof(ResponseToolJson))]
    [JsonSerializable(typeof(JsonElement))]
    [JsonSerializable(typeof(IDictionary<string, object?>))]
    [JsonSerializable(typeof(string[]))]
    private sealed partial class ResponseClientJsonContext : JsonSerializerContext;
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.OpenAI/OpenAISpeechToTextClient.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using OpenAI;
using OpenAI.Audio;

#pragma warning disable S1067 // Expressions should not be too complex
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
#pragma warning disable SA1204 // Static elements should appear before instance elements

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="ISpeechToTextClient"/> for an OpenAI <see cref="OpenAIClient"/> or <see cref="OpenAI.Audio.AudioClient"/>.</summary>
[Experimental("MEAI001")]
internal sealed class OpenAISpeechToTextClient : ISpeechToTextClient
{
    /// <summary>Default OpenAI endpoint.</summary>
    private static readonly Uri _defaultOpenAIEndpoint = new("https://api.openai.com/v1");

    /// <summary>Metadata about the client.</summary>
    private readonly SpeechToTextClientMetadata _metadata;

    /// <summary>The underlying <see cref="AudioClient" />.</summary>
    private readonly AudioClient _audioClient;

    /// <summary>Initializes a new instance of the <see cref="OpenAISpeechToTextClient"/> class for the specified <see cref="AudioClient"/>.</summary>
    /// <param name="audioClient">The underlying client.</param>
    public OpenAISpeechToTextClient(AudioClient audioClient)
    {
        _ = Throw.IfNull(audioClient);

        _audioClient = audioClient;

        // https://github.com/openai/openai-dotnet/issues/215
        // The endpoint and model aren't currently exposed, so use reflection to get at them, temporarily. Once packages
        // implement the abstractions directly rather than providing adapters on top of the public APIs,
        // the package can provide such implementations separate from what's exposed in the public API.
        Uri providerUrl = typeof(AudioClient).GetField("_endpoint", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(audioClient) as Uri ?? _defaultOpenAIEndpoint;
        string? model = typeof(AudioClient).GetField("_model", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(audioClient) as string;

        _metadata = new("openai", providerUrl, model);
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(SpeechToTextClientMetadata) ? _metadata :
            serviceType == typeof(AudioClient) ? _audioClient :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
        Stream audioSpeechStream, SpeechToTextOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(audioSpeechStream);

        var speechResponse = await GetTextAsync(audioSpeechStream, options, cancellationToken).ConfigureAwait(false);

        foreach (var update in speechResponse.ToSpeechToTextResponseUpdates())
        {
            yield return update;
        }
    }

    /// <inheritdoc />
    public async Task<SpeechToTextResponse> GetTextAsync(
        Stream audioSpeechStream, SpeechToTextOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(audioSpeechStream);

        SpeechToTextResponse response = new();

        // <summary>A translation is triggered when the target text language is specified and the source language is not provided or different.</summary>
        static bool IsTranslationRequest(SpeechToTextOptions? options)
             => options is not null && options.TextLanguage is not null
                && (options.SpeechLanguage is null || options.SpeechLanguage != options.TextLanguage);

        if (IsTranslationRequest(options))
        {
            _ = Throw.IfNull(options);

            var openAIOptions = ToOpenAITranslationOptions(options);
            AudioTranslation translationResult;

#if NET
            await using (audioSpeechStream.ConfigureAwait(false))
#else
            using (audioSpeechStream)
#endif
            {
                translationResult = (await _audioClient.TranslateAudioAsync(
                    audioSpeechStream,
                    "file.wav", // this information internally is required but is only being used to create a header name in the multipart request.
                    openAIOptions, cancellationToken).ConfigureAwait(false)).Value;
            }

            UpdateResponseFromOpenAIAudioTranslation(response, translationResult);
        }
        else
        {
            var openAIOptions = ToOpenAITranscriptionOptions(options);

            // Transcription request
            AudioTranscription transcriptionResult;

#if NET
            await using (audioSpeechStream.ConfigureAwait(false))
#else
            using (audioSpeechStream)
#endif
            {
                transcriptionResult = (await _audioClient.TranscribeAudioAsync(
                    audioSpeechStream,
                    "file.wav", // this information internally is required but is only being used to create a header name in the multipart request.
                    openAIOptions, cancellationToken).ConfigureAwait(false)).Value;
            }

            UpdateResponseFromOpenAIAudioTranscription(response, transcriptionResult);
        }

        return response;
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the IAudioTranscriptionClient interface.
    }

    /// <summary>Updates a <see cref="SpeechToTextResponse"/> from an OpenAI <see cref="AudioTranscription"/>.</summary>
    /// <param name="response">The response to update.</param>
    /// <param name="audioTranscription">The OpenAI audio transcription.</param>
    private static void UpdateResponseFromOpenAIAudioTranscription(SpeechToTextResponse response, AudioTranscription audioTranscription)
    {
        _ = Throw.IfNull(audioTranscription);

        var segmentCount = audioTranscription.Segments.Count;
        var wordCount = audioTranscription.Words.Count;

        TimeSpan? endTime = null;
        TimeSpan? startTime = null;
        if (segmentCount > 0)
        {
            endTime = audioTranscription.Segments[segmentCount - 1].EndTime;
            startTime = audioTranscription.Segments[0].StartTime;
        }
        else if (wordCount > 0)
        {
            endTime = audioTranscription.Words[wordCount - 1].EndTime;
            startTime = audioTranscription.Words[0].StartTime;
        }

        // Update the response
        response.RawRepresentation = audioTranscription;
        response.Contents = [new TextContent(audioTranscription.Text)];
        response.StartTime = startTime;
        response.EndTime = endTime;
        response.AdditionalProperties = new AdditionalPropertiesDictionary
        {
            [nameof(audioTranscription.Language)] = audioTranscription.Language,
            [nameof(audioTranscription.Duration)] = audioTranscription.Duration
        };
    }

    /// <summary>Converts an extensions options instance to an OpenAI options instance.</summary>
    private AudioTranscriptionOptions ToOpenAITranscriptionOptions(SpeechToTextOptions? options)
    {
        if (options?.RawRepresentationFactory?.Invoke(this) is not AudioTranscriptionOptions result)
        {
            result = new AudioTranscriptionOptions();
        }

        result.Language ??= options?.SpeechLanguage;
        return result;
    }

    /// <summary>Updates a <see cref="SpeechToTextResponse"/> from an OpenAI <see cref="AudioTranslation"/>.</summary>
    /// <param name="response">The response to update.</param>
    /// <param name="audioTranslation">The OpenAI audio translation.</param>
    private static void UpdateResponseFromOpenAIAudioTranslation(SpeechToTextResponse response, AudioTranslation audioTranslation)
    {
        _ = Throw.IfNull(audioTranslation);

        var segmentCount = audioTranslation.Segments.Count;

        TimeSpan? endTime = null;
        TimeSpan? startTime = null;
        if (segmentCount > 0)
        {
            endTime = audioTranslation.Segments[segmentCount - 1].EndTime;
            startTime = audioTranslation.Segments[0].StartTime;
        }

        // Update the response
        response.RawRepresentation = audioTranslation;
        response.Contents = [new TextContent(audioTranslation.Text)];
        response.StartTime = startTime;
        response.EndTime = endTime;
        response.AdditionalProperties = new AdditionalPropertiesDictionary
        {
            [nameof(audioTranslation.Language)] = audioTranslation.Language,
            [nameof(audioTranslation.Duration)] = audioTranslation.Duration
        };
    }

    /// <summary>Converts an extensions options instance to an OpenAI options instance.</summary>
    private AudioTranslationOptions ToOpenAITranslationOptions(SpeechToTextOptions? options)
        => options?.RawRepresentationFactory?.Invoke(this) as AudioTranslationOptions ?? new AudioTranslationOptions();
}


