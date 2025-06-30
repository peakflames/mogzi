(Files content cropped to 300k characters, download full ingest to see more)
================================================
FILE: src/Libraries/Microsoft.Extensions.AI/README.md
================================================
# Microsoft.Extensions.AI

.NET developers need to integrate and interact with a growing variety of artificial intelligence (AI) services in their apps. The `Microsoft.Extensions.AI` libraries provide a unified approach for representing generative AI components, and enable seamless integration and interoperability with various AI services.

## The packages

The [Microsoft.Extensions.AI.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.AI.Abstractions) package provides the core exchange types, including [`IChatClient`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.ai.ichatclient) and [`IEmbeddingGenerator<TInput,TEmbedding>`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.ai.iembeddinggenerator-2). Any .NET library that provides an LLM client can implement the `IChatClient` interface to enable seamless integration with consuming code.

The [Microsoft.Extensions.AI](https://www.nuget.org/packages/Microsoft.Extensions.AI) package has an implicit dependency on the `Microsoft.Extensions.AI.Abstractions` package. This package enables you to easily integrate components such as automatic function tool invocation, telemetry, and caching into your applications using familiar dependency injection and middleware patterns. For example, it provides the [`UseOpenTelemetry(ChatClientBuilder, ILoggerFactory, String, Action<OpenTelemetryChatClient>)`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.ai.opentelemetrychatclientbuilderextensions.useopentelemetry#microsoft-extensions-ai-opentelemetrychatclientbuilderextensions-useopentelemetry(microsoft-extensions-ai-chatclientbuilder-microsoft-extensions-logging-iloggerfactory-system-string-system-action((microsoft-extensions-ai-opentelemetrychatclient)))) extension method, which adds OpenTelemetry support to the chat client pipeline.

## Which package to reference

Libraries that provide implementations of the abstractions typically reference only `Microsoft.Extensions.AI.Abstractions`.

To also have access to higher-level utilities for working with generative AI components, reference the `Microsoft.Extensions.AI` package instead (which itself references `Microsoft.Extensions.AI.Abstractions`). Most consuming applications and services should reference the `Microsoft.Extensions.AI` package along with one or more libraries that provide concrete implementations of the abstractions.

## Install the package

From the command-line:

```console
dotnet add package Microsoft.Extensions.AI
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.AI" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Documentation

Refer to the [Microsoft.Extensions.AI libraries documentation](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai) for more information and API usage examples.

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/EmptyServiceProvider.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.AI;

/// <summary>Provides an implementation of <see cref="IServiceProvider"/> that contains no services.</summary>
internal sealed class EmptyServiceProvider : IKeyedServiceProvider
{
    /// <summary>Gets a singleton instance of <see cref="EmptyServiceProvider"/>.</summary>
    public static EmptyServiceProvider Instance { get; } = new();

    /// <inheritdoc />
    public object? GetService(Type serviceType) => null;

    /// <inheritdoc />
    public object? GetKeyedService(Type serviceType, object? serviceKey) => null;

    /// <inheritdoc />
    public object GetRequiredKeyedService(Type serviceType, object? serviceKey) =>
        GetKeyedService(serviceType, serviceKey) ??
        throw new InvalidOperationException($"No service for type '{serviceType}' and key '{serviceKey}' has been registered.");
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/LoggingHelpers.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1031 // Do not catch general exception types
#pragma warning disable S108 // Nested blocks of code should not be left empty
#pragma warning disable S2486 // Generic exceptions should not be ignored

using System.Text.Json;

namespace Microsoft.Extensions.AI;

/// <summary>Provides internal helpers for implementing logging.</summary>
internal static class LoggingHelpers
{
    /// <summary>Serializes <paramref name="value"/> as JSON for logging purposes.</summary>
    public static string AsJson<T>(T value, JsonSerializerOptions? options)
    {
        if (options?.TryGetTypeInfo(typeof(T), out var typeInfo) is true ||
            AIJsonUtilities.DefaultOptions.TryGetTypeInfo(typeof(T), out typeInfo))
        {
            try
            {
                return JsonSerializer.Serialize(value, typeInfo);
            }
            catch
            {
            }
        }

        // If we're unable to get a type info for the value, or if we fail to serialize,
        // return an empty JSON object. We do not want lack of type info to disrupt application behavior with exceptions.
        return "{}";
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/Microsoft.Extensions.AI.csproj
================================================
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>Microsoft.Extensions.AI</RootNamespace>
    <Description>Utilities for working with generative AI components.</Description>
    <Workstream>AI</Workstream>
  </PropertyGroup>

  <PropertyGroup>
    <Stage>normal</Stage>
    <MinCodeCoverage>89</MinCodeCoverage>
    <MinMutationScore>85</MinMutationScore>
  </PropertyGroup>

  <PropertyGroup>
    <TargetFrameworks>$(TargetFrameworks);netstandard2.0</TargetFrameworks>
    <NoWarn>$(NoWarn);CA2227;CA1034;SA1316;S1067;S1121;S1994;S3253</NoWarn>

    <!-- CA2007 requires use of ConfigureAwait. While in general we strive to use ConfigureAwait in all our libraries,
         the exemption is when user code that might care about SynchronizationContext is invoked as part of operation.
         FunctionInvokingChatClient may invoke AIFunctions created to run user code that cares about the context, such
         as an AIFunction that invokes code to update a UI. As such, the Microsoft.Extensions.AI library explicitly avoids
         using ConfigureAwait(false). It could use ConfigureAwait(true), but it's easier to spot the presence of ConfigureAwait
         at all then to spot ones that use false rather than true. Alternatively, we could try to avoid using ConfigureAwait(false)
         only on paths that could lead up to the invocation of an AIFunction, but that is challenging to maintain correctly. -->
    <NoWarn>$(NoWarn);CA2007</NoWarn>
    
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <DisableNETStandardCompatErrors>true</DisableNETStandardCompatErrors>
  </PropertyGroup>

  <PropertyGroup>
    <InjectExperimentalAttributeOnLegacy>true</InjectExperimentalAttributeOnLegacy>
    <InjectSharedEmptyCollections>true</InjectSharedEmptyCollections>
    <InjectStringSyntaxAttributeOnLegacy>true</InjectStringSyntaxAttributeOnLegacy>
    <DisableMicrosoftExtensionsLoggingSourceGenerator>false</DisableMicrosoftExtensionsLoggingSourceGenerator>
  </PropertyGroup>

  <PropertyGroup>
    <InjectStringSyntaxAttributeOnLegacy>true</InjectStringSyntaxAttributeOnLegacy>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Caching.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
    <PackageReference Include="System.Diagnostics.DiagnosticSource" />
    <PackageReference Include="System.Text.Json" />
    <PackageReference Include="System.Threading.Channels" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Microsoft.Extensions.AI.Abstractions\Microsoft.Extensions.AI.Abstractions.csproj" />
  </ItemGroup>
  
</Project>



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/Microsoft.Extensions.AI.json
================================================
{
  "Name": "Microsoft.Extensions.AI, Version=9.6.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
  "Types": [
    {
      "Type": "abstract class Microsoft.Extensions.AI.CachingChatClient : Microsoft.Extensions.AI.DelegatingChatClient",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.CachingChatClient.CachingChatClient(Microsoft.Extensions.AI.IChatClient innerClient);",
          "Stage": "Stable"
        },
        {
          // After generating the baseline, manually edit this file to have 'params' instead of 'scoped'
          // This is needed until ICSharpCode.Decompiler adds params collection support
          // See: https://github.com/icsharpcode/ILSpy/issues/829
          "Member": "abstract string Microsoft.Extensions.AI.CachingChatClient.GetCacheKey(System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions? options, params System.ReadOnlySpan<object?> additionalValues);",
          "Stage": "Stable"
        },
        {
          "Member": "virtual bool Microsoft.Extensions.AI.CachingChatClient.EnableCaching(System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions? options);",
          "Stage": "Stable"
        },
        {
          "Member": "override System.Threading.Tasks.Task<Microsoft.Extensions.AI.ChatResponse> Microsoft.Extensions.AI.CachingChatClient.GetResponseAsync(System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          "Member": "override System.Collections.Generic.IAsyncEnumerable<Microsoft.Extensions.AI.ChatResponseUpdate> Microsoft.Extensions.AI.CachingChatClient.GetStreamingResponseAsync(System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          "Member": "abstract System.Threading.Tasks.Task<Microsoft.Extensions.AI.ChatResponse?> Microsoft.Extensions.AI.CachingChatClient.ReadCacheAsync(string key, System.Threading.CancellationToken cancellationToken);",
          "Stage": "Stable"
        },
        {
          "Member": "abstract System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.AI.ChatResponseUpdate>?> Microsoft.Extensions.AI.CachingChatClient.ReadCacheStreamingAsync(string key, System.Threading.CancellationToken cancellationToken);",
          "Stage": "Stable"
        },
        {
          "Member": "abstract System.Threading.Tasks.Task Microsoft.Extensions.AI.CachingChatClient.WriteCacheAsync(string key, Microsoft.Extensions.AI.ChatResponse value, System.Threading.CancellationToken cancellationToken);",
          "Stage": "Stable"
        },
        {
          "Member": "abstract System.Threading.Tasks.Task Microsoft.Extensions.AI.CachingChatClient.WriteCacheStreamingAsync(string key, System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.AI.ChatResponseUpdate> value, System.Threading.CancellationToken cancellationToken);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "bool Microsoft.Extensions.AI.CachingChatClient.CoalesceStreamingUpdates { get; set; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "abstract class Microsoft.Extensions.AI.CachingEmbeddingGenerator<TInput, TEmbedding> : Microsoft.Extensions.AI.DelegatingEmbeddingGenerator<TInput, TEmbedding> where TEmbedding : Microsoft.Extensions.AI.Embedding",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.CachingEmbeddingGenerator<TInput, TEmbedding>.CachingEmbeddingGenerator(Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding> innerGenerator);",
          "Stage": "Stable"
        },
        {
          "Member": "override System.Threading.Tasks.Task<Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding>> Microsoft.Extensions.AI.CachingEmbeddingGenerator<TInput, TEmbedding>.GenerateAsync(System.Collections.Generic.IEnumerable<TInput> values, Microsoft.Extensions.AI.EmbeddingGenerationOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          // After generating the baseline, manually edit this file to have 'params' instead of 'scoped'
          // This is needed until ICSharpCode.Decompiler adds params collection support
          // See: https://github.com/icsharpcode/ILSpy/issues/829
          "Member": "abstract string Microsoft.Extensions.AI.CachingEmbeddingGenerator<TInput, TEmbedding>.GetCacheKey(params System.ReadOnlySpan<object?> values);",
          "Stage": "Stable"
        },
        {
          "Member": "abstract System.Threading.Tasks.Task<TEmbedding?> Microsoft.Extensions.AI.CachingEmbeddingGenerator<TInput, TEmbedding>.ReadCacheAsync(string key, System.Threading.CancellationToken cancellationToken);",
          "Stage": "Stable"
        },
        {
          "Member": "abstract System.Threading.Tasks.Task Microsoft.Extensions.AI.CachingEmbeddingGenerator<TInput, TEmbedding>.WriteCacheAsync(string key, TEmbedding value, System.Threading.CancellationToken cancellationToken);",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.ChatClientBuilder",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.ChatClientBuilder.ChatClientBuilder(Microsoft.Extensions.AI.IChatClient innerClient);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.ChatClientBuilder.ChatClientBuilder(System.Func<System.IServiceProvider, Microsoft.Extensions.AI.IChatClient> innerClientFactory);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.IChatClient Microsoft.Extensions.AI.ChatClientBuilder.Build(System.IServiceProvider? services = null);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.ChatClientBuilder Microsoft.Extensions.AI.ChatClientBuilder.Use(System.Func<Microsoft.Extensions.AI.IChatClient, Microsoft.Extensions.AI.IChatClient> clientFactory);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.ChatClientBuilder Microsoft.Extensions.AI.ChatClientBuilder.Use(System.Func<Microsoft.Extensions.AI.IChatClient, System.IServiceProvider, Microsoft.Extensions.AI.IChatClient> clientFactory);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.ChatClientBuilder Microsoft.Extensions.AI.ChatClientBuilder.Use(System.Func<System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatMessage>, Microsoft.Extensions.AI.ChatOptions?, System.Func<System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatMessage>, Microsoft.Extensions.AI.ChatOptions?, System.Threading.CancellationToken, System.Threading.Tasks.Task>, System.Threading.CancellationToken, System.Threading.Tasks.Task> sharedFunc);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.ChatClientBuilder Microsoft.Extensions.AI.ChatClientBuilder.Use(System.Func<System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatMessage>, Microsoft.Extensions.AI.ChatOptions?, Microsoft.Extensions.AI.IChatClient, System.Threading.CancellationToken, System.Threading.Tasks.Task<Microsoft.Extensions.AI.ChatResponse>>? getResponseFunc, System.Func<System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatMessage>, Microsoft.Extensions.AI.ChatOptions?, Microsoft.Extensions.AI.IChatClient, System.Threading.CancellationToken, System.Collections.Generic.IAsyncEnumerable<Microsoft.Extensions.AI.ChatResponseUpdate>>? getStreamingResponseFunc);",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.AI.ChatClientBuilderChatClientExtensions",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "static Microsoft.Extensions.AI.ChatClientBuilder Microsoft.Extensions.AI.ChatClientBuilderChatClientExtensions.AsBuilder(this Microsoft.Extensions.AI.IChatClient innerClient);",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.DependencyInjection.ChatClientBuilderServiceCollectionExtensions",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "static Microsoft.Extensions.AI.ChatClientBuilder Microsoft.Extensions.DependencyInjection.ChatClientBuilderServiceCollectionExtensions.AddChatClient(this Microsoft.Extensions.DependencyInjection.IServiceCollection serviceCollection, Microsoft.Extensions.AI.IChatClient innerClient, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton);",
          "Stage": "Stable"
        },
        {
          "Member": "static Microsoft.Extensions.AI.ChatClientBuilder Microsoft.Extensions.DependencyInjection.ChatClientBuilderServiceCollectionExtensions.AddChatClient(this Microsoft.Extensions.DependencyInjection.IServiceCollection serviceCollection, System.Func<System.IServiceProvider, Microsoft.Extensions.AI.IChatClient> innerClientFactory, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton);",
          "Stage": "Stable"
        },
        {
          "Member": "static Microsoft.Extensions.AI.ChatClientBuilder Microsoft.Extensions.DependencyInjection.ChatClientBuilderServiceCollectionExtensions.AddKeyedChatClient(this Microsoft.Extensions.DependencyInjection.IServiceCollection serviceCollection, object? serviceKey, Microsoft.Extensions.AI.IChatClient innerClient, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton);",
          "Stage": "Stable"
        },
        {
          "Member": "static Microsoft.Extensions.AI.ChatClientBuilder Microsoft.Extensions.DependencyInjection.ChatClientBuilderServiceCollectionExtensions.AddKeyedChatClient(this Microsoft.Extensions.DependencyInjection.IServiceCollection serviceCollection, object? serviceKey, System.Func<System.IServiceProvider, Microsoft.Extensions.AI.IChatClient> innerClientFactory, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton);",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.AI.ChatClientStructuredOutputExtensions",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "static System.Threading.Tasks.Task<Microsoft.Extensions.AI.ChatResponse<T>> Microsoft.Extensions.AI.ChatClientStructuredOutputExtensions.GetResponseAsync<T>(this Microsoft.Extensions.AI.IChatClient chatClient, System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions? options = null, bool? useJsonSchemaResponseFormat = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          "Member": "static System.Threading.Tasks.Task<Microsoft.Extensions.AI.ChatResponse<T>> Microsoft.Extensions.AI.ChatClientStructuredOutputExtensions.GetResponseAsync<T>(this Microsoft.Extensions.AI.IChatClient chatClient, string chatMessage, Microsoft.Extensions.AI.ChatOptions? options = null, bool? useJsonSchemaResponseFormat = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          "Member": "static System.Threading.Tasks.Task<Microsoft.Extensions.AI.ChatResponse<T>> Microsoft.Extensions.AI.ChatClientStructuredOutputExtensions.GetResponseAsync<T>(this Microsoft.Extensions.AI.IChatClient chatClient, Microsoft.Extensions.AI.ChatMessage chatMessage, Microsoft.Extensions.AI.ChatOptions? options = null, bool? useJsonSchemaResponseFormat = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          "Member": "static System.Threading.Tasks.Task<Microsoft.Extensions.AI.ChatResponse<T>> Microsoft.Extensions.AI.ChatClientStructuredOutputExtensions.GetResponseAsync<T>(this Microsoft.Extensions.AI.IChatClient chatClient, string chatMessage, System.Text.Json.JsonSerializerOptions serializerOptions, Microsoft.Extensions.AI.ChatOptions? options = null, bool? useJsonSchemaResponseFormat = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          "Member": "static System.Threading.Tasks.Task<Microsoft.Extensions.AI.ChatResponse<T>> Microsoft.Extensions.AI.ChatClientStructuredOutputExtensions.GetResponseAsync<T>(this Microsoft.Extensions.AI.IChatClient chatClient, Microsoft.Extensions.AI.ChatMessage chatMessage, System.Text.Json.JsonSerializerOptions serializerOptions, Microsoft.Extensions.AI.ChatOptions? options = null, bool? useJsonSchemaResponseFormat = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          "Member": "static System.Threading.Tasks.Task<Microsoft.Extensions.AI.ChatResponse<T>> Microsoft.Extensions.AI.ChatClientStructuredOutputExtensions.GetResponseAsync<T>(this Microsoft.Extensions.AI.IChatClient chatClient, System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, System.Text.Json.JsonSerializerOptions serializerOptions, Microsoft.Extensions.AI.ChatOptions? options = null, bool? useJsonSchemaResponseFormat = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.ChatResponse<T> : Microsoft.Extensions.AI.ChatResponse",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.ChatResponse<T>.ChatResponse(Microsoft.Extensions.AI.ChatResponse response, System.Text.Json.JsonSerializerOptions serializerOptions);",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.ChatResponse<T>.TryGetResult(out T? result);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "T Microsoft.Extensions.AI.ChatResponse<T>.Result { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.ConfigureOptionsChatClient : Microsoft.Extensions.AI.DelegatingChatClient",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.ConfigureOptionsChatClient.ConfigureOptionsChatClient(Microsoft.Extensions.AI.IChatClient innerClient, System.Action<Microsoft.Extensions.AI.ChatOptions> configure);",
          "Stage": "Stable"
        },
        {
          "Member": "override System.Threading.Tasks.Task<Microsoft.Extensions.AI.ChatResponse> Microsoft.Extensions.AI.ConfigureOptionsChatClient.GetResponseAsync(System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          "Member": "override System.Collections.Generic.IAsyncEnumerable<Microsoft.Extensions.AI.ChatResponseUpdate> Microsoft.Extensions.AI.ConfigureOptionsChatClient.GetStreamingResponseAsync(System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.AI.ConfigureOptionsChatClientBuilderExtensions",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "static Microsoft.Extensions.AI.ChatClientBuilder Microsoft.Extensions.AI.ConfigureOptionsChatClientBuilderExtensions.ConfigureOptions(this Microsoft.Extensions.AI.ChatClientBuilder builder, System.Action<Microsoft.Extensions.AI.ChatOptions> configure);",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.ConfigureOptionsEmbeddingGenerator<TInput, TEmbedding> : Microsoft.Extensions.AI.DelegatingEmbeddingGenerator<TInput, TEmbedding> where TEmbedding : Microsoft.Extensions.AI.Embedding",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.ConfigureOptionsEmbeddingGenerator<TInput, TEmbedding>.ConfigureOptionsEmbeddingGenerator(Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding> innerGenerator, System.Action<Microsoft.Extensions.AI.EmbeddingGenerationOptions> configure);",
          "Stage": "Stable"
        },
        {
          "Member": "override System.Threading.Tasks.Task<Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding>> Microsoft.Extensions.AI.ConfigureOptionsEmbeddingGenerator<TInput, TEmbedding>.GenerateAsync(System.Collections.Generic.IEnumerable<TInput> values, Microsoft.Extensions.AI.EmbeddingGenerationOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.AI.ConfigureOptionsEmbeddingGeneratorBuilderExtensions",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "static Microsoft.Extensions.AI.EmbeddingGeneratorBuilder<TInput, TEmbedding> Microsoft.Extensions.AI.ConfigureOptionsEmbeddingGeneratorBuilderExtensions.ConfigureOptions<TInput, TEmbedding>(this Microsoft.Extensions.AI.EmbeddingGeneratorBuilder<TInput, TEmbedding> builder, System.Action<Microsoft.Extensions.AI.EmbeddingGenerationOptions> configure);",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.ConfigureOptionsSpeechToTextClient : Microsoft.Extensions.AI.DelegatingSpeechToTextClient",
      "Stage": "Experimental",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.ConfigureOptionsSpeechToTextClient.ConfigureOptionsSpeechToTextClient(Microsoft.Extensions.AI.ISpeechToTextClient innerClient, System.Action<Microsoft.Extensions.AI.SpeechToTextOptions> configure);",
          "Stage": "Experimental"
        },
        {
          "Member": "override System.Collections.Generic.IAsyncEnumerable<Microsoft.Extensions.AI.SpeechToTextResponseUpdate> Microsoft.Extensions.AI.ConfigureOptionsSpeechToTextClient.GetStreamingTextAsync(System.IO.Stream audioSpeechStream, Microsoft.Extensions.AI.SpeechToTextOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Experimental"
        },
        {
          "Member": "override System.Threading.Tasks.Task<Microsoft.Extensions.AI.SpeechToTextResponse> Microsoft.Extensions.AI.ConfigureOptionsSpeechToTextClient.GetTextAsync(System.IO.Stream audioSpeechStream, Microsoft.Extensions.AI.SpeechToTextOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Experimental"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.AI.ConfigureOptionsSpeechToTextClientBuilderExtensions",
      "Stage": "Experimental",
      "Methods": [
        {
          "Member": "static Microsoft.Extensions.AI.SpeechToTextClientBuilder Microsoft.Extensions.AI.ConfigureOptionsSpeechToTextClientBuilderExtensions.ConfigureOptions(this Microsoft.Extensions.AI.SpeechToTextClientBuilder builder, System.Action<Microsoft.Extensions.AI.SpeechToTextOptions> configure);",
          "Stage": "Experimental"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.DistributedCachingChatClient : Microsoft.Extensions.AI.CachingChatClient",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.DistributedCachingChatClient.DistributedCachingChatClient(Microsoft.Extensions.AI.IChatClient innerClient, Microsoft.Extensions.Caching.Distributed.IDistributedCache storage);",
          "Stage": "Stable"
        },
        {
          // After generating the baseline, manually edit this file to have 'params' instead of 'scoped'
          // This is needed until ICSharpCode.Decompiler adds params collection support
          // See: https://github.com/icsharpcode/ILSpy/issues/829
          "Member": "override string Microsoft.Extensions.AI.DistributedCachingChatClient.GetCacheKey(System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions? options, params System.ReadOnlySpan<object?> additionalValues);",
          "Stage": "Stable"
        },
        {
          "Member": "override System.Threading.Tasks.Task<Microsoft.Extensions.AI.ChatResponse?> Microsoft.Extensions.AI.DistributedCachingChatClient.ReadCacheAsync(string key, System.Threading.CancellationToken cancellationToken);",
          "Stage": "Stable"
        },
        {
          "Member": "override System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.AI.ChatResponseUpdate>?> Microsoft.Extensions.AI.DistributedCachingChatClient.ReadCacheStreamingAsync(string key, System.Threading.CancellationToken cancellationToken);",
          "Stage": "Stable"
        },
        {
          "Member": "override System.Threading.Tasks.Task Microsoft.Extensions.AI.DistributedCachingChatClient.WriteCacheAsync(string key, Microsoft.Extensions.AI.ChatResponse value, System.Threading.CancellationToken cancellationToken);",
          "Stage": "Stable"
        },
        {
          "Member": "override System.Threading.Tasks.Task Microsoft.Extensions.AI.DistributedCachingChatClient.WriteCacheStreamingAsync(string key, System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.AI.ChatResponseUpdate> value, System.Threading.CancellationToken cancellationToken);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "System.Text.Json.JsonSerializerOptions Microsoft.Extensions.AI.DistributedCachingChatClient.JsonSerializerOptions { get; set; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.AI.DistributedCachingChatClientBuilderExtensions",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "static Microsoft.Extensions.AI.ChatClientBuilder Microsoft.Extensions.AI.DistributedCachingChatClientBuilderExtensions.UseDistributedCache(this Microsoft.Extensions.AI.ChatClientBuilder builder, Microsoft.Extensions.Caching.Distributed.IDistributedCache? storage = null, System.Action<Microsoft.Extensions.AI.DistributedCachingChatClient>? configure = null);",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.DistributedCachingEmbeddingGenerator<TInput, TEmbedding> : Microsoft.Extensions.AI.CachingEmbeddingGenerator<TInput, TEmbedding> where TEmbedding : Microsoft.Extensions.AI.Embedding",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.DistributedCachingEmbeddingGenerator<TInput, TEmbedding>.DistributedCachingEmbeddingGenerator(Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding> innerGenerator, Microsoft.Extensions.Caching.Distributed.IDistributedCache storage);",
          "Stage": "Stable"
        },
        {
          // After generating the baseline, manually edit this file to have 'params' instead of 'scoped'
          // This is needed until ICSharpCode.Decompiler adds params collection support
          // See: https://github.com/icsharpcode/ILSpy/issues/829
          "Member": "override string Microsoft.Extensions.AI.DistributedCachingEmbeddingGenerator<TInput, TEmbedding>.GetCacheKey(params System.ReadOnlySpan<object?> values);",
          "Stage": "Stable"
        },
        {
          "Member": "override System.Threading.Tasks.Task<TEmbedding?> Microsoft.Extensions.AI.DistributedCachingEmbeddingGenerator<TInput, TEmbedding>.ReadCacheAsync(string key, System.Threading.CancellationToken cancellationToken);",
          "Stage": "Stable"
        },
        {
          "Member": "override System.Threading.Tasks.Task Microsoft.Extensions.AI.DistributedCachingEmbeddingGenerator<TInput, TEmbedding>.WriteCacheAsync(string key, TEmbedding value, System.Threading.CancellationToken cancellationToken);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "System.Text.Json.JsonSerializerOptions Microsoft.Extensions.AI.DistributedCachingEmbeddingGenerator<TInput, TEmbedding>.JsonSerializerOptions { get; set; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.AI.DistributedCachingEmbeddingGeneratorBuilderExtensions",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "static Microsoft.Extensions.AI.EmbeddingGeneratorBuilder<TInput, TEmbedding> Microsoft.Extensions.AI.DistributedCachingEmbeddingGeneratorBuilderExtensions.UseDistributedCache<TInput, TEmbedding>(this Microsoft.Extensions.AI.EmbeddingGeneratorBuilder<TInput, TEmbedding> builder, Microsoft.Extensions.Caching.Distributed.IDistributedCache? storage = null, System.Action<Microsoft.Extensions.AI.DistributedCachingEmbeddingGenerator<TInput, TEmbedding>>? configure = null);",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.EmbeddingGeneratorBuilder<TInput, TEmbedding> where TEmbedding : Microsoft.Extensions.AI.Embedding",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.EmbeddingGeneratorBuilder<TInput, TEmbedding>.EmbeddingGeneratorBuilder(Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding> innerGenerator);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.EmbeddingGeneratorBuilder<TInput, TEmbedding>.EmbeddingGeneratorBuilder(System.Func<System.IServiceProvider, Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding>> innerGeneratorFactory);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding> Microsoft.Extensions.AI.EmbeddingGeneratorBuilder<TInput, TEmbedding>.Build(System.IServiceProvider? services = null);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.EmbeddingGeneratorBuilder<TInput, TEmbedding> Microsoft.Extensions.AI.EmbeddingGeneratorBuilder<TInput, TEmbedding>.Use(System.Func<Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding>, Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding>> generatorFactory);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.EmbeddingGeneratorBuilder<TInput, TEmbedding> Microsoft.Extensions.AI.EmbeddingGeneratorBuilder<TInput, TEmbedding>.Use(System.Func<Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding>, System.IServiceProvider, Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding>> generatorFactory);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.EmbeddingGeneratorBuilder<TInput, TEmbedding> Microsoft.Extensions.AI.EmbeddingGeneratorBuilder<TInput, TEmbedding>.Use(System.Func<System.Collections.Generic.IEnumerable<TInput>, Microsoft.Extensions.AI.EmbeddingGenerationOptions?, Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding>, System.Threading.CancellationToken, System.Threading.Tasks.Task<Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding>>>? generateFunc);",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.AI.EmbeddingGeneratorBuilderEmbeddingGeneratorExtensions",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "static Microsoft.Extensions.AI.EmbeddingGeneratorBuilder<TInput, TEmbedding> Microsoft.Extensions.AI.EmbeddingGeneratorBuilderEmbeddingGeneratorExtensions.AsBuilder<TInput, TEmbedding>(this Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding> innerGenerator);",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.DependencyInjection.EmbeddingGeneratorBuilderServiceCollectionExtensions",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "static Microsoft.Extensions.AI.EmbeddingGeneratorBuilder<TInput, TEmbedding> Microsoft.Extensions.DependencyInjection.EmbeddingGeneratorBuilderServiceCollectionExtensions.AddEmbeddingGenerator<TInput, TEmbedding>(this Microsoft.Extensions.DependencyInjection.IServiceCollection serviceCollection, Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding> innerGenerator, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton);",
          "Stage": "Stable"
        },
        {
          "Member": "static Microsoft.Extensions.AI.EmbeddingGeneratorBuilder<TInput, TEmbedding> Microsoft.Extensions.DependencyInjection.EmbeddingGeneratorBuilderServiceCollectionExtensions.AddEmbeddingGenerator<TInput, TEmbedding>(this Microsoft.Extensions.DependencyInjection.IServiceCollection serviceCollection, System.Func<System.IServiceProvider, Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding>> innerGeneratorFactory, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton);",
          "Stage": "Stable"
        },
        {
          "Member": "static Microsoft.Extensions.AI.EmbeddingGeneratorBuilder<TInput, TEmbedding> Microsoft.Extensions.DependencyInjection.EmbeddingGeneratorBuilderServiceCollectionExtensions.AddKeyedEmbeddingGenerator<TInput, TEmbedding>(this Microsoft.Extensions.DependencyInjection.IServiceCollection serviceCollection, object? serviceKey, Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding> innerGenerator, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton);",
          "Stage": "Stable"
        },
        {
          "Member": "static Microsoft.Extensions.AI.EmbeddingGeneratorBuilder<TInput, TEmbedding> Microsoft.Extensions.DependencyInjection.EmbeddingGeneratorBuilderServiceCollectionExtensions.AddKeyedEmbeddingGenerator<TInput, TEmbedding>(this Microsoft.Extensions.DependencyInjection.IServiceCollection serviceCollection, object? serviceKey, System.Func<System.IServiceProvider, Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding>> innerGeneratorFactory, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton);",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.FunctionInvocationContext",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.FunctionInvocationContext.FunctionInvocationContext();",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "Microsoft.Extensions.AI.AIFunctionArguments Microsoft.Extensions.AI.FunctionInvocationContext.Arguments { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.FunctionCallContent Microsoft.Extensions.AI.FunctionInvocationContext.CallContent { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.AIFunction Microsoft.Extensions.AI.FunctionInvocationContext.Function { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "int Microsoft.Extensions.AI.FunctionInvocationContext.FunctionCallIndex { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "int Microsoft.Extensions.AI.FunctionInvocationContext.FunctionCount { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.FunctionInvocationContext.IsStreaming { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "int Microsoft.Extensions.AI.FunctionInvocationContext.Iteration { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Collections.Generic.IList<Microsoft.Extensions.AI.ChatMessage> Microsoft.Extensions.AI.FunctionInvocationContext.Messages { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.ChatOptions? Microsoft.Extensions.AI.FunctionInvocationContext.Options { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.FunctionInvocationContext.Terminate { get; set; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.FunctionInvokingChatClient : Microsoft.Extensions.AI.DelegatingChatClient",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.FunctionInvokingChatClient.FunctionInvokingChatClient(Microsoft.Extensions.AI.IChatClient innerClient, Microsoft.Extensions.Logging.ILoggerFactory? loggerFactory = null, System.IServiceProvider? functionInvocationServices = null);",
          "Stage": "Stable"
        },
        {
          "Member": "virtual System.Collections.Generic.IList<Microsoft.Extensions.AI.ChatMessage> Microsoft.Extensions.AI.FunctionInvokingChatClient.CreateResponseMessages(System.ReadOnlySpan<Microsoft.Extensions.AI.FunctionInvokingChatClient.FunctionInvocationResult> results);",
          "Stage": "Stable"
        },
        {
          "Member": "override System.Threading.Tasks.Task<Microsoft.Extensions.AI.ChatResponse> Microsoft.Extensions.AI.FunctionInvokingChatClient.GetResponseAsync(System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          "Member": "override System.Collections.Generic.IAsyncEnumerable<Microsoft.Extensions.AI.ChatResponseUpdate> Microsoft.Extensions.AI.FunctionInvokingChatClient.GetStreamingResponseAsync(System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          "Member": "virtual System.Threading.Tasks.ValueTask<object?> Microsoft.Extensions.AI.FunctionInvokingChatClient.InvokeFunctionAsync(Microsoft.Extensions.AI.FunctionInvocationContext context, System.Threading.CancellationToken cancellationToken);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "bool Microsoft.Extensions.AI.FunctionInvokingChatClient.AllowConcurrentInvocation { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "static Microsoft.Extensions.AI.FunctionInvocationContext? Microsoft.Extensions.AI.FunctionInvokingChatClient.CurrentContext { get; protected set; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.IServiceProvider? Microsoft.Extensions.AI.FunctionInvokingChatClient.FunctionInvocationServices { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.FunctionInvokingChatClient.IncludeDetailedErrors { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "int Microsoft.Extensions.AI.FunctionInvokingChatClient.MaximumConsecutiveErrorsPerRequest { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "int Microsoft.Extensions.AI.FunctionInvokingChatClient.MaximumIterationsPerRequest { get; set; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.FunctionInvokingChatClient.FunctionInvocationResult",
      "Stage": "Stable",
      "Properties": [
        {
          "Member": "Microsoft.Extensions.AI.FunctionCallContent Microsoft.Extensions.AI.FunctionInvokingChatClient.FunctionInvocationResult.CallContent { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Exception? Microsoft.Extensions.AI.FunctionInvokingChatClient.FunctionInvocationResult.Exception { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "object? Microsoft.Extensions.AI.FunctionInvokingChatClient.FunctionInvocationResult.Result { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.FunctionInvokingChatClient.FunctionInvocationStatus Microsoft.Extensions.AI.FunctionInvokingChatClient.FunctionInvocationResult.Status { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.FunctionInvokingChatClient.FunctionInvocationResult.Terminate { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "enum Microsoft.Extensions.AI.FunctionInvokingChatClient.FunctionInvocationStatus",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.FunctionInvokingChatClient.FunctionInvocationStatus.FunctionInvocationStatus();",
          "Stage": "Stable"
        }
      ],
      "Fields": [
        {
          "Member": "const Microsoft.Extensions.AI.FunctionInvokingChatClient.FunctionInvocationStatus Microsoft.Extensions.AI.FunctionInvokingChatClient.FunctionInvocationStatus.Exception",
          "Stage": "Stable",
          "Value": "2"
        },
        {
          "Member": "const Microsoft.Extensions.AI.FunctionInvokingChatClient.FunctionInvocationStatus Microsoft.Extensions.AI.FunctionInvokingChatClient.FunctionInvocationStatus.NotFound",
          "Stage": "Stable",
          "Value": "1"
        },
        {
          "Member": "const Microsoft.Extensions.AI.FunctionInvokingChatClient.FunctionInvocationStatus Microsoft.Extensions.AI.FunctionInvokingChatClient.FunctionInvocationStatus.RanToCompletion",
          "Stage": "Stable",
          "Value": "0"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.AI.FunctionInvokingChatClientBuilderExtensions",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "static Microsoft.Extensions.AI.ChatClientBuilder Microsoft.Extensions.AI.FunctionInvokingChatClientBuilderExtensions.UseFunctionInvocation(this Microsoft.Extensions.AI.ChatClientBuilder builder, Microsoft.Extensions.Logging.ILoggerFactory? loggerFactory = null, System.Action<Microsoft.Extensions.AI.FunctionInvokingChatClient>? configure = null);",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.LoggingChatClient : Microsoft.Extensions.AI.DelegatingChatClient",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.LoggingChatClient.LoggingChatClient(Microsoft.Extensions.AI.IChatClient innerClient, Microsoft.Extensions.Logging.ILogger logger);",
          "Stage": "Stable"
        },
        {
          "Member": "override System.Threading.Tasks.Task<Microsoft.Extensions.AI.ChatResponse> Microsoft.Extensions.AI.LoggingChatClient.GetResponseAsync(System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          "Member": "override System.Collections.Generic.IAsyncEnumerable<Microsoft.Extensions.AI.ChatResponseUpdate> Microsoft.Extensions.AI.LoggingChatClient.GetStreamingResponseAsync(System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "System.Text.Json.JsonSerializerOptions Microsoft.Extensions.AI.LoggingChatClient.JsonSerializerOptions { get; set; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.AI.LoggingChatClientBuilderExtensions",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "static Microsoft.Extensions.AI.ChatClientBuilder Microsoft.Extensions.AI.LoggingChatClientBuilderExtensions.UseLogging(this Microsoft.Extensions.AI.ChatClientBuilder builder, Microsoft.Extensions.Logging.ILoggerFactory? loggerFactory = null, System.Action<Microsoft.Extensions.AI.LoggingChatClient>? configure = null);",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.LoggingEmbeddingGenerator<TInput, TEmbedding> : Microsoft.Extensions.AI.DelegatingEmbeddingGenerator<TInput, TEmbedding> where TEmbedding : Microsoft.Extensions.AI.Embedding",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.LoggingEmbeddingGenerator<TInput, TEmbedding>.LoggingEmbeddingGenerator(Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding> innerGenerator, Microsoft.Extensions.Logging.ILogger logger);",
          "Stage": "Stable"
        },
        {
          "Member": "override System.Threading.Tasks.Task<Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding>> Microsoft.Extensions.AI.LoggingEmbeddingGenerator<TInput, TEmbedding>.GenerateAsync(System.Collections.Generic.IEnumerable<TInput> values, Microsoft.Extensions.AI.EmbeddingGenerationOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "System.Text.Json.JsonSerializerOptions Microsoft.Extensions.AI.LoggingEmbeddingGenerator<TInput, TEmbedding>.JsonSerializerOptions { get; set; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.AI.LoggingEmbeddingGeneratorBuilderExtensions",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "static Microsoft.Extensions.AI.EmbeddingGeneratorBuilder<TInput, TEmbedding> Microsoft.Extensions.AI.LoggingEmbeddingGeneratorBuilderExtensions.UseLogging<TInput, TEmbedding>(this Microsoft.Extensions.AI.EmbeddingGeneratorBuilder<TInput, TEmbedding> builder, Microsoft.Extensions.Logging.ILoggerFactory? loggerFactory = null, System.Action<Microsoft.Extensions.AI.LoggingEmbeddingGenerator<TInput, TEmbedding>>? configure = null);",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.LoggingSpeechToTextClient : Microsoft.Extensions.AI.DelegatingSpeechToTextClient",
      "Stage": "Experimental",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.LoggingSpeechToTextClient.LoggingSpeechToTextClient(Microsoft.Extensions.AI.ISpeechToTextClient innerClient, Microsoft.Extensions.Logging.ILogger logger);",
          "Stage": "Experimental"
        },
        {
          "Member": "override System.Collections.Generic.IAsyncEnumerable<Microsoft.Extensions.AI.SpeechToTextResponseUpdate> Microsoft.Extensions.AI.LoggingSpeechToTextClient.GetStreamingTextAsync(System.IO.Stream audioSpeechStream, Microsoft.Extensions.AI.SpeechToTextOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Experimental"
        },
        {
          "Member": "override System.Threading.Tasks.Task<Microsoft.Extensions.AI.SpeechToTextResponse> Microsoft.Extensions.AI.LoggingSpeechToTextClient.GetTextAsync(System.IO.Stream audioSpeechStream, Microsoft.Extensions.AI.SpeechToTextOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Experimental"
        }
      ],
      "Properties": [
        {
          "Member": "System.Text.Json.JsonSerializerOptions Microsoft.Extensions.AI.LoggingSpeechToTextClient.JsonSerializerOptions { get; set; }",
          "Stage": "Experimental"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.AI.LoggingSpeechToTextClientBuilderExtensions",
      "Stage": "Experimental",
      "Methods": [
        {
          "Member": "static Microsoft.Extensions.AI.SpeechToTextClientBuilder Microsoft.Extensions.AI.LoggingSpeechToTextClientBuilderExtensions.UseLogging(this Microsoft.Extensions.AI.SpeechToTextClientBuilder builder, Microsoft.Extensions.Logging.ILoggerFactory? loggerFactory = null, System.Action<Microsoft.Extensions.AI.LoggingSpeechToTextClient>? configure = null);",
          "Stage": "Experimental"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.OpenTelemetryChatClient : Microsoft.Extensions.AI.DelegatingChatClient",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.OpenTelemetryChatClient.OpenTelemetryChatClient(Microsoft.Extensions.AI.IChatClient innerClient, Microsoft.Extensions.Logging.ILogger? logger = null, string? sourceName = null);",
          "Stage": "Stable"
        },
        {
          "Member": "override void Microsoft.Extensions.AI.OpenTelemetryChatClient.Dispose(bool disposing);",
          "Stage": "Stable"
        },
        {
          "Member": "override System.Threading.Tasks.Task<Microsoft.Extensions.AI.ChatResponse> Microsoft.Extensions.AI.OpenTelemetryChatClient.GetResponseAsync(System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          "Member": "override object? Microsoft.Extensions.AI.OpenTelemetryChatClient.GetService(System.Type serviceType, object? serviceKey = null);",
          "Stage": "Stable"
        },
        {
          "Member": "override System.Collections.Generic.IAsyncEnumerable<Microsoft.Extensions.AI.ChatResponseUpdate> Microsoft.Extensions.AI.OpenTelemetryChatClient.GetStreamingResponseAsync(System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "bool Microsoft.Extensions.AI.OpenTelemetryChatClient.EnableSensitiveData { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Text.Json.JsonSerializerOptions Microsoft.Extensions.AI.OpenTelemetryChatClient.JsonSerializerOptions { get; set; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.AI.OpenTelemetryChatClientBuilderExtensions",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "static Microsoft.Extensions.AI.ChatClientBuilder Microsoft.Extensions.AI.OpenTelemetryChatClientBuilderExtensions.UseOpenTelemetry(this Microsoft.Extensions.AI.ChatClientBuilder builder, Microsoft.Extensions.Logging.ILoggerFactory? loggerFactory = null, string? sourceName = null, System.Action<Microsoft.Extensions.AI.OpenTelemetryChatClient>? configure = null);",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.OpenTelemetryEmbeddingGenerator<TInput, TEmbedding> : Microsoft.Extensions.AI.DelegatingEmbeddingGenerator<TInput, TEmbedding> where TEmbedding : Microsoft.Extensions.AI.Embedding",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.OpenTelemetryEmbeddingGenerator<TInput, TEmbedding>.OpenTelemetryEmbeddingGenerator(Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding> innerGenerator, Microsoft.Extensions.Logging.ILogger? logger = null, string? sourceName = null);",
          "Stage": "Stable"
        },
        {
          "Member": "override void Microsoft.Extensions.AI.OpenTelemetryEmbeddingGenerator<TInput, TEmbedding>.Dispose(bool disposing);",
          "Stage": "Stable"
        },
        {
          "Member": "override System.Threading.Tasks.Task<Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding>> Microsoft.Extensions.AI.OpenTelemetryEmbeddingGenerator<TInput, TEmbedding>.GenerateAsync(System.Collections.Generic.IEnumerable<TInput> values, Microsoft.Extensions.AI.EmbeddingGenerationOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          "Member": "override object? Microsoft.Extensions.AI.OpenTelemetryEmbeddingGenerator<TInput, TEmbedding>.GetService(System.Type serviceType, object? serviceKey = null);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "bool Microsoft.Extensions.AI.OpenTelemetryEmbeddingGenerator<TInput, TEmbedding>.EnableSensitiveData { get; set; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.AI.OpenTelemetryEmbeddingGeneratorBuilderExtensions",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "static Microsoft.Extensions.AI.EmbeddingGeneratorBuilder<TInput, TEmbedding> Microsoft.Extensions.AI.OpenTelemetryEmbeddingGeneratorBuilderExtensions.UseOpenTelemetry<TInput, TEmbedding>(this Microsoft.Extensions.AI.EmbeddingGeneratorBuilder<TInput, TEmbedding> builder, Microsoft.Extensions.Logging.ILoggerFactory? loggerFactory = null, string? sourceName = null, System.Action<Microsoft.Extensions.AI.OpenTelemetryEmbeddingGenerator<TInput, TEmbedding>>? configure = null);",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.SpeechToTextClientBuilder",
      "Stage": "Experimental",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.SpeechToTextClientBuilder.SpeechToTextClientBuilder(Microsoft.Extensions.AI.ISpeechToTextClient innerClient);",
          "Stage": "Experimental"
        },
        {
          "Member": "Microsoft.Extensions.AI.SpeechToTextClientBuilder.SpeechToTextClientBuilder(System.Func<System.IServiceProvider, Microsoft.Extensions.AI.ISpeechToTextClient> innerClientFactory);",
          "Stage": "Experimental"
        },
        {
          "Member": "Microsoft.Extensions.AI.ISpeechToTextClient Microsoft.Extensions.AI.SpeechToTextClientBuilder.Build(System.IServiceProvider? services = null);",
          "Stage": "Experimental"
        },
        {
          "Member": "Microsoft.Extensions.AI.SpeechToTextClientBuilder Microsoft.Extensions.AI.SpeechToTextClientBuilder.Use(System.Func<Microsoft.Extensions.AI.ISpeechToTextClient, Microsoft.Extensions.AI.ISpeechToTextClient> clientFactory);",
          "Stage": "Experimental"
        },
        {
          "Member": "Microsoft.Extensions.AI.SpeechToTextClientBuilder Microsoft.Extensions.AI.SpeechToTextClientBuilder.Use(System.Func<Microsoft.Extensions.AI.ISpeechToTextClient, System.IServiceProvider, Microsoft.Extensions.AI.ISpeechToTextClient> clientFactory);",
          "Stage": "Experimental"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.DependencyInjection.SpeechToTextClientBuilderServiceCollectionExtensions",
      "Stage": "Experimental",
      "Methods": [
        {
          "Member": "static Microsoft.Extensions.AI.SpeechToTextClientBuilder Microsoft.Extensions.DependencyInjection.SpeechToTextClientBuilderServiceCollectionExtensions.AddKeyedSpeechToTextClient(this Microsoft.Extensions.DependencyInjection.IServiceCollection serviceCollection, object serviceKey, Microsoft.Extensions.AI.ISpeechToTextClient innerClient, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton);",
          "Stage": "Experimental"
        },
        {
          "Member": "static Microsoft.Extensions.AI.SpeechToTextClientBuilder Microsoft.Extensions.DependencyInjection.SpeechToTextClientBuilderServiceCollectionExtensions.AddKeyedSpeechToTextClient(this Microsoft.Extensions.DependencyInjection.IServiceCollection serviceCollection, object serviceKey, System.Func<System.IServiceProvider, Microsoft.Extensions.AI.ISpeechToTextClient> innerClientFactory, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton);",
          "Stage": "Experimental"
        },
        {
          "Member": "static Microsoft.Extensions.AI.SpeechToTextClientBuilder Microsoft.Extensions.DependencyInjection.SpeechToTextClientBuilderServiceCollectionExtensions.AddSpeechToTextClient(this Microsoft.Extensions.DependencyInjection.IServiceCollection serviceCollection, Microsoft.Extensions.AI.ISpeechToTextClient innerClient, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton);",
          "Stage": "Experimental"
        },
        {
          "Member": "static Microsoft.Extensions.AI.SpeechToTextClientBuilder Microsoft.Extensions.DependencyInjection.SpeechToTextClientBuilderServiceCollectionExtensions.AddSpeechToTextClient(this Microsoft.Extensions.DependencyInjection.IServiceCollection serviceCollection, System.Func<System.IServiceProvider, Microsoft.Extensions.AI.ISpeechToTextClient> innerClientFactory, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton);",
          "Stage": "Experimental"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.AI.SpeechToTextClientBuilderSpeechToTextClientExtensions",
      "Stage": "Experimental",
      "Methods": [
        {
          "Member": "static Microsoft.Extensions.AI.SpeechToTextClientBuilder Microsoft.Extensions.AI.SpeechToTextClientBuilderSpeechToTextClientExtensions.AsBuilder(this Microsoft.Extensions.AI.ISpeechToTextClient innerClient);",
          "Stage": "Experimental"
        }
      ]
    }
  ]
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/OpenTelemetryConsts.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

#pragma warning disable CA1716 // Identifiers should not match keywords
#pragma warning disable S4041 // Type names should not match namespaces

/// <summary>Provides constants used by various telemetry services.</summary>
internal static class OpenTelemetryConsts
{
    public const string DefaultSourceName = "Experimental.Microsoft.Extensions.AI";

    public const string SecondsUnit = "s";
    public const string TokensUnit = "token";

    public static class Event
    {
        public const string Name = "event.name";
    }

    public static class Error
    {
        public const string Type = "error.type";
    }

    public static class GenAI
    {
        public const string Choice = "gen_ai.choice";
        public const string SystemName = "gen_ai.system";

        public const string Chat = "chat";
        public const string Embeddings = "embeddings";
        public const string ExecuteTool = "execute_tool";

        public static class Assistant
        {
            public const string Message = "gen_ai.assistant.message";
        }

        public static class Client
        {
            public static class OperationDuration
            {
                public const string Description = "Measures the duration of a GenAI operation";
                public const string Name = "gen_ai.client.operation.duration";
                public static readonly double[] ExplicitBucketBoundaries = [0.01, 0.02, 0.04, 0.08, 0.16, 0.32, 0.64, 1.28, 2.56, 5.12, 10.24, 20.48, 40.96, 81.92];
            }

            public static class TokenUsage
            {
                public const string Description = "Measures number of input and output tokens used";
                public const string Name = "gen_ai.client.token.usage";
                public static readonly int[] ExplicitBucketBoundaries = [1, 4, 16, 64, 256, 1_024, 4_096, 16_384, 65_536, 262_144, 1_048_576, 4_194_304, 16_777_216, 67_108_864];
            }
        }

        public static class Conversation
        {
            public const string Id = "gen_ai.conversation.id";
        }

        public static class Operation
        {
            public const string Name = "gen_ai.operation.name";
        }

        public static class Output
        {
            public const string Type = "gen_ai.output.type";
        }

        public static class Request
        {
            public const string EmbeddingDimensions = "gen_ai.request.embedding.dimensions";
            public const string FrequencyPenalty = "gen_ai.request.frequency_penalty";
            public const string Model = "gen_ai.request.model";
            public const string MaxTokens = "gen_ai.request.max_tokens";
            public const string PresencePenalty = "gen_ai.request.presence_penalty";
            public const string Seed = "gen_ai.request.seed";
            public const string StopSequences = "gen_ai.request.stop_sequences";
            public const string Temperature = "gen_ai.request.temperature";
            public const string TopK = "gen_ai.request.top_k";
            public const string TopP = "gen_ai.request.top_p";

            public static string PerProvider(string providerName, string parameterName) => $"gen_ai.{providerName}.request.{parameterName}";
        }

        public static class Response
        {
            public const string FinishReasons = "gen_ai.response.finish_reasons";
            public const string Id = "gen_ai.response.id";
            public const string Model = "gen_ai.response.model";

            public static string PerProvider(string providerName, string parameterName) => $"gen_ai.{providerName}.response.{parameterName}";
        }

        public static class System
        {
            public const string Message = "gen_ai.system.message";
        }

        public static class Token
        {
            public const string Type = "gen_ai.token.type";
        }

        public static class Tool
        {
            public const string Name = "gen_ai.tool.name";
            public const string Description = "gen_ai.tool.description";
            public const string Message = "gen_ai.tool.message";

            public static class Call
            {
                public const string Id = "gen_ai.tool.call.id";
            }
        }

        public static class Usage
        {
            public const string InputTokens = "gen_ai.usage.input_tokens";
            public const string OutputTokens = "gen_ai.usage.output_tokens";
        }

        public static class User
        {
            public const string Message = "gen_ai.user.message";
        }
    }

    public static class Server
    {
        public const string Address = "server.address";
        public const string Port = "server.port";
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/ChatCompletion/AnonymousDelegatingChatClient.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
#if !NET9_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks

namespace Microsoft.Extensions.AI;

/// <summary>Represents a delegating chat client that wraps an inner client with implementations provided by delegates.</summary>
internal sealed class AnonymousDelegatingChatClient : DelegatingChatClient
{
    /// <summary>The delegate to use as the implementation of <see cref="GetResponseAsync"/>.</summary>
    private readonly Func<IEnumerable<ChatMessage>, ChatOptions?, IChatClient, CancellationToken, Task<ChatResponse>>? _getResponseFunc;

    /// <summary>The delegate to use as the implementation of <see cref="GetStreamingResponseAsync"/>.</summary>
    /// <remarks>
    /// When non-<see langword="null"/>, this delegate is used as the implementation of <see cref="GetStreamingResponseAsync"/> and
    /// will be invoked with the same arguments as the method itself, along with a reference to the inner client.
    /// When <see langword="null"/>, <see cref="GetStreamingResponseAsync"/> will delegate directly to the inner client.
    /// </remarks>
    private readonly Func<IEnumerable<ChatMessage>, ChatOptions?, IChatClient, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>>? _getStreamingResponseFunc;

    /// <summary>The delegate to use as the implementation of both <see cref="GetResponseAsync"/> and <see cref="GetStreamingResponseAsync"/>.</summary>
    private readonly Func<IEnumerable<ChatMessage>, ChatOptions?, Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, Task>, CancellationToken, Task>? _sharedFunc;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnonymousDelegatingChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The inner client.</param>
    /// <param name="sharedFunc">
    /// A delegate that provides the implementation for both <see cref="GetResponseAsync"/> and <see cref="GetStreamingResponseAsync"/>.
    /// In addition to the arguments for the operation, it's provided with a delegate to the inner client that should be
    /// used to perform the operation on the inner client. It will handle both the non-streaming and streaming cases.
    /// </param>
    /// <remarks>
    /// This overload may be used when the anonymous implementation needs to provide pre-processing and/or post-processing, but doesn't
    /// need to interact with the results of the operation, which will come from the inner client.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="sharedFunc"/> is <see langword="null"/>.</exception>
    public AnonymousDelegatingChatClient(
        IChatClient innerClient,
        Func<IEnumerable<ChatMessage>, ChatOptions?, Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, Task>, CancellationToken, Task> sharedFunc)
        : base(innerClient)
    {
        _ = Throw.IfNull(sharedFunc);

        _sharedFunc = sharedFunc;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnonymousDelegatingChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The inner client.</param>
    /// <param name="getResponseFunc">
    /// A delegate that provides the implementation for <see cref="GetResponseAsync"/>. When <see langword="null"/>,
    /// <paramref name="getStreamingResponseFunc"/> must be non-null, and the implementation of <see cref="GetResponseAsync"/>
    /// will use <paramref name="getStreamingResponseFunc"/> for the implementation.
    /// </param>
    /// <param name="getStreamingResponseFunc">
    /// A delegate that provides the implementation for <see cref="GetStreamingResponseAsync"/>. When <see langword="null"/>,
    /// <paramref name="getResponseFunc"/> must be non-null, and the implementation of <see cref="GetStreamingResponseAsync"/>
    /// will use <paramref name="getResponseFunc"/> for the implementation.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException">Both <paramref name="getResponseFunc"/> and <paramref name="getStreamingResponseFunc"/> are <see langword="null"/>.</exception>
    public AnonymousDelegatingChatClient(
        IChatClient innerClient,
        Func<IEnumerable<ChatMessage>, ChatOptions?, IChatClient, CancellationToken, Task<ChatResponse>>? getResponseFunc,
        Func<IEnumerable<ChatMessage>, ChatOptions?, IChatClient, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>>? getStreamingResponseFunc)
        : base(innerClient)
    {
        ThrowIfBothDelegatesNull(getResponseFunc, getStreamingResponseFunc);

        _getResponseFunc = getResponseFunc;
        _getStreamingResponseFunc = getStreamingResponseFunc;
    }

    /// <inheritdoc/>
    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        if (_sharedFunc is not null)
        {
            return GetResponseViaSharedAsync(messages, options, cancellationToken);

            async Task<ChatResponse> GetResponseViaSharedAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options, CancellationToken cancellationToken)
            {
                ChatResponse? response = null;
                await _sharedFunc(messages, options, async (messages, options, cancellationToken) =>
                {
                    response = await InnerClient.GetResponseAsync(messages, options, cancellationToken);
                }, cancellationToken);

                if (response is null)
                {
                    Throw.InvalidOperationException("The wrapper completed successfully without producing a ChatResponse.");
                }

                return response;
            }
        }
        else if (_getResponseFunc is not null)
        {
            return _getResponseFunc(messages, options, InnerClient, cancellationToken);
        }
        else
        {
            Debug.Assert(_getStreamingResponseFunc is not null, "Expected non-null streaming delegate.");
            return _getStreamingResponseFunc!(messages, options, InnerClient, cancellationToken)
                .ToChatResponseAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        if (_sharedFunc is not null)
        {
            var updates = Channel.CreateBounded<ChatResponseUpdate>(1);

            _ = ProcessAsync();
            async Task ProcessAsync()
            {
                Exception? error = null;
                try
                {
                    await _sharedFunc(messages, options, async (messages, options, cancellationToken) =>
                    {
                        await foreach (var update in InnerClient.GetStreamingResponseAsync(messages, options, cancellationToken))
                        {
                            await updates.Writer.WriteAsync(update, cancellationToken);
                        }
                    }, cancellationToken);
                }
                catch (Exception ex)
                {
                    error = ex;
                    throw;
                }
                finally
                {
                    _ = updates.Writer.TryComplete(error);
                }
            }

#if NET9_0_OR_GREATER
            return updates.Reader.ReadAllAsync(cancellationToken);
#else
            return ReadAllAsync(updates, cancellationToken);
            static async IAsyncEnumerable<ChatResponseUpdate> ReadAllAsync(
                ChannelReader<ChatResponseUpdate> channel, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                while (await channel.WaitToReadAsync(cancellationToken))
                {
                    while (channel.TryRead(out var update))
                    {
                        yield return update;
                    }
                }
            }
#endif
        }
        else if (_getStreamingResponseFunc is not null)
        {
            return _getStreamingResponseFunc(messages, options, InnerClient, cancellationToken);
        }
        else
        {
            Debug.Assert(_getResponseFunc is not null, "Expected non-null non-streaming delegate.");
            return GetStreamingResponseAsyncViaGetResponseAsync(_getResponseFunc!(messages, options, InnerClient, cancellationToken));

            static async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsyncViaGetResponseAsync(Task<ChatResponse> task)
            {
                ChatResponse response = await task;
                foreach (var update in response.ToChatResponseUpdates())
                {
                    yield return update;
                }
            }
        }
    }

    /// <summary>Throws an exception if both of the specified delegates are <see langword="null"/>.</summary>
    /// <exception cref="ArgumentNullException">Both <paramref name="getResponseFunc"/> and <paramref name="getStreamingResponseFunc"/> are <see langword="null"/>.</exception>
    internal static void ThrowIfBothDelegatesNull(object? getResponseFunc, object? getStreamingResponseFunc)
    {
        if (getResponseFunc is null && getStreamingResponseFunc is null)
        {
            Throw.ArgumentNullException(nameof(getResponseFunc), $"At least one of the {nameof(getResponseFunc)} or {nameof(getStreamingResponseFunc)} delegates must be non-null.");
        }
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/ChatCompletion/CachingChatClient.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S127 // "for" loop stop conditions should be invariant
#pragma warning disable SA1202 // Elements should be ordered by access

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a delegating chat client that caches the results of chat calls.
/// </summary>
public abstract class CachingChatClient : DelegatingChatClient
{
    /// <summary>A boxed <see langword="true"/> value.</summary>
    private static readonly object _boxedTrue = true;

    /// <summary>A boxed <see langword="false"/> value.</summary>
    private static readonly object _boxedFalse = false;

    /// <summary>Initializes a new instance of the <see cref="CachingChatClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="IChatClient"/>.</param>
    protected CachingChatClient(IChatClient innerClient)
        : base(innerClient)
    {
    }

    /// <summary>Gets or sets a value indicating whether streaming updates are coalesced.</summary>
    /// <value>
    /// <para>
    /// <see langword="true"/> if the client attempts to coalesce contiguous streaming updates
    /// into a single update, to reduce the number of individual items that are yielded on
    /// subsequent enumerations of the cached data; <see langword="false"/> if the updates are
    /// kept unaltered.
    /// </para>
    /// <para>
    /// The default is <see langword="true"/>.
    /// </para>
    /// </value>
    public bool CoalesceStreamingUpdates { get; set; } = true;

    /// <inheritdoc />
    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        return EnableCaching(messages, options) ?
            GetCachedResponseAsync(messages, options, cancellationToken) :
            base.GetResponseAsync(messages, options, cancellationToken);
    }

    private async Task<ChatResponse> GetCachedResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        // We're only storing the final result, not the in-flight task, so that we can avoid caching failures
        // or having problems when one of the callers cancels but others don't. This has the drawback that
        // concurrent callers might trigger duplicate requests, but that's acceptable.
        var cacheKey = GetCacheKey(messages, options, _boxedFalse);

        if (await ReadCacheAsync(cacheKey, cancellationToken) is not { } result)
        {
            result = await base.GetResponseAsync(messages, options, cancellationToken);
            await WriteCacheAsync(cacheKey, result, cancellationToken);
        }

        return result;
    }

    /// <inheritdoc />
    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        return EnableCaching(messages, options) ?
            GetCachedStreamingResponseAsync(messages, options, cancellationToken) :
            base.GetStreamingResponseAsync(messages, options, cancellationToken);
    }

    private async IAsyncEnumerable<ChatResponseUpdate> GetCachedStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (CoalesceStreamingUpdates)
        {
            // When coalescing updates, we cache non-streaming results coalesced from streaming ones. That means
            // we make a streaming request, yielding those results, but then convert those into a non-streaming
            // result and cache it. When we get a cache hit, we yield the non-streaming result as a streaming one.

            var cacheKey = GetCacheKey(messages, options, _boxedTrue);
            if (await ReadCacheAsync(cacheKey, cancellationToken) is { } chatResponse)
            {
                // Yield all of the cached items.
                foreach (var chunk in chatResponse.ToChatResponseUpdates())
                {
                    yield return chunk;
                }
            }
            else
            {
                // Yield and store all of the items.
                List<ChatResponseUpdate> capturedItems = [];
                await foreach (var chunk in base.GetStreamingResponseAsync(messages, options, cancellationToken))
                {
                    capturedItems.Add(chunk);
                    yield return chunk;
                }

                // Write the captured items to the cache as a non-streaming result.
                await WriteCacheAsync(cacheKey, capturedItems.ToChatResponse(), cancellationToken);
            }
        }
        else
        {
            var cacheKey = GetCacheKey(messages, options, _boxedTrue);
            if (await ReadCacheStreamingAsync(cacheKey, cancellationToken) is { } existingChunks)
            {
                // Yield all of the cached items.
                string? conversationId = null;
                foreach (var chunk in existingChunks)
                {
                    conversationId ??= chunk.ConversationId;
                    yield return chunk;
                }
            }
            else
            {
                // Yield and store all of the items.
                List<ChatResponseUpdate> capturedItems = [];
                await foreach (var chunk in base.GetStreamingResponseAsync(messages, options, cancellationToken))
                {
                    capturedItems.Add(chunk);
                    yield return chunk;
                }

                // Write the captured items to the cache.
                await WriteCacheStreamingAsync(cacheKey, capturedItems, cancellationToken);
            }
        }
    }

    /// <summary>Computes a cache key for the specified values.</summary>
    /// <param name="messages">The messages to inform the key.</param>
    /// <param name="options">The <see cref="ChatOptions"/> to inform the key.</param>
    /// <param name="additionalValues">Any other values to inform the key.</param>
    /// <returns>The computed key.</returns>
    protected abstract string GetCacheKey(IEnumerable<ChatMessage> messages, ChatOptions? options, params ReadOnlySpan<object?> additionalValues);

    /// <summary>
    /// Returns a previously cached <see cref="ChatResponse"/>, if available.
    /// This is used when there is a call to <see cref="IChatClient.GetResponseAsync"/>.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The previously cached data, if available, otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    protected abstract Task<ChatResponse?> ReadCacheAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a previously cached list of <see cref="ChatResponseUpdate"/> values, if available.
    /// This is used when there is a call to <see cref="IChatClient.GetStreamingResponseAsync"/>.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The previously cached data, if available, otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    protected abstract Task<IReadOnlyList<ChatResponseUpdate>?> ReadCacheStreamingAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Stores a <see cref="ChatResponse"/> in the underlying cache.
    /// This is used when there is a call to <see cref="IChatClient.GetResponseAsync"/>.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The <see cref="ChatResponse"/> to be stored.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    protected abstract Task WriteCacheAsync(string key, ChatResponse value, CancellationToken cancellationToken);

    /// <summary>
    /// Stores a list of <see cref="ChatResponseUpdate"/> values in the underlying cache.
    /// This is used when there is a call to <see cref="IChatClient.GetStreamingResponseAsync"/>.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The <see cref="ChatResponse"/> to be stored.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    protected abstract Task WriteCacheStreamingAsync(string key, IReadOnlyList<ChatResponseUpdate> value, CancellationToken cancellationToken);

    /// <summary>Determines whether caching should be used with the specified request.</summary>
    /// <param name="messages">The sequence of chat messages included in the request.</param>
    /// <param name="options">The chat options included in the request.</param>
    /// <returns>
    /// <see langword="true"/> if caching should be used for the request, such that the <see cref="CachingChatClient"/>
    /// will try to satisfy the request from the cache, or if it can't, will try to cache the fetched response.
    /// <see langword="false"/> if caching should not be used for the request, such that the request will
    /// be passed through to the inner <see cref="IChatClient"/> without attempting to read from or write to the cache.
    /// </returns>
    /// <remarks>
    /// The default implementation returns <see langword="true"/> as long as the <paramref name="options"/>
    /// does not have a <see cref="ChatOptions.ConversationId"/> set.
    /// </remarks>
    protected virtual bool EnableCaching(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        // We want to skip caching if options.ConversationId is set. If it's set, that implies there's
        // some state that will impact the response and that's not represented in the messages. Since
        // that state could change even with the same ID (e.g. if it's a thread ID representing the
        // mutable state of a conversation), we have to assume caching isn't valid.
        return options?.ConversationId is null;
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/ChatCompletion/ChatClientBuilder.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A builder for creating pipelines of <see cref="IChatClient"/>.</summary>
public sealed class ChatClientBuilder
{
    private readonly Func<IServiceProvider, IChatClient> _innerClientFactory;

    /// <summary>The registered client factory instances.</summary>
    private List<Func<IChatClient, IServiceProvider, IChatClient>>? _clientFactories;

    /// <summary>Initializes a new instance of the <see cref="ChatClientBuilder"/> class.</summary>
    /// <param name="innerClient">The inner <see cref="IChatClient"/> that represents the underlying backend.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> is <see langword="null"/>.</exception>
    public ChatClientBuilder(IChatClient innerClient)
    {
        _ = Throw.IfNull(innerClient);
        _innerClientFactory = _ => innerClient;
    }

    /// <summary>Initializes a new instance of the <see cref="ChatClientBuilder"/> class.</summary>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="IChatClient"/> that represents the underlying backend.</param>
    public ChatClientBuilder(Func<IServiceProvider, IChatClient> innerClientFactory)
    {
        _innerClientFactory = Throw.IfNull(innerClientFactory);
    }

    /// <summary>Builds an <see cref="IChatClient"/> that represents the entire pipeline. Calls to this instance will pass through each of the pipeline stages in turn.</summary>
    /// <param name="services">
    /// The <see cref="IServiceProvider"/> that should provide services to the <see cref="IChatClient"/> instances.
    /// If <see langword="null"/>, an empty <see cref="IServiceProvider"/> will be used.
    /// </param>
    /// <returns>An instance of <see cref="IChatClient"/> that represents the entire pipeline.</returns>
    public IChatClient Build(IServiceProvider? services = null)
    {
        services ??= EmptyServiceProvider.Instance;
        var chatClient = _innerClientFactory(services);

        // To match intuitive expectations, apply the factories in reverse order, so that the first factory added is the outermost.
        if (_clientFactories is not null)
        {
            for (var i = _clientFactories.Count - 1; i >= 0; i--)
            {
                chatClient = _clientFactories[i](chatClient, services);
                if (chatClient is null)
                {
                    Throw.InvalidOperationException(
                        $"The {nameof(ChatClientBuilder)} entry at index {i} returned null. " +
                        $"Ensure that the callbacks passed to {nameof(Use)} return non-null {nameof(IChatClient)} instances.");
                }
            }
        }

        return chatClient;
    }

    /// <summary>Adds a factory for an intermediate chat client to the chat client pipeline.</summary>
    /// <param name="clientFactory">The client factory function.</param>
    /// <returns>The updated <see cref="ChatClientBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="clientFactory"/> is <see langword="null"/>.</exception>
    /// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai#functionality-pipelines">Pipelines of functionality.</related>
    public ChatClientBuilder Use(Func<IChatClient, IChatClient> clientFactory)
    {
        _ = Throw.IfNull(clientFactory);

        return Use((innerClient, _) => clientFactory(innerClient));
    }

    /// <summary>Adds a factory for an intermediate chat client to the chat client pipeline.</summary>
    /// <param name="clientFactory">The client factory function.</param>
    /// <returns>The updated <see cref="ChatClientBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="clientFactory"/> is <see langword="null"/>.</exception>
    /// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai#functionality-pipelines">Pipelines of functionality.</related>
    public ChatClientBuilder Use(Func<IChatClient, IServiceProvider, IChatClient> clientFactory)
    {
        _ = Throw.IfNull(clientFactory);

        (_clientFactories ??= []).Add(clientFactory);
        return this;
    }

    /// <summary>
    /// Adds to the chat client pipeline an anonymous delegating chat client based on a delegate that provides
    /// an implementation for both <see cref="IChatClient.GetResponseAsync"/> and <see cref="IChatClient.GetStreamingResponseAsync"/>.
    /// </summary>
    /// <param name="sharedFunc">
    /// A delegate that provides the implementation for both <see cref="IChatClient.GetResponseAsync"/> and
    /// <see cref="IChatClient.GetStreamingResponseAsync"/>. This delegate is invoked with the list of chat messages, the chat
    /// options, a delegate that represents invoking the inner client, and a cancellation token. The delegate should be passed
    /// whatever chat messages, options, and cancellation token should be passed along to the next stage in the pipeline.
    /// It will handle both the non-streaming and streaming cases.
    /// </param>
    /// <returns>The updated <see cref="ChatClientBuilder"/> instance.</returns>
    /// <remarks>
    /// This overload can be used when the anonymous implementation needs to provide pre-processing and/or post-processing, but doesn't
    /// need to interact with the results of the operation, which will come from the inner client.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="sharedFunc"/> is <see langword="null"/>.</exception>
    /// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai#functionality-pipelines">Pipelines of functionality.</related>
    public ChatClientBuilder Use(Func<IEnumerable<ChatMessage>, ChatOptions?, Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, Task>, CancellationToken, Task> sharedFunc)
    {
        _ = Throw.IfNull(sharedFunc);

        return Use((innerClient, _) => new AnonymousDelegatingChatClient(innerClient, sharedFunc));
    }

    /// <summary>
    /// Adds to the chat client pipeline an anonymous delegating chat client based on a delegate that provides
    /// an implementation for both <see cref="IChatClient.GetResponseAsync"/> and <see cref="IChatClient.GetStreamingResponseAsync"/>.
    /// </summary>
    /// <param name="getResponseFunc">
    /// A delegate that provides the implementation for <see cref="IChatClient.GetResponseAsync"/>. When <see langword="null"/>,
    /// <paramref name="getStreamingResponseFunc"/> must be non-null, and the implementation of <see cref="IChatClient.GetResponseAsync"/>
    /// will use <paramref name="getStreamingResponseFunc"/> for the implementation.
    /// </param>
    /// <param name="getStreamingResponseFunc">
    /// A delegate that provides the implementation for <see cref="IChatClient.GetStreamingResponseAsync"/>. When <see langword="null"/>,
    /// <paramref name="getResponseFunc"/> must be non-null, and the implementation of <see cref="IChatClient.GetStreamingResponseAsync"/>
    /// will use <paramref name="getResponseFunc"/> for the implementation.
    /// </param>
    /// <returns>The updated <see cref="ChatClientBuilder"/> instance.</returns>
    /// <remarks>
    /// One or both delegates can be provided. If both are provided, they will be used for their respective methods:
    /// <paramref name="getResponseFunc"/> will provide the implementation of <see cref="IChatClient.GetResponseAsync"/>, and
    /// <paramref name="getStreamingResponseFunc"/> will provide the implementation of <see cref="IChatClient.GetStreamingResponseAsync"/>.
    /// If only one of the delegates is provided, it will be used for both methods. That means that if <paramref name="getResponseFunc"/>
    /// is supplied without <paramref name="getStreamingResponseFunc"/>, the implementation of <see cref="IChatClient.GetStreamingResponseAsync"/>
    /// will employ limited streaming, as it will be operating on the batch output produced by <paramref name="getResponseFunc"/>. And if
    /// <paramref name="getStreamingResponseFunc"/> is supplied without <paramref name="getResponseFunc"/>, the implementation of
    /// <see cref="IChatClient.GetResponseAsync"/> will be implemented by combining the updates from <paramref name="getStreamingResponseFunc"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Both <paramref name="getResponseFunc"/> and <paramref name="getStreamingResponseFunc"/> are <see langword="null"/>.</exception>
    /// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai#functionality-pipelines">Pipelines of functionality.</related>
    public ChatClientBuilder Use(
        Func<IEnumerable<ChatMessage>, ChatOptions?, IChatClient, CancellationToken, Task<ChatResponse>>? getResponseFunc,
        Func<IEnumerable<ChatMessage>, ChatOptions?, IChatClient, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>>? getStreamingResponseFunc)
    {
        AnonymousDelegatingChatClient.ThrowIfBothDelegatesNull(getResponseFunc, getStreamingResponseFunc);

        return Use((innerClient, _) => new AnonymousDelegatingChatClient(innerClient, getResponseFunc, getStreamingResponseFunc));
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/ChatCompletion/ChatClientBuilderChatClientExtensions.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for working with <see cref="IChatClient"/> in the context of <see cref="ChatClientBuilder"/>.</summary>
public static class ChatClientBuilderChatClientExtensions
{
    /// <summary>Creates a new <see cref="ChatClientBuilder"/> using <paramref name="innerClient"/> as its inner client.</summary>
    /// <param name="innerClient">The client to use as the inner client.</param>
    /// <returns>The new <see cref="ChatClientBuilder"/> instance.</returns>
    /// <remarks>
    /// This method is equivalent to using the <see cref="ChatClientBuilder"/> constructor directly,
    /// specifying <paramref name="innerClient"/> as the inner client.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> is <see langword="null"/>.</exception>
    public static ChatClientBuilder AsBuilder(this IChatClient innerClient)
    {
        _ = Throw.IfNull(innerClient);

        return new ChatClientBuilder(innerClient);
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/ChatCompletion/ChatClientBuilderServiceCollectionExtensions.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Provides extension methods for registering <see cref="IChatClient"/> with a <see cref="IServiceCollection"/>.</summary>
public static class ChatClientBuilderServiceCollectionExtensions
{
    /// <summary>Registers a singleton <see cref="IChatClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="innerClient">The inner <see cref="IChatClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>A <see cref="ChatClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <remarks>The client is registered as a singleton service.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="serviceCollection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> is <see langword="null"/>.</exception>
    public static ChatClientBuilder AddChatClient(
        this IServiceCollection serviceCollection,
        IChatClient innerClient,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerClient);

        return AddChatClient(serviceCollection, _ => innerClient, lifetime);
    }

    /// <summary>Registers a singleton <see cref="IChatClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="IChatClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>A <see cref="ChatClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <remarks>The client is registered as a singleton service.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="serviceCollection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="innerClientFactory"/> is <see langword="null"/>.</exception>
    public static ChatClientBuilder AddChatClient(
        this IServiceCollection serviceCollection,
        Func<IServiceProvider, IChatClient> innerClientFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerClientFactory);

        var builder = new ChatClientBuilder(innerClientFactory);
        serviceCollection.Add(new ServiceDescriptor(typeof(IChatClient), builder.Build, lifetime));
        return builder;
    }

    /// <summary>Registers a keyed singleton <see cref="IChatClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="serviceKey">The key with which to associate the client.</param>
    /// <param name="innerClient">The inner <see cref="IChatClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>A <see cref="ChatClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <remarks>The client is registered as a scoped service.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="serviceCollection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> is <see langword="null"/>.</exception>
    public static ChatClientBuilder AddKeyedChatClient(
        this IServiceCollection serviceCollection,
        object? serviceKey,
        IChatClient innerClient,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerClient);

        return AddKeyedChatClient(serviceCollection, serviceKey, _ => innerClient, lifetime);
    }

    /// <summary>Registers a keyed singleton <see cref="IChatClient"/> in the <see cref="IServiceCollection"/>.</summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the client should be added.</param>
    /// <param name="serviceKey">The key with which to associate the client.</param>
    /// <param name="innerClientFactory">A callback that produces the inner <see cref="IChatClient"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>A <see cref="ChatClientBuilder"/> that can be used to build a pipeline around the inner client.</returns>
    /// <remarks>The client is registered as a scoped service.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="serviceCollection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="innerClientFactory"/> is <see langword="null"/>.</exception>
    public static ChatClientBuilder AddKeyedChatClient(
        this IServiceCollection serviceCollection,
        object? serviceKey,
        Func<IServiceProvider, IChatClient> innerClientFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerClientFactory);

        var builder = new ChatClientBuilder(innerClientFactory);
        serviceCollection.Add(new ServiceDescriptor(typeof(IChatClient), serviceKey, factory: (services, serviceKey) => builder.Build(services), lifetime));
        return builder;
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/ChatCompletion/ChatClientStructuredOutputExtensions.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

#pragma warning disable SA1118 // Parameter should not span multiple lines
#pragma warning disable S2333 // Redundant modifiers should not be used

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides extension methods on <see cref="IChatClient"/> that simplify working with structured output.
/// </summary>
/// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/quickstarts/structured-output">Request a response with structured output.</related>
public static partial class ChatClientStructuredOutputExtensions
{
    private static readonly AIJsonSchemaCreateOptions _inferenceOptions = new()
    {
        IncludeSchemaKeyword = true,
        TransformOptions = new AIJsonSchemaTransformOptions
        {
            DisallowAdditionalProperties = true,
            RequireAllProperties = true,
            MoveDefaultKeywordToDescription = true,
        },
    };

    /// <summary>Sends chat messages, requesting a response matching the type <typeparamref name="T"/>.</summary>
    /// <param name="chatClient">The <see cref="IChatClient"/>.</param>
    /// <param name="messages">The chat content to send.</param>
    /// <param name="options">The chat options to configure the request.</param>
    /// <param name="useJsonSchemaResponseFormat">
    /// <see langword="true" /> to set a JSON schema on the <see cref="ChatResponseFormat"/>; otherwise, <see langword="false" />. The default is <see langword="true" />.
    /// Using a JSON schema improves reliability if the underlying model supports native structured output with a schema, but might cause an error if the model does not support it.
    /// </param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The response messages generated by the client.</returns>
    /// <typeparam name="T">The type of structured output to request.</typeparam>
    public static Task<ChatResponse<T>> GetResponseAsync<T>(
        this IChatClient chatClient,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        bool? useJsonSchemaResponseFormat = null,
        CancellationToken cancellationToken = default) =>
        GetResponseAsync<T>(chatClient, messages, AIJsonUtilities.DefaultOptions, options, useJsonSchemaResponseFormat, cancellationToken);

    /// <summary>Sends a user chat text message, requesting a response matching the type <typeparamref name="T"/>.</summary>
    /// <param name="chatClient">The <see cref="IChatClient"/>.</param>
    /// <param name="chatMessage">The text content for the chat message to send.</param>
    /// <param name="options">The chat options to configure the request.</param>
    /// <param name="useJsonSchemaResponseFormat">
    /// <see langword="true" /> to set a JSON schema on the <see cref="ChatResponseFormat"/>; otherwise, <see langword="false" />.
    /// Using a JSON schema improves reliability if the underlying model supports native structured output with a schema, but might cause an error if the model does not support it.
    /// </param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The response messages generated by the client.</returns>
    /// <typeparam name="T">The type of structured output to request.</typeparam>
    /// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/quickstarts/structured-output">Request a response with structured output.</related>
    public static Task<ChatResponse<T>> GetResponseAsync<T>(
        this IChatClient chatClient,
        string chatMessage,
        ChatOptions? options = null,
        bool? useJsonSchemaResponseFormat = null,
        CancellationToken cancellationToken = default) =>
        GetResponseAsync<T>(chatClient, new ChatMessage(ChatRole.User, chatMessage), options, useJsonSchemaResponseFormat, cancellationToken);

    /// <summary>Sends a chat message, requesting a response matching the type <typeparamref name="T"/>.</summary>
    /// <param name="chatClient">The <see cref="IChatClient"/>.</param>
    /// <param name="chatMessage">The chat message to send.</param>
    /// <param name="options">The chat options to configure the request.</param>
    /// <param name="useJsonSchemaResponseFormat">
    /// <see langword="true" /> to set a JSON schema on the <see cref="ChatResponseFormat"/>; otherwise, <see langword="false" />. The default is <see langword="true" />.
    /// Using a JSON schema improves reliability if the underlying model supports native structured output with a schema, but might cause an error if the model does not support it.
    /// </param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The response messages generated by the client.</returns>
    /// <typeparam name="T">The type of structured output to request.</typeparam>
    public static Task<ChatResponse<T>> GetResponseAsync<T>(
        this IChatClient chatClient,
        ChatMessage chatMessage,
        ChatOptions? options = null,
        bool? useJsonSchemaResponseFormat = null,
        CancellationToken cancellationToken = default) =>
        GetResponseAsync<T>(chatClient, [chatMessage], options, useJsonSchemaResponseFormat, cancellationToken);

    /// <summary>Sends a user chat text message, requesting a response matching the type <typeparamref name="T"/>.</summary>
    /// <param name="chatClient">The <see cref="IChatClient"/>.</param>
    /// <param name="chatMessage">The text content for the chat message to send.</param>
    /// <param name="serializerOptions">The JSON serialization options to use.</param>
    /// <param name="options">The chat options to configure the request.</param>
    /// <param name="useJsonSchemaResponseFormat">
    /// <see langword="true" /> to set a JSON schema on the <see cref="ChatResponseFormat"/>; otherwise, <see langword="false" />. The default is <see langword="true" />.
    /// Using a JSON schema improves reliability if the underlying model supports native structured output with a schema, but might cause an error if the model does not support it.
    /// </param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The response messages generated by the client.</returns>
    /// <typeparam name="T">The type of structured output to request.</typeparam>
    public static Task<ChatResponse<T>> GetResponseAsync<T>(
        this IChatClient chatClient,
        string chatMessage,
        JsonSerializerOptions serializerOptions,
        ChatOptions? options = null,
        bool? useJsonSchemaResponseFormat = null,
        CancellationToken cancellationToken = default) =>
        GetResponseAsync<T>(chatClient, new ChatMessage(ChatRole.User, chatMessage), serializerOptions, options, useJsonSchemaResponseFormat, cancellationToken);

    /// <summary>Sends a chat message, requesting a response matching the type <typeparamref name="T"/>.</summary>
    /// <param name="chatClient">The <see cref="IChatClient"/>.</param>
    /// <param name="chatMessage">The chat message to send.</param>
    /// <param name="serializerOptions">The JSON serialization options to use.</param>
    /// <param name="options">The chat options to configure the request.</param>
    /// <param name="useJsonSchemaResponseFormat">
    /// <see langword="true" /> to set a JSON schema on the <see cref="ChatResponseFormat"/>; otherwise, <see langword="false" />. The default is <see langword="true" />.
    /// Using a JSON schema improves reliability if the underlying model supports native structured output with a schema, but might cause an error if the model does not support it.
    /// </param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The response messages generated by the client.</returns>
    /// <typeparam name="T">The type of structured output to request.</typeparam>
    public static Task<ChatResponse<T>> GetResponseAsync<T>(
        this IChatClient chatClient,
        ChatMessage chatMessage,
        JsonSerializerOptions serializerOptions,
        ChatOptions? options = null,
        bool? useJsonSchemaResponseFormat = null,
        CancellationToken cancellationToken = default) =>
        GetResponseAsync<T>(chatClient, [chatMessage], serializerOptions, options, useJsonSchemaResponseFormat, cancellationToken);

    /// <summary>Sends chat messages, requesting a response matching the type <typeparamref name="T"/>.</summary>
    /// <param name="chatClient">The <see cref="IChatClient"/>.</param>
    /// <param name="messages">The chat content to send.</param>
    /// <param name="serializerOptions">The JSON serialization options to use.</param>
    /// <param name="options">The chat options to configure the request.</param>
    /// <param name="useJsonSchemaResponseFormat">
    /// <see langword="true" /> to set a JSON schema on the <see cref="ChatResponseFormat"/>; otherwise, <see langword="false" />. The default is <see langword="true" />.
    /// Using a JSON schema improves reliability if the underlying model supports native structured output with a schema, but might cause an error if the model does not support it.
    /// </param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The response messages generated by the client.</returns>
    /// <typeparam name="T">The type of structured output to request.</typeparam>
    /// <exception cref="ArgumentNullException"><paramref name="chatClient"/> or <paramref name="messages"/> or <paramref name="serializerOptions"/> is <see langword="null"/>.</exception>
    public static async Task<ChatResponse<T>> GetResponseAsync<T>(
        this IChatClient chatClient,
        IEnumerable<ChatMessage> messages,
        JsonSerializerOptions serializerOptions,
        ChatOptions? options = null,
        bool? useJsonSchemaResponseFormat = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatClient);
        _ = Throw.IfNull(messages);
        _ = Throw.IfNull(serializerOptions);

        serializerOptions.MakeReadOnly();

        var schemaElement = AIJsonUtilities.CreateJsonSchema(
            type: typeof(T),
            serializerOptions: serializerOptions,
            inferenceOptions: _inferenceOptions);

        bool isWrappedInObject;
        JsonElement schema;
        if (SchemaRepresentsObject(schemaElement))
        {
            // For object-representing schemas, we can use them as-is
            isWrappedInObject = false;
            schema = schemaElement;
        }
        else
        {
            // For non-object-representing schemas, we wrap them in an object schema, because all
            // the real LLM providers today require an object schema as the root. This is currently
            // true even for providers that support native structured output.
            isWrappedInObject = true;
            schema = JsonSerializer.SerializeToElement(new JsonObject
            {
                { "$schema", "https://json-schema.org/draft/2020-12/schema" },
                { "type", "object" },
                { "properties", new JsonObject { { "data", JsonElementToJsonNode(schemaElement) } } },
                { "additionalProperties", false },
                { "required", new JsonArray("data") },
            }, AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(JsonObject)));
        }

        ChatMessage? promptAugmentation = null;
        options = options is not null ? options.Clone() : new();

        // We default to assuming that models support JSON schema because developers will normally use
        // GetResponseAsync<T> only with models that do. If the model doesn't support JSON schema, it may
        // throw or it may ignore the schema. In these cases developers should pass useJsonSchemaResponseFormat: false.
        if (useJsonSchemaResponseFormat ?? true)
        {
            // When using native structured output, we don't add any additional prompt, because
            // the LLM backend is meant to do whatever's needed to explain the schema to the LLM.
            options.ResponseFormat = ChatResponseFormat.ForJsonSchema(
                schema,
                schemaName: SanitizeMemberName(typeof(T).Name),
                schemaDescription: typeof(T).GetCustomAttribute<DescriptionAttribute>()?.Description);
        }
        else
        {
            options.ResponseFormat = ChatResponseFormat.Json;

            // When not using native JSON schema, augment the chat messages with a schema prompt
            promptAugmentation = new ChatMessage(ChatRole.User, $$"""
                Respond with a JSON value conforming to the following schema:
                ```
                {{schema}}
                ```
                """);

            messages = [.. messages, promptAugmentation];
        }

        var result = await chatClient.GetResponseAsync(messages, options, cancellationToken);
        return new ChatResponse<T>(result, serializerOptions) { IsWrappedInObject = isWrappedInObject };
    }

    private static bool SchemaRepresentsObject(JsonElement schemaElement)
    {
        if (schemaElement.ValueKind is JsonValueKind.Object)
        {
            foreach (var property in schemaElement.EnumerateObject())
            {
                if (property.NameEquals("type"u8))
                {
                    return property.Value.ValueKind == JsonValueKind.String
                        && property.Value.ValueEquals("object"u8);
                }
            }
        }

        return false;
    }

    private static JsonNode? JsonElementToJsonNode(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.Array => JsonArray.Create(element),
            JsonValueKind.Object => JsonObject.Create(element),
            _ => JsonValue.Create(element)
        };
    }

    /// <summary>
    /// Removes characters from a .NET member name that shouldn't be used in an AI function name.
    /// </summary>
    /// <param name="memberName">The .NET member name that should be sanitized.</param>
    /// <returns>
    /// Replaces non-alphanumeric characters in the identifier with the underscore character.
    /// Primarily intended to remove characters produced by compiler-generated method name mangling.
    /// </returns>
    private static string SanitizeMemberName(string memberName) =>
        InvalidNameCharsRegex().Replace(memberName, "_");

    /// <summary>Regex that flags any character other than ASCII digits or letters or the underscore.</summary>
#if NET
    [GeneratedRegex("[^0-9A-Za-z_]")]
    private static partial Regex InvalidNameCharsRegex();
#else
    private static Regex InvalidNameCharsRegex() => _invalidNameCharsRegex;
    private static readonly Regex _invalidNameCharsRegex = new("[^0-9A-Za-z_]", RegexOptions.Compiled);
#endif
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/ChatCompletion/ChatResponse{T}.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the response to a chat request with structured output.</summary>
/// <typeparam name="T">The type of value expected from the chat response.</typeparam>
/// <remarks>
/// Language models are not guaranteed to honor the requested schema. If the model's output is not
/// parseable as the expected type, then <see cref="TryGetResult(out T)"/> will return <see langword="false"/>.
/// You can access the underlying JSON response on the <see cref="ChatResponse.Text"/> property.
/// </remarks>
public class ChatResponse<T> : ChatResponse
{
    private static readonly JsonReaderOptions _allowMultipleValuesJsonReaderOptions = new()
    {
#if NET9_0_OR_GREATER
        AllowMultipleValues = true
#endif
    };
    private readonly JsonSerializerOptions _serializerOptions;

    private T? _deserializedResult;
    private bool _hasDeserializedResult;

    /// <summary>Initializes a new instance of the <see cref="ChatResponse{T}"/> class.</summary>
    /// <param name="response">The unstructured <see cref="ChatResponse"/> that is being wrapped.</param>
    /// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the result.</param>
    public ChatResponse(ChatResponse response, JsonSerializerOptions serializerOptions)
        : base(Throw.IfNull(response).Messages)
    {
        _serializerOptions = Throw.IfNull(serializerOptions);
        AdditionalProperties = response.AdditionalProperties;
        ConversationId = response.ConversationId;
        CreatedAt = response.CreatedAt;
        FinishReason = response.FinishReason;
        ModelId = response.ModelId;
        RawRepresentation = response.RawRepresentation;
        ResponseId = response.ResponseId;
        Usage = response.Usage;
    }

    /// <summary>
    /// Gets the result value of the chat response as an instance of <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// If the response did not contain JSON, or if deserialization fails, this property will throw.
    /// To avoid exceptions, use <see cref="TryGetResult(out T)"/> instead.
    /// </remarks>
    public T Result
    {
        get
        {
            var result = GetResultCore(out var failureReason);
            return failureReason switch
            {
                FailureReason.ResultDidNotContainJson => throw new InvalidOperationException("The response did not contain JSON to be deserialized."),
                FailureReason.DeserializationProducedNull => throw new InvalidOperationException("The deserialized response is null."),
                FailureReason.ResultDidNotContainDataProperty => throw new InvalidOperationException("The response did not contain the expected 'data' property."),
                _ => result!,
            };
        }
    }

    /// <summary>
    /// Attempts to deserialize the result to produce an instance of <typeparamref name="T"/>.
    /// </summary>
    /// <param name="result">When this method returns, contains the result.</param>
    /// <returns><see langword="true"/> if the result was produced, otherwise <see langword="false"/>.</returns>
    public bool TryGetResult([NotNullWhen(true)] out T? result)
    {
        try
        {
            result = GetResultCore(out var failureReason);
            return failureReason is null;
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch
        {
            result = default;
            return false;
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    private static T? DeserializeFirstTopLevelObject(string json, JsonTypeInfo<T> typeInfo)
    {
        // We need to deserialize only the first top-level object as a workaround for a common LLM backend
        // issue. GPT 3.5 Turbo commonly returns multiple top-level objects after doing a function call.
        // See https://community.openai.com/t/2-json-objects-returned-when-using-function-calling-and-json-mode/574348
        var utf8ByteLength = Encoding.UTF8.GetByteCount(json);
        var buffer = ArrayPool<byte>.Shared.Rent(utf8ByteLength);
        try
        {
            var utf8SpanLength = Encoding.UTF8.GetBytes(json, 0, json.Length, buffer, 0);
            var utf8Span = new ReadOnlySpan<byte>(buffer, 0, utf8SpanLength);
            var reader = new Utf8JsonReader(utf8Span, _allowMultipleValuesJsonReaderOptions);
            return JsonSerializer.Deserialize(ref reader, typeInfo);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the JSON schema has an extra object wrapper.
    /// </summary>
    /// <remarks>
    /// The wrapper is required for any non-JSON-object-typed values such as numbers, enum values, and arrays.
    /// </remarks>
    internal bool IsWrappedInObject { get; set; }

    private T? GetResultCore(out FailureReason? failureReason)
    {
        if (_hasDeserializedResult)
        {
            failureReason = default;
            return _deserializedResult;
        }

        var json = Text;
        if (string.IsNullOrEmpty(json))
        {
            failureReason = FailureReason.ResultDidNotContainJson;
            return default;
        }

        T? deserialized = default;

        // If there's an exception here, we want it to propagate, since the Result property is meant to throw directly

        if (IsWrappedInObject)
        {
            if (JsonDocument.Parse(json!).RootElement.TryGetProperty("data", out var data))
            {
                json = data.GetRawText();
            }
            else
            {
                failureReason = FailureReason.ResultDidNotContainDataProperty;
                return default;
            }
        }

        deserialized = DeserializeFirstTopLevelObject(json!, (JsonTypeInfo<T>)_serializerOptions.GetTypeInfo(typeof(T)));

        if (deserialized is null)
        {
            failureReason = FailureReason.DeserializationProducedNull;
            return default;
        }

        _deserializedResult = deserialized;
        _hasDeserializedResult = true;
        failureReason = default;
        return deserialized;
    }

    private enum FailureReason
    {
        ResultDidNotContainJson,
        DeserializationProducedNull,
        ResultDidNotContainDataProperty,
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/ChatCompletion/ConfigureOptionsChatClient.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a delegating chat client that configures a <see cref="ChatOptions"/> instance used by the remainder of the pipeline.</summary>
public sealed class ConfigureOptionsChatClient : DelegatingChatClient
{
    /// <summary>The callback delegate used to configure options.</summary>
    private readonly Action<ChatOptions> _configureOptions;

    /// <summary>Initializes a new instance of the <see cref="ConfigureOptionsChatClient"/> class with the specified <paramref name="configure"/> callback.</summary>
    /// <param name="innerClient">The inner client.</param>
    /// <param name="configure">
    /// The delegate to invoke to configure the <see cref="ChatOptions"/> instance. It is passed a clone of the caller-supplied <see cref="ChatOptions"/> instance
    /// (or a newly constructed instance if the caller-supplied instance is <see langword="null"/>).
    /// </param>
    /// <remarks>
    /// The <paramref name="configure"/> delegate is passed either a new instance of <see cref="ChatOptions"/> if
    /// the caller didn't supply a <see cref="ChatOptions"/> instance, or a clone (via <see cref="ChatOptions.Clone"/> of the caller-supplied
    /// instance if one was supplied.
    /// </remarks>
    public ConfigureOptionsChatClient(IChatClient innerClient, Action<ChatOptions> configure)
        : base(innerClient)
    {
        _configureOptions = Throw.IfNull(configure);
    }

    /// <inheritdoc/>
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        await base.GetResponseAsync(messages, Configure(options), cancellationToken);

    /// <inheritdoc/>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in base.GetStreamingResponseAsync(messages, Configure(options), cancellationToken))
        {
            yield return update;
        }
    }

    /// <summary>Creates and configures the <see cref="ChatOptions"/> to pass along to the inner client.</summary>
    private ChatOptions Configure(ChatOptions? options)
    {
        options = options?.Clone() ?? new();

        _configureOptions(options);

        return options;
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/ChatCompletion/ConfigureOptionsChatClientBuilderExtensions.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;

#pragma warning disable SA1629 // Documentation text should end with a period

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="ConfigureOptionsChatClient"/> instances.</summary>
public static class ConfigureOptionsChatClientBuilderExtensions
{
    /// <summary>
    /// Adds a callback that configures a <see cref="ChatOptions"/> to be passed to the next client in the pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="ChatClientBuilder"/>.</param>
    /// <param name="configure">
    /// The delegate to invoke to configure the <see cref="ChatOptions"/> instance.
    /// It is passed a clone of the caller-supplied <see cref="ChatOptions"/> instance (or a newly constructed instance if the caller-supplied instance is <see langword="null"/>).
    /// </param>
    /// <remarks>
    /// This method can be used to set default options. The <paramref name="configure"/> delegate is passed either a new instance of
    /// <see cref="ChatOptions"/> if the caller didn't supply a <see cref="ChatOptions"/> instance, or a clone (via <see cref="ChatOptions.Clone"/>)
    /// of the caller-supplied instance if one was supplied.
    /// </remarks>
    /// <returns>The <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai#provide-options">Provide options.</related>
    public static ChatClientBuilder ConfigureOptions(
        this ChatClientBuilder builder, Action<ChatOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.Use(innerClient => new ConfigureOptionsChatClient(innerClient, configure));
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/ChatCompletion/DistributedCachingChatClient.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S109 // Magic numbers should not be used
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1502 // Element should not be on a single line

namespace Microsoft.Extensions.AI;

/// <summary>
/// A delegating chat client that caches the results of response calls, storing them as JSON in an <see cref="IDistributedCache"/>.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="DistributedCachingChatClient"/> employs JSON serialization as part of storing cached data. It is not guaranteed that
/// the object models used by <see cref="ChatMessage"/>, <see cref="ChatOptions"/>, <see cref="ChatResponse"/>, <see cref="ChatResponseUpdate"/>,
/// or any of the other objects in the chat client pipeline will roundtrip through JSON serialization with full fidelity. For example,
/// <see cref="ChatMessage.RawRepresentation"/> will be ignored, and <see cref="object"/> values in <see cref="ChatMessage.AdditionalProperties"/>
/// will deserialize as <see cref="JsonElement"/> rather than as the original type. In general, code using <see cref="DistributedCachingChatClient"/>
/// should only rely on accessing data that can be preserved well enough through JSON serialization and deserialization.
/// </para>
/// <para>
/// The provided implementation of <see cref="IChatClient"/> is thread-safe for concurrent use so long as the employed
/// <see cref="IDistributedCache"/> is similarly thread-safe for concurrent use.
/// </para>
/// </remarks>
public class DistributedCachingChatClient : CachingChatClient
{
    /// <summary>The <see cref="IDistributedCache"/> instance that will be used as the backing store for the cache.</summary>
    private readonly IDistributedCache _storage;

    /// <summary>The <see cref="JsonSerializerOptions"/> to use when serializing cache data.</summary>
    private JsonSerializerOptions _jsonSerializerOptions = AIJsonUtilities.DefaultOptions;

    /// <summary>Initializes a new instance of the <see cref="DistributedCachingChatClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="IChatClient"/>.</param>
    /// <param name="storage">An <see cref="IDistributedCache"/> instance that will be used as the backing store for the cache.</param>
    public DistributedCachingChatClient(IChatClient innerClient, IDistributedCache storage)
        : base(innerClient)
    {
        _storage = Throw.IfNull(storage);
    }

    /// <summary>Gets or sets JSON serialization options to use when serializing cache data.</summary>
    public JsonSerializerOptions JsonSerializerOptions
    {
        get => _jsonSerializerOptions;
        set => _jsonSerializerOptions = Throw.IfNull(value);
    }

    /// <inheritdoc />
    protected override async Task<ChatResponse?> ReadCacheAsync(string key, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(key);
        _jsonSerializerOptions.MakeReadOnly();

        if (await _storage.GetAsync(key, cancellationToken) is byte[] existingJson)
        {
            return (ChatResponse?)JsonSerializer.Deserialize(existingJson, _jsonSerializerOptions.GetTypeInfo(typeof(ChatResponse)));
        }

        return null;
    }

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<ChatResponseUpdate>?> ReadCacheStreamingAsync(string key, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(key);
        _jsonSerializerOptions.MakeReadOnly();

        if (await _storage.GetAsync(key, cancellationToken) is byte[] existingJson)
        {
            return (IReadOnlyList<ChatResponseUpdate>?)JsonSerializer.Deserialize(existingJson, _jsonSerializerOptions.GetTypeInfo(typeof(IReadOnlyList<ChatResponseUpdate>)));
        }

        return null;
    }

    /// <inheritdoc />
    protected override async Task WriteCacheAsync(string key, ChatResponse value, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(key);
        _ = Throw.IfNull(value);
        _jsonSerializerOptions.MakeReadOnly();

        var newJson = JsonSerializer.SerializeToUtf8Bytes(value, _jsonSerializerOptions.GetTypeInfo(typeof(ChatResponse)));
        await _storage.SetAsync(key, newJson, cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task WriteCacheStreamingAsync(string key, IReadOnlyList<ChatResponseUpdate> value, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(key);
        _ = Throw.IfNull(value);
        _jsonSerializerOptions.MakeReadOnly();

        var newJson = JsonSerializer.SerializeToUtf8Bytes(value, _jsonSerializerOptions.GetTypeInfo(typeof(IReadOnlyList<ChatResponseUpdate>)));
        await _storage.SetAsync(key, newJson, cancellationToken);
    }

    /// <summary>Computes a cache key for the specified values.</summary>
    /// <param name="messages">The messages to inform the key.</param>
    /// <param name="options">The <see cref="ChatOptions"/> to inform the key.</param>
    /// <param name="additionalValues">Any other values to inform the key.</param>
    /// <returns>The computed key.</returns>
    /// <remarks>
    /// <para>
    /// The <paramref name="messages"/>, <paramref name="options"/>, and <paramref name="additionalValues"/> are serialized to JSON using <see cref="JsonSerializerOptions"/>
    /// in order to compute the key.
    /// </para>
    /// <para>
    /// The generated cache key is not guaranteed to be stable across releases of the library.
    /// </para>
    /// </remarks>
    protected override string GetCacheKey(IEnumerable<ChatMessage> messages, ChatOptions? options, params ReadOnlySpan<object?> additionalValues)
    {
        // Bump the cache version to invalidate existing caches if the serialization format changes in a breaking way.
        const int CacheVersion = 2;

        return AIJsonUtilities.HashDataToString([CacheVersion, messages, options, .. additionalValues], _jsonSerializerOptions);
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/ChatCompletion/DistributedCachingChatClientBuilderExtensions.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Extension methods for adding a <see cref="DistributedCachingChatClient"/> to an <see cref="IChatClient"/> pipeline.
/// </summary>
public static class DistributedCachingChatClientBuilderExtensions
{
    /// <summary>
    /// Adds a <see cref="DistributedCachingChatClient"/> as the next stage in the pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="ChatClientBuilder"/>.</param>
    /// <param name="storage">
    /// An optional <see cref="IDistributedCache"/> instance that will be used as the backing store for the cache. If not supplied, an instance will be resolved from the service provider.
    /// </param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="DistributedCachingChatClient"/> instance.</param>
    /// <returns>The <see cref="ChatClientBuilder"/> provided as <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The <see cref="DistributedCachingChatClient"/> employs JSON serialization as part of storing the cached data. It is not guaranteed that
    /// the object models used by <see cref="ChatMessage"/>, <see cref="ChatOptions"/>, <see cref="ChatResponse"/>, <see cref="ChatResponseUpdate"/>,
    /// or any of the other objects in the chat client pipeline will roundtrip through JSON serialization with full fidelity. For example,
    /// <see cref="ChatMessage.RawRepresentation"/> will be ignored, and <see cref="object"/> values in <see cref="ChatMessage.AdditionalProperties"/>
    /// will deserialize as <see cref="JsonElement"/> rather than as the original type. In general, code using <see cref="DistributedCachingChatClient"/>
    /// should only rely on accessing data that can be preserved well enough through JSON serialization and deserialization.
    /// </remarks>
    public static ChatClientBuilder UseDistributedCache(this ChatClientBuilder builder, IDistributedCache? storage = null, Action<DistributedCachingChatClient>? configure = null)
    {
        _ = Throw.IfNull(builder);
        return builder.Use((innerClient, services) =>
        {
            storage ??= services.GetRequiredService<IDistributedCache>();
            var chatClient = new DistributedCachingChatClient(innerClient, storage);
            configure?.Invoke(chatClient);
            return chatClient;
        });
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/ChatCompletion/FunctionInvocationContext.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Shared.Collections;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides context for an in-flight function invocation.</summary>
public class FunctionInvocationContext
{
    /// <summary>
    /// A nop function used to allow <see cref="Function"/> to be non-nullable. Default instances of
    /// <see cref="FunctionInvocationContext"/> start with this as the target function.
    /// </summary>
    private static readonly AIFunction _nopFunction = AIFunctionFactory.Create(() => { }, nameof(FunctionInvocationContext));

    /// <summary>The chat contents associated with the operation that initiated this function call request.</summary>
    private IList<ChatMessage> _messages = Array.Empty<ChatMessage>();

    /// <summary>The AI function to be invoked.</summary>
    private AIFunction _function = _nopFunction;

    /// <summary>The function call content information associated with this invocation.</summary>
    private FunctionCallContent? _callContent;

    /// <summary>The arguments used with the function.</summary>
    private AIFunctionArguments? _arguments;

    /// <summary>Initializes a new instance of the <see cref="FunctionInvocationContext"/> class.</summary>
    public FunctionInvocationContext()
    {
    }

    /// <summary>Gets or sets the AI function to be invoked.</summary>
    public AIFunction Function
    {
        get => _function;
        set => _function = Throw.IfNull(value);
    }

    /// <summary>Gets or sets the arguments associated with this invocation.</summary>
    public AIFunctionArguments Arguments
    {
        get => _arguments ??= [];
        set => _arguments = Throw.IfNull(value);
    }

    /// <summary>Gets or sets the function call content information associated with this invocation.</summary>
    public FunctionCallContent CallContent
    {
        get => _callContent ??= new(string.Empty, _nopFunction.Name, EmptyReadOnlyDictionary<string, object?>.Instance);
        set => _callContent = Throw.IfNull(value);
    }

    /// <summary>Gets or sets the chat contents associated with the operation that initiated this function call request.</summary>
    public IList<ChatMessage> Messages
    {
        get => _messages;
        set => _messages = Throw.IfNull(value);
    }

    /// <summary>Gets or sets the chat options associated with the operation that initiated this function call request.</summary>
    public ChatOptions? Options { get; set; }

    /// <summary>Gets or sets the number of this iteration with the underlying client.</summary>
    /// <remarks>
    /// The initial request to the client that passes along the chat contents provided to the <see cref="FunctionInvokingChatClient"/>
    /// is iteration 1. If the client responds with a function call request, the next request to the client is iteration 2, and so on.
    /// </remarks>
    public int Iteration { get; set; }

    /// <summary>Gets or sets the index of the function call within the iteration.</summary>
    /// <remarks>
    /// The response from the underlying client may include multiple function call requests.
    /// This index indicates the position of the function call within the iteration.
    /// </remarks>
    public int FunctionCallIndex { get; set; }

    /// <summary>Gets or sets the total number of function call requests within the iteration.</summary>
    /// <remarks>
    /// The response from the underlying client might include multiple function call requests.
    /// This count indicates how many there were.
    /// </remarks>
    public int FunctionCount { get; set; }

    /// <summary>Gets or sets a value indicating whether to terminate the request.</summary>
    /// <remarks>
    /// In response to a function call request, the function might be invoked, its result added to the chat contents,
    /// and a new request issued to the wrapped client. If this property is set to <see langword="true"/>, that subsequent request
    /// will not be issued and instead the loop immediately terminated rather than continuing until there are no
    /// more function call requests in responses.
    /// <para>
    /// If multiple function call requests are issued as part of a single iteration (a single response from the inner <see cref="IChatClient"/>),
    /// setting <see cref="Terminate" /> to <see langword="true" /> may also prevent subsequent requests within that same iteration from being processed.
    /// </para>
    /// </remarks>
    public bool Terminate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the function invocation is occurring as part of a
    /// <see cref="IChatClient.GetStreamingResponseAsync"/> call as opposed to a <see cref="IChatClient.GetResponseAsync"/> call.
    /// </summary>
    public bool IsStreaming { get; set; }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/ChatCompletion/FunctionInvokingChatClient.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Shared.Diagnostics;

#pragma warning disable CA2213 // Disposable fields should be disposed
#pragma warning disable EA0002 // Use 'System.TimeProvider' to make the code easier to test
#pragma warning disable SA1202 // 'protected' members should come before 'private' members
#pragma warning disable S107 // Methods should not have too many parameters

namespace Microsoft.Extensions.AI;

/// <summary>
/// A delegating chat client that invokes functions defined on <see cref="ChatOptions"/>.
/// Include this in a chat pipeline to resolve function calls automatically.
/// </summary>
/// <remarks>
/// <para>
/// When this client receives a <see cref="FunctionCallContent"/> in a chat response, it responds
/// by calling the corresponding <see cref="AIFunction"/> defined in <see cref="ChatOptions.Tools"/>,
/// producing a <see cref="FunctionResultContent"/> that it sends back to the inner client. This loop
/// is repeated until there are no more function calls to make, or until another stop condition is met,
/// such as hitting <see cref="MaximumIterationsPerRequest"/>.
/// </para>
/// <para>
/// The provided implementation of <see cref="IChatClient"/> is thread-safe for concurrent use so long as the
/// <see cref="AIFunction"/> instances employed as part of the supplied <see cref="ChatOptions"/> are also safe.
/// The <see cref="AllowConcurrentInvocation"/> property can be used to control whether multiple function invocation
/// requests as part of the same request are invocable concurrently, but even with that set to <see langword="false"/>
/// (the default), multiple concurrent requests to this same instance and using the same tools could result in those
/// tools being used concurrently (one per request). For example, a function that accesses the HttpContext of a specific
/// ASP.NET web request should only be used as part of a single <see cref="ChatOptions"/> at a time, and only with
/// <see cref="AllowConcurrentInvocation"/> set to <see langword="false"/>, in case the inner client decided to issue multiple
/// invocation requests to that same function.
/// </para>
/// </remarks>
public partial class FunctionInvokingChatClient : DelegatingChatClient
{
    /// <summary>The <see cref="FunctionInvocationContext"/> for the current function invocation.</summary>
    private static readonly AsyncLocal<FunctionInvocationContext?> _currentContext = new();

    /// <summary>Gets the <see cref="IServiceProvider"/> specified when constructing the <see cref="FunctionInvokingChatClient"/>, if any.</summary>
    protected IServiceProvider? FunctionInvocationServices { get; }

    /// <summary>The logger to use for logging information about function invocation.</summary>
    private readonly ILogger _logger;

    /// <summary>The <see cref="ActivitySource"/> to use for telemetry.</summary>
    /// <remarks>This component does not own the instance and should not dispose it.</remarks>
    private readonly ActivitySource? _activitySource;

    /// <summary>Maximum number of roundtrips allowed to the inner client.</summary>
    private int _maximumIterationsPerRequest = 10;

    /// <summary>Maximum number of consecutive iterations that are allowed contain at least one exception result. If the limit is exceeded, we rethrow the exception instead of continuing.</summary>
    private int _maximumConsecutiveErrorsPerRequest = 3;

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionInvokingChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The underlying <see cref="IChatClient"/>, or the next instance in a chain of clients.</param>
    /// <param name="loggerFactory">An <see cref="ILoggerFactory"/> to use for logging information about function invocation.</param>
    /// <param name="functionInvocationServices">An optional <see cref="IServiceProvider"/> to use for resolving services required by the <see cref="AIFunction"/> instances being invoked.</param>
    public FunctionInvokingChatClient(IChatClient innerClient, ILoggerFactory? loggerFactory = null, IServiceProvider? functionInvocationServices = null)
        : base(innerClient)
    {
        _logger = (ILogger?)loggerFactory?.CreateLogger<FunctionInvokingChatClient>() ?? NullLogger.Instance;
        _activitySource = innerClient.GetService<ActivitySource>();
        FunctionInvocationServices = functionInvocationServices;
    }

    /// <summary>
    /// Gets or sets the <see cref="FunctionInvocationContext"/> for the current function invocation.
    /// </summary>
    /// <remarks>
    /// This value flows across async calls.
    /// </remarks>
    public static FunctionInvocationContext? CurrentContext
    {
        get => _currentContext.Value;
        protected set => _currentContext.Value = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether detailed exception information should be included
    /// in the chat history when calling the underlying <see cref="IChatClient"/>.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the full exception message is added to the chat history
    /// when calling the underlying <see cref="IChatClient"/>.
    /// <see langword="false"/> if a generic error message is included in the chat history.
    /// The default value is <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// <para>
    /// Setting the value to <see langword="false"/> prevents the underlying language model from disclosing
    /// raw exception details to the end user, since it doesn't receive that information. Even in this
    /// case, the raw <see cref="Exception"/> object is available to application code by inspecting
    /// the <see cref="FunctionResultContent.Exception"/> property.
    /// </para>
    /// <para>
    /// Setting the value to <see langword="true"/> can help the underlying <see cref="IChatClient"/> bypass problems on
    /// its own, for example by retrying the function call with different arguments. However it might
    /// result in disclosing the raw exception information to external users, which can be a security
    /// concern depending on the application scenario.
    /// </para>
    /// <para>
    /// Changing the value of this property while the client is in use might result in inconsistencies
    /// as to whether detailed errors are provided during an in-flight request.
    /// </para>
    /// </remarks>
    public bool IncludeDetailedErrors { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to allow concurrent invocation of functions.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if multiple function calls can execute in parallel.
    /// <see langword="false"/> if function calls are processed serially.
    /// The default value is <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// An individual response from the inner client might contain multiple function call requests.
    /// By default, such function calls are processed serially. Set <see cref="AllowConcurrentInvocation"/> to
    /// <see langword="true"/> to enable concurrent invocation such that multiple function calls can execute in parallel.
    /// </remarks>
    public bool AllowConcurrentInvocation { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of iterations per request.
    /// </summary>
    /// <value>
    /// The maximum number of iterations per request.
    /// The default value is 10.
    /// </value>
    /// <remarks>
    /// <para>
    /// Each request to this <see cref="FunctionInvokingChatClient"/> might end up making
    /// multiple requests to the inner client. Each time the inner client responds with
    /// a function call request, this client might perform that invocation and send the results
    /// back to the inner client in a new request. This property limits the number of times
    /// such a roundtrip is performed. The value must be at least one, as it includes the initial request.
    /// </para>
    /// <para>
    /// Changing the value of this property while the client is in use might result in inconsistencies
    /// as to how many iterations are allowed for an in-flight request.
    /// </para>
    /// </remarks>
    public int MaximumIterationsPerRequest
    {
        get => _maximumIterationsPerRequest;
        set
        {
            if (value < 1)
            {
                Throw.ArgumentOutOfRangeException(nameof(value));
            }

            _maximumIterationsPerRequest = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of consecutive iterations that are allowed to fail with an error.
    /// </summary>
    /// <value>
    /// The maximum number of consecutive iterations that are allowed to fail with an error.
    /// The default value is 3.
    /// </value>
    /// <remarks>
    /// <para>
    /// When function invocations fail with an exception, the <see cref="FunctionInvokingChatClient"/>
    /// continues to make requests to the inner client, optionally supplying exception information (as
    /// controlled by <see cref="IncludeDetailedErrors"/>). This allows the <see cref="IChatClient"/> to
    /// recover from errors by trying other function parameters that may succeed.
    /// </para>
    /// <para>
    /// However, in case function invocations continue to produce exceptions, this property can be used to
    /// limit the number of consecutive failing attempts. When the limit is reached, the exception will be
    /// rethrown to the caller.
    /// </para>
    /// <para>
    /// If the value is set to zero, all function calling exceptions immediately terminate the function
    /// invocation loop and the exception will be rethrown to the caller.
    /// </para>
    /// <para>
    /// Changing the value of this property while the client is in use might result in inconsistencies
    /// as to how many iterations are allowed for an in-flight request.
    /// </para>
    /// </remarks>
    public int MaximumConsecutiveErrorsPerRequest
    {
        get => _maximumConsecutiveErrorsPerRequest;
        set => _maximumConsecutiveErrorsPerRequest = Throw.IfLessThan(value, 0);
    }

    /// <inheritdoc/>
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        // A single request into this GetResponseAsync may result in multiple requests to the inner client.
        // Create an activity to group them together for better observability.
        using Activity? activity = _activitySource?.StartActivity($"{nameof(FunctionInvokingChatClient)}.{nameof(GetResponseAsync)}");

        // Copy the original messages in order to avoid enumerating the original messages multiple times.
        // The IEnumerable can represent an arbitrary amount of work.
        List<ChatMessage> originalMessages = [.. messages];
        messages = originalMessages;

        List<ChatMessage>? augmentedHistory = null; // the actual history of messages sent on turns other than the first
        ChatResponse? response = null; // the response from the inner client, which is possibly modified and then eventually returned
        List<ChatMessage>? responseMessages = null; // tracked list of messages, across multiple turns, to be used for the final response
        UsageDetails? totalUsage = null; // tracked usage across all turns, to be used for the final response
        List<FunctionCallContent>? functionCallContents = null; // function call contents that need responding to in the current turn
        bool lastIterationHadConversationId = false; // whether the last iteration's response had a ConversationId set
        int consecutiveErrorCount = 0;

        for (int iteration = 0; ; iteration++)
        {
            functionCallContents?.Clear();

            // Make the call to the inner client.
            response = await base.GetResponseAsync(messages, options, cancellationToken);
            if (response is null)
            {
                Throw.InvalidOperationException($"The inner {nameof(IChatClient)} returned a null {nameof(ChatResponse)}.");
            }

            // Any function call work to do? If yes, ensure we're tracking that work in functionCallContents.
            bool requiresFunctionInvocation =
                options?.Tools is { Count: > 0 } &&
                iteration < MaximumIterationsPerRequest &&
                CopyFunctionCalls(response.Messages, ref functionCallContents);

            // In a common case where we make a request and there's no function calling work required,
            // fast path out by just returning the original response.
            if (iteration == 0 && !requiresFunctionInvocation)
            {
                return response;
            }

            // Track aggregate details from the response, including all of the response messages and usage details.
            (responseMessages ??= []).AddRange(response.Messages);
            if (response.Usage is not null)
            {
                if (totalUsage is not null)
                {
                    totalUsage.Add(response.Usage);
                }
                else
                {
                    totalUsage = response.Usage;
                }
            }

            // If there are no tools to call, or for any other reason we should stop, we're done.
            // Break out of the loop and allow the handling at the end to configure the response
            // with aggregated data from previous requests.
            if (!requiresFunctionInvocation)
            {
                break;
            }

            // Prepare the history for the next iteration.
            FixupHistories(originalMessages, ref messages, ref augmentedHistory, response, responseMessages, ref lastIterationHadConversationId);

            // Add the responses from the function calls into the augmented history and also into the tracked
            // list of response messages.
            var modeAndMessages = await ProcessFunctionCallsAsync(augmentedHistory, options!, functionCallContents!, iteration, consecutiveErrorCount, isStreaming: false, cancellationToken);
            responseMessages.AddRange(modeAndMessages.MessagesAdded);
            consecutiveErrorCount = modeAndMessages.NewConsecutiveErrorCount;

            if (modeAndMessages.ShouldTerminate)
            {
                break;
            }

            UpdateOptionsForNextIteration(ref options!, response.ConversationId);
        }

        Debug.Assert(responseMessages is not null, "Expected to only be here if we have response messages.");
        response.Messages = responseMessages!;
        response.Usage = totalUsage;

        AddUsageTags(activity, totalUsage);

        return response;
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        // A single request into this GetStreamingResponseAsync may result in multiple requests to the inner client.
        // Create an activity to group them together for better observability.
        using Activity? activity = _activitySource?.StartActivity($"{nameof(FunctionInvokingChatClient)}.{nameof(GetStreamingResponseAsync)}");
        UsageDetails? totalUsage = activity is { IsAllDataRequested: true } ? new() : null; // tracked usage across all turns, to be used for activity purposes

        // Copy the original messages in order to avoid enumerating the original messages multiple times.
        // The IEnumerable can represent an arbitrary amount of work.
        List<ChatMessage> originalMessages = [.. messages];
        messages = originalMessages;

        List<ChatMessage>? augmentedHistory = null; // the actual history of messages sent on turns other than the first
        List<FunctionCallContent>? functionCallContents = null; // function call contents that need responding to in the current turn
        List<ChatMessage>? responseMessages = null; // tracked list of messages, across multiple turns, to be used in fallback cases to reconstitute history
        bool lastIterationHadConversationId = false; // whether the last iteration's response had a ConversationId set
        List<ChatResponseUpdate> updates = []; // updates from the current response
        int consecutiveErrorCount = 0;

        for (int iteration = 0; ; iteration++)
        {
            updates.Clear();
            functionCallContents?.Clear();

            await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
            {
                if (update is null)
                {
                    Throw.InvalidOperationException($"The inner {nameof(IChatClient)} streamed a null {nameof(ChatResponseUpdate)}.");
                }

                updates.Add(update);

                _ = CopyFunctionCalls(update.Contents, ref functionCallContents);

                if (totalUsage is not null)
                {
                    IList<AIContent> contents = update.Contents;
                    int contentsCount = contents.Count;
                    for (int i = 0; i < contentsCount; i++)
                    {
                        if (contents[i] is UsageContent uc)
                        {
                            totalUsage.Add(uc.Details);
                        }
                    }
                }

                yield return update;
                Activity.Current = activity; // workaround for https://github.com/dotnet/runtime/issues/47802
            }

            // If there are no tools to call, or for any other reason we should stop, return the response.
            if (functionCallContents is not { Count: > 0 } ||
                options?.Tools is not { Count: > 0 } ||
                iteration >= _maximumIterationsPerRequest)
            {
                break;
            }

            // Reconstitute a response from the response updates.
            var response = updates.ToChatResponse();
            (responseMessages ??= []).AddRange(response.Messages);

            // Prepare the history for the next iteration.
            FixupHistories(originalMessages, ref messages, ref augmentedHistory, response, responseMessages, ref lastIterationHadConversationId);

            // Process all of the functions, adding their results into the history.
            var modeAndMessages = await ProcessFunctionCallsAsync(augmentedHistory, options, functionCallContents, iteration, consecutiveErrorCount, isStreaming: true, cancellationToken);
            responseMessages.AddRange(modeAndMessages.MessagesAdded);
            consecutiveErrorCount = modeAndMessages.NewConsecutiveErrorCount;

            // This is a synthetic ID since we're generating the tool messages instead of getting them from
            // the underlying provider. When emitting the streamed chunks, it's perfectly valid for us to
            // use the same message ID for all of them within a given iteration, as this is a single logical
            // message with multiple content items. We could also use different message IDs per tool content,
            // but there's no benefit to doing so.
            string toolResponseId = Guid.NewGuid().ToString("N");

            // Stream any generated function results. This mirrors what's done for GetResponseAsync, where the returned messages
            // includes all activities, including generated function results.
            foreach (var message in modeAndMessages.MessagesAdded)
            {
                var toolResultUpdate = new ChatResponseUpdate
                {
                    AdditionalProperties = message.AdditionalProperties,
                    AuthorName = message.AuthorName,
                    ConversationId = response.ConversationId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    Contents = message.Contents,
                    RawRepresentation = message.RawRepresentation,
                    ResponseId = toolResponseId,
                    MessageId = toolResponseId, // See above for why this can be the same as ResponseId
                    Role = message.Role,
                };

                yield return toolResultUpdate;
                Activity.Current = activity; // workaround for https://github.com/dotnet/runtime/issues/47802
            }

            if (modeAndMessages.ShouldTerminate)
            {
                break;
            }

            UpdateOptionsForNextIteration(ref options, response.ConversationId);
        }

        AddUsageTags(activity, totalUsage);
    }

    /// <summary>Adds tags to <paramref name="activity"/> for usage details in <paramref name="usage"/>.</summary>
    private static void AddUsageTags(Activity? activity, UsageDetails? usage)
    {
        if (usage is not null && activity is { IsAllDataRequested: true })
        {
            if (usage.InputTokenCount is long inputTokens)
            {
                _ = activity.AddTag(OpenTelemetryConsts.GenAI.Usage.InputTokens, (int)inputTokens);
            }

            if (usage.OutputTokenCount is long outputTokens)
            {
                _ = activity.AddTag(OpenTelemetryConsts.GenAI.Usage.OutputTokens, (int)outputTokens);
            }
        }
    }

    /// <summary>Prepares the various chat message lists after a response from the inner client and before invoking functions.</summary>
    /// <param name="originalMessages">The original messages provided by the caller.</param>
    /// <param name="messages">The messages reference passed to the inner client.</param>
    /// <param name="augmentedHistory">The augmented history containing all the messages to be sent.</param>
    /// <param name="response">The most recent response being handled.</param>
    /// <param name="allTurnsResponseMessages">A list of all response messages received up until this point.</param>
    /// <param name="lastIterationHadConversationId">Whether the previous iteration's response had a conversation ID.</param>
    private static void FixupHistories(
        IEnumerable<ChatMessage> originalMessages,
        ref IEnumerable<ChatMessage> messages,
        [NotNull] ref List<ChatMessage>? augmentedHistory,
        ChatResponse response,
        List<ChatMessage> allTurnsResponseMessages,
        ref bool lastIterationHadConversationId)
    {
        // We're now going to need to augment the history with function result contents.
        // That means we need a separate list to store the augmented history.
        if (response.ConversationId is not null)
        {
            // The response indicates the inner client is tracking the history, so we don't want to send
            // anything we've already sent or received.
            if (augmentedHistory is not null)
            {
                augmentedHistory.Clear();
            }
            else
            {
                augmentedHistory = [];
            }

            lastIterationHadConversationId = true;
        }
        else if (lastIterationHadConversationId)
        {
            // In the very rare case where the inner client returned a response with a conversation ID but then
            // returned a subsequent response without one, we want to reconstitute the full history. To do that,
            // we can populate the history with the original chat messages and then all of the response
            // messages up until this point, which includes the most recent ones.
            augmentedHistory ??= [];
            augmentedHistory.Clear();
            augmentedHistory.AddRange(originalMessages);
            augmentedHistory.AddRange(allTurnsResponseMessages);

            lastIterationHadConversationId = false;
        }
        else
        {
            // If augmentedHistory is already non-null, then we've already populated it with everything up
            // until this point (except for the most recent response). If it's null, we need to seed it with
            // the chat history provided by the caller.
            augmentedHistory ??= originalMessages.ToList();

            // Now add the most recent response messages.
            augmentedHistory.AddMessages(response);

            lastIterationHadConversationId = false;
        }

        // Use the augmented history as the new set of messages to send.
        messages = augmentedHistory;
    }

    /// <summary>Copies any <see cref="FunctionCallContent"/> from <paramref name="messages"/> to <paramref name="functionCalls"/>.</summary>
    private static bool CopyFunctionCalls(
        IList<ChatMessage> messages, [NotNullWhen(true)] ref List<FunctionCallContent>? functionCalls)
    {
        bool any = false;
        int count = messages.Count;
        for (int i = 0; i < count; i++)
        {
            any |= CopyFunctionCalls(messages[i].Contents, ref functionCalls);
        }

        return any;
    }

    /// <summary>Copies any <see cref="FunctionCallContent"/> from <paramref name="content"/> to <paramref name="functionCalls"/>.</summary>
    private static bool CopyFunctionCalls(
        IList<AIContent> content, [NotNullWhen(true)] ref List<FunctionCallContent>? functionCalls)
    {
        bool any = false;
        int count = content.Count;
        for (int i = 0; i < count; i++)
        {
            if (content[i] is FunctionCallContent functionCall)
            {
                (functionCalls ??= []).Add(functionCall);
                any = true;
            }
        }

        return any;
    }

    private static void UpdateOptionsForNextIteration(ref ChatOptions options, string? conversationId)
    {
        if (options.ToolMode is RequiredChatToolMode)
        {
            // We have to reset the tool mode to be non-required after the first iteration,
            // as otherwise we'll be in an infinite loop.
            options = options.Clone();
            options.ToolMode = null;
            options.ConversationId = conversationId;
        }
        else if (options.ConversationId != conversationId)
        {
            // As with the other modes, ensure we've propagated the chat conversation ID to the options.
            // We only need to clone the options if we're actually mutating it.
            options = options.Clone();
            options.ConversationId = conversationId;
        }
    }

    /// <summary>
    /// Processes the function calls in the <paramref name="functionCallContents"/> list.
    /// </summary>
    /// <param name="messages">The current chat contents, inclusive of the function call contents being processed.</param>
    /// <param name="options">The options used for the response being processed.</param>
    /// <param name="functionCallContents">The function call contents representing the functions to be invoked.</param>
    /// <param name="iteration">The iteration number of how many roundtrips have been made to the inner client.</param>
    /// <param name="consecutiveErrorCount">The number of consecutive iterations, prior to this one, that were recorded as having function invocation errors.</param>
    /// <param name="isStreaming">Whether the function calls are being processed in a streaming context.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A value indicating how the caller should proceed.</returns>
    private async Task<(bool ShouldTerminate, int NewConsecutiveErrorCount, IList<ChatMessage> MessagesAdded)> ProcessFunctionCallsAsync(
        List<ChatMessage> messages, ChatOptions options, List<FunctionCallContent> functionCallContents, int iteration, int consecutiveErrorCount,
        bool isStreaming, CancellationToken cancellationToken)
    {
        // We must add a response for every tool call, regardless of whether we successfully executed it or not.
        // If we successfully execute it, we'll add the result. If we don't, we'll add an error.

        Debug.Assert(functionCallContents.Count > 0, "Expected at least one function call.");
        var shouldTerminate = false;
        var captureCurrentIterationExceptions = consecutiveErrorCount < _maximumConsecutiveErrorsPerRequest;

        // Process all functions. If there's more than one and concurrent invocation is enabled, do so in parallel.
        if (functionCallContents.Count == 1)
        {
            FunctionInvocationResult result = await ProcessFunctionCallAsync(
                messages, options, functionCallContents,
                iteration, 0, captureCurrentIterationExceptions, isStreaming, cancellationToken);

            IList<ChatMessage> addedMessages = CreateResponseMessages([result]);
            ThrowIfNoFunctionResultsAdded(addedMessages);
            UpdateConsecutiveErrorCountOrThrow(addedMessages, ref consecutiveErrorCount);
            messages.AddRange(addedMessages);

            return (result.Terminate, consecutiveErrorCount, addedMessages);
        }
        else
        {
            List<FunctionInvocationResult> results = [];

            if (AllowConcurrentInvocation)
            {
                // Rather than awaiting each function before invoking the next, invoke all of them
                // and then await all of them. We avoid forcibly introducing parallelism via Task.Run,
                // but if a function invocation completes asynchronously, its processing can overlap
                // with the processing of other the other invocation invocations.
                results.AddRange(await Task.WhenAll(
                    from callIndex in Enumerable.Range(0, functionCallContents.Count)
                    select ProcessFunctionCallAsync(
                        messages, options, functionCallContents,
                        iteration, callIndex, captureExceptions: true, isStreaming, cancellationToken)));

                shouldTerminate = results.Any(r => r.Terminate);
            }
            else
            {
                // Invoke each function serially.
                for (int callIndex = 0; callIndex < functionCallContents.Count; callIndex++)
                {
                    var functionResult = await ProcessFunctionCallAsync(
                        messages, options, functionCallContents,
                        iteration, callIndex, captureCurrentIterationExceptions, isStreaming, cancellationToken);

                    results.Add(functionResult);

                    // If any function requested termination, we should stop right away.
                    if (functionResult.Terminate)
                    {
                        shouldTerminate = true;
                        break;
                    }
                }
            }

            IList<ChatMessage> addedMessages = CreateResponseMessages(results.ToArray());
            ThrowIfNoFunctionResultsAdded(addedMessages);
            UpdateConsecutiveErrorCountOrThrow(addedMessages, ref consecutiveErrorCount);
            messages.AddRange(addedMessages);

            return (shouldTerminate, consecutiveErrorCount, addedMessages);
        }
    }

#pragma warning disable CA1851 // Possible multiple enumerations of 'IEnumerable' collection
    /// <summary>
    /// Updates the consecutive error count, and throws an exception if the count exceeds the maximum.
    /// </summary>
    /// <param name="added">Added messages.</param>
    /// <param name="consecutiveErrorCount">Consecutive error count.</param>
    /// <exception cref="AggregateException">Thrown if the maximum consecutive error count is exceeded.</exception>
    private void UpdateConsecutiveErrorCountOrThrow(IList<ChatMessage> added, ref int consecutiveErrorCount)
    {
        var allExceptions = added.SelectMany(m => m.Contents.OfType<FunctionResultContent>())
            .Select(frc => frc.Exception!)
            .Where(e => e is not null);

        if (allExceptions.Any())
        {
            consecutiveErrorCount++;
            if (consecutiveErrorCount > _maximumConsecutiveErrorsPerRequest)
            {
                var allExceptionsArray = allExceptions.ToArray();
                if (allExceptionsArray.Length == 1)
                {
                    ExceptionDispatchInfo.Capture(allExceptionsArray[0]).Throw();
                }
                else
                {
                    throw new AggregateException(allExceptionsArray);
                }
            }
        }
        else
        {
            consecutiveErrorCount = 0;
        }
    }
#pragma warning restore CA1851

    /// <summary>
    /// Throws an exception if <see cref="CreateResponseMessages"/> doesn't create any messages.
    /// </summary>
    private void ThrowIfNoFunctionResultsAdded(IList<ChatMessage>? messages)
    {
        if (messages is null || messages.Count == 0)
        {
            Throw.InvalidOperationException($"{GetType().Name}.{nameof(CreateResponseMessages)} returned null or an empty collection of messages.");
        }
    }

    /// <summary>Processes the function call described in <paramref name="callContents"/>[<paramref name="iteration"/>].</summary>
    /// <param name="messages">The current chat contents, inclusive of the function call contents being processed.</param>
    /// <param name="options">The options used for the response being processed.</param>
    /// <param name="callContents">The function call contents representing all the functions being invoked.</param>
    /// <param name="iteration">The iteration number of how many roundtrips have been made to the inner client.</param>
    /// <param name="functionCallIndex">The 0-based index of the function being called out of <paramref name="callContents"/>.</param>
    /// <param name="captureExceptions">If true, handles function-invocation exceptions by returning a value with <see cref="FunctionInvocationStatus.Exception"/>. Otherwise, rethrows.</param>
    /// <param name="isStreaming">Whether the function calls are being processed in a streaming context.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A value indicating how the caller should proceed.</returns>
    private async Task<FunctionInvocationResult> ProcessFunctionCallAsync(
        List<ChatMessage> messages, ChatOptions options, List<FunctionCallContent> callContents,
        int iteration, int functionCallIndex, bool captureExceptions, bool isStreaming, CancellationToken cancellationToken)
    {
        var callContent = callContents[functionCallIndex];

        // Look up the AIFunction for the function call. If the requested function isn't available, send back an error.
        AIFunction? aiFunction = options.Tools!.OfType<AIFunction>().FirstOrDefault(t => t.Name == callContent.Name);
        if (aiFunction is null)
        {
            return new(terminate: false, FunctionInvocationStatus.NotFound, callContent, result: null, exception: null);
        }

        FunctionInvocationContext context = new()
        {
            Function = aiFunction,
            Arguments = new(callContent.Arguments) { Services = FunctionInvocationServices },
            Messages = messages,
            Options = options,
            CallContent = callContent,
            Iteration = iteration,
            FunctionCallIndex = functionCallIndex,
            FunctionCount = callContents.Count,
            IsStreaming = isStreaming
        };

        object? result;
        try
        {
            result = await InstrumentedInvokeFunctionAsync(context, cancellationToken);
        }
        catch (Exception e) when (!cancellationToken.IsCancellationRequested)
        {
            if (!captureExceptions)
            {
                throw;
            }

            return new(
                terminate: false,
                FunctionInvocationStatus.Exception,
                callContent,
                result: null,
                exception: e);
        }

        return new(
            terminate: context.Terminate,
            FunctionInvocationStatus.RanToCompletion,
            callContent,
            result,
            exception: null);
    }

    /// <summary>Creates one or more response messages for function invocation results.</summary>
    /// <param name="results">Information about the function call invocations and results.</param>
    /// <returns>A list of all chat messages created from <paramref name="results"/>.</returns>
    protected virtual IList<ChatMessage> CreateResponseMessages(
        ReadOnlySpan<FunctionInvocationResult> results)
    {
        var contents = new List<AIContent>(results.Length);
        for (int i = 0; i < results.Length; i++)
        {
            contents.Add(CreateFunctionResultContent(results[i]));
        }

        return [new(ChatRole.Tool, contents)];

        FunctionResultContent CreateFunctionResultContent(FunctionInvocationResult result)
        {
            _ = Throw.IfNull(result);

            object? functionResult;
            if (result.Status == FunctionInvocationStatus.RanToCompletion)
            {
                functionResult = result.Result ?? "Success: Function completed.";
            }
            else
            {
                string message = result.Status switch
                {
                    FunctionInvocationStatus.NotFound => $"Error: Requested function \"{result.CallContent.Name}\" not found.",
                    FunctionInvocationStatus.Exception => "Error: Function failed.",
                    _ => "Error: Unknown error.",
                };

                if (IncludeDetailedErrors && result.Exception is not null)
                {
                    message = $"{message} Exception: {result.Exception.Message}";
                }

                functionResult = message;
            }

            return new FunctionResultContent(result.CallContent.CallId, functionResult) { Exception = result.Exception };
        }
    }

    /// <summary>Invokes the function asynchronously.</summary>
    /// <param name="context">
    /// The function invocation context detailing the function to be invoked and its arguments along with additional request information.
    /// </param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The result of the function invocation, or <see langword="null"/> if the function invocation returned <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    private async Task<object?> InstrumentedInvokeFunctionAsync(FunctionInvocationContext context, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(context);

        using Activity? activity = _activitySource?.StartActivity(
            $"{OpenTelemetryConsts.GenAI.ExecuteTool} {context.Function.Name}",
            ActivityKind.Internal,
            default(ActivityContext),
            [
                new(OpenTelemetryConsts.GenAI.Operation.Name, "execute_tool"),
                new(OpenTelemetryConsts.GenAI.Tool.Call.Id, context.CallContent.CallId),
                new(OpenTelemetryConsts.GenAI.Tool.Name, context.Function.Name),
                new(OpenTelemetryConsts.GenAI.Tool.Description, context.Function.Description),
            ]);

        long startingTimestamp = 0;
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            startingTimestamp = Stopwatch.GetTimestamp();
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogInvokingSensitive(context.Function.Name, LoggingHelpers.AsJson(context.Arguments, context.Function.JsonSerializerOptions));
            }
            else
            {
                LogInvoking(context.Function.Name);
            }
        }

        object? result = null;
        try
        {
            CurrentContext = context; // doesn't need to be explicitly reset after, as that's handled automatically at async method exit
            result = await InvokeFunctionAsync(context, cancellationToken);
        }
        catch (Exception e)
        {
            if (activity is not null)
            {
                _ = activity.SetTag("error.type", e.GetType().FullName)
                            .SetStatus(ActivityStatusCode.Error, e.Message);
            }

            if (e is OperationCanceledException)
            {
                LogInvocationCanceled(context.Function.Name);
            }
            else
            {
                LogInvocationFailed(context.Function.Name, e);
            }

            throw;
        }
        finally
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                TimeSpan elapsed = GetElapsedTime(startingTimestamp);

                if (result is not null && _logger.IsEnabled(LogLevel.Trace))
                {
                    LogInvocationCompletedSensitive(context.Function.Name, elapsed, LoggingHelpers.AsJson(result, context.Function.JsonSerializerOptions));
                }
                else
                {
                    LogInvocationCompleted(context.Function.Name, elapsed);
                }
            }
        }

        return result;
    }

    /// <summary>This method will invoke the function within the try block.</summary>
    /// <param name="context">The function invocation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The function result.</returns>
    protected virtual ValueTask<object?> InvokeFunctionAsync(FunctionInvocationContext context, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(context);

        return context.Function.InvokeAsync(context.Arguments, cancellationToken);
    }

    private static TimeSpan GetElapsedTime(long startingTimestamp) =>
#if NET
        Stopwatch.GetElapsedTime(startingTimestamp);
#else
        new((long)((Stopwatch.GetTimestamp() - startingTimestamp) * ((double)TimeSpan.TicksPerSecond / Stopwatch.Frequency)));
#endif

    [LoggerMessage(LogLevel.Debug, "Invoking {MethodName}.", SkipEnabledCheck = true)]
    private partial void LogInvoking(string methodName);

    [LoggerMessage(LogLevel.Trace, "Invoking {MethodName}({Arguments}).", SkipEnabledCheck = true)]
    private partial void LogInvokingSensitive(string methodName, string arguments);

    [LoggerMessage(LogLevel.Debug, "{MethodName} invocation completed. Duration: {Duration}", SkipEnabledCheck = true)]
    private partial void LogInvocationCompleted(string methodName, TimeSpan duration);

    [LoggerMessage(LogLevel.Trace, "{MethodName} invocation completed. Duration: {Duration}. Result: {Result}", SkipEnabledCheck = true)]
    private partial void LogInvocationCompletedSensitive(string methodName, TimeSpan duration, string result);

    [LoggerMessage(LogLevel.Debug, "{MethodName} invocation canceled.")]
    private partial void LogInvocationCanceled(string methodName);

    [LoggerMessage(LogLevel.Error, "{MethodName} invocation failed.")]
    private partial void LogInvocationFailed(string methodName, Exception error);

    /// <summary>Provides information about the invocation of a function call.</summary>
    public sealed class FunctionInvocationResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionInvocationResult"/> class.
        /// </summary>
        /// <param name="terminate">Indicates whether the caller should terminate the processing loop.</param>
        /// <param name="status">Indicates the status of the function invocation.</param>
        /// <param name="callContent">Contains information about the function call.</param>
        /// <param name="result">The result of the function call.</param>
        /// <param name="exception">The exception thrown by the function call, if any.</param>
        internal FunctionInvocationResult(bool terminate, FunctionInvocationStatus status, FunctionCallContent callContent, object? result, Exception? exception)
        {
            Terminate = terminate;
            Status = status;
            CallContent = callContent;
            Result = result;
            Exception = exception;
        }

        /// <summary>Gets status about how the function invocation completed.</summary>
        public FunctionInvocationStatus Status { get; }

        /// <summary>Gets the function call content information associated with this invocation.</summary>
        public FunctionCallContent CallContent { get; }

        /// <summary>Gets the result of the function call.</summary>
        public object? Result { get; }

        /// <summary>Gets any exception the function call threw.</summary>
        public Exception? Exception { get; }

        /// <summary>Gets a value indicating whether the caller should terminate the processing loop.</summary>
        public bool Terminate { get; }
    }

    /// <summary>Provides error codes for when errors occur as part of the function calling loop.</summary>
    public enum FunctionInvocationStatus
    {
        /// <summary>The operation completed successfully.</summary>
        RanToCompletion,

        /// <summary>The requested function could not be found.</summary>
        NotFound,

        /// <summary>The function call failed with an exception.</summary>
        Exception,
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/ChatCompletion/FunctionInvokingChatClientBuilderExtensions.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides extension methods for attaching a <see cref="FunctionInvokingChatClient"/> to a chat pipeline.
/// </summary>
public static class FunctionInvokingChatClientBuilderExtensions
{
    /// <summary>
    /// Enables automatic function call invocation on the chat pipeline.
    /// </summary>
    /// <remarks>This works by adding an instance of <see cref="FunctionInvokingChatClient"/> with default options.</remarks>
    /// <param name="builder">The <see cref="ChatClientBuilder"/> being used to build the chat pipeline.</param>
    /// <param name="loggerFactory">An optional <see cref="ILoggerFactory"/> to use to create a logger for logging function invocations.</param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="FunctionInvokingChatClient"/> instance.</param>
    /// <returns>The supplied <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static ChatClientBuilder UseFunctionInvocation(
        this ChatClientBuilder builder,
        ILoggerFactory? loggerFactory = null,
        Action<FunctionInvokingChatClient>? configure = null)
    {
        _ = Throw.IfNull(builder);

        return builder.Use((innerClient, services) =>
        {
            loggerFactory ??= services.GetService<ILoggerFactory>();

            var chatClient = new FunctionInvokingChatClient(innerClient, loggerFactory, services);
            configure?.Invoke(chatClient);
            return chatClient;
        });
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/ChatCompletion/LoggingChatClient.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A delegating chat client that logs chat operations to an <see cref="ILogger"/>.</summary>
/// <remarks>
/// <para>
/// The provided implementation of <see cref="IChatClient"/> is thread-safe for concurrent use so long as the
/// <see cref="ILogger"/> employed is also thread-safe for concurrent use.
/// </para>
/// <para>
/// When the employed <see cref="ILogger"/> enables <see cref="Logging.LogLevel.Trace"/>, the contents of
/// chat messages and options are logged. These messages and options may contain sensitive application data.
/// <see cref="Logging.LogLevel.Trace"/> is disabled by default and should never be enabled in a production environment.
/// Messages and options are not logged at other logging levels.
/// </para>
/// </remarks>
public partial class LoggingChatClient : DelegatingChatClient
{
    /// <summary>An <see cref="ILogger"/> instance used for all logging.</summary>
    private readonly ILogger _logger;

    /// <summary>The <see cref="JsonSerializerOptions"/> to use for serialization of state written to the logger.</summary>
    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the <see cref="LoggingChatClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="IChatClient"/>.</param>
    /// <param name="logger">An <see cref="ILogger"/> instance that will be used for all logging.</param>
    public LoggingChatClient(IChatClient innerClient, ILogger logger)
        : base(innerClient)
    {
        _logger = Throw.IfNull(logger);
        _jsonSerializerOptions = AIJsonUtilities.DefaultOptions;
    }

    /// <summary>Gets or sets JSON serialization options to use when serializing logging data.</summary>
    public JsonSerializerOptions JsonSerializerOptions
    {
        get => _jsonSerializerOptions;
        set => _jsonSerializerOptions = Throw.IfNull(value);
    }

    /// <inheritdoc/>
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogInvokedSensitive(nameof(GetResponseAsync), AsJson(messages), AsJson(options), AsJson(this.GetService<ChatClientMetadata>()));
            }
            else
            {
                LogInvoked(nameof(GetResponseAsync));
            }
        }

        try
        {
            var response = await base.GetResponseAsync(messages, options, cancellationToken);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    LogCompletedSensitive(nameof(GetResponseAsync), AsJson(response));
                }
                else
                {
                    LogCompleted(nameof(GetResponseAsync));
                }
            }

            return response;
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(GetResponseAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(GetResponseAsync), ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogInvokedSensitive(nameof(GetStreamingResponseAsync), AsJson(messages), AsJson(options), AsJson(this.GetService<ChatClientMetadata>()));
            }
            else
            {
                LogInvoked(nameof(GetStreamingResponseAsync));
            }
        }

        IAsyncEnumerator<ChatResponseUpdate> e;
        try
        {
            e = base.GetStreamingResponseAsync(messages, options, cancellationToken).GetAsyncEnumerator(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled(nameof(GetStreamingResponseAsync));
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(nameof(GetStreamingResponseAsync), ex);
            throw;
        }

        try
        {
            ChatResponseUpdate? update = null;
            while (true)
            {
                try
                {
                    if (!await e.MoveNextAsync())
                    {
                        break;
                    }

                    update = e.Current;
                }
                catch (OperationCanceledException)
                {
                    LogInvocationCanceled(nameof(GetStreamingResponseAsync));
                    throw;
                }
                catch (Exception ex)
                {
                    LogInvocationFailed(nameof(GetStreamingResponseAsync), ex);
                    throw;
                }

                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    LogStreamingUpdateSensitive(AsJson(update));
                }

                yield return update;
            }

            LogCompleted(nameof(GetStreamingResponseAsync));
        }
        finally
        {
            await e.DisposeAsync();
        }
    }

    private string AsJson<T>(T value) => LoggingHelpers.AsJson(value, _jsonSerializerOptions);

    [LoggerMessage(LogLevel.Debug, "{MethodName} invoked.")]
    private partial void LogInvoked(string methodName);

    [LoggerMessage(LogLevel.Trace, "{MethodName} invoked: {Messages}. Options: {ChatOptions}. Metadata: {ChatClientMetadata}.")]
    private partial void LogInvokedSensitive(string methodName, string messages, string chatOptions, string chatClientMetadata);

    [LoggerMessage(LogLevel.Debug, "{MethodName} completed.")]
    private partial void LogCompleted(string methodName);

    [LoggerMessage(LogLevel.Trace, "{MethodName} completed: {ChatResponse}.")]
    private partial void LogCompletedSensitive(string methodName, string chatResponse);

    [LoggerMessage(LogLevel.Trace, "GetStreamingResponseAsync received update: {ChatResponseUpdate}")]
    private partial void LogStreamingUpdateSensitive(string chatResponseUpdate);

    [LoggerMessage(LogLevel.Debug, "{MethodName} canceled.")]
    private partial void LogInvocationCanceled(string methodName);

    [LoggerMessage(LogLevel.Error, "{MethodName} failed.")]
    private partial void LogInvocationFailed(string methodName, Exception error);
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/ChatCompletion/LoggingChatClientBuilderExtensions.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="LoggingChatClient"/> instances.</summary>
public static class LoggingChatClientBuilderExtensions
{
    /// <summary>Adds logging to the chat client pipeline.</summary>
    /// <param name="builder">The <see cref="ChatClientBuilder"/>.</param>
    /// <param name="loggerFactory">
    /// An optional <see cref="ILoggerFactory"/> used to create a logger with which logging should be performed.
    /// If not supplied, a required instance will be resolved from the service provider.
    /// </param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="LoggingChatClient"/> instance.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// When the employed <see cref="ILogger"/> enables <see cref="Logging.LogLevel.Trace"/>, the contents of
    /// chat messages and options are logged. These messages and options may contain sensitive application data.
    /// <see cref="Logging.LogLevel.Trace"/> is disabled by default and should never be enabled in a production environment.
    /// Messages and options are not logged at other logging levels.
    /// </para>
    /// </remarks>
    public static ChatClientBuilder UseLogging(
        this ChatClientBuilder builder,
        ILoggerFactory? loggerFactory = null,
        Action<LoggingChatClient>? configure = null)
    {
        _ = Throw.IfNull(builder);

        return builder.Use((innerClient, services) =>
        {
            loggerFactory ??= services.GetRequiredService<ILoggerFactory>();

            // If the factory we resolve is for the null logger, the LoggingChatClient will end up
            // being an expensive nop, so skip adding it and just return the inner client.
            if (loggerFactory == NullLoggerFactory.Instance)
            {
                return innerClient;
            }

            var chatClient = new LoggingChatClient(innerClient, loggerFactory.CreateLogger(typeof(LoggingChatClient)));
            configure?.Invoke(chatClient);
            return chatClient;
        });
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/ChatCompletion/OpenTelemetryChatClient.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S3358 // Ternary operators should not be nested
#pragma warning disable SA1111 // Closing parenthesis should be on line of last parameter
#pragma warning disable SA1113 // Comma should be on the same line as previous parameter

namespace Microsoft.Extensions.AI;

/// <summary>Represents a delegating chat client that implements the OpenTelemetry Semantic Conventions for Generative AI systems.</summary>
/// <remarks>
/// This class provides an implementation of the Semantic Conventions for Generative AI systems v1.34, defined at <see href="https://opentelemetry.io/docs/specs/semconv/gen-ai/" />.
/// The specification is still experimental and subject to change; as such, the telemetry output by this client is also subject to change.
/// </remarks>
public sealed partial class OpenTelemetryChatClient : DelegatingChatClient
{
    private const LogLevel EventLogLevel = LogLevel.Information;

    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;
    private readonly ILogger _logger;

    private readonly Histogram<int> _tokenUsageHistogram;
    private readonly Histogram<double> _operationDurationHistogram;

    private readonly string? _defaultModelId;
    private readonly string? _system;
    private readonly string? _serverAddress;
    private readonly int _serverPort;

    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the <see cref="OpenTelemetryChatClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="IChatClient"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/> to use for emitting events.</param>
    /// <param name="sourceName">An optional source name that will be used on the telemetry data.</param>
    public OpenTelemetryChatClient(IChatClient innerClient, ILogger? logger = null, string? sourceName = null)
        : base(innerClient)
    {
        Debug.Assert(innerClient is not null, "Should have been validated by the base ctor");

        _logger = logger ?? NullLogger.Instance;

        if (innerClient!.GetService<ChatClientMetadata>() is ChatClientMetadata metadata)
        {
            _defaultModelId = metadata.DefaultModelId;
            _system = metadata.ProviderName;
            _serverAddress = metadata.ProviderUri?.GetLeftPart(UriPartial.Path);
            _serverPort = metadata.ProviderUri?.Port ?? 0;
        }

        string name = string.IsNullOrEmpty(sourceName) ? OpenTelemetryConsts.DefaultSourceName : sourceName!;
        _activitySource = new(name);
        _meter = new(name);

        _tokenUsageHistogram = _meter.CreateHistogram<int>(
            OpenTelemetryConsts.GenAI.Client.TokenUsage.Name,
            OpenTelemetryConsts.TokensUnit,
            OpenTelemetryConsts.GenAI.Client.TokenUsage.Description
#if NET9_0_OR_GREATER
            , advice: new() { HistogramBucketBoundaries = OpenTelemetryConsts.GenAI.Client.TokenUsage.ExplicitBucketBoundaries }
#endif
            );

        _operationDurationHistogram = _meter.CreateHistogram<double>(
            OpenTelemetryConsts.GenAI.Client.OperationDuration.Name,
            OpenTelemetryConsts.SecondsUnit,
            OpenTelemetryConsts.GenAI.Client.OperationDuration.Description
#if NET9_0_OR_GREATER
            , advice: new() { HistogramBucketBoundaries = OpenTelemetryConsts.GenAI.Client.OperationDuration.ExplicitBucketBoundaries }
#endif
            );

        _jsonSerializerOptions = AIJsonUtilities.DefaultOptions;
    }

    /// <summary>Gets or sets JSON serialization options to use when formatting chat data into telemetry strings.</summary>
    public JsonSerializerOptions JsonSerializerOptions
    {
        get => _jsonSerializerOptions;
        set => _jsonSerializerOptions = Throw.IfNull(value);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _activitySource.Dispose();
            _meter.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Gets or sets a value indicating whether potentially sensitive information should be included in telemetry.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if potentially sensitive information should be included in telemetry;
    /// <see langword="false"/> if telemetry shouldn't include raw inputs and outputs.
    /// The default value is <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// By default, telemetry includes metadata, such as token counts, but not raw inputs
    /// and outputs, such as message content, function call arguments, and function call results.
    /// </remarks>
    public bool EnableSensitiveData { get; set; }

    /// <inheritdoc/>
    public override object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType == typeof(ActivitySource) ? _activitySource :
        base.GetService(serviceType, serviceKey);

    /// <inheritdoc/>
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);
        _jsonSerializerOptions.MakeReadOnly();

        using Activity? activity = CreateAndConfigureActivity(options);
        Stopwatch? stopwatch = _operationDurationHistogram.Enabled ? Stopwatch.StartNew() : null;
        string? requestModelId = options?.ModelId ?? _defaultModelId;

        LogChatMessages(messages);

        ChatResponse? response = null;
        Exception? error = null;
        try
        {
            response = await base.GetResponseAsync(messages, options, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
        finally
        {
            TraceResponse(activity, requestModelId, response, error, stopwatch);
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);
        _jsonSerializerOptions.MakeReadOnly();

        using Activity? activity = CreateAndConfigureActivity(options);
        Stopwatch? stopwatch = _operationDurationHistogram.Enabled ? Stopwatch.StartNew() : null;
        string? requestModelId = options?.ModelId ?? _defaultModelId;

        LogChatMessages(messages);

        IAsyncEnumerable<ChatResponseUpdate> updates;
        try
        {
            updates = base.GetStreamingResponseAsync(messages, options, cancellationToken);
        }
        catch (Exception ex)
        {
            TraceResponse(activity, requestModelId, response: null, ex, stopwatch);
            throw;
        }

        var responseEnumerator = updates.GetAsyncEnumerator(cancellationToken);
        List<ChatResponseUpdate> trackedUpdates = [];
        Exception? error = null;
        try
        {
            while (true)
            {
                ChatResponseUpdate update;
                try
                {
                    if (!await responseEnumerator.MoveNextAsync())
                    {
                        break;
                    }

                    update = responseEnumerator.Current;
                }
                catch (Exception ex)
                {
                    error = ex;
                    throw;
                }

                trackedUpdates.Add(update);
                yield return update;
                Activity.Current = activity; // workaround for https://github.com/dotnet/runtime/issues/47802
            }
        }
        finally
        {
            TraceResponse(activity, requestModelId, trackedUpdates.ToChatResponse(), error, stopwatch);

            await responseEnumerator.DisposeAsync();
        }
    }

    /// <summary>Creates an activity for a chat request, or returns <see langword="null"/> if not enabled.</summary>
    private Activity? CreateAndConfigureActivity(ChatOptions? options)
    {
        Activity? activity = null;
        if (_activitySource.HasListeners())
        {
            string? modelId = options?.ModelId ?? _defaultModelId;

            activity = _activitySource.StartActivity(
                string.IsNullOrWhiteSpace(modelId) ? OpenTelemetryConsts.GenAI.Chat : $"{OpenTelemetryConsts.GenAI.Chat} {modelId}",
                ActivityKind.Client);

            if (activity is not null)
            {
                _ = activity
                    .AddTag(OpenTelemetryConsts.GenAI.Operation.Name, OpenTelemetryConsts.GenAI.Chat)
                    .AddTag(OpenTelemetryConsts.GenAI.Request.Model, modelId)
                    .AddTag(OpenTelemetryConsts.GenAI.SystemName, _system);

                if (_serverAddress is not null)
                {
                    _ = activity
                        .AddTag(OpenTelemetryConsts.Server.Address, _serverAddress)
                        .AddTag(OpenTelemetryConsts.Server.Port, _serverPort);
                }

                if (options is not null)
                {
                    if (options.ConversationId is string conversationId)
                    {
                        _ = activity.AddTag(OpenTelemetryConsts.GenAI.Conversation.Id, conversationId);
                    }

                    if (options.FrequencyPenalty is float frequencyPenalty)
                    {
                        _ = activity.AddTag(OpenTelemetryConsts.GenAI.Request.FrequencyPenalty, frequencyPenalty);
                    }

                    if (options.MaxOutputTokens is int maxTokens)
                    {
                        _ = activity.AddTag(OpenTelemetryConsts.GenAI.Request.MaxTokens, maxTokens);
                    }

                    if (options.PresencePenalty is float presencePenalty)
                    {
                        _ = activity.AddTag(OpenTelemetryConsts.GenAI.Request.PresencePenalty, presencePenalty);
                    }

                    if (options.Seed is long seed)
                    {
                        _ = activity.AddTag(OpenTelemetryConsts.GenAI.Request.Seed, seed);
                    }

                    if (options.StopSequences is IList<string> stopSequences)
                    {
                        _ = activity.AddTag(OpenTelemetryConsts.GenAI.Request.StopSequences, $"[{string.Join(", ", stopSequences.Select(s => $"\"{s}\""))}]");
                    }

                    if (options.Temperature is float temperature)
                    {
                        _ = activity.AddTag(OpenTelemetryConsts.GenAI.Request.Temperature, temperature);
                    }

                    if (options.TopK is int topK)
                    {
                        _ = activity.AddTag(OpenTelemetryConsts.GenAI.Request.TopK, topK);
                    }

                    if (options.TopP is float top_p)
                    {
                        _ = activity.AddTag(OpenTelemetryConsts.GenAI.Request.TopP, top_p);
                    }

                    if (options.ResponseFormat is not null)
                    {
                        switch (options.ResponseFormat)
                        {
                            case ChatResponseFormatText:
                                _ = activity.AddTag(OpenTelemetryConsts.GenAI.Output.Type, "text");
                                break;
                            case ChatResponseFormatJson:
                                _ = activity.AddTag(OpenTelemetryConsts.GenAI.Output.Type, "json");
                                break;
                        }
                    }

                    if (_system is not null)
                    {
                        // Since AdditionalProperties has undefined meaning, we treat it as potentially sensitive data
                        if (EnableSensitiveData && options.AdditionalProperties is { } props)
                        {
                            // Log all additional request options as per-provider tags. This is non-normative, but it covers cases where
                            // there's a per-provider specification in a best-effort manner (e.g. gen_ai.openai.request.service_tier),
                            // and more generally cases where there's additional useful information to be logged.
                            foreach (KeyValuePair<string, object?> prop in props)
                            {
                                _ = activity.AddTag(
                                    OpenTelemetryConsts.GenAI.Request.PerProvider(_system, JsonNamingPolicy.SnakeCaseLower.ConvertName(prop.Key)),
                                    prop.Value);
                            }
                        }
                    }
                }
            }
        }

        return activity;
    }

    /// <summary>Adds chat response information to the activity.</summary>
    private void TraceResponse(
        Activity? activity,
        string? requestModelId,
        ChatResponse? response,
        Exception? error,
        Stopwatch? stopwatch)
    {
        if (_operationDurationHistogram.Enabled && stopwatch is not null)
        {
            TagList tags = default;

            AddMetricTags(ref tags, requestModelId, response);
            if (error is not null)
            {
                tags.Add(OpenTelemetryConsts.Error.Type, error.GetType().FullName);
            }

            _operationDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds, tags);
        }

        if (_tokenUsageHistogram.Enabled && response?.Usage is { } usage)
        {
            if (usage.InputTokenCount is long inputTokens)
            {
                TagList tags = default;
                tags.Add(OpenTelemetryConsts.GenAI.Token.Type, "input");
                AddMetricTags(ref tags, requestModelId, response);
                _tokenUsageHistogram.Record((int)inputTokens);
            }

            if (usage.OutputTokenCount is long outputTokens)
            {
                TagList tags = default;
                tags.Add(OpenTelemetryConsts.GenAI.Token.Type, "output");
                AddMetricTags(ref tags, requestModelId, response);
                _tokenUsageHistogram.Record((int)outputTokens);
            }
        }

        if (error is not null)
        {
            _ = activity?
                .AddTag(OpenTelemetryConsts.Error.Type, error.GetType().FullName)
                .SetStatus(ActivityStatusCode.Error, error.Message);
        }

        if (response is not null)
        {
            LogChatResponse(response);

            if (activity is not null)
            {
                if (response.FinishReason is ChatFinishReason finishReason)
                {
#pragma warning disable CA1308 // Normalize strings to uppercase
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Response.FinishReasons, $"[\"{finishReason.Value.ToLowerInvariant()}\"]");
#pragma warning restore CA1308
                }

                if (!string.IsNullOrWhiteSpace(response.ResponseId))
                {
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Response.Id, response.ResponseId);
                }

                if (response.ModelId is not null)
                {
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Response.Model, response.ModelId);
                }

                if (response.Usage?.InputTokenCount is long inputTokens)
                {
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Usage.InputTokens, (int)inputTokens);
                }

                if (response.Usage?.OutputTokenCount is long outputTokens)
                {
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Usage.OutputTokens, (int)outputTokens);
                }

                if (_system is not null)
                {
                    // Since AdditionalProperties has undefined meaning, we treat it as potentially sensitive data
                    if (EnableSensitiveData && response.AdditionalProperties is { } props)
                    {
                        // Log all additional response properties as per-provider tags. This is non-normative, but it covers cases where
                        // there's a per-provider specification in a best-effort manner (e.g. gen_ai.openai.response.system_fingerprint),
                        // and more generally cases where there's additional useful information to be logged.
                        foreach (KeyValuePair<string, object?> prop in props)
                        {
                            _ = activity.AddTag(
                                OpenTelemetryConsts.GenAI.Response.PerProvider(_system, JsonNamingPolicy.SnakeCaseLower.ConvertName(prop.Key)),
                                prop.Value);
                        }
                    }
                }
            }
        }

        void AddMetricTags(ref TagList tags, string? requestModelId, ChatResponse? response)
        {
            tags.Add(OpenTelemetryConsts.GenAI.Operation.Name, OpenTelemetryConsts.GenAI.Chat);

            if (requestModelId is not null)
            {
                tags.Add(OpenTelemetryConsts.GenAI.Request.Model, requestModelId);
            }

            tags.Add(OpenTelemetryConsts.GenAI.SystemName, _system);

            if (_serverAddress is string endpointAddress)
            {
                tags.Add(OpenTelemetryConsts.Server.Address, endpointAddress);
                tags.Add(OpenTelemetryConsts.Server.Port, _serverPort);
            }

            if (response?.ModelId is string responseModel)
            {
                tags.Add(OpenTelemetryConsts.GenAI.Response.Model, responseModel);
            }
        }
    }

    private void LogChatMessages(IEnumerable<ChatMessage> messages)
    {
        if (!_logger.IsEnabled(EventLogLevel))
        {
            return;
        }

        foreach (ChatMessage message in messages)
        {
            if (message.Role == ChatRole.Assistant)
            {
                Log(new(1, OpenTelemetryConsts.GenAI.Assistant.Message),
                    JsonSerializer.Serialize(CreateAssistantEvent(message.Contents), OtelContext.Default.AssistantEvent));
            }
            else if (message.Role == ChatRole.Tool)
            {
                foreach (FunctionResultContent frc in message.Contents.OfType<FunctionResultContent>())
                {
                    Log(new(1, OpenTelemetryConsts.GenAI.Tool.Message),
                        JsonSerializer.Serialize(new()
                        {
                            Id = frc.CallId,
                            Content = EnableSensitiveData && frc.Result is object result ?
                                JsonSerializer.SerializeToNode(result, _jsonSerializerOptions.GetTypeInfo(result.GetType())) :
                                null,
                        }, OtelContext.Default.ToolEvent));
                }
            }
            else
            {
                Log(new(1, message.Role == ChatRole.System ? OpenTelemetryConsts.GenAI.System.Message : OpenTelemetryConsts.GenAI.User.Message),
                    JsonSerializer.Serialize(new()
                    {
                        Role = message.Role != ChatRole.System && message.Role != ChatRole.User && !string.IsNullOrWhiteSpace(message.Role.Value) ? message.Role.Value : null,
                        Content = GetMessageContent(message.Contents),
                    }, OtelContext.Default.SystemOrUserEvent));
            }
        }
    }

    private void LogChatResponse(ChatResponse response)
    {
        if (!_logger.IsEnabled(EventLogLevel))
        {
            return;
        }

        EventId id = new(1, OpenTelemetryConsts.GenAI.Choice);
        Log(id, JsonSerializer.Serialize(new()
        {
            FinishReason = response.FinishReason?.Value ?? "error",
            Index = 0,
            Message = CreateAssistantEvent(response.Messages is { Count: 1 } ? response.Messages[0].Contents : response.Messages.SelectMany(m => m.Contents)),
        }, OtelContext.Default.ChoiceEvent));
    }

    private void Log(EventId id, [StringSyntax(StringSyntaxAttribute.Json)] string eventBodyJson)
    {
        // This is not the idiomatic way to log, but it's necessary for now in order to structure
        // the data in a way that the OpenTelemetry collector can work with it. The event body
        // can be very large and should not be logged as an attribute.

        KeyValuePair<string, object?>[] tags =
        [
            new(OpenTelemetryConsts.Event.Name, id.Name),
            new(OpenTelemetryConsts.GenAI.SystemName, _system),
        ];

        _logger.Log(EventLogLevel, id, tags, null, (_, __) => eventBodyJson);
    }

    private AssistantEvent CreateAssistantEvent(IEnumerable<AIContent> contents)
    {
        var toolCalls = contents.OfType<FunctionCallContent>().Select(fc => new ToolCall
        {
            Id = fc.CallId,
            Function = new()
            {
                Name = fc.Name,
                Arguments = EnableSensitiveData ?
                    JsonSerializer.SerializeToNode(fc.Arguments, _jsonSerializerOptions.GetTypeInfo(typeof(IDictionary<string, object?>))) :
                    null,
            },
        }).ToArray();

        return new()
        {
            Content = GetMessageContent(contents),
            ToolCalls = toolCalls.Length > 0 ? toolCalls : null,
        };
    }

    private string? GetMessageContent(IEnumerable<AIContent> contents)
    {
        if (EnableSensitiveData)
        {
            string content = string.Concat(contents.OfType<TextContent>());
            if (content.Length > 0)
            {
                return content;
            }
        }

        return null;
    }

    private sealed class SystemOrUserEvent
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }

    private sealed class AssistantEvent
    {
        public string? Content { get; set; }
        public ToolCall[]? ToolCalls { get; set; }
    }

    private sealed class ToolEvent
    {
        public string? Id { get; set; }
        public JsonNode? Content { get; set; }
    }

    private sealed class ChoiceEvent
    {
        public string? FinishReason { get; set; }
        public int Index { get; set; }
        public AssistantEvent? Message { get; set; }
    }

    private sealed class ToolCall
    {
        public string? Id { get; set; }
        public string? Type { get; set; } = "function";
        public ToolCallFunction? Function { get; set; }
    }

    private sealed class ToolCallFunction
    {
        public string? Name { get; set; }
        public JsonNode? Arguments { get; set; }
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(SystemOrUserEvent))]
    [JsonSerializable(typeof(AssistantEvent))]
    [JsonSerializable(typeof(ToolEvent))]
    [JsonSerializable(typeof(ChoiceEvent))]
    [JsonSerializable(typeof(object))]
    private sealed partial class OtelContext : JsonSerializerContext;
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/ChatCompletion/OpenTelemetryChatClientBuilderExtensions.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="OpenTelemetryChatClient"/> instances.</summary>
public static class OpenTelemetryChatClientBuilderExtensions
{
    /// <summary>
    /// Adds OpenTelemetry support to the chat client pipeline, following the OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </summary>
    /// <remarks>
    /// The draft specification this follows is available at <see href="https://opentelemetry.io/docs/specs/semconv/gen-ai/" />.
    /// The specification is still experimental and subject to change; as such, the telemetry output by this client is also subject to change.
    /// </remarks>
    /// <param name="builder">The <see cref="ChatClientBuilder"/>.</param>
    /// <param name="loggerFactory">An optional <see cref="ILoggerFactory"/> to use to create a logger for logging events.</param>
    /// <param name="sourceName">An optional source name that will be used on the telemetry data.</param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="OpenTelemetryChatClient"/> instance.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static ChatClientBuilder UseOpenTelemetry(
        this ChatClientBuilder builder,
        ILoggerFactory? loggerFactory = null,
        string? sourceName = null,
        Action<OpenTelemetryChatClient>? configure = null) =>
        Throw.IfNull(builder).Use((innerClient, services) =>
        {
            loggerFactory ??= services.GetService<ILoggerFactory>();

            var chatClient = new OpenTelemetryChatClient(innerClient, loggerFactory?.CreateLogger(typeof(OpenTelemetryChatClient)), sourceName);
            configure?.Invoke(chatClient);

            return chatClient;
        });
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/Embeddings/AnonymousDelegatingEmbeddingGenerator.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A delegating embedding generator that wraps an inner generator with implementations provided by delegates.</summary>
/// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
/// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
internal sealed class AnonymousDelegatingEmbeddingGenerator<TInput, TEmbedding> : DelegatingEmbeddingGenerator<TInput, TEmbedding>
    where TEmbedding : Embedding
{
    /// <summary>The delegate to use as the implementation of <see cref="GenerateAsync"/>.</summary>
    private readonly Func<IEnumerable<TInput>, EmbeddingGenerationOptions?, IEmbeddingGenerator<TInput, TEmbedding>, CancellationToken, Task<GeneratedEmbeddings<TEmbedding>>> _generateFunc;

    /// <summary>Initializes a new instance of the <see cref="AnonymousDelegatingEmbeddingGenerator{TInput, TEmbedding}"/> class.</summary>
    /// <param name="innerGenerator">The inner generator.</param>
    /// <param name="generateFunc">A delegate that provides the implementation for <see cref="GenerateAsync"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerGenerator"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="generateFunc"/> is <see langword="null"/>.</exception>
    public AnonymousDelegatingEmbeddingGenerator(
        IEmbeddingGenerator<TInput, TEmbedding> innerGenerator,
        Func<IEnumerable<TInput>, EmbeddingGenerationOptions?, IEmbeddingGenerator<TInput, TEmbedding>, CancellationToken, Task<GeneratedEmbeddings<TEmbedding>>> generateFunc)
        : base(innerGenerator)
    {
        _ = Throw.IfNull(generateFunc);

        _generateFunc = generateFunc;
    }

    /// <inheritdoc/>
    public override async Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(
        IEnumerable<TInput> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(values);

        return await _generateFunc(values, options, InnerGenerator, cancellationToken);
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/Embeddings/CachingEmbeddingGenerator.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a delegating embedding generator that caches the results of embedding generation calls.</summary>
/// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
/// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
public abstract class CachingEmbeddingGenerator<TInput, TEmbedding> : DelegatingEmbeddingGenerator<TInput, TEmbedding>
    where TEmbedding : Embedding
{
    /// <summary>Initializes a new instance of the <see cref="CachingEmbeddingGenerator{TInput, TEmbedding}"/> class.</summary>
    /// <param name="innerGenerator">The underlying <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.</param>
    protected CachingEmbeddingGenerator(IEmbeddingGenerator<TInput, TEmbedding> innerGenerator)
        : base(innerGenerator)
    {
    }

    /// <inheritdoc />
    public override async Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(
        IEnumerable<TInput> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(values);

        // Optimize for the common-case of a single value in a list/array.
        if (values is IList<TInput> valuesList)
        {
            switch (valuesList.Count)
            {
                case 0:
                    return [];

                case 1:
                    // In the expected common case where we can cheaply tell there's only a single value and access it,
                    // we can avoid all the overhead of splitting the list and reassembling it.
                    var cacheKey = GetCacheKey(valuesList[0], options);
                    if (await ReadCacheAsync(cacheKey, cancellationToken) is TEmbedding e)
                    {
                        return [e];
                    }
                    else
                    {
                        var generated = await base.GenerateAsync(valuesList, options, cancellationToken);
                        if (generated.Count != 1)
                        {
                            Throw.InvalidOperationException($"Expected exactly one embedding to be generated, but received {generated.Count}.");
                        }

                        await WriteCacheAsync(cacheKey, generated[0], cancellationToken);
                        return generated;
                    }
            }
        }

        // Some of the inputs may already be cached. Go through each, checking to see whether each individually is cached.
        // Split those that are cached into one list and those that aren't into another. We retain their original positions
        // so that we can reassemble the results in the correct order.
        GeneratedEmbeddings<TEmbedding> results = [];
        List<(int Index, string CacheKey, TInput Input)>? uncached = null;
        foreach (TInput input in values)
        {
            // We're only storing the final result, not the in-flight task, so that we can avoid caching failures
            // or having problems when one of the callers cancels but others don't. This has the drawback that
            // concurrent callers might trigger duplicate requests, but that's acceptable.
            var cacheKey = GetCacheKey(input, options);

            if (await ReadCacheAsync(cacheKey, cancellationToken) is TEmbedding existing)
            {
                results.Add(existing);
            }
            else
            {
                (uncached ??= []).Add((results.Count, cacheKey, input));
                results.Add(null!); // temporary placeholder
            }
        }

        // If anything wasn't cached, we need to generate embeddings for those.
        if (uncached is not null)
        {
            // Now make a single call to the wrapped generator to generate embeddings for all of the uncached inputs.
            var uncachedResults = await base.GenerateAsync(uncached.Select(e => e.Input), options, cancellationToken);

            // Store the resulting embeddings into the cache individually.
            for (int i = 0; i < uncachedResults.Count; i++)
            {
                await WriteCacheAsync(uncached[i].CacheKey, uncachedResults[i], cancellationToken);
            }

            // Fill in the gaps with the newly generated results.
            for (int i = 0; i < uncachedResults.Count; i++)
            {
                results[uncached[i].Index] = uncachedResults[i];
            }
        }

        Debug.Assert(results.All(e => e is not null), "Expected all values to be non-null");
        return results;
    }

    /// <summary>Computes a cache key for the specified values.</summary>
    /// <param name="values">The values to inform the key.</param>
    /// <returns>The computed key.</returns>
    protected abstract string GetCacheKey(params ReadOnlySpan<object?> values);

    /// <summary>Returns a previously cached <see cref="Embedding{TEmbedding}"/>, if available.</summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The previously cached data, if available, otherwise <see langword="null"/>.</returns>
    protected abstract Task<TEmbedding?> ReadCacheAsync(string key, CancellationToken cancellationToken);

    /// <summary>Stores a <typeparamref name="TEmbedding"/> in the underlying cache.</summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The <typeparamref name="TEmbedding"/> to be stored.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the completion of the operation.</returns>
    protected abstract Task WriteCacheAsync(string key, TEmbedding value, CancellationToken cancellationToken);
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/Embeddings/ConfigureOptionsEmbeddingGenerator.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a delegating embedding generator that configures a <see cref="EmbeddingGenerationOptions"/> instance used by the remainder of the pipeline.</summary>
/// <typeparam name="TInput">The type of the input passed to the generator.</typeparam>
/// <typeparam name="TEmbedding">The type of the embedding instance produced by the generator.</typeparam>
public sealed class ConfigureOptionsEmbeddingGenerator<TInput, TEmbedding> : DelegatingEmbeddingGenerator<TInput, TEmbedding>
    where TEmbedding : Embedding
{
    /// <summary>The callback delegate used to configure options.</summary>
    private readonly Action<EmbeddingGenerationOptions> _configureOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigureOptionsEmbeddingGenerator{TInput, TEmbedding}"/> class with the
    /// specified <paramref name="configure"/> callback.
    /// </summary>
    /// <param name="innerGenerator">The inner generator.</param>
    /// <param name="configure">
    /// The delegate to invoke to configure the <see cref="EmbeddingGenerationOptions"/> instance. It is passed a clone of the caller-supplied
    /// <see cref="EmbeddingGenerationOptions"/> instance (or a newly constructed instance if the caller-supplied instance is <see langword="null"/>).
    /// </param>
    /// <remarks>
    /// The <paramref name="configure"/> delegate is passed either a new instance of <see cref="EmbeddingGenerationOptions"/> if
    /// the caller didn't supply a <see cref="EmbeddingGenerationOptions"/> instance, or a clone (via <see cref="EmbeddingGenerationOptions.Clone"/> of the caller-supplied
    /// instance if one was supplied.
    /// </remarks>
    public ConfigureOptionsEmbeddingGenerator(
        IEmbeddingGenerator<TInput, TEmbedding> innerGenerator,
        Action<EmbeddingGenerationOptions> configure)
        : base(innerGenerator)
    {
        _configureOptions = Throw.IfNull(configure);
    }

    /// <inheritdoc/>
    public override async Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(
        IEnumerable<TInput> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await base.GenerateAsync(values, Configure(options), cancellationToken);
    }

    /// <summary>Creates and configures the <see cref="EmbeddingGenerationOptions"/> to pass along to the inner client.</summary>
    private EmbeddingGenerationOptions Configure(EmbeddingGenerationOptions? options)
    {
        options = options?.Clone() ?? new();

        _configureOptions(options);

        return options;
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/Embeddings/ConfigureOptionsEmbeddingGeneratorBuilderExtensions.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;

#pragma warning disable SA1629 // Documentation text should end with a period

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="ConfigureOptionsEmbeddingGenerator{TInput, TEmbedding}"/> instances.</summary>
public static class ConfigureOptionsEmbeddingGeneratorBuilderExtensions
{
    /// <summary>
    /// Adds a callback that configures a <see cref="EmbeddingGenerationOptions"/> to be passed to the next client in the pipeline.
    /// </summary>
    /// <typeparam name="TInput">The type of the input passed to the generator.</typeparam>
    /// <typeparam name="TEmbedding">The type of the embedding instance produced by the generator.</typeparam>
    /// <param name="builder">The <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/>.</param>
    /// <param name="configure">
    /// The delegate to invoke to configure the <see cref="EmbeddingGenerationOptions"/> instance. It is passed a clone of the caller-supplied
    /// <see cref="EmbeddingGenerationOptions"/> instance (or a new constructed instance if the caller-supplied instance is <see langword="null"/>).
    /// </param>
    /// <remarks>
    /// This can be used to set default options. The <paramref name="configure"/> delegate is passed either a new instance of
    /// <see cref="EmbeddingGenerationOptions"/> if the caller didn't supply a <see cref="EmbeddingGenerationOptions"/> instance, or
    /// a clone (via <see cref="EmbeddingGenerationOptions.Clone"/>
    /// of the caller-supplied instance if one was supplied.
    /// </remarks>
    /// <returns>The <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/>.</exception>
    public static EmbeddingGeneratorBuilder<TInput, TEmbedding> ConfigureOptions<TInput, TEmbedding>(
        this EmbeddingGeneratorBuilder<TInput, TEmbedding> builder,
        Action<EmbeddingGenerationOptions> configure)
        where TEmbedding : Embedding
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.Use(innerGenerator => new ConfigureOptionsEmbeddingGenerator<TInput, TEmbedding>(innerGenerator, configure));
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/Embeddings/DistributedCachingEmbeddingGenerator.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a delegating embedding generator that caches the results of embedding generation calls,
/// storing them as JSON in an <see cref="IDistributedCache"/>.
/// </summary>
/// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
/// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
/// <remarks>
/// The provided implementation of <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> is thread-safe for concurrent
/// use so long as the employed <see cref="IDistributedCache"/> is similarly thread-safe for concurrent use.
/// </remarks>
public class DistributedCachingEmbeddingGenerator<TInput, TEmbedding> : CachingEmbeddingGenerator<TInput, TEmbedding>
    where TEmbedding : Embedding
{
    private readonly IDistributedCache _storage;
    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the <see cref="DistributedCachingEmbeddingGenerator{TInput, TEmbedding}"/> class.</summary>
    /// <param name="innerGenerator">The underlying <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.</param>
    /// <param name="storage">A <see cref="IDistributedCache"/> instance that will be used as the backing store for the cache.</param>
    /// <exception cref="ArgumentNullException"><paramref name="storage"/> is <see langword="null"/>.</exception>
    public DistributedCachingEmbeddingGenerator(IEmbeddingGenerator<TInput, TEmbedding> innerGenerator, IDistributedCache storage)
        : base(innerGenerator)
    {
        _ = Throw.IfNull(storage);
        _storage = storage;
        _jsonSerializerOptions = AIJsonUtilities.DefaultOptions;
    }

    /// <summary>Gets or sets JSON serialization options to use when serializing cache data.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public JsonSerializerOptions JsonSerializerOptions
    {
        get => _jsonSerializerOptions;
        set
        {
            _ = Throw.IfNull(value);
            _jsonSerializerOptions = value;
        }
    }

    /// <inheritdoc />
    protected override async Task<TEmbedding?> ReadCacheAsync(string key, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(key);
        _jsonSerializerOptions.MakeReadOnly();

        if (await _storage.GetAsync(key, cancellationToken) is byte[] existingJson)
        {
            return JsonSerializer.Deserialize(existingJson, (JsonTypeInfo<TEmbedding>)_jsonSerializerOptions.GetTypeInfo(typeof(TEmbedding)));
        }

        return null;
    }

    /// <inheritdoc />
    protected override async Task WriteCacheAsync(string key, TEmbedding value, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(key);
        _ = Throw.IfNull(value);
        _jsonSerializerOptions.MakeReadOnly();

        var newJson = JsonSerializer.SerializeToUtf8Bytes(value, (JsonTypeInfo<TEmbedding>)_jsonSerializerOptions.GetTypeInfo(typeof(TEmbedding)));
        await _storage.SetAsync(key, newJson, cancellationToken);
    }

    /// <summary>Computes a cache key for the specified values.</summary>
    /// <param name="values">The values to inform the key.</param>
    /// <returns>The computed key.</returns>
    /// <remarks>
    /// <para>
    /// The <paramref name="values"/> are serialized to JSON using <see cref="JsonSerializerOptions"/> in order to compute the key.
    /// </para>
    /// <para>
    /// The generated cache key is not guaranteed to be stable across releases of the library.
    /// </para>
    /// </remarks>
    protected override string GetCacheKey(params ReadOnlySpan<object?> values) =>
        AIJsonUtilities.HashDataToString(values, _jsonSerializerOptions);
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/Embeddings/DistributedCachingEmbeddingGeneratorBuilderExtensions.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Extension methods for adding a <see cref="DistributedCachingEmbeddingGenerator{TInput, TEmbedding}"/> to an
/// <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> pipeline.
/// </summary>
public static class DistributedCachingEmbeddingGeneratorBuilderExtensions
{
    /// <summary>
    /// Adds a <see cref="DistributedCachingEmbeddingGenerator{TInput, TEmbedding}"/> as the next stage in the pipeline.
    /// </summary>
    /// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
    /// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
    /// <param name="builder">The <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/>.</param>
    /// <param name="storage">
    /// An optional <see cref="IDistributedCache"/> instance that will be used as the backing store for the cache. If not supplied, an instance will be resolved from the service provider.
    /// </param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="DistributedCachingEmbeddingGenerator{TInput, TEmbedding}"/> instance.</param>
    /// <returns>The <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> provided as <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static EmbeddingGeneratorBuilder<TInput, TEmbedding> UseDistributedCache<TInput, TEmbedding>(
        this EmbeddingGeneratorBuilder<TInput, TEmbedding> builder,
        IDistributedCache? storage = null,
        Action<DistributedCachingEmbeddingGenerator<TInput, TEmbedding>>? configure = null)
        where TEmbedding : Embedding
    {
        _ = Throw.IfNull(builder);
        return builder.Use((innerGenerator, services) =>
        {
            storage ??= services.GetRequiredService<IDistributedCache>();
            var result = new DistributedCachingEmbeddingGenerator<TInput, TEmbedding>(innerGenerator, storage);
            configure?.Invoke(result);
            return result;
        });
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/Embeddings/EmbeddingGeneratorBuilder.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A builder for creating pipelines of <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.</summary>
/// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
/// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
public sealed class EmbeddingGeneratorBuilder<TInput, TEmbedding>
    where TEmbedding : Embedding
{
    private readonly Func<IServiceProvider, IEmbeddingGenerator<TInput, TEmbedding>> _innerGeneratorFactory;

    /// <summary>The registered client factory instances.</summary>
    private List<Func<IEmbeddingGenerator<TInput, TEmbedding>, IServiceProvider, IEmbeddingGenerator<TInput, TEmbedding>>>? _generatorFactories;

    /// <summary>Initializes a new instance of the <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> class.</summary>
    /// <param name="innerGenerator">The inner <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> that represents the underlying backend.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerGenerator"/> is <see langword="null"/>.</exception>
    public EmbeddingGeneratorBuilder(IEmbeddingGenerator<TInput, TEmbedding> innerGenerator)
    {
        _ = Throw.IfNull(innerGenerator);
        _innerGeneratorFactory = _ => innerGenerator;
    }

    /// <summary>Initializes a new instance of the <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> class.</summary>
    /// <param name="innerGeneratorFactory">A callback that produces the inner <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> that represents the underlying backend.</param>
    public EmbeddingGeneratorBuilder(Func<IServiceProvider, IEmbeddingGenerator<TInput, TEmbedding>> innerGeneratorFactory)
    {
        _innerGeneratorFactory = Throw.IfNull(innerGeneratorFactory);
    }

    /// <summary>
    /// Builds an <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> that represents the entire pipeline. Calls to this instance will pass through each of the pipeline stages in turn.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceProvider"/> that should provide services to the <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> instances.
    /// If <see langword="null"/>, an empty <see cref="IServiceProvider"/> will be used.
    /// </param>
    /// <returns>An instance of <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> that represents the entire pipeline.</returns>
    public IEmbeddingGenerator<TInput, TEmbedding> Build(IServiceProvider? services = null)
    {
        services ??= EmptyServiceProvider.Instance;
        var embeddingGenerator = _innerGeneratorFactory(services);

        // To match intuitive expectations, apply the factories in reverse order, so that the first factory added is the outermost.
        if (_generatorFactories is not null)
        {
            for (var i = _generatorFactories.Count - 1; i >= 0; i--)
            {
                embeddingGenerator = _generatorFactories[i](embeddingGenerator, services);
                if (embeddingGenerator is null)
                {
                    Throw.InvalidOperationException(
                        $"The {nameof(IEmbeddingGenerator<TInput, TEmbedding>)} entry at index {i} returned null. " +
                        $"Ensure that the callbacks passed to {nameof(Use)} return non-null {nameof(IEmbeddingGenerator<TInput, TEmbedding>)} instances.");
                }
            }
        }

        return embeddingGenerator;
    }

    /// <summary>Adds a factory for an intermediate embedding generator to the embedding generator pipeline.</summary>
    /// <param name="generatorFactory">The generator factory function.</param>
    /// <returns>The updated <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="generatorFactory"/> is <see langword="null"/>.</exception>
    public EmbeddingGeneratorBuilder<TInput, TEmbedding> Use(Func<IEmbeddingGenerator<TInput, TEmbedding>, IEmbeddingGenerator<TInput, TEmbedding>> generatorFactory)
    {
        _ = Throw.IfNull(generatorFactory);

        return Use((innerGenerator, _) => generatorFactory(innerGenerator));
    }

    /// <summary>Adds a factory for an intermediate embedding generator to the embedding generator pipeline.</summary>
    /// <param name="generatorFactory">The generator factory function.</param>
    /// <returns>The updated <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="generatorFactory"/> is <see langword="null"/>.</exception>
    public EmbeddingGeneratorBuilder<TInput, TEmbedding> Use(
        Func<IEmbeddingGenerator<TInput, TEmbedding>, IServiceProvider, IEmbeddingGenerator<TInput, TEmbedding>> generatorFactory)
    {
        _ = Throw.IfNull(generatorFactory);

        _generatorFactories ??= [];
        _generatorFactories.Add(generatorFactory);
        return this;
    }

    /// <summary>
    /// Adds to the embedding generator pipeline an anonymous delegating embedding generator based on a delegate that provides
    /// an implementation for <see cref="IEmbeddingGenerator{TInput, TEmbedding}.GenerateAsync"/>.
    /// </summary>
    /// <param name="generateFunc">
    /// A delegate that provides the implementation for <see cref="IEmbeddingGenerator{TInput, TEmbedding}.GenerateAsync"/>.
    /// </param>
    /// <returns>The updated <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="generateFunc"/> is <see langword="null"/>.</exception>
    public EmbeddingGeneratorBuilder<TInput, TEmbedding> Use(
        Func<IEnumerable<TInput>, EmbeddingGenerationOptions?, IEmbeddingGenerator<TInput, TEmbedding>, CancellationToken, Task<GeneratedEmbeddings<TEmbedding>>>? generateFunc)
    {
        _ = Throw.IfNull(generateFunc);

        return Use((innerGenerator, _) => new AnonymousDelegatingEmbeddingGenerator<TInput, TEmbedding>(innerGenerator, generateFunc));
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/Embeddings/EmbeddingGeneratorBuilderEmbeddingGeneratorExtensions.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for working with <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>
/// in the context of <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/>.</summary>
public static class EmbeddingGeneratorBuilderEmbeddingGeneratorExtensions
{
    /// <summary>
    /// Creates a new <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> using
    /// <paramref name="innerGenerator"/> as its inner generator.
    /// </summary>
    /// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
    /// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
    /// <param name="innerGenerator">The generator to use as the inner generator.</param>
    /// <returns>The new <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> instance.</returns>
    /// <remarks>
    /// This method is equivalent to using the <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/>
    /// constructor directly, specifying <paramref name="innerGenerator"/> as the inner generator.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="innerGenerator"/> is <see langword="null"/>.</exception>
    public static EmbeddingGeneratorBuilder<TInput, TEmbedding> AsBuilder<TInput, TEmbedding>(
        this IEmbeddingGenerator<TInput, TEmbedding> innerGenerator)
        where TEmbedding : Embedding
    {
        _ = Throw.IfNull(innerGenerator);

        return new EmbeddingGeneratorBuilder<TInput, TEmbedding>(innerGenerator);
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/Embeddings/EmbeddingGeneratorBuilderServiceCollectionExtensions.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Provides extension methods for registering <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> with a <see cref="IServiceCollection"/>.</summary>
public static class EmbeddingGeneratorBuilderServiceCollectionExtensions
{
    /// <summary>Registers a singleton embedding generator in the <see cref="IServiceCollection"/>.</summary>
    /// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
    /// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the generator should be added.</param>
    /// <param name="innerGenerator">The inner <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> that can be used to build a pipeline around the inner generator.</returns>
    /// <remarks>The generator is registered as a singleton service.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="serviceCollection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="innerGenerator"/> is <see langword="null"/>.</exception>
    public static EmbeddingGeneratorBuilder<TInput, TEmbedding> AddEmbeddingGenerator<TInput, TEmbedding>(
        this IServiceCollection serviceCollection,
        IEmbeddingGenerator<TInput, TEmbedding> innerGenerator,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TEmbedding : Embedding
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerGenerator);

        return AddEmbeddingGenerator(serviceCollection, _ => innerGenerator, lifetime);
    }

    /// <summary>Registers a singleton embedding generator in the <see cref="IServiceCollection"/>.</summary>
    /// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
    /// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the generator should be added.</param>
    /// <param name="innerGeneratorFactory">A callback that produces the inner <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> that can be used to build a pipeline around the inner generator.</returns>
    /// <remarks>The generator is registered as a singleton service.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="serviceCollection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="innerGeneratorFactory"/> is <see langword="null"/>.</exception>
    public static EmbeddingGeneratorBuilder<TInput, TEmbedding> AddEmbeddingGenerator<TInput, TEmbedding>(
        this IServiceCollection serviceCollection,
        Func<IServiceProvider, IEmbeddingGenerator<TInput, TEmbedding>> innerGeneratorFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TEmbedding : Embedding
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerGeneratorFactory);

        var builder = new EmbeddingGeneratorBuilder<TInput, TEmbedding>(innerGeneratorFactory);
        serviceCollection.Add(new ServiceDescriptor(typeof(IEmbeddingGenerator<TInput, TEmbedding>), builder.Build, lifetime));
        serviceCollection.Add(new ServiceDescriptor(typeof(IEmbeddingGenerator),
            static services => services.GetRequiredService<IEmbeddingGenerator<TInput, TEmbedding>>(), lifetime));
        return builder;
    }

    /// <summary>Registers a keyed singleton embedding generator in the <see cref="IServiceCollection"/>.</summary>
    /// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
    /// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the generator should be added.</param>
    /// <param name="serviceKey">The key with which to associated the generator.</param>
    /// <param name="innerGenerator">The inner <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> that can be used to build a pipeline around the inner generator.</returns>
    /// <remarks>The generator is registered as a singleton service.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="serviceCollection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="innerGenerator"/> is <see langword="null"/>.</exception>
    public static EmbeddingGeneratorBuilder<TInput, TEmbedding> AddKeyedEmbeddingGenerator<TInput, TEmbedding>(
        this IServiceCollection serviceCollection,
        object? serviceKey,
        IEmbeddingGenerator<TInput, TEmbedding> innerGenerator,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TEmbedding : Embedding
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerGenerator);

        return AddKeyedEmbeddingGenerator(serviceCollection, serviceKey, _ => innerGenerator, lifetime);
    }

    /// <summary>Registers a keyed singleton embedding generator in the <see cref="IServiceCollection"/>.</summary>
    /// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
    /// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to which the generator should be added.</param>
    /// <param name="serviceKey">The key with which to associated the generator.</param>
    /// <param name="innerGeneratorFactory">A callback that produces the inner <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> that represents the underlying backend.</param>
    /// <param name="lifetime">The service lifetime for the client. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
    /// <returns>An <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/> that can be used to build a pipeline around the inner generator.</returns>
    /// <remarks>The generator is registered as a singleton service.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="serviceCollection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="innerGeneratorFactory"/> is <see langword="null"/>.</exception>
    public static EmbeddingGeneratorBuilder<TInput, TEmbedding> AddKeyedEmbeddingGenerator<TInput, TEmbedding>(
        this IServiceCollection serviceCollection,
        object? serviceKey,
        Func<IServiceProvider, IEmbeddingGenerator<TInput, TEmbedding>> innerGeneratorFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TEmbedding : Embedding
    {
        _ = Throw.IfNull(serviceCollection);
        _ = Throw.IfNull(innerGeneratorFactory);

        var builder = new EmbeddingGeneratorBuilder<TInput, TEmbedding>(innerGeneratorFactory);
        serviceCollection.Add(new ServiceDescriptor(typeof(IEmbeddingGenerator<TInput, TEmbedding>), serviceKey, factory: (services, serviceKey) => builder.Build(services), lifetime));
        serviceCollection.Add(new ServiceDescriptor(typeof(IEmbeddingGenerator), serviceKey,
            static (services, serviceKey) => services.GetRequiredKeyedService<IEmbeddingGenerator<TInput, TEmbedding>>(serviceKey), lifetime));
        return builder;
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/Embeddings/LoggingEmbeddingGenerator.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A delegating embedding generator that logs embedding generation operations to an <see cref="ILogger"/>.</summary>
/// <typeparam name="TInput">Specifies the type of the input passed to the generator.</typeparam>
/// <typeparam name="TEmbedding">Specifies the type of the embedding instance produced by the generator.</typeparam>
/// <remarks>
/// <para>
/// The provided implementation of <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> is thread-safe for concurrent use
/// so long as the <see cref="ILogger"/> employed is also thread-safe for concurrent use.
/// </para>
/// <para>
/// When the employed <see cref="ILogger"/> enables <see cref="Logging.LogLevel.Trace"/>, the contents of
/// values and options are logged. These values and options may contain sensitive application data.
/// <see cref="Logging.LogLevel.Trace"/> is disabled by default and should never be enabled in a production environment.
/// Messages and options are not logged at other logging levels.
/// </para>
/// </remarks>
public partial class LoggingEmbeddingGenerator<TInput, TEmbedding> : DelegatingEmbeddingGenerator<TInput, TEmbedding>
    where TEmbedding : Embedding
{
    /// <summary>An <see cref="ILogger"/> instance used for all logging.</summary>
    private readonly ILogger _logger;

    /// <summary>The <see cref="JsonSerializerOptions"/> to use for serialization of state written to the logger.</summary>
    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the <see cref="LoggingEmbeddingGenerator{TInput, TEmbedding}"/> class.</summary>
    /// <param name="innerGenerator">The underlying <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.</param>
    /// <param name="logger">An <see cref="ILogger"/> instance that will be used for all logging.</param>
    public LoggingEmbeddingGenerator(IEmbeddingGenerator<TInput, TEmbedding> innerGenerator, ILogger logger)
        : base(innerGenerator)
    {
        _logger = Throw.IfNull(logger);
        _jsonSerializerOptions = AIJsonUtilities.DefaultOptions;
    }

    /// <summary>Gets or sets JSON serialization options to use when serializing logging data.</summary>
    public JsonSerializerOptions JsonSerializerOptions
    {
        get => _jsonSerializerOptions;
        set => _jsonSerializerOptions = Throw.IfNull(value);
    }

    /// <inheritdoc/>
    public override async Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(IEnumerable<TInput> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                LogInvokedSensitive(AsJson(values), AsJson(options), AsJson(this.GetService<EmbeddingGeneratorMetadata>()));
            }
            else
            {
                LogInvoked();
            }
        }

        try
        {
            var embeddings = await base.GenerateAsync(values, options, cancellationToken);

            LogCompleted(embeddings.Count);

            return embeddings;
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled();
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(ex);
            throw;
        }
    }

    private string AsJson<T>(T value) => JsonSerializer.Serialize(value, _jsonSerializerOptions.GetTypeInfo(typeof(T)));

    [LoggerMessage(LogLevel.Debug, "GenerateAsync invoked.")]
    private partial void LogInvoked();

    [LoggerMessage(LogLevel.Trace, "GenerateAsync invoked: {Values}. Options: {EmbeddingGenerationOptions}. Metadata: {EmbeddingGeneratorMetadata}.")]
    private partial void LogInvokedSensitive(string values, string embeddingGenerationOptions, string embeddingGeneratorMetadata);

    [LoggerMessage(LogLevel.Debug, "GenerateAsync generated {EmbeddingsCount} embedding(s).")]
    private partial void LogCompleted(int embeddingsCount);

    [LoggerMessage(LogLevel.Debug, "GenerateAsync canceled.")]
    private partial void LogInvocationCanceled();

    [LoggerMessage(LogLevel.Error, "GenerateAsync failed.")]
    private partial void LogInvocationFailed(Exception error);
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/Embeddings/LoggingEmbeddingGeneratorBuilderExtensions.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="LoggingEmbeddingGenerator{TInput, TEmbedding}"/> instances.</summary>
public static class LoggingEmbeddingGeneratorBuilderExtensions
{
    /// <summary>Adds logging to the embedding generator pipeline.</summary>
    /// <typeparam name="TInput">Specifies the type of the input passed to the generator.</typeparam>
    /// <typeparam name="TEmbedding">Specifies the type of the embedding instance produced by the generator.</typeparam>
    /// <param name="builder">The <see cref="EmbeddingGeneratorBuilder{TInput, TEmbedding}"/>.</param>
    /// <param name="loggerFactory">
    /// An optional <see cref="ILoggerFactory"/> used to create a logger with which logging should be performed.
    /// If not supplied, a required instance will be resolved from the service provider.
    /// </param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="LoggingEmbeddingGenerator{TInput, TEmbedding}"/> instance.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// When the employed <see cref="ILogger"/> enables <see cref="Logging.LogLevel.Trace"/>, the contents of
    /// values and options are logged. These values and options may contain sensitive application data.
    /// <see cref="Logging.LogLevel.Trace"/> is disabled by default and should never be enabled in a production environment.
    /// Messages and options are not logged at other logging levels.
    /// </para>
    /// </remarks>
    public static EmbeddingGeneratorBuilder<TInput, TEmbedding> UseLogging<TInput, TEmbedding>(
        this EmbeddingGeneratorBuilder<TInput, TEmbedding> builder,
        ILoggerFactory? loggerFactory = null,
        Action<LoggingEmbeddingGenerator<TInput, TEmbedding>>? configure = null)
        where TEmbedding : Embedding
    {
        _ = Throw.IfNull(builder);

        return builder.Use((innerGenerator, services) =>
        {
            loggerFactory ??= services.GetRequiredService<ILoggerFactory>();

            // If the factory we resolve is for the null logger, the LoggingEmbeddingGenerator will end up
            // being an expensive nop, so skip adding it and just return the inner generator.
            if (loggerFactory == NullLoggerFactory.Instance)
            {
                return innerGenerator;
            }

            var generator = new LoggingEmbeddingGenerator<TInput, TEmbedding>(innerGenerator, loggerFactory.CreateLogger(typeof(LoggingEmbeddingGenerator<TInput, TEmbedding>)));
            configure?.Invoke(generator);
            return generator;
        });
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/Embeddings/OpenTelemetryEmbeddingGenerator.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

#pragma warning disable SA1111 // Closing parenthesis should be on line of last parameter
#pragma warning disable SA1113 // Comma should be on the same line as previous parameter

namespace Microsoft.Extensions.AI;

/// <summary>Represents a delegating embedding generator that implements the OpenTelemetry Semantic Conventions for Generative AI systems.</summary>
/// <remarks>
/// This class provides an implementation of the Semantic Conventions for Generative AI systems v1.34, defined at <see href="https://opentelemetry.io/docs/specs/semconv/gen-ai/" />.
/// The specification is still experimental and subject to change; as such, the telemetry output by this client is also subject to change.
/// </remarks>
/// <typeparam name="TInput">The type of input used to produce embeddings.</typeparam>
/// <typeparam name="TEmbedding">The type of embedding generated.</typeparam>
public sealed class OpenTelemetryEmbeddingGenerator<TInput, TEmbedding> : DelegatingEmbeddingGenerator<TInput, TEmbedding>
    where TEmbedding : Embedding
{
    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;

    private readonly Histogram<int> _tokenUsageHistogram;
    private readonly Histogram<double> _operationDurationHistogram;

    private readonly string? _system;
    private readonly string? _defaultModelId;
    private readonly int? _defaultModelDimensions;
    private readonly string? _modelProvider;
    private readonly string? _endpointAddress;
    private readonly int _endpointPort;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenTelemetryEmbeddingGenerator{TInput, TEmbedding}"/> class.
    /// </summary>
    /// <param name="innerGenerator">The underlying <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>, which is the next stage of the pipeline.</param>
    /// <param name="logger">The <see cref="ILogger"/> to use for emitting events.</param>
    /// <param name="sourceName">An optional source name that will be used on the telemetry data.</param>
#pragma warning disable IDE0060 // Remove unused parameter; it exists for future use and consistency with OpenTelemetryChatClient
    public OpenTelemetryEmbeddingGenerator(IEmbeddingGenerator<TInput, TEmbedding> innerGenerator, ILogger? logger = null, string? sourceName = null)
#pragma warning restore IDE0060
        : base(innerGenerator)
    {
        Debug.Assert(innerGenerator is not null, "Should have been validated by the base ctor.");

        if (innerGenerator!.GetService<EmbeddingGeneratorMetadata>() is EmbeddingGeneratorMetadata metadata)
        {
            _system = metadata.ProviderName;
            _defaultModelId = metadata.DefaultModelId;
            _defaultModelDimensions = metadata.DefaultModelDimensions;
            _modelProvider = metadata.ProviderName;
            _endpointAddress = metadata.ProviderUri?.GetLeftPart(UriPartial.Path);
            _endpointPort = metadata.ProviderUri?.Port ?? 0;
        }

        string name = string.IsNullOrEmpty(sourceName) ? OpenTelemetryConsts.DefaultSourceName : sourceName!;
        _activitySource = new(name);
        _meter = new(name);

        _tokenUsageHistogram = _meter.CreateHistogram<int>(
            OpenTelemetryConsts.GenAI.Client.TokenUsage.Name,
            OpenTelemetryConsts.TokensUnit,
            OpenTelemetryConsts.GenAI.Client.TokenUsage.Description
#if NET9_0_OR_GREATER
            , advice: new() { HistogramBucketBoundaries = OpenTelemetryConsts.GenAI.Client.TokenUsage.ExplicitBucketBoundaries }
#endif
            );

        _operationDurationHistogram = _meter.CreateHistogram<double>(
            OpenTelemetryConsts.GenAI.Client.OperationDuration.Name,
            OpenTelemetryConsts.SecondsUnit,
            OpenTelemetryConsts.GenAI.Client.OperationDuration.Description
#if NET9_0_OR_GREATER
            , advice: new() { HistogramBucketBoundaries = OpenTelemetryConsts.GenAI.Client.OperationDuration.ExplicitBucketBoundaries }
#endif
            );
    }

    /// <summary>
    /// Gets or sets a value indicating whether potentially sensitive information should be included in telemetry.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if potentially sensitive information should be included in telemetry;
    /// <see langword="false"/> if telemetry shouldn't include raw inputs and outputs.
    /// The default value is <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// By default, telemetry includes metadata, such as token counts, but not raw inputs
    /// and outputs or additional options data.
    /// </remarks>
    public bool EnableSensitiveData { get; set; }

    /// <inheritdoc/>
    public override object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType == typeof(ActivitySource) ? _activitySource :
        base.GetService(serviceType, serviceKey);

    /// <inheritdoc/>
    public override async Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(IEnumerable<TInput> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(values);

        using Activity? activity = CreateAndConfigureActivity(options);
        Stopwatch? stopwatch = _operationDurationHistogram.Enabled ? Stopwatch.StartNew() : null;
        string? requestModelId = options?.ModelId ?? _defaultModelId;

        GeneratedEmbeddings<TEmbedding>? response = null;
        Exception? error = null;
        try
        {
            response = await base.GenerateAsync(values, options, cancellationToken);
        }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
        finally
        {
            TraceResponse(activity, requestModelId, response, error, stopwatch);
        }

        return response;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _activitySource.Dispose();
            _meter.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>Creates an activity for an embedding generation request, or returns <see langword="null"/> if not enabled.</summary>
    private Activity? CreateAndConfigureActivity(EmbeddingGenerationOptions? options)
    {
        Activity? activity = null;
        if (_activitySource.HasListeners())
        {
            string? modelId = options?.ModelId ?? _defaultModelId;

            activity = _activitySource.StartActivity(
                string.IsNullOrWhiteSpace(modelId) ? OpenTelemetryConsts.GenAI.Embeddings : $"{OpenTelemetryConsts.GenAI.Embeddings} {modelId}",
                ActivityKind.Client,
                default(ActivityContext),
                [
                    new(OpenTelemetryConsts.GenAI.Operation.Name, OpenTelemetryConsts.GenAI.Embeddings),
                    new(OpenTelemetryConsts.GenAI.Request.Model, modelId),
                    new(OpenTelemetryConsts.GenAI.SystemName, _modelProvider),
                ]);

            if (activity is not null)
            {
                if (_endpointAddress is not null)
                {
                    _ = activity
                        .AddTag(OpenTelemetryConsts.Server.Address, _endpointAddress)
                        .AddTag(OpenTelemetryConsts.Server.Port, _endpointPort);
                }

                if ((options?.Dimensions ?? _defaultModelDimensions) is int dimensionsValue)
                {
                    _ = activity.AddTag(OpenTelemetryConsts.GenAI.Request.EmbeddingDimensions, dimensionsValue);
                }

                // Log all additional request options as per-provider tags. This is non-normative, but it covers cases where
                // there's a per-provider specification in a best-effort manner (e.g. gen_ai.openai.request.service_tier),
                // and more generally cases where there's additional useful information to be logged.
                // Since AdditionalProperties has undefined meaning, we treat it as potentially sensitive data.
                if (EnableSensitiveData &&
                    _system is not null &&
                    options?.AdditionalProperties is { } props)
                {
                    foreach (KeyValuePair<string, object?> prop in props)
                    {
                        _ = activity.AddTag(
                            OpenTelemetryConsts.GenAI.Request.PerProvider(_system, JsonNamingPolicy.SnakeCaseLower.ConvertName(prop.Key)),
                            prop.Value);
                    }
                }
            }
        }

        return activity;
    }

    /// <summary>Adds embedding generation response information to the activity.</summary>
    private void TraceResponse(
        Activity? activity,
        string? requestModelId,
        GeneratedEmbeddings<TEmbedding>? embeddings,
        Exception? error,
        Stopwatch? stopwatch)
    {
        int? inputTokens = null;
        string? responseModelId = null;
        if (embeddings is not null)
        {
            responseModelId = embeddings.FirstOrDefault()?.ModelId;
            if (embeddings.Usage?.InputTokenCount is long i)
            {
                inputTokens = inputTokens.GetValueOrDefault() + (int)i;
            }
        }

        if (_operationDurationHistogram.Enabled && stopwatch is not null)
        {
            TagList tags = default;
            AddMetricTags(ref tags, requestModelId, responseModelId);
            if (error is not null)
            {
                tags.Add(OpenTelemetryConsts.Error.Type, error.GetType().FullName);
            }

            _operationDurationHistogram.Record(stopwatch.Elapsed.TotalSeconds, tags);
        }

        if (_tokenUsageHistogram.Enabled && inputTokens.HasValue)
        {
            TagList tags = default;
            tags.Add(OpenTelemetryConsts.GenAI.Token.Type, "input");
            AddMetricTags(ref tags, requestModelId, responseModelId);

            _tokenUsageHistogram.Record(inputTokens.Value);
        }

        if (activity is not null)
        {
            if (error is not null)
            {
                _ = activity
                    .AddTag(OpenTelemetryConsts.Error.Type, error.GetType().FullName)
                    .SetStatus(ActivityStatusCode.Error, error.Message);
            }

            if (inputTokens.HasValue)
            {
                _ = activity.AddTag(OpenTelemetryConsts.GenAI.Usage.InputTokens, inputTokens);
            }

            if (responseModelId is not null)
            {
                _ = activity.AddTag(OpenTelemetryConsts.GenAI.Response.Model, responseModelId);
            }

            // Log all additional response properties as per-provider tags. This is non-normative, but it covers cases where
            // there's a per-provider specification in a best-effort manner (e.g. gen_ai.openai.response.system_fingerprint),
            // and more generally cases where there's additional useful information to be logged.
            if (EnableSensitiveData &&
                _system is not null &&
                embeddings?.AdditionalProperties is { } props)
            {
                foreach (KeyValuePair<string, object?> prop in props)
                {
                    _ = activity.AddTag(
                        OpenTelemetryConsts.GenAI.Response.PerProvider(_system, JsonNamingPolicy.SnakeCaseLower.ConvertName(prop.Key)),
                        prop.Value);
                }
            }
        }
    }

    private void AddMetricTags(ref TagList tags, string? requestModelId, string? responseModelId)
    {
        tags.Add(OpenTelemetryConsts.GenAI.Operation.Name, OpenTelemetryConsts.GenAI.Embeddings);

        if (requestModelId is not null)
        {
            tags.Add(OpenTelemetryConsts.GenAI.Request.Model, requestModelId);
        }

        tags.Add(OpenTelemetryConsts.GenAI.SystemName, _modelProvider);

        if (_endpointAddress is string endpointAddress)
        {
            tags.Add(OpenTelemetryConsts.Server.Address, endpointAddress);
            tags.Add(OpenTelemetryConsts.Server.Port, _endpointPort);
        }

        // Assume all of the embeddings in the same batch used the same model
        if (responseModelId is not null)
        {
            tags.Add(OpenTelemetryConsts.GenAI.Response.Model, responseModelId);
        }
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI/Embeddings/OpenTelemetryEmbeddingGeneratorBuilderExtensions.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="OpenTelemetryEmbeddingGenerator{TInput, TEmbedding}"/> instances.</summary>
public static class OpenTelemetryEmbeddingGeneratorBuilderExtensions
{
    /// <summary>
    /// Adds OpenTelemetr