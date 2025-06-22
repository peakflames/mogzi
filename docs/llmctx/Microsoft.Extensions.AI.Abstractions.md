(Files content cropped to 300k characters, download full ingest to see more)
================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/README.md
================================================
# Microsoft.Extensions.AI.Abstractions

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
dotnet add package Microsoft.Extensions.AI.Abstractions
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.AI.Abstractions" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Documentation

Refer to the [Microsoft.Extensions.AI libraries documentation](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai) for more information and API usage examples.

## Feedback & Contributing

We welcome feedback and contributions in [our GitHub repo](https://github.com/dotnet/extensions).



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/AdditionalPropertiesDictionary.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S2365 // Properties should not make collection or array copies
#pragma warning disable S3604 // Member initializer values should not be redundant

using System.Collections.Generic;

namespace Microsoft.Extensions.AI;

/// <summary>Provides a dictionary used as the AdditionalProperties dictionary on Microsoft.Extensions.AI objects.</summary>
public sealed class AdditionalPropertiesDictionary : AdditionalPropertiesDictionary<object?>
{
    /// <summary>Initializes a new instance of the <see cref="AdditionalPropertiesDictionary"/> class.</summary>
    public AdditionalPropertiesDictionary()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AdditionalPropertiesDictionary"/> class.</summary>
    public AdditionalPropertiesDictionary(IDictionary<string, object?> dictionary)
        : base(dictionary)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AdditionalPropertiesDictionary"/> class.</summary>
    public AdditionalPropertiesDictionary(IEnumerable<KeyValuePair<string, object?>> collection)
        : base(collection)
    {
    }

    /// <summary>Creates a shallow clone of the properties dictionary.</summary>
    /// <returns>
    /// A shallow clone of the properties dictionary. The instance will not be the same as the current instance,
    /// but it will contain all of the same key-value pairs.
    /// </returns>
    public new AdditionalPropertiesDictionary Clone() => new(this);
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/AdditionalPropertiesDictionary{TValue}.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S2365 // Properties should not make collection or array copies
#pragma warning disable S3604 // Member initializer values should not be redundant
#pragma warning disable S4039 // Interface methods should be callable by derived types
#pragma warning disable CA1033 // Interface methods should be callable by derived types

namespace Microsoft.Extensions.AI;

/// <summary>Provides a dictionary used as the AdditionalProperties dictionary on Microsoft.Extensions.AI objects.</summary>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(AdditionalPropertiesDictionary<>.DebugView))]
public class AdditionalPropertiesDictionary<TValue> : IDictionary<string, TValue>, IReadOnlyDictionary<string, TValue>
{
    /// <summary>The underlying dictionary.</summary>
    private readonly Dictionary<string, TValue> _dictionary;

    /// <summary>Initializes a new instance of the <see cref="AdditionalPropertiesDictionary{TValue}"/> class.</summary>
    public AdditionalPropertiesDictionary()
    {
        _dictionary = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Initializes a new instance of the <see cref="AdditionalPropertiesDictionary{TValue}"/> class.</summary>
    public AdditionalPropertiesDictionary(IDictionary<string, TValue> dictionary)
    {
        _dictionary = new(dictionary, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Initializes a new instance of the <see cref="AdditionalPropertiesDictionary{TValue}"/> class.</summary>
    public AdditionalPropertiesDictionary(IEnumerable<KeyValuePair<string, TValue>> collection)
    {
#if NET
        _dictionary = new(collection, StringComparer.OrdinalIgnoreCase);
#else
        _dictionary = new Dictionary<string, TValue>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in collection)
        {
            _dictionary.Add(item.Key, item.Value);
        }
#endif
    }

    /// <summary>Creates a shallow clone of the properties dictionary.</summary>
    /// <returns>
    /// A shallow clone of the properties dictionary. The instance will not be the same as the current instance,
    /// but it will contain all of the same key-value pairs.
    /// </returns>
    public AdditionalPropertiesDictionary<TValue> Clone() => new(_dictionary);

    /// <inheritdoc />
    public TValue this[string key]
    {
        get => _dictionary[key];
        set => _dictionary[key] = value;
    }

    /// <inheritdoc />
    public ICollection<string> Keys => _dictionary.Keys;

    /// <inheritdoc />
    public ICollection<TValue> Values => _dictionary.Values;

    /// <inheritdoc />
    public int Count => _dictionary.Count;

    /// <inheritdoc />
    bool ICollection<KeyValuePair<string, TValue>>.IsReadOnly => false;

    /// <inheritdoc />
    IEnumerable<string> IReadOnlyDictionary<string, TValue>.Keys => _dictionary.Keys;

    /// <inheritdoc />
    IEnumerable<TValue> IReadOnlyDictionary<string, TValue>.Values => _dictionary.Values;

    /// <inheritdoc />
    public void Add(string key, TValue value) => _dictionary.Add(key, value);

    /// <summary>Attempts to add the specified key and value to the dictionary.</summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    /// <returns><see langword="true"/> if the key/value pair was added to the dictionary successfully; otherwise, <see langword="false"/>.</returns>
    public bool TryAdd(string key, TValue value)
    {
#if NET
        return _dictionary.TryAdd(key, value);
#else
        if (!_dictionary.ContainsKey(key))
        {
            _dictionary.Add(key, value);
            return true;
        }

        return false;
#endif
    }

    /// <inheritdoc />
    void ICollection<KeyValuePair<string, TValue>>.Add(KeyValuePair<string, TValue> item) => ((ICollection<KeyValuePair<string, TValue>>)_dictionary).Add(item);

    /// <inheritdoc />
    public void Clear() => _dictionary.Clear();

    /// <inheritdoc />
    bool ICollection<KeyValuePair<string, TValue>>.Contains(KeyValuePair<string, TValue> item) =>
        ((ICollection<KeyValuePair<string, TValue>>)_dictionary).Contains(item);

    /// <inheritdoc />
    public bool ContainsKey(string key) => _dictionary.ContainsKey(key);

    /// <inheritdoc />
    void ICollection<KeyValuePair<string, TValue>>.CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex) =>
        ((ICollection<KeyValuePair<string, TValue>>)_dictionary).CopyTo(array, arrayIndex);

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="AdditionalPropertiesDictionary{TValue}"/>.
    /// </summary>
    /// <returns>An <see cref="AdditionalPropertiesDictionary{TValue}.Enumerator"/> that enumerates the contents of the <see cref="AdditionalPropertiesDictionary{TValue}"/>.</returns>
    public Enumerator GetEnumerator() => new(_dictionary.GetEnumerator());

    /// <inheritdoc />
    IEnumerator<KeyValuePair<string, TValue>> IEnumerable<KeyValuePair<string, TValue>>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public bool Remove(string key) => _dictionary.Remove(key);

    /// <inheritdoc />
    bool ICollection<KeyValuePair<string, TValue>>.Remove(KeyValuePair<string, TValue> item) => ((ICollection<KeyValuePair<string, TValue>>)_dictionary).Remove(item);

    /// <summary>Attempts to extract a typed value from the dictionary.</summary>
    /// <typeparam name="T">The type of the value to be retrieved.</typeparam>
    /// <param name="key">The key to locate.</param>
    /// <param name="value">
    /// When this method returns, contains the value retrieved from the dictionary, if found and successfully converted to the requested type;
    /// otherwise, the default value of <typeparamref name="T"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a non-<see langword="null"/> value was found for <paramref name="key"/>
    /// in the dictionary and converted to the requested type; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// If a non-<see langword="null"/> value is found for the key in the dictionary, but the value is not of the requested type and is
    /// an <see cref="IConvertible"/> object, the method attempts to convert the object to the requested type.
    /// </remarks>
    public bool TryGetValue<T>(string key, [NotNullWhen(true)] out T? value)
    {
        if (TryGetValue(key, out TValue? obj))
        {
            switch (obj)
            {
                case T t:
                    // The object is already of the requested type. Return it.
                    value = t;
                    return true;

                case IConvertible:
                    // The object is convertible; try to convert it to the requested type. Unfortunately, there's no
                    // convenient way to do this that avoids exceptions and that doesn't involve a ton of boilerplate,
                    // so we only try when the source object is at least an IConvertible, which is what ChangeType uses.
                    try
                    {
                        value = (T)Convert.ChangeType(obj, typeof(T), CultureInfo.InvariantCulture);
                        return true;
                    }
                    catch (Exception e) when (e is ArgumentException or FormatException or InvalidCastException or OverflowException)
                    {
                        // Ignore known failure modes.
                    }

                    break;
            }
        }

        // Unable to find the value or convert it to the requested type.
        value = default;
        return false;
    }

    /// <summary>Gets the value associated with the specified key.</summary>
    /// <returns><see langword="true"/> if the <see cref="AdditionalPropertiesDictionary{TValue}"/> contains an element with the specified key; otherwise <see langword="false"/>.</returns>
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out TValue value) => _dictionary.TryGetValue(key, out value);

    /// <inheritdoc />
    bool IDictionary<string, TValue>.TryGetValue(string key, out TValue value) => _dictionary.TryGetValue(key, out value!);

    /// <inheritdoc />
    bool IReadOnlyDictionary<string, TValue>.TryGetValue(string key, out TValue value) => _dictionary.TryGetValue(key, out value!);

    /// <summary>Copies all of the entries from <paramref name="items"/> into the dictionary, overwriting any existing items in the dictionary with the same key.</summary>
    /// <param name="items">The items to add.</param>
    internal void SetAll(IEnumerable<KeyValuePair<string, TValue>> items)
    {
        _ = Throw.IfNull(items);

        foreach (var item in items)
        {
            _dictionary[item.Key] = item.Value;
        }
    }

    /// <summary>Enumerates the elements of an <see cref="AdditionalPropertiesDictionary{TValue}"/>.</summary>
    public struct Enumerator : IEnumerator<KeyValuePair<string, TValue>>
    {
        /// <summary>The wrapped dictionary enumerator.</summary>
        private Dictionary<string, TValue>.Enumerator _dictionaryEnumerator;

        /// <summary>Initializes a new instance of the <see cref="Enumerator"/> struct with the dictionary enumerator to wrap.</summary>
        /// <param name="dictionaryEnumerator">The dictionary enumerator to wrap.</param>
        internal Enumerator(Dictionary<string, TValue>.Enumerator dictionaryEnumerator)
        {
            _dictionaryEnumerator = dictionaryEnumerator;
        }

        /// <inheritdoc />
        public KeyValuePair<string, TValue> Current => _dictionaryEnumerator.Current;

        /// <inheritdoc />
        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public void Dispose() => _dictionaryEnumerator.Dispose();

        /// <inheritdoc />
        public bool MoveNext() => _dictionaryEnumerator.MoveNext();

        /// <inheritdoc />
        public void Reset() => Reset(ref _dictionaryEnumerator);

        /// <summary>Calls <see cref="IEnumerator.Reset"/> on an enumerator.</summary>
        private static void Reset<TEnumerator>(ref TEnumerator enumerator)
            where TEnumerator : struct, IEnumerator
        {
            enumerator.Reset();
        }
    }

    /// <summary>Provides a debugger view for the collection.</summary>
    private sealed class DebugView(AdditionalPropertiesDictionary<TValue> properties)
    {
        private readonly AdditionalPropertiesDictionary<TValue> _properties = Throw.IfNull(properties);

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public AdditionalProperty[] Items => (from p in _properties select new AdditionalProperty(p.Key, p.Value)).ToArray();

        [DebuggerDisplay("{Value}", Name = "[{Key}]")]
        public readonly struct AdditionalProperty(string key, TValue value)
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
            public string Key { get; } = key;

            [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
            public TValue Value { get; } = value;
        }
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/AITool.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Shared.Collections;

namespace Microsoft.Extensions.AI;

#pragma warning disable S1694 // An abstract class should have both abstract and concrete methods

/// <summary>Represents a tool that can be specified to an AI service.</summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public abstract class AITool
{
    /// <summary>Initializes a new instance of the <see cref="AITool"/> class.</summary>
    protected AITool()
    {
    }

    /// <summary>Gets the name of the tool.</summary>
    public virtual string Name => GetType().Name;

    /// <summary>Gets a description of the tool, suitable for use in describing the purpose to a model.</summary>
    public virtual string Description => string.Empty;

    /// <summary>Gets any additional properties associated with the tool.</summary>
    public virtual IReadOnlyDictionary<string, object?> AdditionalProperties => EmptyReadOnlyDictionary<string, object?>.Instance;

    /// <inheritdoc/>
    public override string ToString() => Name;

    /// <summary>Gets the string to display in the debugger for this instance.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay
    {
        get
        {
            StringBuilder sb = new(Name);

            if (Description is string description && !string.IsNullOrEmpty(description))
            {
                _ = sb.Append(" (").Append(description).Append(')');
            }

            foreach (var entry in AdditionalProperties)
            {
                _ = sb.Append(", ").Append(entry.Key).Append(" = ").Append(entry.Value);
            }

            return sb.ToString();
        }
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/HostedCodeInterpreterTool.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

/// <summary>Represents a hosted tool that can be specified to an AI service to enable it to execute code it generates.</summary>
/// <remarks>
/// This tool does not itself implement code interpretation. It is a marker that can be used to inform a service
/// that the service is allowed to execute its generated code if the service is capable of doing so.
/// </remarks>
public class HostedCodeInterpreterTool : AITool
{
    /// <summary>Initializes a new instance of the <see cref="HostedCodeInterpreterTool"/> class.</summary>
    public HostedCodeInterpreterTool()
    {
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/HostedWebSearchTool.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

/// <summary>Represents a hosted tool that can be specified to an AI service to enable it to perform web searches.</summary>
/// <remarks>
/// This tool does not itself implement web searches. It is a marker that can be used to inform a service
/// that the service is allowed to perform web searches if the service is capable of doing so.
/// </remarks>
public class HostedWebSearchTool : AITool
{
    /// <summary>Initializes a new instance of the <see cref="HostedWebSearchTool"/> class.</summary>
    public HostedWebSearchTool()
    {
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/Microsoft.Extensions.AI.Abstractions.csproj
================================================
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>Microsoft.Extensions.AI</RootNamespace>
    <Description>Abstractions representing generative AI components.</Description>
    <Workstream>AI</Workstream>
  </PropertyGroup>

  <PropertyGroup>
    <Stage>normal</Stage>
    <MinCodeCoverage>82</MinCodeCoverage>
    <MinMutationScore>85</MinMutationScore>
  </PropertyGroup>

  <PropertyGroup>
    <TargetFrameworks>$(TargetFrameworks);netstandard2.0</TargetFrameworks>
    <NoWarn>$(NoWarn);CA2227;CA1034;SA1316;S3253</NoWarn>
    <NoWarn>$(NoWarn);MEAI001</NoWarn>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <DisableNETStandardCompatErrors>true</DisableNETStandardCompatErrors>
  </PropertyGroup>

  <PropertyGroup>
    <InjectExperimentalAttributeOnLegacy>true</InjectExperimentalAttributeOnLegacy>
    <InjectJsonSchemaExporterOnLegacy>true</InjectJsonSchemaExporterOnLegacy>
    <InjectRequiredMemberOnLegacy>true</InjectRequiredMemberOnLegacy>
    <InjectSharedEmptyCollections>true</InjectSharedEmptyCollections>
    <InjectStringHashOnLegacy>true</InjectStringHashOnLegacy>
    <InjectStringSyntaxAttributeOnLegacy>true</InjectStringSyntaxAttributeOnLegacy>
    <InjectSystemIndexOnLegacy>true</InjectSystemIndexOnLegacy>
  </PropertyGroup>

  <ItemGroup Condition="'$(TargetFrameworkIdentifier)' != '.NETCoreApp'">
    <PackageReference Include="System.Text.Json" />
  </ItemGroup>

  <ItemGroup Condition="'$(TargetFramework)' == 'net462'">
    <Reference Include="System.Net.Http" />
  </ItemGroup>
  
</Project>



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/Microsoft.Extensions.AI.Abstractions.json
================================================
{
  "Name": "Microsoft.Extensions.AI.Abstractions, Version=9.6.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
  "Types": [
    {
      "Type": "sealed class Microsoft.Extensions.AI.AdditionalPropertiesDictionary : Microsoft.Extensions.AI.AdditionalPropertiesDictionary<object?>",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.AdditionalPropertiesDictionary.AdditionalPropertiesDictionary();",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.AdditionalPropertiesDictionary.AdditionalPropertiesDictionary(System.Collections.Generic.IDictionary<string, object?> dictionary);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.AdditionalPropertiesDictionary.AdditionalPropertiesDictionary(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>> collection);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.AdditionalPropertiesDictionary Microsoft.Extensions.AI.AdditionalPropertiesDictionary.Clone();",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue> : System.Collections.Generic.IDictionary<string, TValue>, System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, TValue>>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, TValue>>, System.Collections.IEnumerable, System.Collections.Generic.IReadOnlyDictionary<string, TValue>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<string, TValue>>",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue>.AdditionalPropertiesDictionary();",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue>.AdditionalPropertiesDictionary(System.Collections.Generic.IDictionary<string, TValue> dictionary);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue>.AdditionalPropertiesDictionary(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, TValue>> collection);",
          "Stage": "Stable"
        },
        {
          "Member": "void Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue>.Add(string key, TValue value);",
          "Stage": "Stable"
        },
        {
          "Member": "void Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue>.Clear();",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue> Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue>.Clone();",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue>.ContainsKey(string key);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue>.Enumerator Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue>.GetEnumerator();",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue>.Remove(string key);",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue>.TryAdd(string key, TValue value);",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue>.TryGetValue<T>(string key, out T? value);",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue>.TryGetValue(string key, out TValue value);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "int Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue>.Count { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "TValue Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue>.this[string key] { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Collections.Generic.ICollection<string> Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue>.Keys { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Collections.Generic.ICollection<TValue> Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue>.Values { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "struct Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue>.Enumerator",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue>.Enumerator.Enumerator();",
          "Stage": "Stable"
        },
        {
          "Member": "void Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue>.Enumerator.Dispose();",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue>.Enumerator.MoveNext();",
          "Stage": "Stable"
        },
        {
          "Member": "void Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue>.Enumerator.Reset();",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "System.Collections.Generic.KeyValuePair<string, TValue> Microsoft.Extensions.AI.AdditionalPropertiesDictionary<TValue>.Enumerator.Current { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.AIContent",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.AIContent.AIContent();",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "Microsoft.Extensions.AI.AdditionalPropertiesDictionary? Microsoft.Extensions.AI.AIContent.AdditionalProperties { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "object? Microsoft.Extensions.AI.AIContent.RawRepresentation { get; set; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "abstract class Microsoft.Extensions.AI.AIFunction : Microsoft.Extensions.AI.AITool",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.AIFunction.AIFunction();",
          "Stage": "Stable"
        },
        {
          "Member": "System.Threading.Tasks.ValueTask<object?> Microsoft.Extensions.AI.AIFunction.InvokeAsync(Microsoft.Extensions.AI.AIFunctionArguments? arguments = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          "Member": "abstract System.Threading.Tasks.ValueTask<object?> Microsoft.Extensions.AI.AIFunction.InvokeCoreAsync(Microsoft.Extensions.AI.AIFunctionArguments arguments, System.Threading.CancellationToken cancellationToken);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "virtual System.Text.Json.JsonElement Microsoft.Extensions.AI.AIFunction.JsonSchema { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "virtual System.Text.Json.JsonSerializerOptions Microsoft.Extensions.AI.AIFunction.JsonSerializerOptions { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "virtual System.Text.Json.JsonElement? Microsoft.Extensions.AI.AIFunction.ReturnJsonSchema { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "virtual System.Reflection.MethodInfo? Microsoft.Extensions.AI.AIFunction.UnderlyingMethod { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.AIFunctionArguments : System.Collections.Generic.IDictionary<string, object?>, System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>>, System.Collections.IEnumerable, System.Collections.Generic.IReadOnlyDictionary<string, object?>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<string, object?>>",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.AIFunctionArguments.AIFunctionArguments();",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.AIFunctionArguments.AIFunctionArguments(System.Collections.Generic.IDictionary<string, object?>? arguments);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.AIFunctionArguments.AIFunctionArguments(System.Collections.Generic.IEqualityComparer<string>? comparer);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.AIFunctionArguments.AIFunctionArguments(System.Collections.Generic.IDictionary<string, object?>? arguments, System.Collections.Generic.IEqualityComparer<string>? comparer);",
          "Stage": "Stable"
        },
        {
          "Member": "void Microsoft.Extensions.AI.AIFunctionArguments.Add(string key, object? value);",
          "Stage": "Stable"
        },
        {
          "Member": "void Microsoft.Extensions.AI.AIFunctionArguments.Clear();",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.AIFunctionArguments.ContainsKey(string key);",
          "Stage": "Stable"
        },
        {
          "Member": "void Microsoft.Extensions.AI.AIFunctionArguments.CopyTo(System.Collections.Generic.KeyValuePair<string, object?>[] array, int arrayIndex);",
          "Stage": "Stable"
        },
        {
          "Member": "System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object?>> Microsoft.Extensions.AI.AIFunctionArguments.GetEnumerator();",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.AIFunctionArguments.Remove(string key);",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.AIFunctionArguments.TryGetValue(string key, out object? value);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "System.Collections.Generic.IDictionary<object, object?>? Microsoft.Extensions.AI.AIFunctionArguments.Context { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "int Microsoft.Extensions.AI.AIFunctionArguments.Count { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "object? Microsoft.Extensions.AI.AIFunctionArguments.this[string key] { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Collections.Generic.ICollection<string> Microsoft.Extensions.AI.AIFunctionArguments.Keys { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.IServiceProvider? Microsoft.Extensions.AI.AIFunctionArguments.Services { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Collections.Generic.ICollection<object?> Microsoft.Extensions.AI.AIFunctionArguments.Values { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.AI.AIFunctionFactory",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "static Microsoft.Extensions.AI.AIFunction Microsoft.Extensions.AI.AIFunctionFactory.Create(System.Delegate method, Microsoft.Extensions.AI.AIFunctionFactoryOptions? options);",
          "Stage": "Stable"
        },
        {
          "Member": "static Microsoft.Extensions.AI.AIFunction Microsoft.Extensions.AI.AIFunctionFactory.Create(System.Delegate method, string? name = null, string? description = null, System.Text.Json.JsonSerializerOptions? serializerOptions = null);",
          "Stage": "Stable"
        },
        {
          "Member": "static Microsoft.Extensions.AI.AIFunction Microsoft.Extensions.AI.AIFunctionFactory.Create(System.Reflection.MethodInfo method, object? target, Microsoft.Extensions.AI.AIFunctionFactoryOptions? options);",
          "Stage": "Stable"
        },
        {
          "Member": "static Microsoft.Extensions.AI.AIFunction Microsoft.Extensions.AI.AIFunctionFactory.Create(System.Reflection.MethodInfo method, object? target, string? name = null, string? description = null, System.Text.Json.JsonSerializerOptions? serializerOptions = null);",
          "Stage": "Stable"
        },
        {
          "Member": "static Microsoft.Extensions.AI.AIFunction Microsoft.Extensions.AI.AIFunctionFactory.Create(System.Reflection.MethodInfo method, System.Func<Microsoft.Extensions.AI.AIFunctionArguments, object> createInstanceFunc, Microsoft.Extensions.AI.AIFunctionFactoryOptions? options = null);",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.AIFunctionFactoryOptions",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.AIFunctionFactoryOptions.AIFunctionFactoryOptions();",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "System.Collections.Generic.IReadOnlyDictionary<string, object?>? Microsoft.Extensions.AI.AIFunctionFactoryOptions.AdditionalProperties { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Func<System.Reflection.ParameterInfo, Microsoft.Extensions.AI.AIFunctionFactoryOptions.ParameterBindingOptions>? Microsoft.Extensions.AI.AIFunctionFactoryOptions.ConfigureParameterBinding { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.AIFunctionFactoryOptions.Description { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.AIJsonSchemaCreateOptions? Microsoft.Extensions.AI.AIFunctionFactoryOptions.JsonSchemaCreateOptions { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Func<object?, System.Type?, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<object?>>? Microsoft.Extensions.AI.AIFunctionFactoryOptions.MarshalResult { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.AIFunctionFactoryOptions.Name { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Text.Json.JsonSerializerOptions? Microsoft.Extensions.AI.AIFunctionFactoryOptions.SerializerOptions { get; set; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "readonly record struct Microsoft.Extensions.AI.AIFunctionFactoryOptions.ParameterBindingOptions",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.AIFunctionFactoryOptions.ParameterBindingOptions.ParameterBindingOptions();",
          "Stage": "Stable"
        },
        {
          "Member": "override bool Microsoft.Extensions.AI.AIFunctionFactoryOptions.ParameterBindingOptions.Equals(object obj);",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.AIFunctionFactoryOptions.ParameterBindingOptions.Equals(Microsoft.Extensions.AI.AIFunctionFactoryOptions.ParameterBindingOptions other);",
          "Stage": "Stable"
        },
        {
          "Member": "override int Microsoft.Extensions.AI.AIFunctionFactoryOptions.ParameterBindingOptions.GetHashCode();",
          "Stage": "Stable"
        },
        {
          "Member": "static bool Microsoft.Extensions.AI.AIFunctionFactoryOptions.ParameterBindingOptions.operator ==(Microsoft.Extensions.AI.AIFunctionFactoryOptions.ParameterBindingOptions left, Microsoft.Extensions.AI.AIFunctionFactoryOptions.ParameterBindingOptions right);",
          "Stage": "Stable"
        },
        {
          "Member": "static bool Microsoft.Extensions.AI.AIFunctionFactoryOptions.ParameterBindingOptions.operator !=(Microsoft.Extensions.AI.AIFunctionFactoryOptions.ParameterBindingOptions left, Microsoft.Extensions.AI.AIFunctionFactoryOptions.ParameterBindingOptions right);",
          "Stage": "Stable"
        },
        {
          "Member": "override string Microsoft.Extensions.AI.AIFunctionFactoryOptions.ParameterBindingOptions.ToString();",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "System.Func<System.Reflection.ParameterInfo, Microsoft.Extensions.AI.AIFunctionArguments, object?>? Microsoft.Extensions.AI.AIFunctionFactoryOptions.ParameterBindingOptions.BindParameter { get; init; }",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.AIFunctionFactoryOptions.ParameterBindingOptions.ExcludeFromSchema { get; init; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "readonly struct Microsoft.Extensions.AI.AIJsonSchemaCreateContext",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.AIJsonSchemaCreateContext.AIJsonSchemaCreateContext();",
          "Stage": "Stable"
        },
        {
          "Member": "TAttribute? Microsoft.Extensions.AI.AIJsonSchemaCreateContext.GetCustomAttribute<TAttribute>(bool inherit = false);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "System.Text.Json.Serialization.Metadata.JsonTypeInfo? Microsoft.Extensions.AI.AIJsonSchemaCreateContext.BaseTypeInfo { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Type? Microsoft.Extensions.AI.AIJsonSchemaCreateContext.DeclaringType { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Reflection.ICustomAttributeProvider? Microsoft.Extensions.AI.AIJsonSchemaCreateContext.ParameterAttributeProvider { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.ReadOnlySpan<string> Microsoft.Extensions.AI.AIJsonSchemaCreateContext.Path { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Reflection.ICustomAttributeProvider? Microsoft.Extensions.AI.AIJsonSchemaCreateContext.PropertyAttributeProvider { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Text.Json.Serialization.Metadata.JsonPropertyInfo? Microsoft.Extensions.AI.AIJsonSchemaCreateContext.PropertyInfo { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Text.Json.Serialization.Metadata.JsonTypeInfo Microsoft.Extensions.AI.AIJsonSchemaCreateContext.TypeInfo { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed record Microsoft.Extensions.AI.AIJsonSchemaCreateOptions",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.AIJsonSchemaCreateOptions.AIJsonSchemaCreateOptions();",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.AIJsonSchemaCreateOptions Microsoft.Extensions.AI.AIJsonSchemaCreateOptions.<Clone>$();",
          "Stage": "Stable"
        },
        {
          "Member": "override bool Microsoft.Extensions.AI.AIJsonSchemaCreateOptions.Equals(object? obj);",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.AIJsonSchemaCreateOptions.Equals(Microsoft.Extensions.AI.AIJsonSchemaCreateOptions? other);",
          "Stage": "Stable"
        },
        {
          "Member": "override int Microsoft.Extensions.AI.AIJsonSchemaCreateOptions.GetHashCode();",
          "Stage": "Stable"
        },
        {
          "Member": "static bool Microsoft.Extensions.AI.AIJsonSchemaCreateOptions.operator ==(Microsoft.Extensions.AI.AIJsonSchemaCreateOptions? left, Microsoft.Extensions.AI.AIJsonSchemaCreateOptions? right);",
          "Stage": "Stable"
        },
        {
          "Member": "static bool Microsoft.Extensions.AI.AIJsonSchemaCreateOptions.operator !=(Microsoft.Extensions.AI.AIJsonSchemaCreateOptions? left, Microsoft.Extensions.AI.AIJsonSchemaCreateOptions? right);",
          "Stage": "Stable"
        },
        {
          "Member": "override string Microsoft.Extensions.AI.AIJsonSchemaCreateOptions.ToString();",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "static Microsoft.Extensions.AI.AIJsonSchemaCreateOptions Microsoft.Extensions.AI.AIJsonSchemaCreateOptions.Default { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Func<System.Reflection.ParameterInfo, bool>? Microsoft.Extensions.AI.AIJsonSchemaCreateOptions.IncludeParameter { get; init; }",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.AIJsonSchemaCreateOptions.IncludeSchemaKeyword { get; init; }",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.AIJsonSchemaTransformOptions? Microsoft.Extensions.AI.AIJsonSchemaCreateOptions.TransformOptions { get; init; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Func<Microsoft.Extensions.AI.AIJsonSchemaCreateContext, System.Text.Json.Nodes.JsonNode, System.Text.Json.Nodes.JsonNode>? Microsoft.Extensions.AI.AIJsonSchemaCreateOptions.TransformSchemaNode { get; init; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.AIJsonSchemaTransformCache",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.AIJsonSchemaTransformCache.AIJsonSchemaTransformCache(Microsoft.Extensions.AI.AIJsonSchemaTransformOptions transformOptions);",
          "Stage": "Stable"
        },
        {
          "Member": "System.Text.Json.JsonElement Microsoft.Extensions.AI.AIJsonSchemaTransformCache.GetOrCreateTransformedSchema(Microsoft.Extensions.AI.AIFunction function);",
          "Stage": "Stable"
        },
        {
          "Member": "System.Text.Json.JsonElement? Microsoft.Extensions.AI.AIJsonSchemaTransformCache.GetOrCreateTransformedSchema(Microsoft.Extensions.AI.ChatResponseFormatJson responseFormat);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "Microsoft.Extensions.AI.AIJsonSchemaTransformOptions Microsoft.Extensions.AI.AIJsonSchemaTransformCache.TransformOptions { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "readonly struct Microsoft.Extensions.AI.AIJsonSchemaTransformContext",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.AIJsonSchemaTransformContext.AIJsonSchemaTransformContext();",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "bool Microsoft.Extensions.AI.AIJsonSchemaTransformContext.IsCollectionElementSchema { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.AIJsonSchemaTransformContext.IsDictionaryValueSchema { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.ReadOnlySpan<string> Microsoft.Extensions.AI.AIJsonSchemaTransformContext.Path { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.AIJsonSchemaTransformContext.PropertyName { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed record Microsoft.Extensions.AI.AIJsonSchemaTransformOptions",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.AIJsonSchemaTransformOptions.AIJsonSchemaTransformOptions();",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.AIJsonSchemaTransformOptions Microsoft.Extensions.AI.AIJsonSchemaTransformOptions.<Clone>$();",
          "Stage": "Stable"
        },
        {
          "Member": "override bool Microsoft.Extensions.AI.AIJsonSchemaTransformOptions.Equals(object? obj);",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.AIJsonSchemaTransformOptions.Equals(Microsoft.Extensions.AI.AIJsonSchemaTransformOptions? other);",
          "Stage": "Stable"
        },
        {
          "Member": "override int Microsoft.Extensions.AI.AIJsonSchemaTransformOptions.GetHashCode();",
          "Stage": "Stable"
        },
        {
          "Member": "static bool Microsoft.Extensions.AI.AIJsonSchemaTransformOptions.operator ==(Microsoft.Extensions.AI.AIJsonSchemaTransformOptions? left, Microsoft.Extensions.AI.AIJsonSchemaTransformOptions? right);",
          "Stage": "Stable"
        },
        {
          "Member": "static bool Microsoft.Extensions.AI.AIJsonSchemaTransformOptions.operator !=(Microsoft.Extensions.AI.AIJsonSchemaTransformOptions? left, Microsoft.Extensions.AI.AIJsonSchemaTransformOptions? right);",
          "Stage": "Stable"
        },
        {
          "Member": "override string Microsoft.Extensions.AI.AIJsonSchemaTransformOptions.ToString();",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "bool Microsoft.Extensions.AI.AIJsonSchemaTransformOptions.ConvertBooleanSchemas { get; init; }",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.AIJsonSchemaTransformOptions.DisallowAdditionalProperties { get; init; }",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.AIJsonSchemaTransformOptions.MoveDefaultKeywordToDescription { get; init; }",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.AIJsonSchemaTransformOptions.RequireAllProperties { get; init; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Func<Microsoft.Extensions.AI.AIJsonSchemaTransformContext, System.Text.Json.Nodes.JsonNode, System.Text.Json.Nodes.JsonNode>? Microsoft.Extensions.AI.AIJsonSchemaTransformOptions.TransformSchemaNode { get; init; }",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.AIJsonSchemaTransformOptions.UseNullableKeyword { get; init; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.AI.AIJsonUtilities",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "static void Microsoft.Extensions.AI.AIJsonUtilities.AddAIContentType<TContent>(this System.Text.Json.JsonSerializerOptions options, string typeDiscriminatorId);",
          "Stage": "Stable"
        },
        {
          "Member": "static void Microsoft.Extensions.AI.AIJsonUtilities.AddAIContentType(this System.Text.Json.JsonSerializerOptions options, System.Type contentType, string typeDiscriminatorId);",
          "Stage": "Stable"
        },
        {
          "Member": "static System.Text.Json.JsonElement Microsoft.Extensions.AI.AIJsonUtilities.CreateFunctionJsonSchema(System.Reflection.MethodBase method, string? title = null, string? description = null, System.Text.Json.JsonSerializerOptions? serializerOptions = null, Microsoft.Extensions.AI.AIJsonSchemaCreateOptions? inferenceOptions = null);",
          "Stage": "Stable"
        },
        {
          "Member": "static System.Text.Json.JsonElement Microsoft.Extensions.AI.AIJsonUtilities.CreateJsonSchema(System.Type? type, string? description = null, bool hasDefaultValue = false, object? defaultValue = null, System.Text.Json.JsonSerializerOptions? serializerOptions = null, Microsoft.Extensions.AI.AIJsonSchemaCreateOptions? inferenceOptions = null);",
          "Stage": "Stable"
        },
        {
          "Member": "static string Microsoft.Extensions.AI.AIJsonUtilities.HashDataToString(System.ReadOnlySpan<object?> values, System.Text.Json.JsonSerializerOptions? serializerOptions = null);",
          "Stage": "Stable"
        },
        {
          "Member": "static System.Text.Json.JsonElement Microsoft.Extensions.AI.AIJsonUtilities.TransformSchema(System.Text.Json.JsonElement schema, Microsoft.Extensions.AI.AIJsonSchemaTransformOptions transformOptions);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "static System.Text.Json.JsonSerializerOptions Microsoft.Extensions.AI.AIJsonUtilities.DefaultOptions { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "abstract class Microsoft.Extensions.AI.AITool",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.AITool.AITool();",
          "Stage": "Stable"
        },
        {
          "Member": "override string Microsoft.Extensions.AI.AITool.ToString();",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "virtual System.Collections.Generic.IReadOnlyDictionary<string, object?> Microsoft.Extensions.AI.AITool.AdditionalProperties { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "virtual string Microsoft.Extensions.AI.AITool.Description { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "virtual string Microsoft.Extensions.AI.AITool.Name { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.AutoChatToolMode : Microsoft.Extensions.AI.ChatToolMode",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.AutoChatToolMode.AutoChatToolMode();",
          "Stage": "Stable"
        },
        {
          "Member": "override bool Microsoft.Extensions.AI.AutoChatToolMode.Equals(object? obj);",
          "Stage": "Stable"
        },
        {
          "Member": "override int Microsoft.Extensions.AI.AutoChatToolMode.GetHashCode();",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.BinaryEmbedding : Microsoft.Extensions.AI.Embedding",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.BinaryEmbedding.BinaryEmbedding(System.Collections.BitArray vector);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "override int Microsoft.Extensions.AI.BinaryEmbedding.Dimensions { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Collections.BitArray Microsoft.Extensions.AI.BinaryEmbedding.Vector { get; set; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.BinaryEmbedding.VectorConverter",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.BinaryEmbedding.VectorConverter.VectorConverter();",
          "Stage": "Stable"
        },
        {
          "Member": "override System.Collections.BitArray Microsoft.Extensions.AI.BinaryEmbedding.VectorConverter.Read(ref System.Text.Json.Utf8JsonReader reader, System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options);",
          "Stage": "Stable"
        },
        {
          "Member": "override void Microsoft.Extensions.AI.BinaryEmbedding.VectorConverter.Write(System.Text.Json.Utf8JsonWriter writer, System.Collections.BitArray value, System.Text.Json.JsonSerializerOptions options);",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.AI.ChatClientExtensions",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "static object Microsoft.Extensions.AI.ChatClientExtensions.GetRequiredService(this Microsoft.Extensions.AI.IChatClient client, System.Type serviceType, object? serviceKey = null);",
          "Stage": "Stable"
        },
        {
          "Member": "static TService Microsoft.Extensions.AI.ChatClientExtensions.GetRequiredService<TService>(this Microsoft.Extensions.AI.IChatClient client, object? serviceKey = null);",
          "Stage": "Stable"
        },
        {
          "Member": "static System.Threading.Tasks.Task<Microsoft.Extensions.AI.ChatResponse> Microsoft.Extensions.AI.ChatClientExtensions.GetResponseAsync(this Microsoft.Extensions.AI.IChatClient client, string chatMessage, Microsoft.Extensions.AI.ChatOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          "Member": "static System.Threading.Tasks.Task<Microsoft.Extensions.AI.ChatResponse> Microsoft.Extensions.AI.ChatClientExtensions.GetResponseAsync(this Microsoft.Extensions.AI.IChatClient client, Microsoft.Extensions.AI.ChatMessage chatMessage, Microsoft.Extensions.AI.ChatOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          "Member": "static TService? Microsoft.Extensions.AI.ChatClientExtensions.GetService<TService>(this Microsoft.Extensions.AI.IChatClient client, object? serviceKey = null);",
          "Stage": "Stable"
        },
        {
          "Member": "static System.Collections.Generic.IAsyncEnumerable<Microsoft.Extensions.AI.ChatResponseUpdate> Microsoft.Extensions.AI.ChatClientExtensions.GetStreamingResponseAsync(this Microsoft.Extensions.AI.IChatClient client, string chatMessage, Microsoft.Extensions.AI.ChatOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          "Member": "static System.Collections.Generic.IAsyncEnumerable<Microsoft.Extensions.AI.ChatResponseUpdate> Microsoft.Extensions.AI.ChatClientExtensions.GetStreamingResponseAsync(this Microsoft.Extensions.AI.IChatClient client, Microsoft.Extensions.AI.ChatMessage chatMessage, Microsoft.Extensions.AI.ChatOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.ChatClientMetadata",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.ChatClientMetadata.ChatClientMetadata(string? providerName = null, System.Uri? providerUri = null, string? defaultModelId = null);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "string? Microsoft.Extensions.AI.ChatClientMetadata.DefaultModelId { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.ChatClientMetadata.ProviderName { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Uri? Microsoft.Extensions.AI.ChatClientMetadata.ProviderUri { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "readonly struct Microsoft.Extensions.AI.ChatFinishReason : System.IEquatable<Microsoft.Extensions.AI.ChatFinishReason>",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.ChatFinishReason.ChatFinishReason(string value);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.ChatFinishReason.ChatFinishReason();",
          "Stage": "Stable"
        },
        {
          "Member": "override bool Microsoft.Extensions.AI.ChatFinishReason.Equals(object? obj);",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.ChatFinishReason.Equals(Microsoft.Extensions.AI.ChatFinishReason other);",
          "Stage": "Stable"
        },
        {
          "Member": "override int Microsoft.Extensions.AI.ChatFinishReason.GetHashCode();",
          "Stage": "Stable"
        },
        {
          "Member": "static bool Microsoft.Extensions.AI.ChatFinishReason.operator ==(Microsoft.Extensions.AI.ChatFinishReason left, Microsoft.Extensions.AI.ChatFinishReason right);",
          "Stage": "Stable"
        },
        {
          "Member": "static bool Microsoft.Extensions.AI.ChatFinishReason.operator !=(Microsoft.Extensions.AI.ChatFinishReason left, Microsoft.Extensions.AI.ChatFinishReason right);",
          "Stage": "Stable"
        },
        {
          "Member": "override string Microsoft.Extensions.AI.ChatFinishReason.ToString();",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "static Microsoft.Extensions.AI.ChatFinishReason Microsoft.Extensions.AI.ChatFinishReason.ContentFilter { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "static Microsoft.Extensions.AI.ChatFinishReason Microsoft.Extensions.AI.ChatFinishReason.Length { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "static Microsoft.Extensions.AI.ChatFinishReason Microsoft.Extensions.AI.ChatFinishReason.Stop { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "static Microsoft.Extensions.AI.ChatFinishReason Microsoft.Extensions.AI.ChatFinishReason.ToolCalls { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "string Microsoft.Extensions.AI.ChatFinishReason.Value { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.ChatFinishReason.Converter",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.ChatFinishReason.Converter.Converter();",
          "Stage": "Stable"
        },
        {
          "Member": "override Microsoft.Extensions.AI.ChatFinishReason Microsoft.Extensions.AI.ChatFinishReason.Converter.Read(ref System.Text.Json.Utf8JsonReader reader, System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options);",
          "Stage": "Stable"
        },
        {
          "Member": "override void Microsoft.Extensions.AI.ChatFinishReason.Converter.Write(System.Text.Json.Utf8JsonWriter writer, Microsoft.Extensions.AI.ChatFinishReason value, System.Text.Json.JsonSerializerOptions options);",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.ChatMessage",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.ChatMessage.ChatMessage();",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.ChatMessage.ChatMessage(Microsoft.Extensions.AI.ChatRole role, string? content);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.ChatMessage.ChatMessage(Microsoft.Extensions.AI.ChatRole role, System.Collections.Generic.IList<Microsoft.Extensions.AI.AIContent>? contents);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.ChatMessage Microsoft.Extensions.AI.ChatMessage.Clone();",
          "Stage": "Stable"
        },
        {
          "Member": "override string Microsoft.Extensions.AI.ChatMessage.ToString();",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "Microsoft.Extensions.AI.AdditionalPropertiesDictionary? Microsoft.Extensions.AI.ChatMessage.AdditionalProperties { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.ChatMessage.AuthorName { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Collections.Generic.IList<Microsoft.Extensions.AI.AIContent> Microsoft.Extensions.AI.ChatMessage.Contents { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.ChatMessage.MessageId { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "object? Microsoft.Extensions.AI.ChatMessage.RawRepresentation { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.ChatRole Microsoft.Extensions.AI.ChatMessage.Role { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "string Microsoft.Extensions.AI.ChatMessage.Text { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.ChatOptions",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.ChatOptions.ChatOptions();",
          "Stage": "Stable"
        },
        {
          "Member": "virtual Microsoft.Extensions.AI.ChatOptions Microsoft.Extensions.AI.ChatOptions.Clone();",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "Microsoft.Extensions.AI.AdditionalPropertiesDictionary? Microsoft.Extensions.AI.ChatOptions.AdditionalProperties { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "bool? Microsoft.Extensions.AI.ChatOptions.AllowMultipleToolCalls { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.ChatOptions.ConversationId { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.ChatOptions.Instructions { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "float? Microsoft.Extensions.AI.ChatOptions.FrequencyPenalty { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "int? Microsoft.Extensions.AI.ChatOptions.MaxOutputTokens { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.ChatOptions.ModelId { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "float? Microsoft.Extensions.AI.ChatOptions.PresencePenalty { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Func<Microsoft.Extensions.AI.IChatClient, object?>? Microsoft.Extensions.AI.ChatOptions.RawRepresentationFactory { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.ChatResponseFormat? Microsoft.Extensions.AI.ChatOptions.ResponseFormat { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "long? Microsoft.Extensions.AI.ChatOptions.Seed { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Collections.Generic.IList<string>? Microsoft.Extensions.AI.ChatOptions.StopSequences { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "float? Microsoft.Extensions.AI.ChatOptions.Temperature { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.ChatToolMode? Microsoft.Extensions.AI.ChatOptions.ToolMode { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Collections.Generic.IList<Microsoft.Extensions.AI.AITool>? Microsoft.Extensions.AI.ChatOptions.Tools { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "int? Microsoft.Extensions.AI.ChatOptions.TopK { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "float? Microsoft.Extensions.AI.ChatOptions.TopP { get; set; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.ChatResponse",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.ChatResponse.ChatResponse();",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.ChatResponse.ChatResponse(Microsoft.Extensions.AI.ChatMessage message);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.ChatResponse.ChatResponse(System.Collections.Generic.IList<Microsoft.Extensions.AI.ChatMessage>? messages);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.ChatResponseUpdate[] Microsoft.Extensions.AI.ChatResponse.ToChatResponseUpdates();",
          "Stage": "Stable"
        },
        {
          "Member": "override string Microsoft.Extensions.AI.ChatResponse.ToString();",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "Microsoft.Extensions.AI.AdditionalPropertiesDictionary? Microsoft.Extensions.AI.ChatResponse.AdditionalProperties { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.ChatResponse.ConversationId { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.DateTimeOffset? Microsoft.Extensions.AI.ChatResponse.CreatedAt { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.ChatFinishReason? Microsoft.Extensions.AI.ChatResponse.FinishReason { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Collections.Generic.IList<Microsoft.Extensions.AI.ChatMessage> Microsoft.Extensions.AI.ChatResponse.Messages { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.ChatResponse.ModelId { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "object? Microsoft.Extensions.AI.ChatResponse.RawRepresentation { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.ChatResponse.ResponseId { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "string Microsoft.Extensions.AI.ChatResponse.Text { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.UsageDetails? Microsoft.Extensions.AI.ChatResponse.Usage { get; set; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.AI.ChatResponseExtensions",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "static void Microsoft.Extensions.AI.ChatResponseExtensions.AddMessages(this System.Collections.Generic.IList<Microsoft.Extensions.AI.ChatMessage> list, Microsoft.Extensions.AI.ChatResponse response);",
          "Stage": "Stable"
        },
        {
          "Member": "static void Microsoft.Extensions.AI.ChatResponseExtensions.AddMessages(this System.Collections.Generic.IList<Microsoft.Extensions.AI.ChatMessage> list, System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatResponseUpdate> updates);",
          "Stage": "Stable"
        },
        {
          "Member": "static void Microsoft.Extensions.AI.ChatResponseExtensions.AddMessages(this System.Collections.Generic.IList<Microsoft.Extensions.AI.ChatMessage> list, Microsoft.Extensions.AI.ChatResponseUpdate update, System.Func<Microsoft.Extensions.AI.AIContent, bool>? filter = null);",
          "Stage": "Stable"
        },
        {
          "Member": "static System.Threading.Tasks.Task Microsoft.Extensions.AI.ChatResponseExtensions.AddMessagesAsync(this System.Collections.Generic.IList<Microsoft.Extensions.AI.ChatMessage> list, System.Collections.Generic.IAsyncEnumerable<Microsoft.Extensions.AI.ChatResponseUpdate> updates, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          "Member": "static Microsoft.Extensions.AI.ChatResponse Microsoft.Extensions.AI.ChatResponseExtensions.ToChatResponse(this System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatResponseUpdate> updates);",
          "Stage": "Stable"
        },
        {
          "Member": "static System.Threading.Tasks.Task<Microsoft.Extensions.AI.ChatResponse> Microsoft.Extensions.AI.ChatResponseExtensions.ToChatResponseAsync(this System.Collections.Generic.IAsyncEnumerable<Microsoft.Extensions.AI.ChatResponseUpdate> updates, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.ChatResponseFormat",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "static Microsoft.Extensions.AI.ChatResponseFormatJson Microsoft.Extensions.AI.ChatResponseFormat.ForJsonSchema(System.Text.Json.JsonElement schema, string? schemaName = null, string? schemaDescription = null);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "static Microsoft.Extensions.AI.ChatResponseFormatJson Microsoft.Extensions.AI.ChatResponseFormat.Json { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "static Microsoft.Extensions.AI.ChatResponseFormatText Microsoft.Extensions.AI.ChatResponseFormat.Text { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.ChatResponseFormatJson : Microsoft.Extensions.AI.ChatResponseFormat",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.ChatResponseFormatJson.ChatResponseFormatJson(System.Text.Json.JsonElement? schema, string? schemaName = null, string? schemaDescription = null);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "System.Text.Json.JsonElement? Microsoft.Extensions.AI.ChatResponseFormatJson.Schema { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.ChatResponseFormatJson.SchemaDescription { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.ChatResponseFormatJson.SchemaName { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.ChatResponseFormatText : Microsoft.Extensions.AI.ChatResponseFormat",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.ChatResponseFormatText.ChatResponseFormatText();",
          "Stage": "Stable"
        },
        {
          "Member": "override bool Microsoft.Extensions.AI.ChatResponseFormatText.Equals(object? obj);",
          "Stage": "Stable"
        },
        {
          "Member": "override int Microsoft.Extensions.AI.ChatResponseFormatText.GetHashCode();",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.ChatResponseUpdate",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.ChatResponseUpdate.ChatResponseUpdate();",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.ChatResponseUpdate.ChatResponseUpdate(Microsoft.Extensions.AI.ChatRole? role, string? content);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.ChatResponseUpdate.ChatResponseUpdate(Microsoft.Extensions.AI.ChatRole? role, System.Collections.Generic.IList<Microsoft.Extensions.AI.AIContent>? contents);",
          "Stage": "Stable"
        },
        {
          "Member": "override string Microsoft.Extensions.AI.ChatResponseUpdate.ToString();",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "Microsoft.Extensions.AI.AdditionalPropertiesDictionary? Microsoft.Extensions.AI.ChatResponseUpdate.AdditionalProperties { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.ChatResponseUpdate.AuthorName { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Collections.Generic.IList<Microsoft.Extensions.AI.AIContent> Microsoft.Extensions.AI.ChatResponseUpdate.Contents { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.ChatResponseUpdate.ConversationId { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.DateTimeOffset? Microsoft.Extensions.AI.ChatResponseUpdate.CreatedAt { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.ChatFinishReason? Microsoft.Extensions.AI.ChatResponseUpdate.FinishReason { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.ChatResponseUpdate.MessageId { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.ChatResponseUpdate.ModelId { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "object? Microsoft.Extensions.AI.ChatResponseUpdate.RawRepresentation { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.ChatResponseUpdate.ResponseId { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.ChatRole? Microsoft.Extensions.AI.ChatResponseUpdate.Role { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "string Microsoft.Extensions.AI.ChatResponseUpdate.Text { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "readonly struct Microsoft.Extensions.AI.ChatRole : System.IEquatable<Microsoft.Extensions.AI.ChatRole>",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.ChatRole.ChatRole(string value);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.ChatRole.ChatRole();",
          "Stage": "Stable"
        },
        {
          "Member": "override bool Microsoft.Extensions.AI.ChatRole.Equals(object? obj);",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.ChatRole.Equals(Microsoft.Extensions.AI.ChatRole other);",
          "Stage": "Stable"
        },
        {
          "Member": "override int Microsoft.Extensions.AI.ChatRole.GetHashCode();",
          "Stage": "Stable"
        },
        {
          "Member": "static bool Microsoft.Extensions.AI.ChatRole.operator ==(Microsoft.Extensions.AI.ChatRole left, Microsoft.Extensions.AI.ChatRole right);",
          "Stage": "Stable"
        },
        {
          "Member": "static bool Microsoft.Extensions.AI.ChatRole.operator !=(Microsoft.Extensions.AI.ChatRole left, Microsoft.Extensions.AI.ChatRole right);",
          "Stage": "Stable"
        },
        {
          "Member": "override string Microsoft.Extensions.AI.ChatRole.ToString();",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "static Microsoft.Extensions.AI.ChatRole Microsoft.Extensions.AI.ChatRole.Assistant { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "static Microsoft.Extensions.AI.ChatRole Microsoft.Extensions.AI.ChatRole.System { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "static Microsoft.Extensions.AI.ChatRole Microsoft.Extensions.AI.ChatRole.Tool { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "static Microsoft.Extensions.AI.ChatRole Microsoft.Extensions.AI.ChatRole.User { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "string Microsoft.Extensions.AI.ChatRole.Value { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.ChatRole.Converter",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.ChatRole.Converter.Converter();",
          "Stage": "Stable"
        },
        {
          "Member": "override Microsoft.Extensions.AI.ChatRole Microsoft.Extensions.AI.ChatRole.Converter.Read(ref System.Text.Json.Utf8JsonReader reader, System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options);",
          "Stage": "Stable"
        },
        {
          "Member": "override void Microsoft.Extensions.AI.ChatRole.Converter.Write(System.Text.Json.Utf8JsonWriter writer, Microsoft.Extensions.AI.ChatRole value, System.Text.Json.JsonSerializerOptions options);",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.ChatToolMode",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "static Microsoft.Extensions.AI.RequiredChatToolMode Microsoft.Extensions.AI.ChatToolMode.RequireSpecific(string functionName);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "static Microsoft.Extensions.AI.AutoChatToolMode Microsoft.Extensions.AI.ChatToolMode.Auto { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "static Microsoft.Extensions.AI.NoneChatToolMode Microsoft.Extensions.AI.ChatToolMode.None { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "static Microsoft.Extensions.AI.RequiredChatToolMode Microsoft.Extensions.AI.ChatToolMode.RequireAny { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.DataContent : Microsoft.Extensions.AI.AIContent",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.DataContent.DataContent(System.Uri uri, string? mediaType = null);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.DataContent.DataContent(string uri, string? mediaType = null);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.DataContent.DataContent(System.ReadOnlyMemory<byte> data, string mediaType);",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.DataContent.HasTopLevelMediaType(string topLevelType);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "System.ReadOnlyMemory<char> Microsoft.Extensions.AI.DataContent.Base64Data { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.ReadOnlyMemory<byte> Microsoft.Extensions.AI.DataContent.Data { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "string Microsoft.Extensions.AI.DataContent.MediaType { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "string Microsoft.Extensions.AI.DataContent.Uri { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.DelegatingChatClient : Microsoft.Extensions.AI.IChatClient, System.IDisposable",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.DelegatingChatClient.DelegatingChatClient(Microsoft.Extensions.AI.IChatClient innerClient);",
          "Stage": "Stable"
        },
        {
          "Member": "void Microsoft.Extensions.AI.DelegatingChatClient.Dispose();",
          "Stage": "Stable"
        },
        {
          "Member": "virtual void Microsoft.Extensions.AI.DelegatingChatClient.Dispose(bool disposing);",
          "Stage": "Stable"
        },
        {
          "Member": "virtual System.Threading.Tasks.Task<Microsoft.Extensions.AI.ChatResponse> Microsoft.Extensions.AI.DelegatingChatClient.GetResponseAsync(System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          "Member": "virtual object? Microsoft.Extensions.AI.DelegatingChatClient.GetService(System.Type serviceType, object? serviceKey = null);",
          "Stage": "Stable"
        },
        {
          "Member": "virtual System.Collections.Generic.IAsyncEnumerable<Microsoft.Extensions.AI.ChatResponseUpdate> Microsoft.Extensions.AI.DelegatingChatClient.GetStreamingResponseAsync(System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "Microsoft.Extensions.AI.IChatClient Microsoft.Extensions.AI.DelegatingChatClient.InnerClient { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.DelegatingEmbeddingGenerator<TInput, TEmbedding> : Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding>, Microsoft.Extensions.AI.IEmbeddingGenerator, System.IDisposable where TEmbedding : Microsoft.Extensions.AI.Embedding",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.DelegatingEmbeddingGenerator<TInput, TEmbedding>.DelegatingEmbeddingGenerator(Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding> innerGenerator);",
          "Stage": "Stable"
        },
        {
          "Member": "void Microsoft.Extensions.AI.DelegatingEmbeddingGenerator<TInput, TEmbedding>.Dispose();",
          "Stage": "Stable"
        },
        {
          "Member": "virtual void Microsoft.Extensions.AI.DelegatingEmbeddingGenerator<TInput, TEmbedding>.Dispose(bool disposing);",
          "Stage": "Stable"
        },
        {
          "Member": "virtual System.Threading.Tasks.Task<Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding>> Microsoft.Extensions.AI.DelegatingEmbeddingGenerator<TInput, TEmbedding>.GenerateAsync(System.Collections.Generic.IEnumerable<TInput> values, Microsoft.Extensions.AI.EmbeddingGenerationOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          "Member": "virtual object? Microsoft.Extensions.AI.DelegatingEmbeddingGenerator<TInput, TEmbedding>.GetService(System.Type serviceType, object? serviceKey = null);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding> Microsoft.Extensions.AI.DelegatingEmbeddingGenerator<TInput, TEmbedding>.InnerGenerator { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.DelegatingSpeechToTextClient : Microsoft.Extensions.AI.ISpeechToTextClient, System.IDisposable",
      "Stage": "Experimental",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.DelegatingSpeechToTextClient.DelegatingSpeechToTextClient(Microsoft.Extensions.AI.ISpeechToTextClient innerClient);",
          "Stage": "Experimental"
        },
        {
          "Member": "void Microsoft.Extensions.AI.DelegatingSpeechToTextClient.Dispose();",
          "Stage": "Experimental"
        },
        {
          "Member": "virtual void Microsoft.Extensions.AI.DelegatingSpeechToTextClient.Dispose(bool disposing);",
          "Stage": "Experimental"
        },
        {
          "Member": "virtual object? Microsoft.Extensions.AI.DelegatingSpeechToTextClient.GetService(System.Type serviceType, object? serviceKey = null);",
          "Stage": "Experimental"
        },
        {
          "Member": "virtual System.Collections.Generic.IAsyncEnumerable<Microsoft.Extensions.AI.SpeechToTextResponseUpdate> Microsoft.Extensions.AI.DelegatingSpeechToTextClient.GetStreamingTextAsync(System.IO.Stream audioSpeechStream, Microsoft.Extensions.AI.SpeechToTextOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Experimental"
        },
        {
          "Member": "virtual System.Threading.Tasks.Task<Microsoft.Extensions.AI.SpeechToTextResponse> Microsoft.Extensions.AI.DelegatingSpeechToTextClient.GetTextAsync(System.IO.Stream audioSpeechStream, Microsoft.Extensions.AI.SpeechToTextOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Experimental"
        }
      ],
      "Properties": [
        {
          "Member": "Microsoft.Extensions.AI.ISpeechToTextClient Microsoft.Extensions.AI.DelegatingSpeechToTextClient.InnerClient { get; }",
          "Stage": "Experimental"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.Embedding",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.Embedding.Embedding();",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "Microsoft.Extensions.AI.AdditionalPropertiesDictionary? Microsoft.Extensions.AI.Embedding.AdditionalProperties { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.DateTimeOffset? Microsoft.Extensions.AI.Embedding.CreatedAt { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "virtual int Microsoft.Extensions.AI.Embedding.Dimensions { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.Embedding.ModelId { get; set; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.Embedding<T> : Microsoft.Extensions.AI.Embedding",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.Embedding<T>.Embedding(System.ReadOnlyMemory<T> vector);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "override int Microsoft.Extensions.AI.Embedding<T>.Dimensions { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.ReadOnlyMemory<T> Microsoft.Extensions.AI.Embedding<T>.Vector { get; set; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.EmbeddingGenerationOptions",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.EmbeddingGenerationOptions.EmbeddingGenerationOptions();",
          "Stage": "Stable"
        },
        {
          "Member": "virtual Microsoft.Extensions.AI.EmbeddingGenerationOptions Microsoft.Extensions.AI.EmbeddingGenerationOptions.Clone();",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "Microsoft.Extensions.AI.AdditionalPropertiesDictionary? Microsoft.Extensions.AI.EmbeddingGenerationOptions.AdditionalProperties { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "int? Microsoft.Extensions.AI.EmbeddingGenerationOptions.Dimensions { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.EmbeddingGenerationOptions.ModelId { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Func<Microsoft.Extensions.AI.IEmbeddingGenerator, object?>? Microsoft.Extensions.AI.EmbeddingGenerationOptions.RawRepresentationFactory { get; set; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.AI.EmbeddingGeneratorExtensions",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "static System.Threading.Tasks.Task<(TInput Value, TEmbedding Embedding)[]> Microsoft.Extensions.AI.EmbeddingGeneratorExtensions.GenerateAndZipAsync<TInput, TEmbedding>(this Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding> generator, System.Collections.Generic.IEnumerable<TInput> values, Microsoft.Extensions.AI.EmbeddingGenerationOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          "Member": "static System.Threading.Tasks.Task<TEmbedding> Microsoft.Extensions.AI.EmbeddingGeneratorExtensions.GenerateAsync<TInput, TEmbedding>(this Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding> generator, TInput value, Microsoft.Extensions.AI.EmbeddingGenerationOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          "Member": "static System.Threading.Tasks.Task<System.ReadOnlyMemory<TEmbeddingElement>> Microsoft.Extensions.AI.EmbeddingGeneratorExtensions.GenerateVectorAsync<TInput, TEmbeddingElement>(this Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, Microsoft.Extensions.AI.Embedding<TEmbeddingElement>> generator, TInput value, Microsoft.Extensions.AI.EmbeddingGenerationOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          "Member": "static object Microsoft.Extensions.AI.EmbeddingGeneratorExtensions.GetRequiredService(this Microsoft.Extensions.AI.IEmbeddingGenerator generator, System.Type serviceType, object? serviceKey = null);",
          "Stage": "Stable"
        },
        {
          "Member": "static TService Microsoft.Extensions.AI.EmbeddingGeneratorExtensions.GetRequiredService<TService>(this Microsoft.Extensions.AI.IEmbeddingGenerator generator, object? serviceKey = null);",
          "Stage": "Stable"
        },
        {
          "Member": "static TService? Microsoft.Extensions.AI.EmbeddingGeneratorExtensions.GetService<TService>(this Microsoft.Extensions.AI.IEmbeddingGenerator generator, object? serviceKey = null);",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.EmbeddingGeneratorMetadata",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.EmbeddingGeneratorMetadata.EmbeddingGeneratorMetadata(string? providerName = null, System.Uri? providerUri = null, string? defaultModelId = null, int? defaultModelDimensions = null);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "int? Microsoft.Extensions.AI.EmbeddingGeneratorMetadata.DefaultModelDimensions { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.EmbeddingGeneratorMetadata.DefaultModelId { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.EmbeddingGeneratorMetadata.ProviderName { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Uri? Microsoft.Extensions.AI.EmbeddingGeneratorMetadata.ProviderUri { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.ErrorContent : Microsoft.Extensions.AI.AIContent",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.ErrorContent.ErrorContent(string? message);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "string? Microsoft.Extensions.AI.ErrorContent.Details { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.ErrorContent.ErrorCode { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "string Microsoft.Extensions.AI.ErrorContent.Message { get; set; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.FunctionCallContent : Microsoft.Extensions.AI.AIContent",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.FunctionCallContent.FunctionCallContent(string callId, string name, System.Collections.Generic.IDictionary<string, object?>? arguments = null);",
          "Stage": "Stable"
        },
        {
          "Member": "static Microsoft.Extensions.AI.FunctionCallContent Microsoft.Extensions.AI.FunctionCallContent.CreateFromParsedArguments<TEncoding>(TEncoding encodedArguments, string callId, string name, System.Func<TEncoding, System.Collections.Generic.IDictionary<string, object?>?> argumentParser);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "System.Collections.Generic.IDictionary<string, object?>? Microsoft.Extensions.AI.FunctionCallContent.Arguments { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "string Microsoft.Extensions.AI.FunctionCallContent.CallId { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Exception? Microsoft.Extensions.AI.FunctionCallContent.Exception { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "string Microsoft.Extensions.AI.FunctionCallContent.Name { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.FunctionResultContent : Microsoft.Extensions.AI.AIContent",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.FunctionResultContent.FunctionResultContent(string callId, object? result);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "string Microsoft.Extensions.AI.FunctionResultContent.CallId { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Exception? Microsoft.Extensions.AI.FunctionResultContent.Exception { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "object? Microsoft.Extensions.AI.FunctionResultContent.Result { get; set; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding> : System.Collections.Generic.IList<TEmbedding>, System.Collections.Generic.ICollection<TEmbedding>, System.Collections.Generic.IEnumerable<TEmbedding>, System.Collections.IEnumerable, System.Collections.Generic.IReadOnlyList<TEmbedding>, System.Collections.Generic.IReadOnlyCollection<TEmbedding> where TEmbedding : Microsoft.Extensions.AI.Embedding",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding>.GeneratedEmbeddings();",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding>.GeneratedEmbeddings(int capacity);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding>.GeneratedEmbeddings(System.Collections.Generic.IEnumerable<TEmbedding> embeddings);",
          "Stage": "Stable"
        },
        {
          "Member": "void Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding>.Add(TEmbedding item);",
          "Stage": "Stable"
        },
        {
          "Member": "void Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding>.AddRange(System.Collections.Generic.IEnumerable<TEmbedding> items);",
          "Stage": "Stable"
        },
        {
          "Member": "void Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding>.Clear();",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding>.Contains(TEmbedding item);",
          "Stage": "Stable"
        },
        {
          "Member": "void Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding>.CopyTo(TEmbedding[] array, int arrayIndex);",
          "Stage": "Stable"
        },
        {
          "Member": "System.Collections.Generic.IEnumerator<TEmbedding> Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding>.GetEnumerator();",
          "Stage": "Stable"
        },
        {
          "Member": "int Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding>.IndexOf(TEmbedding item);",
          "Stage": "Stable"
        },
        {
          "Member": "void Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding>.Insert(int index, TEmbedding item);",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding>.Remove(TEmbedding item);",
          "Stage": "Stable"
        },
        {
          "Member": "void Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding>.RemoveAt(int index);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "Microsoft.Extensions.AI.AdditionalPropertiesDictionary? Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding>.AdditionalProperties { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "int Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding>.Count { get; }",
          "Stage": "Stable"
        },
        {
          "Member": "TEmbedding Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding>.this[int index] { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.UsageDetails? Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding>.Usage { get; set; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.HostedCodeInterpreterTool : Microsoft.Extensions.AI.AITool",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.HostedCodeInterpreterTool.HostedCodeInterpreterTool();",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.HostedWebSearchTool : Microsoft.Extensions.AI.AITool",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.HostedWebSearchTool.HostedWebSearchTool();",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "interface Microsoft.Extensions.AI.IChatClient : System.IDisposable",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "System.Threading.Tasks.Task<Microsoft.Extensions.AI.ChatResponse> Microsoft.Extensions.AI.IChatClient.GetResponseAsync(System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        },
        {
          "Member": "object? Microsoft.Extensions.AI.IChatClient.GetService(System.Type serviceType, object? serviceKey = null);",
          "Stage": "Stable"
        },
        {
          "Member": "System.Collections.Generic.IAsyncEnumerable<Microsoft.Extensions.AI.ChatResponseUpdate> Microsoft.Extensions.AI.IChatClient.GetStreamingResponseAsync(System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "interface Microsoft.Extensions.AI.IEmbeddingGenerator : System.IDisposable",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "object? Microsoft.Extensions.AI.IEmbeddingGenerator.GetService(System.Type serviceType, object? serviceKey = null);",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "interface Microsoft.Extensions.AI.IEmbeddingGenerator<in TInput, TEmbedding> : Microsoft.Extensions.AI.IEmbeddingGenerator, System.IDisposable where TEmbedding : Microsoft.Extensions.AI.Embedding",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "System.Threading.Tasks.Task<Microsoft.Extensions.AI.GeneratedEmbeddings<TEmbedding>> Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding>.GenerateAsync(System.Collections.Generic.IEnumerable<TInput> values, Microsoft.Extensions.AI.EmbeddingGenerationOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "interface Microsoft.Extensions.AI.ISpeechToTextClient : System.IDisposable",
      "Stage": "Experimental",
      "Methods": [
        {
          "Member": "object? Microsoft.Extensions.AI.ISpeechToTextClient.GetService(System.Type serviceType, object? serviceKey = null);",
          "Stage": "Experimental"
        },
        {
          "Member": "System.Collections.Generic.IAsyncEnumerable<Microsoft.Extensions.AI.SpeechToTextResponseUpdate> Microsoft.Extensions.AI.ISpeechToTextClient.GetStreamingTextAsync(System.IO.Stream audioSpeechStream, Microsoft.Extensions.AI.SpeechToTextOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Experimental"
        },
        {
          "Member": "System.Threading.Tasks.Task<Microsoft.Extensions.AI.SpeechToTextResponse> Microsoft.Extensions.AI.ISpeechToTextClient.GetTextAsync(System.IO.Stream audioSpeechStream, Microsoft.Extensions.AI.SpeechToTextOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Experimental"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.NoneChatToolMode : Microsoft.Extensions.AI.ChatToolMode",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.NoneChatToolMode.NoneChatToolMode();",
          "Stage": "Stable"
        },
        {
          "Member": "override bool Microsoft.Extensions.AI.NoneChatToolMode.Equals(object? obj);",
          "Stage": "Stable"
        },
        {
          "Member": "override int Microsoft.Extensions.AI.NoneChatToolMode.GetHashCode();",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.RequiredChatToolMode : Microsoft.Extensions.AI.ChatToolMode",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.RequiredChatToolMode.RequiredChatToolMode(string? requiredFunctionName);",
          "Stage": "Stable"
        },
        {
          "Member": "override bool Microsoft.Extensions.AI.RequiredChatToolMode.Equals(object? obj);",
          "Stage": "Stable"
        },
        {
          "Member": "override int Microsoft.Extensions.AI.RequiredChatToolMode.GetHashCode();",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "string? Microsoft.Extensions.AI.RequiredChatToolMode.RequiredFunctionName { get; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.AI.SpeechToTextClientExtensions",
      "Stage": "Experimental",
      "Methods": [
        {
          "Member": "static TService? Microsoft.Extensions.AI.SpeechToTextClientExtensions.GetService<TService>(this Microsoft.Extensions.AI.ISpeechToTextClient client, object? serviceKey = null);",
          "Stage": "Experimental"
        },
        {
          "Member": "static System.Collections.Generic.IAsyncEnumerable<Microsoft.Extensions.AI.SpeechToTextResponseUpdate> Microsoft.Extensions.AI.SpeechToTextClientExtensions.GetStreamingTextAsync(this Microsoft.Extensions.AI.ISpeechToTextClient client, Microsoft.Extensions.AI.DataContent audioSpeechContent, Microsoft.Extensions.AI.SpeechToTextOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Experimental"
        },
        {
          "Member": "static System.Threading.Tasks.Task<Microsoft.Extensions.AI.SpeechToTextResponse> Microsoft.Extensions.AI.SpeechToTextClientExtensions.GetTextAsync(this Microsoft.Extensions.AI.ISpeechToTextClient client, Microsoft.Extensions.AI.DataContent audioSpeechContent, Microsoft.Extensions.AI.SpeechToTextOptions? options = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Experimental"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.SpeechToTextClientMetadata",
      "Stage": "Experimental",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.SpeechToTextClientMetadata.SpeechToTextClientMetadata(string? providerName = null, System.Uri? providerUri = null, string? defaultModelId = null);",
          "Stage": "Experimental"
        }
      ],
      "Properties": [
        {
          "Member": "string? Microsoft.Extensions.AI.SpeechToTextClientMetadata.DefaultModelId { get; }",
          "Stage": "Experimental"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.SpeechToTextClientMetadata.ProviderName { get; }",
          "Stage": "Experimental"
        },
        {
          "Member": "System.Uri? Microsoft.Extensions.AI.SpeechToTextClientMetadata.ProviderUri { get; }",
          "Stage": "Experimental"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.SpeechToTextOptions",
      "Stage": "Experimental",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.SpeechToTextOptions.SpeechToTextOptions();",
          "Stage": "Experimental"
        },
        {
          "Member": "virtual Microsoft.Extensions.AI.SpeechToTextOptions Microsoft.Extensions.AI.SpeechToTextOptions.Clone();",
          "Stage": "Experimental"
        }
      ],
      "Properties": [
        {
          "Member": "Microsoft.Extensions.AI.AdditionalPropertiesDictionary? Microsoft.Extensions.AI.SpeechToTextOptions.AdditionalProperties { get; set; }",
          "Stage": "Experimental"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.SpeechToTextOptions.ModelId { get; set; }",
          "Stage": "Experimental"
        },
        {
          "Member": "System.Func<Microsoft.Extensions.AI.ISpeechToTextClient, object?>? Microsoft.Extensions.AI.SpeechToTextOptions.RawRepresentationFactory { get; set; }",
          "Stage": "Experimental"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.SpeechToTextOptions.SpeechLanguage { get; set; }",
          "Stage": "Experimental"
        },
        {
          "Member": "int? Microsoft.Extensions.AI.SpeechToTextOptions.SpeechSampleRate { get; set; }",
          "Stage": "Experimental"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.SpeechToTextOptions.TextLanguage { get; set; }",
          "Stage": "Experimental"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.SpeechToTextResponse",
      "Stage": "Experimental",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.SpeechToTextResponse.SpeechToTextResponse();",
          "Stage": "Experimental"
        },
        {
          "Member": "Microsoft.Extensions.AI.SpeechToTextResponse.SpeechToTextResponse(System.Collections.Generic.IList<Microsoft.Extensions.AI.AIContent> contents);",
          "Stage": "Experimental"
        },
        {
          "Member": "Microsoft.Extensions.AI.SpeechToTextResponse.SpeechToTextResponse(string? content);",
          "Stage": "Experimental"
        },
        {
          "Member": "Microsoft.Extensions.AI.SpeechToTextResponseUpdate[] Microsoft.Extensions.AI.SpeechToTextResponse.ToSpeechToTextResponseUpdates();",
          "Stage": "Experimental"
        },
        {
          "Member": "override string Microsoft.Extensions.AI.SpeechToTextResponse.ToString();",
          "Stage": "Experimental"
        }
      ],
      "Properties": [
        {
          "Member": "Microsoft.Extensions.AI.AdditionalPropertiesDictionary? Microsoft.Extensions.AI.SpeechToTextResponse.AdditionalProperties { get; set; }",
          "Stage": "Experimental"
        },
        {
          "Member": "System.Collections.Generic.IList<Microsoft.Extensions.AI.AIContent> Microsoft.Extensions.AI.SpeechToTextResponse.Contents { get; set; }",
          "Stage": "Experimental"
        },
        {
          "Member": "System.TimeSpan? Microsoft.Extensions.AI.SpeechToTextResponse.EndTime { get; set; }",
          "Stage": "Experimental"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.SpeechToTextResponse.ModelId { get; set; }",
          "Stage": "Experimental"
        },
        {
          "Member": "object? Microsoft.Extensions.AI.SpeechToTextResponse.RawRepresentation { get; set; }",
          "Stage": "Experimental"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.SpeechToTextResponse.ResponseId { get; set; }",
          "Stage": "Experimental"
        },
        {
          "Member": "System.TimeSpan? Microsoft.Extensions.AI.SpeechToTextResponse.StartTime { get; set; }",
          "Stage": "Experimental"
        },
        {
          "Member": "string Microsoft.Extensions.AI.SpeechToTextResponse.Text { get; }",
          "Stage": "Experimental"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.SpeechToTextResponseUpdate",
      "Stage": "Experimental",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.SpeechToTextResponseUpdate.SpeechToTextResponseUpdate();",
          "Stage": "Experimental"
        },
        {
          "Member": "Microsoft.Extensions.AI.SpeechToTextResponseUpdate.SpeechToTextResponseUpdate(System.Collections.Generic.IList<Microsoft.Extensions.AI.AIContent> contents);",
          "Stage": "Experimental"
        },
        {
          "Member": "Microsoft.Extensions.AI.SpeechToTextResponseUpdate.SpeechToTextResponseUpdate(string? content);",
          "Stage": "Experimental"
        },
        {
          "Member": "override string Microsoft.Extensions.AI.SpeechToTextResponseUpdate.ToString();",
          "Stage": "Experimental"
        }
      ],
      "Properties": [
        {
          "Member": "Microsoft.Extensions.AI.AdditionalPropertiesDictionary? Microsoft.Extensions.AI.SpeechToTextResponseUpdate.AdditionalProperties { get; set; }",
          "Stage": "Experimental"
        },
        {
          "Member": "System.Collections.Generic.IList<Microsoft.Extensions.AI.AIContent> Microsoft.Extensions.AI.SpeechToTextResponseUpdate.Contents { get; set; }",
          "Stage": "Experimental"
        },
        {
          "Member": "System.TimeSpan? Microsoft.Extensions.AI.SpeechToTextResponseUpdate.EndTime { get; set; }",
          "Stage": "Experimental"
        },
        {
          "Member": "Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind Microsoft.Extensions.AI.SpeechToTextResponseUpdate.Kind { get; set; }",
          "Stage": "Experimental"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.SpeechToTextResponseUpdate.ModelId { get; set; }",
          "Stage": "Experimental"
        },
        {
          "Member": "object? Microsoft.Extensions.AI.SpeechToTextResponseUpdate.RawRepresentation { get; set; }",
          "Stage": "Experimental"
        },
        {
          "Member": "string? Microsoft.Extensions.AI.SpeechToTextResponseUpdate.ResponseId { get; set; }",
          "Stage": "Experimental"
        },
        {
          "Member": "System.TimeSpan? Microsoft.Extensions.AI.SpeechToTextResponseUpdate.StartTime { get; set; }",
          "Stage": "Experimental"
        },
        {
          "Member": "string Microsoft.Extensions.AI.SpeechToTextResponseUpdate.Text { get; }",
          "Stage": "Experimental"
        }
      ]
    },
    {
      "Type": "static class Microsoft.Extensions.AI.SpeechToTextResponseUpdateExtensions",
      "Stage": "Experimental",
      "Methods": [
        {
          "Member": "static Microsoft.Extensions.AI.SpeechToTextResponse Microsoft.Extensions.AI.SpeechToTextResponseUpdateExtensions.ToSpeechToTextResponse(this System.Collections.Generic.IEnumerable<Microsoft.Extensions.AI.SpeechToTextResponseUpdate> updates);",
          "Stage": "Experimental"
        },
        {
          "Member": "static System.Threading.Tasks.Task<Microsoft.Extensions.AI.SpeechToTextResponse> Microsoft.Extensions.AI.SpeechToTextResponseUpdateExtensions.ToSpeechToTextResponseAsync(this System.Collections.Generic.IAsyncEnumerable<Microsoft.Extensions.AI.SpeechToTextResponseUpdate> updates, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));",
          "Stage": "Experimental"
        }
      ]
    },
    {
      "Type": "readonly struct Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind : System.IEquatable<Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind>",
      "Stage": "Experimental",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind.SpeechToTextResponseUpdateKind(string value);",
          "Stage": "Experimental"
        },
        {
          "Member": "Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind.SpeechToTextResponseUpdateKind();",
          "Stage": "Experimental"
        },
        {
          "Member": "override bool Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind.Equals(object? obj);",
          "Stage": "Experimental"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind.Equals(Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind other);",
          "Stage": "Experimental"
        },
        {
          "Member": "override int Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind.GetHashCode();",
          "Stage": "Experimental"
        },
        {
          "Member": "static bool Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind.operator ==(Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind left, Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind right);",
          "Stage": "Experimental"
        },
        {
          "Member": "static bool Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind.operator !=(Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind left, Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind right);",
          "Stage": "Experimental"
        },
        {
          "Member": "override string Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind.ToString();",
          "Stage": "Experimental"
        }
      ],
      "Properties": [
        {
          "Member": "static Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind.Error { get; }",
          "Stage": "Experimental"
        },
        {
          "Member": "static Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind.SessionClose { get; }",
          "Stage": "Experimental"
        },
        {
          "Member": "static Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind.SessionOpen { get; }",
          "Stage": "Experimental"
        },
        {
          "Member": "static Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind.TextUpdated { get; }",
          "Stage": "Experimental"
        },
        {
          "Member": "static Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind.TextUpdating { get; }",
          "Stage": "Experimental"
        },
        {
          "Member": "string Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind.Value { get; }",
          "Stage": "Experimental"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind.Converter",
      "Stage": "Experimental",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind.Converter.Converter();",
          "Stage": "Experimental"
        },
        {
          "Member": "override Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind.Converter.Read(ref System.Text.Json.Utf8JsonReader reader, System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options);",
          "Stage": "Experimental"
        },
        {
          "Member": "override void Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind.Converter.Write(System.Text.Json.Utf8JsonWriter writer, Microsoft.Extensions.AI.SpeechToTextResponseUpdateKind value, System.Text.Json.JsonSerializerOptions options);",
          "Stage": "Experimental"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.TextContent : Microsoft.Extensions.AI.AIContent",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.TextContent.TextContent(string? text);",
          "Stage": "Stable"
        },
        {
          "Member": "override string Microsoft.Extensions.AI.TextContent.ToString();",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "string Microsoft.Extensions.AI.TextContent.Text { get; set; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "sealed class Microsoft.Extensions.AI.TextReasoningContent : Microsoft.Extensions.AI.AIContent",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.TextReasoningContent.TextReasoningContent(string? text);",
          "Stage": "Stable"
        },
        {
          "Member": "override string Microsoft.Extensions.AI.TextReasoningContent.ToString();",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "string Microsoft.Extensions.AI.TextReasoningContent.Text { get; set; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.UriContent : Microsoft.Extensions.AI.AIContent",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.UriContent.UriContent(string uri, string mediaType);",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.UriContent.UriContent(System.Uri uri, string mediaType);",
          "Stage": "Stable"
        },
        {
          "Member": "bool Microsoft.Extensions.AI.UriContent.HasTopLevelMediaType(string topLevelType);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "string Microsoft.Extensions.AI.UriContent.MediaType { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "System.Uri Microsoft.Extensions.AI.UriContent.Uri { get; set; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.UsageContent : Microsoft.Extensions.AI.AIContent",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.UsageContent.UsageContent();",
          "Stage": "Stable"
        },
        {
          "Member": "Microsoft.Extensions.AI.UsageContent.UsageContent(Microsoft.Extensions.AI.UsageDetails details);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "Microsoft.Extensions.AI.UsageDetails Microsoft.Extensions.AI.UsageContent.Details { get; set; }",
          "Stage": "Stable"
        }
      ]
    },
    {
      "Type": "class Microsoft.Extensions.AI.UsageDetails",
      "Stage": "Stable",
      "Methods": [
        {
          "Member": "Microsoft.Extensions.AI.UsageDetails.UsageDetails();",
          "Stage": "Stable"
        },
        {
          "Member": "void Microsoft.Extensions.AI.UsageDetails.Add(Microsoft.Extensions.AI.UsageDetails usage);",
          "Stage": "Stable"
        }
      ],
      "Properties": [
        {
          "Member": "Microsoft.Extensions.AI.AdditionalPropertiesDictionary<long>? Microsoft.Extensions.AI.UsageDetails.AdditionalCounts { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "long? Microsoft.Extensions.AI.UsageDetails.InputTokenCount { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "long? Microsoft.Extensions.AI.UsageDetails.OutputTokenCount { get; set; }",
          "Stage": "Stable"
        },
        {
          "Member": "long? Microsoft.Extensions.AI.UsageDetails.TotalTokenCount { get; set; }",
          "Stage": "Stable"
        }
      ]
    }
  ]
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/Throw.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Shared.Diagnostics;

internal static partial class Throw
{
    /// <summary>Throws an exception indicating that a required service is not available.</summary>
    public static InvalidOperationException CreateMissingServiceException(Type serviceType, object? serviceKey) =>
        new InvalidOperationException(serviceKey is null ?
            $"No service of type '{serviceType}' is available." :
            $"No service of type '{serviceType}' for the key '{serviceKey}' is available.");
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/UsageDetails.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides usage details about a request/response.</summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class UsageDetails
{
    /// <summary>Gets or sets the number of tokens in the input.</summary>
    public long? InputTokenCount { get; set; }

    /// <summary>Gets or sets the number of tokens in the output.</summary>
    public long? OutputTokenCount { get; set; }

    /// <summary>Gets or sets the total number of tokens used to produce the response.</summary>
    public long? TotalTokenCount { get; set; }

    /// <summary>Gets or sets a dictionary of additional usage counts.</summary>
    /// <remarks>
    /// All values set here are assumed to be summable. For example, when middleware makes multiple calls to an underlying
    /// service, it may sum the counts from multiple results to produce an overall <see cref="UsageDetails"/>.
    /// </remarks>
    public AdditionalPropertiesDictionary<long>? AdditionalCounts { get; set; }

    /// <summary>Adds usage data from another <see cref="UsageDetails"/> into this instance.</summary>
    /// <param name="usage">The source <see cref="UsageDetails"/> with which to augment this instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="usage"/> is <see langword="null"/>.</exception>
    public void Add(UsageDetails usage)
    {
        _ = Throw.IfNull(usage);

        InputTokenCount = NullableSum(InputTokenCount, usage.InputTokenCount);
        OutputTokenCount = NullableSum(OutputTokenCount, usage.OutputTokenCount);
        TotalTokenCount = NullableSum(TotalTokenCount, usage.TotalTokenCount);

        if (usage.AdditionalCounts is { } countsToAdd)
        {
            if (AdditionalCounts is null)
            {
                AdditionalCounts = new(countsToAdd);
            }
            else
            {
                foreach (var kvp in countsToAdd)
                {
                    AdditionalCounts[kvp.Key] = AdditionalCounts.TryGetValue(kvp.Key, out var existingValue) ?
                        kvp.Value + existingValue :
                        kvp.Value;
                }
            }
        }
    }

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal string DebuggerDisplay
    {
        get
        {
            List<string> parts = [];

            if (InputTokenCount is { } input)
            {
                parts.Add($"{nameof(InputTokenCount)} = {input}");
            }

            if (OutputTokenCount is { } output)
            {
                parts.Add($"{nameof(OutputTokenCount)} = {output}");
            }

            if (TotalTokenCount is { } total)
            {
                parts.Add($"{nameof(TotalTokenCount)} = {total}");
            }

            if (AdditionalCounts is { } additionalCounts)
            {
                foreach (var entry in additionalCounts)
                {
                    parts.Add($"{entry.Key} = {entry.Value}");
                }
            }

            return string.Join(", ", parts);
        }
    }

    private static long? NullableSum(long? a, long? b) => (a.HasValue || b.HasValue) ? (a ?? 0) + (b ?? 0) : null;
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/AutoChatToolMode.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Indicates that an <see cref="IChatClient"/> is free to select any of the available tools, or none at all.
/// </summary>
/// <remarks>
/// Use <see cref="ChatToolMode.Auto"/> to get an instance of <see cref="AutoChatToolMode"/>.
/// </remarks>
[DebuggerDisplay("Auto")]
public sealed class AutoChatToolMode : ChatToolMode
{
    /// <summary>Initializes a new instance of the <see cref="AutoChatToolMode"/> class.</summary>
    /// <remarks>Use <see cref="ChatToolMode.Auto"/> to get an instance of <see cref="AutoChatToolMode"/>.</remarks>
    public AutoChatToolMode()
    {
    } // must exist in support of polymorphic deserialization of a ChatToolMode

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is AutoChatToolMode;

    /// <inheritdoc/>
    public override int GetHashCode() => typeof(AutoChatToolMode).GetHashCode();
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/ChatClientExtensions.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides a collection of static methods for extending <see cref="IChatClient"/> instances.</summary>
public static class ChatClientExtensions
{
    /// <summary>Asks the <see cref="IChatClient"/> for an object of type <typeparamref name="TService"/>.</summary>
    /// <typeparam name="TService">The type of the object to be retrieved.</typeparam>
    /// <param name="client">The client.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that may be provided by the <see cref="IChatClient"/>,
    /// including itself or any services it might be wrapping.
    /// </remarks>
    public static TService? GetService<TService>(this IChatClient client, object? serviceKey = null)
    {
        _ = Throw.IfNull(client);

        return client.GetService(typeof(TService), serviceKey) is TService service ? service : default;
    }

    /// <summary>
    /// Asks the <see cref="IChatClient"/> for an object of the specified type <paramref name="serviceType"/>
    /// and throws an exception if one isn't available.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="serviceType">The type of object being requested.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No service of the requested type for the specified key is available.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of services that are required to be provided by the <see cref="IChatClient"/>,
    /// including itself or any services it might be wrapping.
    /// </remarks>
    public static object GetRequiredService(this IChatClient client, Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(client);
        _ = Throw.IfNull(serviceType);

        return
            client.GetService(serviceType, serviceKey) ??
            throw Throw.CreateMissingServiceException(serviceType, serviceKey);
    }

    /// <summary>
    /// Asks the <see cref="IChatClient"/> for an object of type <typeparamref name="TService"/>
    /// and throws an exception if one isn't available.
    /// </summary>
    /// <typeparam name="TService">The type of the object to be retrieved.</typeparam>
    /// <param name="client">The client.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No service of the requested type for the specified key is available.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that are required to be provided by the <see cref="IChatClient"/>,
    /// including itself or any services it might be wrapping.
    /// </remarks>
    public static TService GetRequiredService<TService>(this IChatClient client, object? serviceKey = null)
    {
        _ = Throw.IfNull(client);

        if (client.GetService(typeof(TService), serviceKey) is not TService service)
        {
            throw Throw.CreateMissingServiceException(typeof(TService), serviceKey);
        }

        return service;
    }

    /// <summary>Sends a user chat text message and returns the response messages.</summary>
    /// <param name="client">The chat client.</param>
    /// <param name="chatMessage">The text content for the chat message to send.</param>
    /// <param name="options">The chat options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The response messages generated by the client.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="chatMessage"/> is <see langword="null"/>.</exception>
    public static Task<ChatResponse> GetResponseAsync(
        this IChatClient client,
        string chatMessage,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(client);
        _ = Throw.IfNull(chatMessage);

        return client.GetResponseAsync(new ChatMessage(ChatRole.User, chatMessage), options, cancellationToken);
    }

    /// <summary>Sends a chat message and returns the response messages.</summary>
    /// <param name="client">The chat client.</param>
    /// <param name="chatMessage">The chat message to send.</param>
    /// <param name="options">The chat options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The response messages generated by the client.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="chatMessage"/> is <see langword="null"/>.</exception>
    public static Task<ChatResponse> GetResponseAsync(
        this IChatClient client,
        ChatMessage chatMessage,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(client);
        _ = Throw.IfNull(chatMessage);

        return client.GetResponseAsync([chatMessage], options, cancellationToken);
    }

    /// <summary>Sends a user chat text message and streams the response messages.</summary>
    /// <param name="client">The chat client.</param>
    /// <param name="chatMessage">The text content for the chat message to send.</param>
    /// <param name="options">The chat options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The response messages generated by the client.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="chatMessage"/> is <see langword="null"/>.</exception>
    public static IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        this IChatClient client,
        string chatMessage,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(client);
        _ = Throw.IfNull(chatMessage);

        return client.GetStreamingResponseAsync(new ChatMessage(ChatRole.User, chatMessage), options, cancellationToken);
    }

    /// <summary>Sends a chat message and streams the response messages.</summary>
    /// <param name="client">The chat client.</param>
    /// <param name="chatMessage">The chat message to send.</param>
    /// <param name="options">The chat options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The response messages generated by the client.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="chatMessage"/> is <see langword="null"/>.</exception>
    public static IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        this IChatClient client,
        ChatMessage chatMessage,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(client);
        _ = Throw.IfNull(chatMessage);

        return client.GetStreamingResponseAsync([chatMessage], options, cancellationToken);
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/ChatClientMetadata.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI;

/// <summary>Provides metadata about an <see cref="IChatClient"/>.</summary>
public class ChatClientMetadata
{
    /// <summary>Initializes a new instance of the <see cref="ChatClientMetadata"/> class.</summary>
    /// <param name="providerName">
    /// The name of the chat provider, if applicable. Where possible, this should map to the
    /// appropriate name defined in the OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </param>
    /// <param name="providerUri">The URL for accessing the chat provider, if applicable.</param>
    /// <param name="defaultModelId">The ID of the chat model used by default, if applicable.</param>
    public ChatClientMetadata(string? providerName = null, Uri? providerUri = null, string? defaultModelId = null)
    {
        DefaultModelId = defaultModelId;
        ProviderName = providerName;
        ProviderUri = providerUri;
    }

    /// <summary>Gets the name of the chat provider.</summary>
    /// <remarks>
    /// Where possible, this maps to the appropriate name defined in the
    /// OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </remarks>
    public string? ProviderName { get; }

    /// <summary>Gets the URL for accessing the chat provider.</summary>
    public Uri? ProviderUri { get; }

    /// <summary>Gets the ID of the default model used by this chat client.</summary>
    /// <remarks>
    /// This value can be <see langword="null"/> if no default model is set on the corresponding <see cref="IChatClient"/>.
    /// An individual request may override this value via <see cref="ChatOptions.ModelId"/>.
    /// </remarks>
    public string? DefaultModelId { get; }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/ChatFinishReason.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the reason a chat response completed.</summary>
[JsonConverter(typeof(Converter))]
public readonly struct ChatFinishReason : IEquatable<ChatFinishReason>
{
    /// <summary>The finish reason value. If <see langword="null"/> because `default(ChatFinishReason)` was used, the instance will behave like <see cref="Stop"/>.</summary>
    private readonly string? _value;

    /// <summary>Initializes a new instance of the <see cref="ChatFinishReason"/> struct with a string that describes the reason.</summary>
    /// <param name="value">The reason value.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty or composed entirely of whitespace.</exception>
    [JsonConstructor]
    public ChatFinishReason(string value)
    {
        _value = Throw.IfNullOrWhitespace(value);
    }

    /// <summary>Gets the finish reason value.</summary>
    public string Value => _value ?? Stop.Value;

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is ChatFinishReason other && Equals(other);

    /// <inheritdoc />
    public bool Equals(ChatFinishReason other) => StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <summary>
    /// Compares two instances.
    /// </summary>
    /// <param name="left">The left argument of the comparison.</param>
    /// <param name="right">The right argument of the comparison.</param>
    /// <returns><see langword="true" /> if the two instances are equal; <see langword="false" /> if they aren't equal.</returns>
    public static bool operator ==(ChatFinishReason left, ChatFinishReason right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two instances.
    /// </summary>
    /// <param name="left">The left argument of the comparison.</param>
    /// <param name="right">The right argument of the comparison.</param>
    /// <returns><see langword="true" /> if the two instances aren't equal; <see langword="false" /> if they are equal.</returns>
    public static bool operator !=(ChatFinishReason left, ChatFinishReason right)
    {
        return !(left == right);
    }

    /// <summary>Gets the <see cref="Value"/> of the finish reason.</summary>
    /// <returns>The <see cref="Value"/> of the finish reason.</returns>
    public override string ToString() => Value;

    /// <summary>Gets a <see cref="ChatFinishReason"/> representing the model encountering a natural stop point or provided stop sequence.</summary>
    public static ChatFinishReason Stop { get; } = new("stop");

    /// <summary>Gets a <see cref="ChatFinishReason"/> representing the model reaching the maximum length allowed for the request and/or response (typically in terms of tokens).</summary>
    public static ChatFinishReason Length { get; } = new("length");

    /// <summary>Gets a <see cref="ChatFinishReason"/> representing the model requesting the use of a tool that was defined in the request.</summary>
    public static ChatFinishReason ToolCalls { get; } = new("tool_calls");

    /// <summary>Gets a <see cref="ChatFinishReason"/> representing the model filtering content, whether for safety, prohibited content, sensitive content, or other such issues.</summary>
    public static ChatFinishReason ContentFilter { get; } = new("content_filter");

    /// <summary>Provides a <see cref="JsonConverter{ChatFinishReason}"/> for serializing <see cref="ChatFinishReason"/> instances.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class Converter : JsonConverter<ChatFinishReason>
    {
        /// <inheritdoc/>
        public override ChatFinishReason Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString()!);

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, ChatFinishReason value, JsonSerializerOptions options) =>
            Throw.IfNull(writer).WriteStringValue(value.Value);
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/ChatMessage.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a chat message used by an <see cref="IChatClient" />.</summary>
/// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/quickstarts/build-chat-app">Build an AI chat app with .NET.</related>
[DebuggerDisplay("[{Role}] {ContentForDebuggerDisplay}{EllipsesForDebuggerDisplay,nq}")]
public class ChatMessage
{
    private IList<AIContent>? _contents;
    private string? _authorName;

    /// <summary>Initializes a new instance of the <see cref="ChatMessage"/> class.</summary>
    /// <remarks>The instance defaults to having a role of <see cref="ChatRole.User"/>.</remarks>
    [JsonConstructor]
    public ChatMessage()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ChatMessage"/> class.</summary>
    /// <param name="role">The role of the author of the message.</param>
    /// <param name="content">The text content of the message.</param>
    public ChatMessage(ChatRole role, string? content)
        : this(role, content is null ? [] : [new TextContent(content)])
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ChatMessage"/> class.</summary>
    /// <param name="role">The role of the author of the message.</param>
    /// <param name="contents">The contents for this message.</param>
    public ChatMessage(ChatRole role, IList<AIContent>? contents)
    {
        Role = role;
        _contents = contents;
    }

    /// <summary>Clones the <see cref="ChatMessage"/> to a new <see cref="ChatMessage"/> instance.</summary>
    /// <returns>A shallow clone of the original message object.</returns>
    /// <remarks>
    /// This is a shallow clone. The returned instance is different from the original, but all properties
    /// refer to the same objects as the original.
    /// </remarks>
    public ChatMessage Clone() =>
        new()
        {
            AdditionalProperties = AdditionalProperties,
            _authorName = _authorName,
            _contents = _contents,
            RawRepresentation = RawRepresentation,
            Role = Role,
            MessageId = MessageId,
        };

    /// <summary>Gets or sets the name of the author of the message.</summary>
    public string? AuthorName
    {
        get => _authorName;
        set => _authorName = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>Gets or sets the role of the author of the message.</summary>
    public ChatRole Role { get; set; } = ChatRole.User;

    /// <summary>Gets the text of this message.</summary>
    /// <remarks>
    /// This property concatenates the text of all <see cref="TextContent"/> objects in <see cref="Contents"/>.
    /// </remarks>
    [JsonIgnore]
    public string Text => Contents.ConcatText();

    /// <summary>Gets or sets the chat message content items.</summary>
    [AllowNull]
    public IList<AIContent> Contents
    {
        get => _contents ??= [];
        set => _contents = value;
    }

    /// <summary>Gets or sets the ID of the chat message.</summary>
    public string? MessageId { get; set; }

    /// <summary>Gets or sets the raw representation of the chat message from an underlying implementation.</summary>
    /// <remarks>
    /// If a <see cref="ChatMessage"/> is created to represent some underlying object from another object
    /// model, this property can be used to store that original object. This can be useful for debugging or
    /// for enabling a consumer to access the underlying object model if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets any additional properties associated with the message.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <inheritdoc/>
    public override string ToString() => Text;

    /// <summary>Gets a <see cref="AIContent"/> object to display in the debugger display.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private AIContent? ContentForDebuggerDisplay => _contents is { Count: > 0 } ? _contents[0] : null;

    /// <summary>Gets an indication for the debugger display of whether there's more content.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string EllipsesForDebuggerDisplay => _contents is { Count: > 1 } ? ", ..." : string.Empty;
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/ChatOptions.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the options for a chat request.</summary>
/// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai#provide-options">Provide options.</related>
public class ChatOptions
{
    /// <summary>Gets or sets an optional identifier used to associate a request with an existing conversation.</summary>
    /// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai#stateless-vs-stateful-clients">Stateless vs. stateful clients.</related>
    public string? ConversationId { get; set; }

    /// <summary>Gets or sets additional per-request instructions to be provided to the <see cref="IChatClient"/>.</summary>
    public string? Instructions { get; set; }

    /// <summary>Gets or sets the temperature for generating chat responses.</summary>
    /// <remarks>
    /// This value controls the randomness of predictions made by the model. Use a lower value to decrease randomness in the response.
    /// </remarks>
    public float? Temperature { get; set; }

    /// <summary>Gets or sets the maximum number of tokens in the generated chat response.</summary>
    public int? MaxOutputTokens { get; set; }

    /// <summary>Gets or sets the "nucleus sampling" factor (or "top p") for generating chat responses.</summary>
    /// <remarks>
    /// Nucleus sampling is an alternative to sampling with temperature where the model
    /// considers the results of the tokens with <see cref="TopP"/> probability mass.
    /// For example, 0.1 means only the tokens comprising the top 10% probability mass are considered.
    /// </remarks>
    public float? TopP { get; set; }

    /// <summary>
    /// Gets or sets the number of most probable tokens that the model considers when generating the next part of the text.
    /// </summary>
    /// <remarks>
    /// This property reduces the probability of generating nonsense. A higher value gives more diverse answers, while a lower value is more conservative.
    /// </remarks>
    public int? TopK { get; set; }

    /// <summary>
    /// Gets or sets the penalty for repeated tokens in chat responses proportional to how many times they've appeared.
    /// </summary>
    /// <remarks>
    /// You can modify this value to reduce the repetitiveness of generated tokens. The higher the value, the stronger a penalty
    /// is applied to previously present tokens, proportional to how many times they've already appeared in the prompt or prior generation.
    /// </remarks>
    public float? FrequencyPenalty { get; set; }

    /// <summary>
    /// Gets or sets a value that influences the probability of generated tokens appearing based on their existing presence in generated text.
    /// </summary>
    /// <remarks>
    /// You can modify this value to reduce repetitiveness of generated tokens. Similar to <see cref="FrequencyPenalty"/>,
    /// except that this penalty is applied equally to all tokens that have already appeared, regardless of their exact frequencies.
    /// </remarks>
    public float? PresencePenalty { get; set; }

    /// <summary>Gets or sets a seed value used by a service to control the reproducibility of results.</summary>
    public long? Seed { get; set; }

    /// <summary>
    /// Gets or sets the response format for the chat request.
    /// </summary>
    /// <remarks>
    /// If <see langword="null"/>, no response format is specified and the client will use its default.
    /// This property can be set to <see cref="ChatResponseFormat.Text"/> to specify that the response should be unstructured text,
    /// to <see cref="ChatResponseFormat.Json"/> to specify that the response should be structured JSON data, or
    /// an instance of <see cref="ChatResponseFormatJson"/> constructed with a specific JSON schema to request that the
    /// response be structured JSON data according to that schema. It is up to the client implementation if or how
    /// to honor the request. If the client implementation doesn't recognize the specific kind of <see cref="ChatResponseFormat"/>,
    /// it can be ignored.
    /// </remarks>
    public ChatResponseFormat? ResponseFormat { get; set; }

    /// <summary>Gets or sets the model ID for the chat request.</summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// Gets or sets the list of stop sequences.
    /// </summary>
    /// <remarks>
    /// After a stop sequence is detected, the model stops generating further tokens for chat responses.
    /// </remarks>
    public IList<string>? StopSequences { get; set; }

    /// <summary>
    /// Gets or sets a flag to indicate whether a single response is allowed to include multiple tool calls.
    /// If <see langword="false"/>, the <see cref="IChatClient"/> is asked to return a maximum of one tool call per request.
    /// If <see langword="true"/>, there is no limit.
    /// If <see langword="null"/>, the provider may select its own default.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When used with function calling middleware, this does not affect the ability to perform multiple function calls in sequence.
    /// It only affects the number of function calls within a single iteration of the function calling loop.
    /// </para>
    /// <para>
    /// The underlying provider is not guaranteed to support or honor this flag. For example it may choose to ignore it and return multiple tool calls regardless.
    /// </para>
    /// </remarks>
    public bool? AllowMultipleToolCalls { get; set; }

    /// <summary>Gets or sets the tool mode for the chat request.</summary>
    /// <remarks>The default value is <see langword="null"/>, which is treated the same as <see cref="ChatToolMode.Auto"/>.</remarks>
    public ChatToolMode? ToolMode { get; set; }

    /// <summary>Gets or sets the list of tools to include with a chat request.</summary>
    /// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai#tool-calling">Tool calling.</related>
    [JsonIgnore]
    public IList<AITool>? Tools { get; set; }

    /// <summary>
    /// Gets or sets a callback responsible for creating the raw representation of the chat options from an underlying implementation.
    /// </summary>
    /// <remarks>
    /// The underlying <see cref="IChatClient" /> implementation may have its own representation of options.
    /// When <see cref="IChatClient.GetResponseAsync" /> or <see cref="IChatClient.GetStreamingResponseAsync" />
    /// is invoked with a <see cref="ChatOptions" />, that implementation may convert the provided options into
    /// its own representation in order to use it while performing the operation. For situations where a consumer knows
    /// which concrete <see cref="IChatClient" /> is being used and how it represents options, a new instance of that
    /// implementation-specific options type may be returned by this callback, for the <see cref="IChatClient" />
    /// implementation to use instead of creating a new instance. Such implementations may mutate the supplied options
    /// instance further based on other settings supplied on this <see cref="ChatOptions" /> instance or from other inputs,
    /// like the enumerable of <see cref="ChatMessage"/>s, therefore, it is <b>strongly recommended</b> to not return shared instances
    /// and instead make the callback return a new instance on each call.
    /// This is typically used to set an implementation-specific setting that isn't otherwise exposed from the strongly-typed
    /// properties on <see cref="ChatOptions" />.
    /// </remarks>
    [JsonIgnore]
    public Func<IChatClient, object?>? RawRepresentationFactory { get; set; }

    /// <summary>Gets or sets any additional properties associated with the options.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>Produces a clone of the current <see cref="ChatOptions"/> instance.</summary>
    /// <returns>A clone of the current <see cref="ChatOptions"/> instance.</returns>
    /// <remarks>
    /// The clone will have the same values for all properties as the original instance. Any collections, like <see cref="Tools"/>,
    /// <see cref="StopSequences"/>, and <see cref="AdditionalProperties"/>, are shallow-cloned, meaning a new collection instance is created,
    /// but any references contained by the collections are shared with the original.
    /// </remarks>
    public virtual ChatOptions Clone()
    {
        ChatOptions options = new()
        {
            AdditionalProperties = AdditionalProperties?.Clone(),
            AllowMultipleToolCalls = AllowMultipleToolCalls,
            ConversationId = ConversationId,
            FrequencyPenalty = FrequencyPenalty,
            Instructions = Instructions,
            MaxOutputTokens = MaxOutputTokens,
            ModelId = ModelId,
            PresencePenalty = PresencePenalty,
            RawRepresentationFactory = RawRepresentationFactory,
            ResponseFormat = ResponseFormat,
            Seed = Seed,
            Temperature = Temperature,
            ToolMode = ToolMode,
            TopK = TopK,
            TopP = TopP,
        };

        if (StopSequences is not null)
        {
            options.StopSequences = new List<string>(StopSequences);
        }

        if (Tools is not null)
        {
            options.Tools = new List<AITool>(Tools);
        }

        return options;
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/ChatResponse.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the response to a chat request.</summary>
/// <remarks>
/// <see cref="ChatResponse"/> provides one or more response messages and metadata about the response.
/// A typical response will contain a single message, however a response may contain multiple messages
/// in a variety of scenarios. For example, if automatic function calling is employed, such that a single
/// request to a <see cref="IChatClient"/> may actually generate multiple roundtrips to an inner <see cref="IChatClient"/>
/// it uses, all of the involved messages may be surfaced as part of the final <see cref="ChatResponse"/>.
/// </remarks>
public class ChatResponse
{
    /// <summary>The response messages.</summary>
    private IList<ChatMessage>? _messages;

    /// <summary>Initializes a new instance of the <see cref="ChatResponse"/> class.</summary>
    public ChatResponse()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ChatResponse"/> class.</summary>
    /// <param name="message">The response message.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
    public ChatResponse(ChatMessage message)
    {
        _ = Throw.IfNull(message);

        Messages.Add(message);
    }

    /// <summary>Initializes a new instance of the <see cref="ChatResponse"/> class.</summary>
    /// <param name="messages">The response messages.</param>
    public ChatResponse(IList<ChatMessage>? messages)
    {
        _messages = messages;
    }

    /// <summary>Gets or sets the chat response messages.</summary>
    [AllowNull]
    public IList<ChatMessage> Messages
    {
        get => _messages ??= new List<ChatMessage>(1);
        set => _messages = value;
    }

    /// <summary>Gets the text of the response.</summary>
    /// <remarks>
    /// This property concatenates the <see cref="ChatMessage.Text"/> of all <see cref="ChatMessage"/>
    /// instances in <see cref="Messages"/>.
    /// </remarks>
    [JsonIgnore]
    public string Text => _messages?.ConcatText() ?? string.Empty;

    /// <summary>Gets or sets the ID of the chat response.</summary>
    public string? ResponseId { get; set; }

    /// <summary>Gets or sets an identifier for the state of the conversation.</summary>
    /// <remarks>
    /// Some <see cref="IChatClient"/> implementations are capable of storing the state for a conversation, such that
    /// the input messages supplied to <see cref="IChatClient.GetResponseAsync"/> need only be the additional messages beyond
    /// what's already stored. If this property is non-<see langword="null"/>, it represents an identifier for that state,
    /// and it should be used in a subsequent <see cref="ChatOptions.ConversationId"/> instead of supplying the same messages
    /// (and this <see cref="ChatResponse"/>'s message) as part of the <c>messages</c> parameter. Note that the value may
    /// or may not differ on every response, depending on whether the underlying provider uses a fixed ID for each conversation
    /// or updates it for each message.
    /// </remarks>
    /// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai#stateless-vs-stateful-clients">Stateless vs. stateful clients.</related>
    public string? ConversationId { get; set; }

    /// <summary>Gets or sets the model ID used in the creation of the chat response.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets a timestamp for the chat response.</summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>Gets or sets the reason for the chat response.</summary>
    public ChatFinishReason? FinishReason { get; set; }

    /// <summary>Gets or sets usage details for the chat response.</summary>
    public UsageDetails? Usage { get; set; }

    /// <summary>Gets or sets the raw representation of the chat response from an underlying implementation.</summary>
    /// <remarks>
    /// If a <see cref="ChatResponse"/> is created to represent some underlying object from another object
    /// model, this property can be used to store that original object. This can be useful for debugging or
    /// for enabling a consumer to access the underlying object model if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets any additional properties associated with the chat response.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <inheritdoc />
    public override string ToString() => Text;

    /// <summary>Creates an array of <see cref="ChatResponseUpdate" /> instances that represent this <see cref="ChatResponse" />.</summary>
    /// <returns>An array of <see cref="ChatResponseUpdate" /> instances that may be used to represent this <see cref="ChatResponse" />.</returns>
    public ChatResponseUpdate[] ToChatResponseUpdates()
    {
        ChatResponseUpdate? extra = null;
        if (AdditionalProperties is not null || Usage is not null)
        {
            extra = new ChatResponseUpdate
            {
                AdditionalProperties = AdditionalProperties
            };

            if (Usage is { } usage)
            {
                extra.Contents.Add(new UsageContent(usage));
            }
        }

        int messageCount = _messages?.Count ?? 0;
        var updates = new ChatResponseUpdate[messageCount + (extra is not null ? 1 : 0)];

        int i;
        for (i = 0; i < messageCount; i++)
        {
            ChatMessage message = _messages![i];
            updates[i] = new ChatResponseUpdate
            {
                ConversationId = ConversationId,

                AdditionalProperties = message.AdditionalProperties,
                AuthorName = message.AuthorName,
                Contents = message.Contents,
                RawRepresentation = message.RawRepresentation,
                Role = message.Role,

                ResponseId = ResponseId,
                MessageId = message.MessageId,
                CreatedAt = CreatedAt,
                FinishReason = FinishReason,
                ModelId = ModelId
            };
        }

        if (extra is not null)
        {
            updates[i] = extra;
        }

        return updates;
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/ChatResponseExtensions.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S109 // Magic numbers should not be used
#pragma warning disable S1121 // Assignments should not be made from within sub-expressions

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides extension methods for working with <see cref="ChatResponse"/> and <see cref="ChatResponseUpdate"/> instances.
/// </summary>
public static class ChatResponseExtensions
{
    /// <summary>Adds all of the messages from <paramref name="response"/> into <paramref name="list"/>.</summary>
    /// <param name="list">The destination list to which the messages from <paramref name="response"/> should be added.</param>
    /// <param name="response">The response containing the messages to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is <see langword="null"/>.</exception>
    public static void AddMessages(this IList<ChatMessage> list, ChatResponse response)
    {
        _ = Throw.IfNull(list);
        _ = Throw.IfNull(response);

        if (list is List<ChatMessage> listConcrete)
        {
            listConcrete.AddRange(response.Messages);
        }
        else
        {
            foreach (var message in response.Messages)
            {
                list.Add(message);
            }
        }
    }

    /// <summary>Converts the <paramref name="updates"/> into <see cref="ChatMessage"/> instances and adds them to <paramref name="list"/>.</summary>
    /// <param name="list">The destination list to which the newly constructed messages should be added.</param>
    /// <param name="updates">The <see cref="ChatResponseUpdate"/> instances to convert to messages and add to the list.</param>
    /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="updates"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// As part of combining <paramref name="updates"/> into a series of <see cref="ChatMessage"/> instances, the
    /// method may use <see cref="ChatResponseUpdate.MessageId"/> to determine message boundaries, as well as coalesce
    /// contiguous <see cref="AIContent"/> items where applicable, e.g. multiple
    /// <see cref="TextContent"/> instances in a row may be combined into a single <see cref="TextContent"/>.
    /// </remarks>
    public static void AddMessages(this IList<ChatMessage> list, IEnumerable<ChatResponseUpdate> updates)
    {
        _ = Throw.IfNull(list);
        _ = Throw.IfNull(updates);

        if (updates is ICollection<ChatResponseUpdate> { Count: 0 })
        {
            return;
        }

        list.AddMessages(updates.ToChatResponse());
    }

    /// <summary>Converts the <paramref name="update"/> into a <see cref="ChatMessage"/> instance and adds it to <paramref name="list"/>.</summary>
    /// <param name="list">The destination list to which the newly constructed message should be added.</param>
    /// <param name="update">The <see cref="ChatResponseUpdate"/> instance to convert to a message and add to the list.</param>
    /// <param name="filter">A predicate to filter which <see cref="AIContent"/> gets included in the message.</param>
    /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="update"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// If the <see cref="ChatResponseUpdate"/> has no content, or all its content gets excluded by <paramref name="filter"/>, then
    /// no <see cref="ChatMessage"/> will be added to the <paramref name="list"/>.
    /// </remarks>
    public static void AddMessages(this IList<ChatMessage> list, ChatResponseUpdate update, Func<AIContent, bool>? filter = null)
    {
        _ = Throw.IfNull(list);
        _ = Throw.IfNull(update);

        var contentsList = filter is null ? update.Contents : update.Contents.Where(filter).ToList();
        if (contentsList.Count > 0)
        {
            list.Add(new ChatMessage(update.Role ?? ChatRole.Assistant, contentsList)
            {
                AuthorName = update.AuthorName,
                RawRepresentation = update.RawRepresentation,
                AdditionalProperties = update.AdditionalProperties,
            });
        }
    }

    /// <summary>Converts the <paramref name="updates"/> into <see cref="ChatMessage"/> instances and adds them to <paramref name="list"/>.</summary>
    /// <param name="list">The list to which the newly constructed messages should be added.</param>
    /// <param name="updates">The <see cref="ChatResponseUpdate"/> instances to convert to messages and add to the list.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A <see cref="Task"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="updates"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// As part of combining <paramref name="updates"/> into a series of <see cref="ChatMessage"/> instances, tne
    /// method may use <see cref="ChatResponseUpdate.MessageId"/> to determine message boundaries, as well as coalesce
    /// contiguous <see cref="AIContent"/> items where applicable, e.g. multiple
    /// <see cref="TextContent"/> instances in a row may be combined into a single <see cref="TextContent"/>.
    /// </remarks>
    public static Task AddMessagesAsync(
        this IList<ChatMessage> list, IAsyncEnumerable<ChatResponseUpdate> updates, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(list);
        _ = Throw.IfNull(updates);

        return AddMessagesAsync(list, updates, cancellationToken);

        static async Task AddMessagesAsync(
            IList<ChatMessage> list, IAsyncEnumerable<ChatResponseUpdate> updates, CancellationToken cancellationToken) =>
            list.AddMessages(await updates.ToChatResponseAsync(cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Combines <see cref="ChatResponseUpdate"/> instances into a single <see cref="ChatResponse"/>.</summary>
    /// <param name="updates">The updates to be combined.</param>
    /// <returns>The combined <see cref="ChatResponse"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="updates"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// As part of combining <paramref name="updates"/> into a single <see cref="ChatResponse"/>, the method will attempt to reconstruct
    /// <see cref="ChatMessage"/> instances. This includes using <see cref="ChatResponseUpdate.MessageId"/> to determine
    /// message boundaries, as well as coalescing contiguous <see cref="AIContent"/> items where applicable, e.g. multiple
    /// <see cref="TextContent"/> instances in a row may be combined into a single <see cref="TextContent"/>.
    /// </remarks>
    public static ChatResponse ToChatResponse(
        this IEnumerable<ChatResponseUpdate> updates)
    {
        _ = Throw.IfNull(updates);

        ChatResponse response = new();

        foreach (var update in updates)
        {
            ProcessUpdate(update, response);
        }

        FinalizeResponse(response);

        return response;
    }

    /// <summary>Combines <see cref="ChatResponseUpdate"/> instances into a single <see cref="ChatResponse"/>.</summary>
    /// <param name="updates">The updates to be combined.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The combined <see cref="ChatResponse"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="updates"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// As part of combining <paramref name="updates"/> into a single <see cref="ChatResponse"/>, the method will attempt to reconstruct
    /// <see cref="ChatMessage"/> instances. This includes using <see cref="ChatResponseUpdate.MessageId"/> to determine
    /// message boundaries, as well as coalescing contiguous <see cref="AIContent"/> items where applicable, e.g. multiple
    /// <see cref="TextContent"/> instances in a row may be combined into a single <see cref="TextContent"/>.
    /// </remarks>
    public static Task<ChatResponse> ToChatResponseAsync(
        this IAsyncEnumerable<ChatResponseUpdate> updates, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(updates);

        return ToChatResponseAsync(updates, cancellationToken);

        static async Task<ChatResponse> ToChatResponseAsync(
            IAsyncEnumerable<ChatResponseUpdate> updates, CancellationToken cancellationToken)
        {
            ChatResponse response = new();

            await foreach (var update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                ProcessUpdate(update, response);
            }

            FinalizeResponse(response);

            return response;
        }
    }

    /// <summary>Coalesces sequential <see cref="AIContent"/> content elements.</summary>
    internal static void CoalesceTextContent(List<AIContent> contents)
    {
        Coalesce<TextContent>(contents, static text => new(text));
        Coalesce<TextReasoningContent>(contents, static text => new(text));

        // This implementation relies on TContent's ToString returning its exact text.
        static void Coalesce<TContent>(List<AIContent> contents, Func<string, TContent> fromText)
            where TContent : AIContent
        {
            StringBuilder? coalescedText = null;

            // Iterate through all of the items in the list looking for contiguous items that can be coalesced.
            int start = 0;
            while (start < contents.Count - 1)
            {
                // We need at least two TextContents in a row to be able to coalesce.
                if (contents[start] is not TContent firstText)
                {
                    start++;
                    continue;
                }

                if (contents[start + 1] is not TContent secondText)
                {
                    start += 2;
                    continue;
                }

                // Append the text from those nodes and continue appending subsequent TextContents until we run out.
                // We null out nodes as their text is appended so that we can later remove them all in one O(N) operation.
                coalescedText ??= new();
                _ = coalescedText.Clear().Append(firstText).Append(secondText);
                contents[start + 1] = null!;
                int i = start + 2;
                for (; i < contents.Count && contents[i] is TContent next; i++)
                {
                    _ = coalescedText.Append(next);
                    contents[i] = null!;
                }

                // Store the replacement node. We inherit the properties of the first text node. We don't
                // currently propagate additional properties from the subsequent nodes. If we ever need to,
                // we can add that here.
                var newContent = fromText(coalescedText.ToString());
                contents[start] = newContent;
                newContent.AdditionalProperties = firstText.AdditionalProperties?.Clone();

                start = i;
            }

            // Remove all of the null slots left over from the coalescing process.
            _ = contents.RemoveAll(u => u is null);
        }
    }

    /// <summary>Finalizes the <paramref name="response"/> object.</summary>
    private static void FinalizeResponse(ChatResponse response)
    {
        int count = response.Messages.Count;
        for (int i = 0; i < count; i++)
        {
            CoalesceTextContent((List<AIContent>)response.Messages[i].Contents);
        }
    }

    /// <summary>Processes the <see cref="ChatResponseUpdate"/>, incorporating its contents into <paramref name="response"/>.</summary>
    /// <param name="update">The update to process.</param>
    /// <param name="response">The <see cref="ChatResponse"/> object that should be updated based on <paramref name="update"/>.</param>
    private static void ProcessUpdate(ChatResponseUpdate update, ChatResponse response)
    {
        // If there is no message created yet, or if the last update we saw had a different
        // message ID than the newest update, create a new message.
        ChatMessage message;
        var isNewMessage = false;
        if (response.Messages.Count == 0)
        {
            isNewMessage = true;
        }
        else if (update.MessageId is { Length: > 0 } updateMessageId
            && response.Messages[response.Messages.Count - 1].MessageId is string lastMessageId
            && updateMessageId != lastMessageId)
        {
            isNewMessage = true;
        }

        if (isNewMessage)
        {
            message = new ChatMessage(ChatRole.Assistant, []);
            response.Messages.Add(message);
        }
        else
        {
            message = response.Messages[response.Messages.Count - 1];
        }

        // Some members on ChatResponseUpdate map to members of ChatMessage.
        // Incorporate those into the latest message; in cases where the message
        // stores a single value, prefer the latest update's value over anything
        // stored in the message.
        if (update.AuthorName is not null)
        {
            message.AuthorName = update.AuthorName;
        }

        if (update.Role is ChatRole role)
        {
            message.Role = role;
        }

        if (update.MessageId is { Length: > 0 })
        {
            // Note that this must come after the message checks earlier, as they depend
            // on this value for change detection.
            message.MessageId = update.MessageId;
        }

        foreach (var content in update.Contents)
        {
            switch (content)
            {
                // Usage content is treated specially and propagated to the response's Usage.
                case UsageContent usage:
                    (response.Usage ??= new()).Add(usage.Details);
                    break;

                default:
                    message.Contents.Add(content);
                    break;
            }
        }

        // Other members on a ChatResponseUpdate map to members of the ChatResponse.
        // Update the response object with those, preferring the values from later updates.

        if (update.ResponseId is { Length: > 0 })
        {
            response.ResponseId = update.ResponseId;
        }

        if (update.ConversationId is not null)
        {
            response.ConversationId = update.ConversationId;
        }

        if (update.CreatedAt is not null)
        {
            response.CreatedAt = update.CreatedAt;
        }

        if (update.FinishReason is not null)
        {
            response.FinishReason = update.FinishReason;
        }

        if (update.ModelId is not null)
        {
            response.ModelId = update.ModelId;
        }

        if (update.AdditionalProperties is not null)
        {
            if (response.AdditionalProperties is null)
            {
                response.AdditionalProperties = new(update.AdditionalProperties);
            }
            else
            {
                response.AdditionalProperties.SetAll(update.AdditionalProperties);
            }
        }
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/ChatResponseFormat.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the response format that is desired by the caller.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ChatResponseFormatText), typeDiscriminator: "text")]
[JsonDerivedType(typeof(ChatResponseFormatJson), typeDiscriminator: "json")]
#pragma warning disable CA1052 // Static holder types should be Static or NotInheritable
public class ChatResponseFormat
#pragma warning restore CA1052
{
    /// <summary>Initializes a new instance of the <see cref="ChatResponseFormat"/> class.</summary>
    /// <remarks>Prevents external instantiation. Close the inheritance hierarchy for now until we have good reason to open it.</remarks>
    private protected ChatResponseFormat()
    {
    }

    /// <summary>Gets a singleton instance representing unstructured textual data.</summary>
    public static ChatResponseFormatText Text { get; } = new();

    /// <summary>Gets a singleton instance representing structured JSON data but without any particular schema.</summary>
    public static ChatResponseFormatJson Json { get; } = new(schema: null);

    /// <summary>Creates a <see cref="ChatResponseFormatJson"/> representing structured JSON data with the specified schema.</summary>
    /// <param name="schema">The JSON schema.</param>
    /// <param name="schemaName">An optional name of the schema. For example, if the schema represents a particular class, this could be the name of the class.</param>
    /// <param name="schemaDescription">An optional description of the schema.</param>
    /// <returns>The <see cref="ChatResponseFormatJson"/> instance.</returns>
    public static ChatResponseFormatJson ForJsonSchema(
        JsonElement schema, string? schemaName = null, string? schemaDescription = null) =>
        new(schema,
            schemaName,
            schemaDescription);
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/ChatResponseFormatJson.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a response format for structured JSON data.</summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class ChatResponseFormatJson : ChatResponseFormat
{
    /// <summary>Initializes a new instance of the <see cref="ChatResponseFormatJson"/> class with the specified schema.</summary>
    /// <param name="schema">The schema to associate with the JSON response.</param>
    /// <param name="schemaName">A name for the schema.</param>
    /// <param name="schemaDescription">A description of the schema.</param>
    [JsonConstructor]
    public ChatResponseFormatJson(
        JsonElement? schema, string? schemaName = null, string? schemaDescription = null)
    {
        if (schema is null && (schemaName is not null || schemaDescription is not null))
        {
            Throw.ArgumentException(
                schemaName is not null ? nameof(schemaName) : nameof(schemaDescription),
                "Schema name and description can only be specified if a schema is provided.");
        }

        Schema = schema;
        SchemaName = schemaName;
        SchemaDescription = schemaDescription;
    }

    /// <summary>Gets the JSON schema associated with the response, or <see langword="null"/> if there is none.</summary>
    public JsonElement? Schema { get; }

    /// <summary>Gets a name for the schema.</summary>
    public string? SchemaName { get; }

    /// <summary>Gets a description of the schema.</summary>
    public string? SchemaDescription { get; }

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => Schema?.ToString() ?? "JSON";
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/ChatResponseFormatText.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a response format with no constraints around the format.</summary>
/// <remarks>
/// Use <see cref="ChatResponseFormat.Text"/> to get an instance of <see cref="ChatResponseFormatText"/>.
/// </remarks>
[DebuggerDisplay("Text")]
public sealed class ChatResponseFormatText : ChatResponseFormat
{
    /// <summary>Initializes a new instance of the <see cref="ChatResponseFormatText"/> class.</summary>
    /// <remarks> Use <see cref="ChatResponseFormat.Text"/> to get an instance of <see cref="ChatResponseFormatText"/>.</remarks>
    public ChatResponseFormatText()
    {
        // must exist in support of polymorphic deserialization of a ChatResponseFormat
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ChatResponseFormatText;

    /// <inheritdoc/>
    public override int GetHashCode() => typeof(ChatResponseFormatText).GetHashCode();
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/ChatResponseUpdate.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a single streaming response chunk from an <see cref="IChatClient"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ChatResponseUpdate"/> is so named because it represents updates
/// that layer on each other to form a single chat response. Conceptually, this combines the roles of
/// <see cref="ChatResponse"/> and <see cref="ChatMessage"/> in streaming output.
/// </para>
/// <para>
/// The relationship between <see cref="ChatResponse"/> and <see cref="ChatResponseUpdate"/> is
/// codified in the <see cref="ChatResponseExtensions.ToChatResponseAsync"/> and
/// <see cref="ChatResponse.ToChatResponseUpdates"/>, which enable bidirectional conversions
/// between the two. Note, however, that the provided conversions may be lossy, for example if multiple
/// updates all have different <see cref="RawRepresentation"/> objects whereas there's only one slot for
/// such an object available in <see cref="ChatResponse.RawRepresentation"/>. Similarly, if different
/// updates provide different values for properties like <see cref="ModelId"/>,
/// only one of the values will be used to populate <see cref="ChatResponse.ModelId"/>.
/// </para>
/// </remarks>
[DebuggerDisplay("[{Role}] {ContentForDebuggerDisplay}{EllipsesForDebuggerDisplay,nq}")]
public class ChatResponseUpdate
{
    /// <summary>The response update content items.</summary>
    private IList<AIContent>? _contents;

    /// <summary>The name of the author of the update.</summary>
    private string? _authorName;

    /// <summary>Initializes a new instance of the <see cref="ChatResponseUpdate"/> class.</summary>
    [JsonConstructor]
    public ChatResponseUpdate()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ChatResponseUpdate"/> class.</summary>
    /// <param name="role">The role of the author of the update.</param>
    /// <param name="content">The text content of the update.</param>
    public ChatResponseUpdate(ChatRole? role, string? content)
        : this(role, content is null ? null : [new TextContent(content)])
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ChatResponseUpdate"/> class.</summary>
    /// <param name="role">The role of the author of the update.</param>
    /// <param name="contents">The contents of the update.</param>
    public ChatResponseUpdate(ChatRole? role, IList<AIContent>? contents)
    {
        Role = role;
        _contents = contents;
    }

    /// <summary>Gets or sets the name of the author of the response update.</summary>
    public string? AuthorName
    {
        get => _authorName;
        set => _authorName = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>Gets or sets the role of the author of the response update.</summary>
    public ChatRole? Role { get; set; }

    /// <summary>Gets the text of this update.</summary>
    /// <remarks>
    /// This property concatenates the text of all <see cref="TextContent"/> objects in <see cref="Contents"/>.
    /// </remarks>
    [JsonIgnore]
    public string Text => _contents is not null ? _contents.ConcatText() : string.Empty;

    /// <summary>Gets or sets the chat response update content items.</summary>
    [AllowNull]
    public IList<AIContent> Contents
    {
        get => _contents ??= [];
        set => _contents = value;
    }

    /// <summary>Gets or sets the raw representation of the response update from an underlying implementation.</summary>
    /// <remarks>
    /// If a <see cref="ChatResponseUpdate"/> is created to represent some underlying object from another object
    /// model, this property can be used to store that original object. This can be useful for debugging or
    /// for enabling a consumer to access the underlying object model if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets additional properties for the update.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>Gets or sets the ID of the response of which this update is a part.</summary>
    public string? ResponseId { get; set; }

    /// <summary>Gets or sets the ID of the message of which this update is a part.</summary>
    /// <remarks>
    /// A single streaming response may be composed of multiple messages, each of which may be represented
    /// by multiple updates. This property is used to group those updates together into messages.
    ///
    /// Some providers may consider streaming responses to be a single message, and in that case
    /// the value of this property may be the same as the response ID.
    /// 
    /// This value is used when <see cref="ChatResponseExtensions.ToChatResponseAsync(IAsyncEnumerable{ChatResponseUpdate}, System.Threading.CancellationToken)"/>
    /// groups <see cref="ChatResponseUpdate"/> instances into <see cref="ChatMessage"/> instances.
    /// The value must be unique to each call to the underlying provider, and must be shared by
    /// all updates that are part of the same logical message within a streaming response.
    /// </remarks>
    public string? MessageId { get; set; }

    /// <summary>Gets or sets an identifier for the state of the conversation of which this update is a part.</summary>
    /// <remarks>
    /// Some <see cref="IChatClient"/> implementations are capable of storing the state for a conversation, such that
    /// the input messages supplied to <see cref="IChatClient.GetStreamingResponseAsync"/> need only be the additional messages beyond
    /// what's already stored. If this property is non-<see langword="null"/>, it represents an identifier for that state,
    /// and it should be used in a subsequent <see cref="ChatOptions.ConversationId"/> instead of supplying the same messages
    /// (and this streaming message) as part of the <c>messages</c> parameter. Note that the value may or may not differ on every
    /// response, depending on whether the underlying provider uses a fixed ID for each conversation or updates it for each message.
    /// </remarks>
    public string? ConversationId { get; set; }

    /// <summary>Gets or sets a timestamp for the response update.</summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>Gets or sets the finish reason for the operation.</summary>
    public ChatFinishReason? FinishReason { get; set; }

    /// <summary>Gets or sets the model ID associated with this response update.</summary>
    public string? ModelId { get; set; }

    /// <inheritdoc/>
    public override string ToString() => Text;

    /// <summary>Gets a <see cref="AIContent"/> object to display in the debugger display.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private AIContent? ContentForDebuggerDisplay => _contents is { Count: > 0 } ? _contents[0] : null;

    /// <summary>Gets an indication for the debugger display of whether there's more content.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string EllipsesForDebuggerDisplay => _contents is { Count: > 1 } ? ", ..." : string.Empty;
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/ChatRole.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Describes the intended purpose of a message within a chat interaction.
/// </summary>
[JsonConverter(typeof(Converter))]
[DebuggerDisplay("{Value,nq}")]
public readonly struct ChatRole : IEquatable<ChatRole>
{
    /// <summary>Gets the role that instructs or sets the behavior of the system.</summary>
    public static ChatRole System { get; } = new("system");

    /// <summary>Gets the role that provides responses to system-instructed, user-prompted input.</summary>
    public static ChatRole Assistant { get; } = new("assistant");

    /// <summary>Gets the role that provides user input for chat interactions.</summary>
    public static ChatRole User { get; } = new("user");

    /// <summary>Gets the role that provides additional information and references in response to tool use requests.</summary>
    public static ChatRole Tool { get; } = new("tool");

    /// <summary>
    /// Gets the value associated with this <see cref="ChatRole"/>.
    /// </summary>
    /// <remarks>
    /// The value will be serialized into the "role" message field of the Chat Message format.
    /// </remarks>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatRole"/> struct with the provided value.
    /// </summary>
    /// <param name="value">The value to associate with this <see cref="ChatRole"/>.</param>
    [JsonConstructor]
    public ChatRole(string value)
    {
        Value = Throw.IfNullOrWhitespace(value);
    }

    /// <summary>
    /// Returns a value indicating whether two <see cref="ChatRole"/> instances are equivalent, as determined by a
    /// case-insensitive comparison of their values.
    /// </summary>
    /// <param name="left">The first <see cref="ChatRole"/> instance to compare.</param>
    /// <param name="right">The second <see cref="ChatRole"/> instance to compare.</param>
    /// <returns><see langword="true"/> if left and right are both <see langword="null"/> or have equivalent values; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(ChatRole left, ChatRole right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Returns a value indicating whether two <see cref="ChatRole"/> instances are not equivalent, as determined by a
    /// case-insensitive comparison of their values.
    /// </summary>
    /// <param name="left">The first <see cref="ChatRole"/> instance to compare. </param>
    /// <param name="right">The second <see cref="ChatRole"/> instance to compare. </param>
    /// <returns><see langword="true"/> if left and right have different values; <see langword="false"/> if they have equivalent values or are both <see langword="null"/>.</returns>
    public static bool operator !=(ChatRole left, ChatRole right)
    {
        return !(left == right);
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is ChatRole otherRole && Equals(otherRole);

    /// <inheritdoc/>
    public bool Equals(ChatRole other)
        => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override int GetHashCode()
        => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <inheritdoc/>
    public override string ToString() => Value;

    /// <summary>Provides a <see cref="JsonConverter{ChatRole}"/> for serializing <see cref="ChatRole"/> instances.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class Converter : JsonConverter<ChatRole>
    {
        /// <inheritdoc />
        public override ChatRole Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString()!);

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, ChatRole value, JsonSerializerOptions options) =>
            Throw.IfNull(writer).WriteStringValue(value.Value);
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/ChatToolMode.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Describes how tools should be selected by a <see cref="IChatClient"/>.
/// </summary>
/// <remarks>
/// The predefined values <see cref="Auto" />, <see cref="None"/>, and <see cref="RequireAny"/> are provided.
/// To nominate a specific function, use <see cref="RequireSpecific(string)"/>.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(NoneChatToolMode), typeDiscriminator: "none")]
[JsonDerivedType(typeof(AutoChatToolMode), typeDiscriminator: "auto")]
[JsonDerivedType(typeof(RequiredChatToolMode), typeDiscriminator: "required")]
#pragma warning disable CA1052 // Static holder types should be Static or NotInheritable
public class ChatToolMode
#pragma warning restore CA1052
{
    /// <summary>Initializes a new instance of the <see cref="ChatToolMode"/> class.</summary>
    /// <remarks>Prevents external instantiation. Close the inheritance hierarchy for now until we have good reason to open it.</remarks>
    private protected ChatToolMode()
    {
    }

    /// <summary>
    /// Gets a predefined <see cref="ChatToolMode"/> indicating that tool usage is optional.
    /// </summary>
    /// <remarks>
    /// <see cref="ChatOptions.Tools"/> can contain zero or more <see cref="AITool"/>
    /// instances, and the <see cref="IChatClient"/> is free to invoke zero or more of them.
    /// </remarks>
    public static AutoChatToolMode Auto { get; } = new();

    /// <summary>
    /// Gets a predefined <see cref="ChatToolMode"/> indicating that tool usage is unsupported.
    /// </summary>
    /// <remarks>
    /// <see cref="ChatOptions.Tools"/> can contain zero or more <see cref="AITool"/>
    /// instances, but the <see cref="IChatClient"/> should not request the invocation of
    /// any of them. This can be used when the <see cref="IChatClient"/> should know about
    /// tools in order to provide information about them or plan out their usage, but should
    /// not request the invocation of any of them.
    /// </remarks>
    public static NoneChatToolMode None { get; } = new();

    /// <summary>
    /// Gets a predefined <see cref="ChatToolMode"/> indicating that tool usage is required,
    /// but that any tool can be selected. At least one tool must be provided in <see cref="ChatOptions.Tools"/>.
    /// </summary>
    public static RequiredChatToolMode RequireAny { get; } = new(requiredFunctionName: null);

    /// <summary>
    /// Instantiates a <see cref="ChatToolMode"/> indicating that tool usage is required,
    /// and that the specified <see cref="AIFunction"/> must be selected. The function name
    /// must match an entry in <see cref="ChatOptions.Tools"/>.
    /// </summary>
    /// <param name="functionName">The name of the required function.</param>
    /// <returns>An instance of <see cref="RequiredChatToolMode"/> for the specified function name.</returns>
    public static RequiredChatToolMode RequireSpecific(string functionName) => new(functionName);
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/DelegatingChatClient.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides an optional base class for an <see cref="IChatClient"/> that passes through calls to another instance.
/// </summary>
/// <remarks>
/// This is recommended as a base type when building clients that can be chained around an underlying <see cref="IChatClient"/>.
/// The default implementation simply passes each call to the inner client instance.
/// </remarks>
/// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai#custom-ichatclient-middleware">Custom IChatClient middleware.</related>
public class DelegatingChatClient : IChatClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DelegatingChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The wrapped client instance.</param>
    protected DelegatingChatClient(IChatClient innerClient)
    {
        InnerClient = Throw.IfNull(innerClient);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Gets the inner <see cref="IChatClient" />.</summary>
    protected IChatClient InnerClient { get; }

    /// <inheritdoc />
    public virtual Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        InnerClient.GetResponseAsync(messages, options, cancellationToken);

    /// <inheritdoc />
    public virtual IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        InnerClient.GetStreamingResponseAsync(messages, options, cancellationToken);

    /// <inheritdoc />
    public virtual object? GetService(Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(serviceType);

        // If the key is non-null, we don't know what it means so pass through to the inner service.
        return
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this :
            InnerClient.GetService(serviceType, serviceKey);
    }

    /// <summary>Provides a mechanism for releasing unmanaged resources.</summary>
    /// <param name="disposing"><see langword="true"/> if being called from <see cref="Dispose()"/>; otherwise, <see langword="false"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            InnerClient.Dispose();
        }
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/IChatClient.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a chat client.</summary>
/// <remarks>
/// <para>
/// Applications must consider risks such as prompt injection attacks, data sizes, and the number of messages
/// sent to the underlying provider or returned from it. Unless a specific <see cref="IChatClient"/> implementation
/// explicitly documents safeguards for these concerns, the application is expected to implement appropriate protections.
/// </para>
/// <para>
/// Unless otherwise specified, all members of <see cref="IChatClient"/> are thread-safe for concurrent use.
/// It is expected that all implementations of <see cref="IChatClient"/> support being used by multiple requests concurrently.
/// Instances must not be disposed of while the instance is still in use.
/// </para>
/// <para>
/// However, implementations of <see cref="IChatClient"/> might mutate the arguments supplied to <see cref="GetResponseAsync"/> and
/// <see cref="GetStreamingResponseAsync"/>, such as by configuring the options instance. Thus, consumers of the interface either
/// should avoid using shared instances of these arguments for concurrent invocations or should otherwise ensure by construction
/// that no <see cref="IChatClient"/> instances are used which might employ such mutation. For example, the ConfigureOptions method is
/// provided with a callback that could mutate the supplied options argument, and that should be avoided if using a singleton options instance.
/// </para>
/// </remarks>
/// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/quickstarts/build-chat-app">Build an AI chat app with .NET.</related>
/// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai#the-ichatclient-interface">The IChatClient interface.</related>
public interface IChatClient : IDisposable
{
    /// <summary>Sends chat messages and returns the response.</summary>
    /// <param name="messages">The sequence of chat messages to send.</param>
    /// <param name="options">The chat options with which to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The response messages generated by the client.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="messages"/> is <see langword="null"/>.</exception>
    /// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai#request-a-chat-response">Request a chat response.</related>
    Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Sends chat messages and streams the response.</summary>
    /// <param name="messages">The sequence of chat messages to send.</param>
    /// <param name="options">The chat options with which to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The response messages generated by the client.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="messages"/> is <see langword="null"/>.</exception>
    /// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai#request-a-streaming-chat-response">Request a streaming chat response.</related>
    IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Asks the <see cref="IChatClient"/> for an object of the specified type <paramref name="serviceType"/>.</summary>
    /// <param name="serviceType">The type of object being requested.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly-typed services that might be provided by the <see cref="IChatClient"/>,
    /// including itself or any services it might be wrapping. For example, to access the <see cref="ChatClientMetadata"/> for the instance,
    /// <see cref="GetService"/> may be used to request it.
    /// </remarks>
    object? GetService(Type serviceType, object? serviceKey = null);
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/NoneChatToolMode.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Indicates that an <see cref="IChatClient"/> should not request the invocation of any tools.
/// </summary>
/// <remarks>
/// Use <see cref="ChatToolMode.None"/> to get an instance of <see cref="NoneChatToolMode"/>.
/// </remarks>
[DebuggerDisplay("None")]
public sealed class NoneChatToolMode : ChatToolMode
{
    /// <summary>Initializes a new instance of the <see cref="NoneChatToolMode"/> class.</summary>
    /// <remarks>Use <see cref="ChatToolMode.None"/> to get an instance of <see cref="NoneChatToolMode"/>.</remarks>
    public NoneChatToolMode()
    {
    } // must exist in support of polymorphic deserialization of a ChatToolMode

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is NoneChatToolMode;

    /// <inheritdoc/>
    public override int GetHashCode() => typeof(NoneChatToolMode).GetHashCode();
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/ChatCompletion/RequiredChatToolMode.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a mode where a chat tool must be called. This class can optionally nominate a specific function
/// or indicate that any of the functions can be selected.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class RequiredChatToolMode : ChatToolMode
{
    /// <summary>
    /// Gets the name of a specific <see cref="AIFunction"/> that must be called.
    /// </summary>
    /// <remarks>
    /// If the value is <see langword="null"/>, any available function can be selected (but at least one must be).
    /// </remarks>
    public string? RequiredFunctionName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequiredChatToolMode"/> class that requires a specific function to be called.
    /// </summary>
    /// <param name="requiredFunctionName">The name of the function that must be called.</param>
    /// <exception cref="ArgumentException"><paramref name="requiredFunctionName"/> is empty or composed entirely of whitespace.</exception>
    /// <remarks>
    /// <paramref name="requiredFunctionName"/> can be <see langword="null"/>. However, it's preferable to use
    /// <see cref="ChatToolMode.RequireAny"/> when any function can be selected.
    /// </remarks>
    public RequiredChatToolMode(string? requiredFunctionName)
    {
        if (requiredFunctionName is not null)
        {
            _ = Throw.IfNullOrWhitespace(requiredFunctionName);
        }

        RequiredFunctionName = requiredFunctionName;
    }

    // The reason for not overriding Equals/GetHashCode (e.g., so two instances are equal if they
    // have the same RequiredFunctionName) is to leave open the option to unseal the type in the
    // future. If we did define equality based on RequiredFunctionName but a subclass added further
    // fields, this would lead to wrong behavior unless the subclass author remembers to re-override
    // Equals/GetHashCode as well, which they likely won't.

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"Required: {RequiredFunctionName ?? "Any"}";

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is RequiredChatToolMode other &&
        RequiredFunctionName == other.RequiredFunctionName;

    /// <inheritdoc/>
    public override int GetHashCode() =>
        RequiredFunctionName?.GetHashCode(StringComparison.Ordinal) ??
        typeof(RequiredChatToolMode).GetHashCode();
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/Contents/AIContent.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>Represents content used by AI services.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(DataContent), typeDiscriminator: "data")]
[JsonDerivedType(typeof(ErrorContent), typeDiscriminator: "error")]
[JsonDerivedType(typeof(FunctionCallContent), typeDiscriminator: "functionCall")]
[JsonDerivedType(typeof(FunctionResultContent), typeDiscriminator: "functionResult")]
[JsonDerivedType(typeof(TextContent), typeDiscriminator: "text")]
[JsonDerivedType(typeof(TextReasoningContent), typeDiscriminator: "reasoning")]
[JsonDerivedType(typeof(UriContent), typeDiscriminator: "uri")]
[JsonDerivedType(typeof(UsageContent), typeDiscriminator: "usage")]
public class AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIContent"/> class.
    /// </summary>
    public AIContent()
    {
    }

    /// <summary>Gets or sets the raw representation of the content from an underlying implementation.</summary>
    /// <remarks>
    /// If an <see cref="AIContent"/> is created to represent some underlying object from another object
    /// model, this property can be used to store that original object. This can be useful for debugging or
    /// for enabling a consumer to access the underlying object model, if needed.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets additional properties for the content.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/Contents/AIContentExtensions.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
#if NET
using System.Runtime.CompilerServices;
#else
using System.Text;
#endif

namespace Microsoft.Extensions.AI;

/// <summary>Internal extensions for working with <see cref="AIContent"/>.</summary>
internal static class AIContentExtensions
{
    /// <summary>Concatenates the text of all <see cref="TextContent"/> instances in the list.</summary>
    public static string ConcatText(this IEnumerable<AIContent> contents)
    {
        if (contents is IList<AIContent> list)
        {
            int count = list.Count;
            switch (count)
            {
                case 0:
                    return string.Empty;

                case 1:
                    return (list[0] as TextContent)?.Text ?? string.Empty;

                default:
#if NET
                    DefaultInterpolatedStringHandler builder = new(count, 0, null, stackalloc char[512]);
                    for (int i = 0; i < count; i++)
                    {
                        if (list[i] is TextContent text)
                        {
                            builder.AppendLiteral(text.Text);
                        }
                    }

                    return builder.ToStringAndClear();
#else
                    StringBuilder builder = new();
                    for (int i = 0; i < count; i++)
                    {
                        if (list[i] is TextContent text)
                        {
                            builder.Append(text.Text);
                        }
                    }

                    return builder.ToString();
#endif
            }
        }

        return string.Concat(contents.OfType<TextContent>());
    }

    /// <summary>Concatenates the <see cref="ChatMessage.Text"/> of all <see cref="ChatMessage"/> instances in the list.</summary>
    /// <remarks>A newline separator is added between each non-empty piece of text.</remarks>
    public static string ConcatText(this IList<ChatMessage> messages)
    {
        int count = messages.Count;
        switch (count)
        {
            case 0:
                return string.Empty;

            case 1:
                return messages[0].Text;

            default:
#if NET
                DefaultInterpolatedStringHandler builder = new(count, 0, null, stackalloc char[512]);
                bool needsSeparator = false;
                for (int i = 0; i < count; i++)
                {
                    string text = messages[i].Text;
                    if (text.Length > 0)
                    {
                        if (needsSeparator)
                        {
                            builder.AppendLiteral(Environment.NewLine);
                        }

                        builder.AppendLiteral(text);

                        needsSeparator = true;
                    }
                }

                return builder.ToStringAndClear();
#else
                StringBuilder builder = new();
                for (int i = 0; i < count; i++)
                {
                    string text = messages[i].Text;
                    if (text.Length > 0)
                    {
                        if (builder.Length > 0)
                        {
                            builder.AppendLine();
                        }

                        builder.Append(text);
                    }
                }

                return builder.ToString();
#endif
        }
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/Contents/DataContent.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
#if NET
using System.Buffers;
using System.Buffers.Text;
#endif
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
#if !NET
using System.Runtime.InteropServices;
#endif
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S3996 // URI properties should not be strings
#pragma warning disable CA1054 // URI-like parameters should not be strings
#pragma warning disable CA1056 // URI-like properties should not be strings
#pragma warning disable CA1307 // Specify StringComparison for clarity

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents binary content with an associated media type (also known as MIME type).
/// </summary>
/// <remarks>
/// <para>
/// The content represents in-memory data. For references to data at a remote URI, use <see cref="UriContent"/> instead.
/// </para>
/// <para>
/// <see cref="Uri"/> always returns a valid URI string, even if the instance was constructed from
/// a <see cref="ReadOnlyMemory{T}"/>. In that case, a data URI will be constructed and returned.
/// </para>
/// </remarks>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class DataContent : AIContent
{
    // Design note:
    // Ideally DataContent would be based in terms of Uri. However, Uri has a length limitation that makes it prohibitive
    // for the kinds of data URIs necessary to support here. As such, this type is based in strings.

    /// <summary>Parsed data URI information.</summary>
    private readonly DataUriParser.DataUri? _dataUri;

    /// <summary>The string-based representation of the URI, including any data in the instance.</summary>
    private string? _uri;

    /// <summary>The data, lazily initialized if the data is provided in a data URI.</summary>
    private ReadOnlyMemory<byte>? _data;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataContent"/> class.
    /// </summary>
    /// <param name="uri">The data URI containing the content.</param>
    /// <param name="mediaType">
    /// The media type (also known as MIME type) represented by the content. If not provided,
    /// it must be provided as part of the <paramref name="uri"/>.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="uri"/> is not a data URI.</exception>
    /// <exception cref="ArgumentException"><paramref name="uri"/> did not contain a media type and <paramref name="mediaType"/> was not supplied.</exception>
    /// <exception cref="ArgumentException"><paramref name="mediaType"/> is an invalid media type.</exception>
    public DataContent(Uri uri, string? mediaType = null)
        : this(Throw.IfNull(uri).ToString(), mediaType)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataContent"/> class.
    /// </summary>
    /// <param name="uri">The data URI containing the content.</param>
    /// <param name="mediaType">The media type (also known as MIME type) represented by the content.</param>
    /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="uri"/> is not a data URI.</exception>
    /// <exception cref="ArgumentException"><paramref name="uri"/> did not contain a media type and <paramref name="mediaType"/> was not supplied.</exception>
    /// <exception cref="ArgumentException"><paramref name="mediaType"/> is an invalid media type.</exception>
    [JsonConstructor]
    public DataContent([StringSyntax(StringSyntaxAttribute.Uri)] string uri, string? mediaType = null)
    {
        // Store and validate the data URI.
        _uri = Throw.IfNullOrWhitespace(uri);
        if (!uri.StartsWith(DataUriParser.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            Throw.ArgumentException(nameof(uri), "The provided URI is not a data URI.");
        }

        // Parse the data URI to extract the data and media type.
        _dataUri = DataUriParser.Parse(uri.AsMemory());

        // Validate and store the media type.
        mediaType ??= _dataUri.MediaType;
        if (mediaType is null)
        {
            Throw.ArgumentNullException(nameof(mediaType), $"{nameof(uri)} did not contain a media type, and {nameof(mediaType)} was not provided.");
        }

        MediaType = DataUriParser.ThrowIfInvalidMediaType(mediaType);

        if (!_dataUri.IsBase64 || mediaType != _dataUri.MediaType)
        {
            // In rare cases, the data URI may contain non-base64 data, in which case we
            // want to normalize it to base64. The supplied media type may also be different
            // from the one in the data URI. In either case, we extract the bytes from the data URI
            // and then throw away the uri; we'll recreate it lazily in the canonical form.
            _data = _dataUri.ToByteArray();
            _dataUri = null;
            _uri = null;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataContent"/> class.
    /// </summary>
    /// <param name="data">The byte contents.</param>
    /// <param name="mediaType">The media type (also known as MIME type) represented by the content.</param>
    /// <exception cref="ArgumentNullException"><paramref name="mediaType"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="mediaType"/> is empty or composed entirely of whitespace.</exception>
    public DataContent(ReadOnlyMemory<byte> data, string mediaType)
    {
        MediaType = DataUriParser.ThrowIfInvalidMediaType(mediaType);

        _data = data;
    }

    /// <summary>
    /// Determines whether the <see cref="MediaType"/>'s top-level type matches the specified <paramref name="topLevelType"/>.
    /// </summary>
    /// <param name="topLevelType">The type to compare against <see cref="MediaType"/>.</param>
    /// <returns><see langword="true"/> if the type portion of <see cref="MediaType"/> matches the specified value; otherwise, false.</returns>
    /// <remarks>
    /// A media type is primarily composed of two parts, a "type" and a "subtype", separated by a slash ("/").
    /// The type portion is also referred to as the "top-level type"; for example,
    /// "image/png" has a top-level type of "image". <see cref="HasTopLevelMediaType"/> compares
    /// the specified <paramref name="topLevelType"/> against the type portion of <see cref="MediaType"/>.
    /// </remarks>
    public bool HasTopLevelMediaType(string topLevelType) => DataUriParser.HasTopLevelMediaType(MediaType, topLevelType);

    /// <summary>Gets the data URI for this <see cref="DataContent"/>.</summary>
    /// <remarks>
    /// The returned URI is always a valid data URI string, even if the instance was constructed from a <see cref="ReadOnlyMemory{Byte}"/>
    /// or from a <see cref="System.Uri"/>.
    /// </remarks>
    [StringSyntax(StringSyntaxAttribute.Uri)]
    public string Uri
    {
        get
        {
            if (_uri is null)
            {
                Debug.Assert(_data is not null, "Expected _data to be initialized.");
                ReadOnlyMemory<byte> data = _data.GetValueOrDefault();

#if NET
                char[] array = ArrayPool<char>.Shared.Rent(
                    "data:".Length + MediaType.Length + ";base64,".Length + Base64.GetMaxEncodedToUtf8Length(data.Length));

                bool wrote = array.AsSpan().TryWrite($"data:{MediaType};base64,", out int prefixLength);
                wrote |= Convert.TryToBase64Chars(data.Span, array.AsSpan(prefixLength), out int dataLength);
                Debug.Assert(wrote, "Expected to successfully write the data URI.");
                _uri = array.AsSpan(0, prefixLength + dataLength).ToString();

                ArrayPool<char>.Shared.Return(array);
#else
                string base64 = MemoryMarshal.TryGetArray(data, out ArraySegment<byte> segment) ?
                    Convert.ToBase64String(segment.Array!, segment.Offset, segment.Count) :
                    Convert.ToBase64String(data.ToArray());

                _uri = $"data:{MediaType};base64,{base64}";
#endif
            }

            return _uri;
        }
    }

    /// <summary>Gets the media type (also known as MIME type) of the content.</summary>
    /// <remarks>
    /// If the media type was explicitly specified, this property returns that value.
    /// If the media type was not explicitly specified, but a data URI was supplied and that data URI contained a non-default
    /// media type, that media type is returned.
    /// </remarks>
    [JsonIgnore]
    public string MediaType { get; }

    /// <summary>Gets the data represented by this instance.</summary>
    /// <remarks>
    /// If the instance was constructed from a <see cref="ReadOnlyMemory{Byte}"/>, this property returns that data.
    /// If the instance was constructed from a data URI, this property the data contained within the data URI.
    /// If, however, the instance was constructed from another form of URI, one that simply references where the
    /// data can be found but doesn't actually contain the data, this property returns <see langword="null"/>;
    /// no attempt is made to retrieve the data from that URI.
    /// </remarks>
    [JsonIgnore]
    public ReadOnlyMemory<byte> Data
    {
        get
        {
            if (_data is null)
            {
                Debug.Assert(_dataUri is not null, "Expected dataUri to be initialized.");
                _data = _dataUri!.ToByteArray();
            }

            Debug.Assert(_data is not null, "Expected data to be initialized.");
            return _data.GetValueOrDefault();
        }
    }

    /// <summary>Gets the data represented by this instance as a Base64 character sequence.</summary>
    /// <returns>The base64 representation of the data.</returns>
    [JsonIgnore]
    public ReadOnlyMemory<char> Base64Data
    {
        get
        {
            string uri = Uri;
            int pos = uri.IndexOf(',');
            Debug.Assert(pos >= 0, "Expected comma to be present in the URI.");
            return uri.AsMemory(pos + 1);
        }
    }

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay
    {
        get
        {
            const int MaxLength = 80;

            string uri = Uri;
            return uri.Length <= MaxLength ?
                $"Data = {uri}" :
                $"Data = {uri.Substring(0, MaxLength)}...";
        }
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/Contents/DataUriParser.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
#if NET8_0_OR_GREATER
using System.Buffers.Text;
#endif
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Shared.Diagnostics;

#pragma warning disable CA1307 // Specify StringComparison for clarity

namespace Microsoft.Extensions.AI;

/// <summary>
/// Minimal data URI parser based on RFC 2397: https://datatracker.ietf.org/doc/html/rfc2397.
/// </summary>
internal static class DataUriParser
{
    public static string Scheme => "data:";

    public static DataUri Parse(ReadOnlyMemory<char> dataUri)
    {
        // Validate, then trim off the "data:" scheme.
        if (!dataUri.Span.StartsWith(Scheme.AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            throw new UriFormatException("Invalid data URI format: the data URI must start with 'data:'.");
        }

        dataUri = dataUri.Slice(Scheme.Length);

        // Find the comma separating the metadata from the data.
        int commaPos = dataUri.Span.IndexOf(',');
        if (commaPos < 0)
        {
            throw new UriFormatException("Invalid data URI format: the data URI must contain a comma separating the metadata and the data.");
        }

        ReadOnlyMemory<char> metadata = dataUri.Slice(0, commaPos);

        ReadOnlyMemory<char> data = dataUri.Slice(commaPos + 1);
        bool isBase64 = false;

        // Determine whether the data is Base64-encoded or percent-encoded (Uri-encoded).
        // If it's base64-encoded, validate it. If it's Uri-encoded, there's nothing to validate,
        // as WebUtility.UrlDecode will successfully decode any input with no sequence considered invalid.
        if (metadata.Span.EndsWith(";base64".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            metadata = metadata.Slice(0, metadata.Length - ";base64".Length);
            isBase64 = true;
            if (!IsValidBase64Data(data.Span))
            {
                throw new UriFormatException("Invalid data URI format: the data URI is base64-encoded, but the data is not a valid base64 string.");
            }
        }

        // Validate the media type, if present.
        ReadOnlySpan<char> span = metadata.Span.Trim();
        string? mediaType = null;
        if (!span.IsEmpty && !IsValidMediaType(span, ref mediaType))
        {
            throw new UriFormatException("Invalid data URI format: the media type is not a valid.");
        }

        return new DataUri(data, isBase64, mediaType);
    }

    public static string ThrowIfInvalidMediaType(
        string mediaType, [CallerArgumentExpression(nameof(mediaType))] string parameterName = "")
    {
        _ = Throw.IfNullOrWhitespace(mediaType, parameterName);

        if (!IsValidMediaType(mediaType))
        {
            Throw.ArgumentException(parameterName, $"An invalid media type was specified: '{mediaType}'");
        }

        return mediaType;
    }

    public static bool IsValidMediaType(string mediaType) =>
        IsValidMediaType(mediaType.AsSpan(), ref mediaType);

    /// <summary>Validates that a media type is valid, and if successful, ensures we have it as a string.</summary>
    public static bool IsValidMediaType(ReadOnlySpan<char> mediaTypeSpan, [NotNull] ref string? mediaType)
    {
        // For common media types, we can avoid both allocating a string for the span and avoid parsing overheads.
        string? knownType = mediaTypeSpan switch
        {
            "application/json" => "application/json",
            "application/octet-stream" => "application/octet-stream",
            "application/pdf" => "application/pdf",
            "application/xml" => "application/xml",
            "audio/mpeg" => "audio/mpeg",
            "audio/ogg" => "audio/ogg",
            "audio/wav" => "audio/wav",
            "image/apng" => "image/apng",
            "image/avif" => "image/avif",
            "image/bmp" => "image/bmp",
            "image/gif" => "image/gif",
            "image/jpeg" => "image/jpeg",
            "image/png" => "image/png",
            "image/svg+xml" => "image/svg+xml",
            "image/tiff" => "image/tiff",
            "image/webp" => "image/webp",
            "text/css" => "text/css",
            "text/csv" => "text/csv",
            "text/html" => "text/html",
            "text/javascript" => "text/javascript",
            "text/plain" => "text/plain",
            "text/plain;charset=UTF-8" => "text/plain;charset=UTF-8",
            "text/xml" => "text/xml",
            _ => null,
        };
        if (knownType is not null)
        {
            mediaType = knownType;
            return true;
        }

        // Otherwise, do the full validation using the same logic as HttpClient.
        mediaType ??= mediaTypeSpan.ToString();
        return MediaTypeHeaderValue.TryParse(mediaType, out _);
    }

    public static bool HasTopLevelMediaType(string mediaType, string topLevelMediaType)
    {
        int slashIndex = mediaType.IndexOf('/');

        ReadOnlySpan<char> span = slashIndex < 0 ? mediaType.AsSpan() : mediaType.AsSpan(0, slashIndex);
        span = span.Trim();

        return span.Equals(topLevelMediaType.AsSpan(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Test whether the value is a base64 string without whitespace.</summary>
    private static bool IsValidBase64Data(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            return true;
        }

#if NET8_0_OR_GREATER
        return Base64.IsValid(value) && !value.ContainsAny(" \t\r\n");
#else
#pragma warning disable S109 // Magic numbers should not be used
        if (value!.Length % 4 != 0)
#pragma warning restore S109
        {
            return false;
        }

        var index = value.Length - 1;

        // Step back over one or two padding chars
        if (value[index] == '=')
        {
            index--;
        }

        if (value[index] == '=')
        {
            index--;
        }

        // Now traverse over characters
        for (var i = 0; i <= index; i++)
        {
#pragma warning disable S1067 // Expressions should not be too complex
            bool validChar = value[i] is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '+' or '/';
#pragma warning restore S1067
            if (!validChar)
            {
                return false;
            }
        }

        return true;
#endif
    }

    /// <summary>Provides the parts of a parsed data URI.</summary>
    public sealed class DataUri(ReadOnlyMemory<char> data, bool isBase64, string? mediaType)
    {
#pragma warning disable S3604 // False positive: Member initializer values should not be redundant
        public string? MediaType { get; } = mediaType;

        public ReadOnlyMemory<char> Data { get; } = data;

        public bool IsBase64 { get; } = isBase64;
#pragma warning restore S3604

        public byte[] ToByteArray() => IsBase64 ?
            Convert.FromBase64String(Data.ToString()) :
            Encoding.UTF8.GetBytes(WebUtility.UrlDecode(Data.ToString()));
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/Contents/ErrorContent.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>Represents an error.</summary>
/// <remarks>
/// Typically, <see cref="ErrorContent"/> is used for non-fatal errors, where something went wrong
/// as part of the operation but the operation was still able to continue.
/// </remarks>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class ErrorContent : AIContent
{
    /// <summary>The error message.</summary>
    private string? _message;

    /// <summary>Initializes a new instance of the <see cref="ErrorContent"/> class with the specified error message.</summary>
    /// <param name="message">The error message to store in this content.</param>
    public ErrorContent(string? message)
    {
        _message = message;
    }

    /// <summary>Gets or sets the error message.</summary>
    [AllowNull]
    public string Message
    {
        get => _message ?? string.Empty;
        set => _message = value;
    }

    /// <summary>Gets or sets an error code associated with the error.</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Gets or sets additional details about the error.</summary>
    public string? Details { get; set; }

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay =>
        $"Error = \"{Message}\"" +
        (!string.IsNullOrWhiteSpace(ErrorCode) ? $" ({ErrorCode})" : string.Empty) +
        (!string.IsNullOrWhiteSpace(Details) ? $" - \"{Details}\"" : string.Empty);
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/Contents/FunctionCallContent.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a function call request.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class FunctionCallContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionCallContent"/> class.
    /// </summary>
    /// <param name="callId">The function call ID.</param>
    /// <param name="name">The function name.</param>
    /// <param name="arguments">The function original arguments.</param>
    [JsonConstructor]
    public FunctionCallContent(string callId, string name, IDictionary<string, object?>? arguments = null)
    {
        CallId = Throw.IfNull(callId);
        Name = Throw.IfNull(name);
        Arguments = arguments;
    }

    /// <summary>
    /// Gets the function call ID.
    /// </summary>
    public string CallId { get; }

    /// <summary>
    /// Gets the name of the function requested.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the arguments requested to be provided to the function.
    /// </summary>
    public IDictionary<string, object?>? Arguments { get; set; }

    /// <summary>
    /// Gets or sets any exception that occurred while mapping the original function call data to this class.
    /// </summary>
    /// <remarks>
    /// This property is for information purposes only. The <see cref="Exception"/> is not serialized as part of serializing
    /// instances of this class with <see cref="JsonSerializer"/>; as such, upon deserialization, this property will be <see langword="null"/>.
    /// Consumers should not rely on <see langword="null"/> indicating success. 
    /// </remarks>
    [JsonIgnore]
    public Exception? Exception { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="FunctionCallContent"/> parsing arguments using a specified encoding and parser.
    /// </summary>
    /// <typeparam name="TEncoding">The encoding format from which to parse function call arguments.</typeparam>
    /// <param name="encodedArguments">The input arguments encoded in <typeparamref name="TEncoding"/>.</param>
    /// <param name="callId">The function call ID.</param>
    /// <param name="name">The function name.</param>
    /// <param name="argumentParser">The parsing implementation converting the encoding to a dictionary of arguments.</param>
    /// <returns>A new instance of <see cref="FunctionCallContent"/> containing the parse result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="callId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="encodedArguments"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="argumentParser"/> is <see langword="null"/>.</exception>
    public static FunctionCallContent CreateFromParsedArguments<TEncoding>(
        TEncoding encodedArguments,
        string callId,
        string name,
        Func<TEncoding, IDictionary<string, object?>?> argumentParser)
    {
        _ = Throw.IfNull(encodedArguments);
        _ = Throw.IfNull(callId);
        _ = Throw.IfNull(name);
        _ = Throw.IfNull(argumentParser);

        IDictionary<string, object?>? arguments = null;
        Exception? parsingException = null;

#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            arguments = argumentParser(encodedArguments);
        }
        catch (Exception ex)
        {
            parsingException = new InvalidOperationException("Error parsing function call arguments.", ex);
        }
#pragma warning restore CA1031 // Do not catch general exception types

        return new FunctionCallContent(callId, name, arguments)
        {
            Exception = parsingException
        };
    }

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay
    {
        get
        {
            string display = "FunctionCall = ";

            if (CallId is not null)
            {
                display += $"{CallId}, ";
            }

            display += Arguments is not null ?
                $"{Name}({string.Join(", ", Arguments)})" :
                $"{Name}()";

            return display;
        }
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/Contents/FunctionResultContent.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents the result of a function call.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class FunctionResultContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionResultContent"/> class.
    /// </summary>
    /// <param name="callId">The function call ID for which this is the result.</param>
    /// <param name="result">
    /// <see langword="null"/> if the function returned <see langword="null"/> or was void-returning
    /// and thus had no result, or if the function call failed. Typically, however, to provide meaningfully representative
    /// information to an AI service, a human-readable representation of those conditions should be supplied.
    /// </param>
    [JsonConstructor]
    public FunctionResultContent(string callId, object? result)
    {
        CallId = Throw.IfNull(callId);
        Result = result;
    }

    /// <summary>
    /// Gets the ID of the function call for which this is the result.
    /// </summary>
    /// <remarks>
    /// If this is the result for a <see cref="FunctionCallContent"/>, this property should contain the same
    /// <see cref="FunctionCallContent.CallId"/> value.
    /// </remarks>
    public string CallId { get; }

    /// <summary>
    /// Gets or sets the result of the function call, or a generic error message if the function call failed.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> if the function returned <see langword="null"/> or was void-returning
    /// and thus had no result, or if the function call failed. Typically, however, to provide meaningfully representative
    /// information to an AI service, a human-readable representation of those conditions should be supplied.
    /// </remarks>
    public object? Result { get; set; }

    /// <summary>
    /// Gets or sets an exception that occurred if the function call failed.
    /// </summary>
    /// <remarks>
    /// This property is for informational purposes only. The <see cref="Exception"/> is not serialized as part of serializing
    /// instances of this class with <see cref="JsonSerializer"/>. As such, upon deserialization, this property will be <see langword="null"/>.
    /// Consumers should not rely on <see langword="null"/> indicating success.
    /// </remarks>
    [JsonIgnore]
    public Exception? Exception { get; set; }

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay
    {
        get
        {
            string display = "FunctionResult = ";

            if (CallId is not null)
            {
                display += $"{CallId}, ";
            }

            display += Exception is not null ?
                $"{Exception.GetType().Name}(\"{Exception.Message}\")" :
                $"{Result?.ToString() ?? "(null)"}";

            return display;
        }
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/Contents/TextContent.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents text content in a chat.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class TextContent : AIContent
{
    private string? _text;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextContent"/> class.
    /// </summary>
    /// <param name="text">The text content.</param>
    public TextContent(string? text)
    {
        _text = text;
    }

    /// <summary>
    /// Gets or sets the text content.
    /// </summary>
    [AllowNull]
    public string Text
    {
        get => _text ?? string.Empty;
        set => _text = value;
    }

    /// <inheritdoc/>
    public override string ToString() => Text;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"Text = \"{Text}\"";
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/Contents/TextReasoningContent.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents text reasoning content in a chat.
/// </summary>
/// <remarks>
/// <see cref="TextReasoningContent"/> is distinct from <see cref="TextContent"/>. <see cref="TextReasoningContent"/>
/// represents "thinking" or "reasoning" performed by the model and is distinct from the actual output text from
/// the model, which is represented by <see cref="TextContent"/>. Neither types derives from the other.
/// </remarks>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class TextReasoningContent : AIContent
{
    private string? _text;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextReasoningContent"/> class.
    /// </summary>
    /// <param name="text">The text reasoning content.</param>
    public TextReasoningContent(string? text)
    {
        _text = text;
    }

    /// <summary>
    /// Gets or sets the text reasoning content.
    /// </summary>
    [AllowNull]
    public string Text
    {
        get => _text ?? string.Empty;
        set => _text = value;
    }

    /// <inheritdoc/>
    public override string ToString() => Text;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"Reasoning = \"{Text}\"";
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/Contents/UriContent.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a URL, typically to hosted content such as an image, audio, or video.
/// </summary>
/// <remarks>
/// This class is intended for use with HTTP or HTTPS URIs that reference hosted content.
/// For data URIs, use <see cref="DataContent"/> instead.
/// </remarks>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class UriContent : AIContent
{
    /// <summary>The URI represented.</summary>
    private Uri _uri;

    /// <summary>The MIME type of the data at the referenced URI.</summary>
    private string _mediaType;

    /// <summary>Initializes a new instance of the <see cref="UriContent"/> class.</summary>
    /// <param name="uri">The URI to the represented content.</param>
    /// <param name="mediaType">The media type (also known as MIME type) represented by the content.</param>
    /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="mediaType"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="mediaType"/> is an invalid media type.</exception>
    /// <exception cref="UriFormat"><paramref name="uri"/> is an invalid URL.</exception>
    /// <remarks>
    /// A media type must be specified, so that consumers know what to do with the content.
    /// If an exact media type is not known, but the category (e.g. image) is known, a wildcard
    /// may be used (e.g. "image/*").
    /// </remarks>
    public UriContent(string uri, string mediaType)
        : this(new Uri(Throw.IfNull(uri)), mediaType)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="UriContent"/> class.</summary>
    /// <param name="uri">The URI to the represented content.</param>
    /// <param name="mediaType">The media type (also known as MIME type) represented by the content.</param>
    /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="mediaType"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="mediaType"/> is an invalid media type.</exception>
    /// <remarks>
    /// A media type must be specified, so that consumers know what to do with the content.
    /// If an exact media type is not known, but the category (e.g. image) is known, a wildcard
    /// may be used (e.g. "image/*").
    /// </remarks>
    [JsonConstructor]
    public UriContent(Uri uri, string mediaType)
    {
        _uri = Throw.IfNull(uri);
        _mediaType = DataUriParser.ThrowIfInvalidMediaType(mediaType);
    }

    /// <summary>Gets or sets the <see cref="Uri"/> for this content.</summary>
    public Uri Uri
    {
        get => _uri;
        set => _uri = Throw.IfNull(value);
    }

    /// <summary>Gets or sets the media type (also known as MIME type) for this content.</summary>
    public string MediaType
    {
        get => _mediaType;
        set => _mediaType = DataUriParser.ThrowIfInvalidMediaType(value);
    }

    /// <summary>
    /// Determines whether the <see cref="MediaType"/>'s top-level type matches the specified <paramref name="topLevelType"/>.
    /// </summary>
    /// <param name="topLevelType">The type to compare against <see cref="MediaType"/>.</param>
    /// <returns><see langword="true"/> if the type portion of <see cref="MediaType"/> matches the specified value; otherwise, false.</returns>
    /// <remarks>
    /// A media type is primarily composed of two parts, a "type" and a "subtype", separated by a slash ("/").
    /// The type portion is also referred to as the "top-level type"; for example,
    /// "image/png" has a top-level type of "image". <see cref="HasTopLevelMediaType"/> compares
    /// the specified <paramref name="topLevelType"/> against the type portion of <see cref="MediaType"/>.
    /// </remarks>
    public bool HasTopLevelMediaType(string topLevelType) => DataUriParser.HasTopLevelMediaType(MediaType, topLevelType);

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"Uri = {_uri}";
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/Contents/UsageContent.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents usage information associated with a chat request and response.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class UsageContent : AIContent
{
    /// <summary>Usage information.</summary>
    private UsageDetails _details;

    /// <summary>Initializes a new instance of the <see cref="UsageContent"/> class with an empty <see cref="UsageDetails"/>.</summary>
    public UsageContent()
    {
        _details = new();
    }

    /// <summary>Initializes a new instance of the <see cref="UsageContent"/> class with the specified <see cref="UsageDetails"/> instance.</summary>
    /// <param name="details">The usage details to store in this content.</param>
    [JsonConstructor]
    public UsageContent(UsageDetails details)
    {
        _details = Throw.IfNull(details);
    }

    /// <summary>Gets or sets the usage information.</summary>
    public UsageDetails Details
    {
        get => _details;
        set => _details = Throw.IfNull(value);
    }

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"Usage = {_details.DebuggerDisplay}";
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/Embeddings/BinaryEmbedding.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents an embedding composed of a bit vector.</summary>
public sealed class BinaryEmbedding : Embedding
{
    /// <summary>The embedding vector this embedding represents.</summary>
    private BitArray _vector;

    /// <summary>Initializes a new instance of the <see cref="BinaryEmbedding"/> class with the embedding vector.</summary>
    /// <param name="vector">The embedding vector this embedding represents.</param>
    /// <exception cref="ArgumentNullException"><paramref name="vector"/> is <see langword="null"/>.</exception>
    public BinaryEmbedding(BitArray vector)
    {
        _vector = Throw.IfNull(vector);
    }

    /// <summary>Gets or sets the embedding vector this embedding represents.</summary>
    [JsonConverter(typeof(VectorConverter))]
    public BitArray Vector
    {
        get => _vector;
        set => _vector = Throw.IfNull(value);
    }

    /// <inheritdoc />
    [JsonIgnore]
    public override int Dimensions => _vector.Length;

    /// <summary>Provides a <see cref="JsonConverter{BitArray}"/> for serializing <see cref="BitArray"/> instances.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class VectorConverter : JsonConverter<BitArray>
    {
        /// <inheritdoc/>
        public override BitArray Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            _ = Throw.IfNull(typeToConvert);
            _ = Throw.IfNull(options);

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Expected string property.");
            }

            ReadOnlySpan<byte> utf8;
            byte[]? tmpArray = null;
            if (!reader.HasValueSequence && !reader.ValueIsEscaped)
            {
                utf8 = reader.ValueSpan;
            }
            else
            {
                // This path should be rare.
                int length = reader.HasValueSequence ? checked((int)reader.ValueSequence.Length) : reader.ValueSpan.Length;
                tmpArray = ArrayPool<byte>.Shared.Rent(length);
                utf8 = tmpArray.AsSpan(0, reader.CopyString(tmpArray));
            }

            BitArray result = new(utf8.Length);

            for (int i = 0; i < utf8.Length; i++)
            {
                result[i] = utf8[i] switch
                {
                    (byte)'0' => false,
                    (byte)'1' => true,
                    _ => throw new JsonException("Expected binary character sequence.")
                };
            }

            if (tmpArray is not null)
            {
                ArrayPool<byte>.Shared.Return(tmpArray);
            }

            return result;
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, BitArray value, JsonSerializerOptions options)
        {
            _ = Throw.IfNull(writer);
            _ = Throw.IfNull(value);
            _ = Throw.IfNull(options);

            int length = value.Length;

            byte[] tmpArray = ArrayPool<byte>.Shared.Rent(length);

            Span<byte> utf8 = tmpArray.AsSpan(0, length);
            for (int i = 0; i < utf8.Length; i++)
            {
                utf8[i] = value[i] ? (byte)'1' : (byte)'0';
            }

            writer.WriteStringValue(utf8);

            ArrayPool<byte>.Shared.Return(tmpArray);
        }
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/Embeddings/DelegatingEmbeddingGenerator.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides an optional base class for an <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> that passes through calls to another instance.
/// </summary>
/// <typeparam name="TInput">The type of the input passed to the generator.</typeparam>
/// <typeparam name="TEmbedding">The type of the embedding instance produced by the generator.</typeparam>
/// <remarks>
/// This type is recommended as a base type when building generators that can be chained around an underlying <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.
/// The default implementation simply passes each call to the inner generator instance.
/// </remarks>
public class DelegatingEmbeddingGenerator<TInput, TEmbedding> : IEmbeddingGenerator<TInput, TEmbedding>
    where TEmbedding : Embedding
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DelegatingEmbeddingGenerator{TInput, TEmbedding}"/> class.
    /// </summary>
    /// <param name="innerGenerator">The wrapped generator instance.</param>
    protected DelegatingEmbeddingGenerator(IEmbeddingGenerator<TInput, TEmbedding> innerGenerator)
    {
        InnerGenerator = Throw.IfNull(innerGenerator);
    }

    /// <summary>Gets the inner <see cref="IEmbeddingGenerator{TInput, TEmbedding}" />.</summary>
    protected IEmbeddingGenerator<TInput, TEmbedding> InnerGenerator { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public virtual Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(IEnumerable<TInput> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default) =>
        InnerGenerator.GenerateAsync(values, options, cancellationToken);

    /// <inheritdoc />
    public virtual object? GetService(Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(serviceType);

        // If the key is non-null, we don't know what it means so pass through to the inner service.
        return
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this :
            InnerGenerator.GetService(serviceType, serviceKey);
    }

    /// <summary>Provides a mechanism for releasing unmanaged resources.</summary>
    /// <param name="disposing"><see langword="true"/> if being called from <see cref="Dispose()"/>; otherwise, <see langword="false"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            InnerGenerator.Dispose();
        }
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/Embeddings/Embedding.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>Represents an embedding generated by a <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.</summary>
/// <remarks>This base class provides metadata about the embedding. Derived types provide the concrete data contained in the embedding.</remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(BinaryEmbedding), typeDiscriminator: "binary")]
[JsonDerivedType(typeof(Embedding<byte>), typeDiscriminator: "uint8")]
[JsonDerivedType(typeof(Embedding<sbyte>), typeDiscriminator: "int8")]
#if NET
[JsonDerivedType(typeof(Embedding<Half>), typeDiscriminator: "float16")]
#endif
[JsonDerivedType(typeof(Embedding<float>), typeDiscriminator: "float32")]
[JsonDerivedType(typeof(Embedding<double>), typeDiscriminator: "float64")]
[DebuggerDisplay("Dimensions = {Dimensions}")]
public class Embedding
{
    /// <summary>Initializes a new instance of the <see cref="Embedding"/> class.</summary>
    protected Embedding()
    {
    }

    /// <summary>Gets or sets a timestamp at which the embedding was created.</summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>Gets the dimensionality of the embedding vector.</summary>
    /// <remarks>
    /// This value corresponds to the number of elements in the embedding vector.
    /// </remarks>
    [JsonIgnore]
    public virtual int Dimensions { get; }

    /// <summary>Gets or sets the model ID using in the creation of the embedding.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets any additional properties associated with the embedding.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/Embeddings/EmbeddingGenerationOptions.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the options for an embedding generation request.</summary>
public class EmbeddingGenerationOptions
{
    private int? _dimensions;

    /// <summary>Gets or sets the number of dimensions requested in the embedding.</summary>
    public int? Dimensions
    {
        get => _dimensions;
        set
        {
            if (value is not null)
            {
                _ = Throw.IfLessThan(value.Value, 1, nameof(value));
            }

            _dimensions = value;
        }
    }

    /// <summary>Gets or sets the model ID for the embedding generation request.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets additional properties for the embedding generation request.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>
    /// Gets or sets a callback responsible for creating the raw representation of the embedding generation options from an underlying implementation.
    /// </summary>
    /// <remarks>
    /// The underlying <see cref="IEmbeddingGenerator" /> implementation may have its own representation of options.
    /// When <see cref="IEmbeddingGenerator{TInput, TEmbedding}.GenerateAsync" /> 
    /// is invoked with an <see cref="EmbeddingGenerationOptions" />, that implementation may convert the provided options into
    /// its own representation in order to use it while performing the operation. For situations where a consumer knows
    /// which concrete <see cref="IEmbeddingGenerator" /> is being used and how it represents options, a new instance of that
    /// implementation-specific options type may be returned by this callback, for the <see cref="IEmbeddingGenerator" />
    /// implementation to use instead of creating a new instance. Such implementations may mutate the supplied options
    /// instance further based on other settings supplied on this <see cref="EmbeddingGenerationOptions" /> instance or from other inputs,
    /// therefore, it is <b>strongly recommended</b> to not return shared instances and instead make the callback return a new instance on each call.
    /// This is typically used to set an implementation-specific setting that isn't otherwise exposed from the strongly-typed
    /// properties on <see cref="EmbeddingGenerationOptions" />.
    /// </remarks>
    [JsonIgnore]
    public Func<IEmbeddingGenerator, object?>? RawRepresentationFactory { get; set; }

    /// <summary>Produces a clone of the current <see cref="EmbeddingGenerationOptions"/> instance.</summary>
    /// <returns>A clone of the current <see cref="EmbeddingGenerationOptions"/> instance.</returns>
    /// <remarks>
    /// The clone will have the same values for all properties as the original instance. Any collections, like <see cref="AdditionalProperties"/>
    /// are shallow-cloned, meaning a new collection instance is created, but any references contained by the collections are shared with the original.
    /// </remarks>
    public virtual EmbeddingGenerationOptions Clone() =>
        new()
        {
            ModelId = ModelId,
            Dimensions = Dimensions,
            AdditionalProperties = AdditionalProperties?.Clone(),
        };
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/Embeddings/EmbeddingGeneratorExtensions.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S2302 // "nameof" should be used
#pragma warning disable S4136 // Method overloads should be grouped together

namespace Microsoft.Extensions.AI;

/// <summary>Provides a collection of static methods for extending <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> instances.</summary>
public static class EmbeddingGeneratorExtensions
{
    /// <summary>Asks the <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> for an object of type <typeparamref name="TService"/>.</summary>
    /// <typeparam name="TService">The type of the object to be retrieved.</typeparam>
    /// <param name="generator">The generator.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="generator"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that may be provided by the
    /// <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/>, including itself or any services it might be wrapping.
    /// </remarks>
    public static TService? GetService<TService>(
        this IEmbeddingGenerator generator, object? serviceKey = null)
    {
        _ = Throw.IfNull(generator);

        return generator.GetService(typeof(TService), serviceKey) is TService service ? service : default;
    }

    /// <summary>
    /// Asks the <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> for an object of the specified type <paramref name="serviceType"/>
    /// and throws an exception if one isn't available.
    /// </summary>
    /// <param name="generator">The generator.</param>
    /// <param name="serviceType">The type of object being requested.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="generator"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No service of the requested type for the specified key is available.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of services that are required to be provided by the
    /// <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/>, including itself or any services it might be wrapping.
    /// </remarks>
    public static object GetRequiredService(
        this IEmbeddingGenerator generator, Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(generator);
        _ = Throw.IfNull(serviceType);

        return
            generator.GetService(serviceType, serviceKey) ??
            throw Throw.CreateMissingServiceException(serviceType, serviceKey);
    }

    /// <summary>
    /// Asks the <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> for an object of type <typeparamref name="TService"/>
    /// and throws an exception if one isn't available.
    /// </summary>
    /// <typeparam name="TService">The type of the object to be retrieved.</typeparam>
    /// <param name="generator">The generator.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="generator"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No service of the requested type for the specified key is available.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that are required to be provided by the
    /// <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/>, including itself or any services it might be wrapping.
    /// </remarks>
    public static TService GetRequiredService<TService>(
        this IEmbeddingGenerator generator, object? serviceKey = null)
    {
        _ = Throw.IfNull(generator);

        if (generator.GetService(typeof(TService), serviceKey) is not TService service)
        {
            throw Throw.CreateMissingServiceException(typeof(TService), serviceKey);
        }

        return service;
    }

    /// <summary>Generates an embedding vector from the specified <paramref name="value"/>.</summary>
    /// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
    /// <typeparam name="TEmbeddingElement">The numeric type of the embedding data.</typeparam>
    /// <param name="generator">The embedding generator.</param>
    /// <param name="value">A value from which an embedding will be generated.</param>
    /// <param name="options">The embedding generation options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The generated embedding for the specified <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="generator"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The generator did not produce exactly one embedding.</exception>
    /// <remarks>
    /// This operation is equivalent to using <see cref="GenerateAsync"/> and returning the
    /// resulting <see cref="Embedding{T}"/>'s <see cref="Embedding{T}.Vector"/> property.
    /// </remarks>
    public static async Task<ReadOnlyMemory<TEmbeddingElement>> GenerateVectorAsync<TInput, TEmbeddingElement>(
        this IEmbeddingGenerator<TInput, Embedding<TEmbeddingElement>> generator,
        TInput value,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var embedding = await GenerateAsync(generator, value, options, cancellationToken).ConfigureAwait(false);
        return embedding.Vector;
    }

    /// <summary>Generates an embedding from the specified <paramref name="value"/>.</summary>
    /// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
    /// <typeparam name="TEmbedding">The type of embedding to generate.</typeparam>
    /// <param name="generator">The embedding generator.</param>
    /// <param name="value">A value from which an embedding will be generated.</param>
    /// <param name="options">The embedding generation options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>
    /// The generated embedding for the specified <paramref name="value"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="generator"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The generator did not produce exactly one embedding.</exception>
    /// <remarks>
    /// This operations is equivalent to using <see cref="IEmbeddingGenerator{TInput, TEmbedding}.GenerateAsync"/> with a
    /// collection composed of the single <paramref name="value"/> and then returning the first embedding element from the
    /// resulting <see cref="GeneratedEmbeddings{TEmbedding}"/> collection.
    /// </remarks>
    public static async Task<TEmbedding> GenerateAsync<TInput, TEmbedding>(
        this IEmbeddingGenerator<TInput, TEmbedding> generator,
        TInput value,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
        where TEmbedding : Embedding
    {
        _ = Throw.IfNull(generator);
        _ = Throw.IfNull(value);

        var embeddings = await generator.GenerateAsync([value], options, cancellationToken).ConfigureAwait(false);

        if (embeddings is null)
        {
            Throw.InvalidOperationException("Embedding generator returned a null collection of embeddings.");
        }

        if (embeddings.Count != 1)
        {
            Throw.InvalidOperationException($"Expected the number of embeddings ({embeddings.Count}) to match the number of inputs (1).");
        }

        TEmbedding embedding = embeddings[0];
        if (embedding is null)
        {
            Throw.InvalidOperationException("Embedding generator generated a null embedding.");
        }

        return embedding;
    }

    /// <summary>
    /// Generates embeddings for each of the supplied <paramref name="values"/> and produces a list that pairs
    /// each input value with its resulting embedding.
    /// </summary>
    /// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
    /// <typeparam name="TEmbedding">The type of embedding to generate.</typeparam>
    /// <param name="generator">The embedding generator.</param>
    /// <param name="values">The collection of values for which to generate embeddings.</param>
    /// <param name="options">The embedding generation options to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>An array containing tuples of the input values and the associated generated embeddings.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="generator"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The generator did not produce one embedding for each input value.</exception>
    public static async Task<(TInput Value, TEmbedding Embedding)[]> GenerateAndZipAsync<TInput, TEmbedding>(
        this IEmbeddingGenerator<TInput, TEmbedding> generator,
        IEnumerable<TInput> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
        where TEmbedding : Embedding
    {
        _ = Throw.IfNull(generator);
        _ = Throw.IfNull(values);

        IList<TInput> inputs = values as IList<TInput> ?? values.ToList();
        int inputsCount = inputs.Count;

        if (inputsCount == 0)
        {
            return Array.Empty<(TInput, TEmbedding)>();
        }

        var embeddings = await generator.GenerateAsync(values, options, cancellationToken).ConfigureAwait(false);
        if (embeddings.Count != inputsCount)
        {
            Throw.InvalidOperationException($"Expected the number of embeddings ({embeddings.Count}) to match the number of inputs ({inputsCount}).");
        }

        var results = new (TInput, TEmbedding)[embeddings.Count];
        for (int i = 0; i < results.Length; i++)
        {
            results[i] = (inputs[i], embeddings[i]);
        }

        return results;
    }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/Embeddings/EmbeddingGeneratorMetadata.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI;

/// <summary>Provides metadata about an <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.</summary>
public class EmbeddingGeneratorMetadata
{
    /// <summary>Initializes a new instance of the <see cref="EmbeddingGeneratorMetadata"/> class.</summary>
    /// <param name="providerName">
    /// The name of the embedding generation provider, if applicable. Where possible, this should map to the
    /// appropriate name defined in the OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </param>
    /// <param name="providerUri">The URL for accessing the embedding generation provider, if applicable.</param>
    /// <param name="defaultModelId">The ID of the default embedding generation model used, if applicable.</param>
    /// <param name="defaultModelDimensions">The number of dimensions in vectors produced by the default model, if applicable.</param>
    public EmbeddingGeneratorMetadata(string? providerName = null, Uri? providerUri = null, string? defaultModelId = null, int? defaultModelDimensions = null)
    {
        DefaultModelId = defaultModelId;
        ProviderName = providerName;
        ProviderUri = providerUri;
        DefaultModelDimensions = defaultModelDimensions;
    }

    /// <summary>Gets the name of the embedding generation provider.</summary>
    /// <remarks>
    /// Where possible, this maps to the appropriate name defined in the
    /// OpenTelemetry Semantic Conventions for Generative AI systems.
    /// </remarks>
    public string? ProviderName { get; }

    /// <summary>Gets the URL for accessing the embedding generation provider.</summary>
    public Uri? ProviderUri { get; }

    /// <summary>Gets the ID of the default model used by this embedding generator.</summary>
    /// <remarks>
    /// This value can be <see langword="null"/> if no default model is set on the corresponding embedding generator.
    /// An individual request may override this value via <see cref="EmbeddingGenerationOptions.ModelId"/>.
    /// </remarks>
    public string? DefaultModelId { get; }

    /// <summary>Gets the number of dimensions in the embeddings produced by the default model.</summary>
    /// <remarks>
    /// This value can be <see langword="null"/> if either the number of dimensions is unknown or there are multiple possible lengths associated with this model.
    /// An individual request may override this value via <see cref="EmbeddingGenerationOptions.Dimensions"/>.
    /// </remarks>
    public int? DefaultModelDimensions { get; }
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/Embeddings/Embedding{T}.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>Represents an embedding composed of a vector of <typeparamref name="T"/> values.</summary>
/// <typeparam name="T">The type of the values in the embedding vector.</typeparam>
/// <remarks>Typical values of <typeparamref name="T"/> are <see cref="float"/>, <see cref="double"/>, or Half.</remarks>
public sealed class Embedding<T> : Embedding
{
    /// <summary>Initializes a new instance of the <see cref="Embedding{T}"/> class with the embedding vector.</summary>
    /// <param name="vector">The embedding vector this embedding represents.</param>
    public Embedding(ReadOnlyMemory<T> vector)
    {
        Vector = vector;
    }

    /// <summary>Gets or sets the embedding vector this embedding represents.</summary>
    public ReadOnlyMemory<T> Vector { get; set; }

    /// <inheritdoc />
    [JsonIgnore]
    public override int Dimensions => Vector.Length;
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/Embeddings/GeneratedEmbeddings.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the result of an operation to generate embeddings.</summary>
/// <typeparam name="TEmbedding">Specifies the type of the generated embeddings.</typeparam>
[DebuggerDisplay("Count = {Count}")]
public sealed class GeneratedEmbeddings<TEmbedding> : IList<TEmbedding>, IReadOnlyList<TEmbedding>
    where TEmbedding : Embedding
{
    /// <summary>The underlying list of embeddings.</summary>
    private List<TEmbedding> _embeddings;

    /// <summary>Initializes a new instance of the <see cref="GeneratedEmbeddings{TEmbedding}"/> class.</summary>
    public GeneratedEmbeddings()
    {
        _embeddings = [];
    }

    /// <summary>Initializes a new instance of the <see cref="GeneratedEmbeddings{TEmbedding}"/> class with the specified capacity.</summary>
    /// <param name="capacity">The number of embeddings that the new list can initially store.</param>
    public GeneratedEmbeddings(int capacity)
    {
        _embeddings = new List<TEmbedding>(Throw.IfLessThan(capacity, 0));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneratedEmbeddings{TEmbedding}"/> class that contains all of the embeddings from the specified collection.
    /// </summary>
    /// <param name="embeddings">The collection whose embeddings are copied to the new list.</param>
    public GeneratedEmbeddings(IEnumerable<TEmbedding> embeddings)
    {
        _embeddings = new List<TEmbedding>(Throw.IfNull(embeddings));
    }

    /// <summary>Gets or sets usage details for the embeddings' generation.</summary>
    public UsageDetails? Usage { get; set; }

    /// <summary>Gets or sets any additional properties associated with the embeddings.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <inheritdoc />
    public TEmbedding this[int index]
    {
        get => _embeddings[index];
        set => _embeddings[index] = value;
    }

    /// <inheritdoc />
    public int Count => _embeddings.Count;

    /// <inheritdoc />
    bool ICollection<TEmbedding>.IsReadOnly => false;

    /// <inheritdoc />
    public void Add(TEmbedding item) => _embeddings.Add(item);

    /// <summary>Adds the embeddings from the specified collection to the end of this list.</summary>
    /// <param name="items">The collection whose elements should be added to this list.</param>
    public void AddRange(IEnumerable<TEmbedding> items) => _embeddings.AddRange(items);

    /// <inheritdoc />
    public void Clear() => _embeddings.Clear();

    /// <inheritdoc />
    public bool Contains(TEmbedding item) => _embeddings.Contains(item);

    /// <inheritdoc />
    public void CopyTo(TEmbedding[] array, int arrayIndex) => _embeddings.CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public IEnumerator<TEmbedding> GetEnumerator() => _embeddings.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public int IndexOf(TEmbedding item) => _embeddings.IndexOf(item);

    /// <inheritdoc />
    public void Insert(int index, TEmbedding item) => _embeddings.Insert(index, item);

    /// <inheritdoc />
    public bool Remove(TEmbedding item) => _embeddings.Remove(item);

    /// <inheritdoc />
    public void RemoveAt(int index) => _embeddings.RemoveAt(index);
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/Embeddings/IEmbeddingGenerator.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a generator of embeddings.</summary>
/// <remarks>
/// This base interface is used to allow for embedding generators to be stored in a non-generic manner.
/// To use the generator to create embeddings, instances typed as this base interface first need to be
/// cast to the generic interface <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.
/// </remarks>
public interface IEmbeddingGenerator : IDisposable
{
    /// <summary>Asks the <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> for an object of the specified type <paramref name="serviceType"/>.</summary>
    /// <param name="serviceType">The type of object being requested.</param>
    /// <param name="serviceKey">An optional key that can be used to help identify the target service.</param>
    /// <returns>The found object, otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The purpose of this method is to allow for the retrieval of strongly typed services that might be provided by the
    /// <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>, including itself or any services it might be wrapping.
    /// For example, to access the <see cref="EmbeddingGeneratorMetadata"/> for the instance, <see cref="GetService"/> may
    /// be used to request it.
    /// </remarks>
    object? GetService(Type serviceType, object? serviceKey = null);
}



================================================
FILE: src/Libraries/Microsoft.Extensions.AI.Abstractions/Embeddings/IEmbeddingGenerator{TInput,TEmbedding}.cs
================================================
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a generator of embeddings.</summary>
/// <typeparam name="TInput">The type from which embeddings will be generated.</typeparam>
/// <typeparam name="TEmbedding">The type of embeddings to generate.</typeparam>
/// <remarks>
/// <para>
/// Unless otherwise specified, all members of <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> are thread-safe for concurrent use.
/// It is expected that all implementations of <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> support being used by multiple requests concurrently.
/// Instances must not be disposed of while the instance is still in use.
/// </para>
/// <para>
/// However, implementations of <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> may mutate the arguments supplied to
/// <see cref="GenerateAsync"/>, such as by configuring the options instance. Thus, consumers of the interface either should
/// avoid using shared instances of these arguments for concurrent invocations or should otherwise ensure by construction that
/// no <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> instances are used which might employ such mutation.
/// </para>
/// </remarks>
/// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai#the-iembeddinggenerator-interface">The IEmbeddingGenerator interface.</related>
public interface IEmbeddingGenerator<in TInput, TEmbedding> : IEmbeddingGenerator
    where TEmbedding : Embedding
{
    /// <summary>Generates embeddings for each of the supplied <paramref name="values"/>.</summary>
    /// <param name="values">The sequence of values for which to generate embeddings.</param>
    /// <param name="options">The embedding generation options with which to configure the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The generated embeddings.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
    /// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai#create-embeddings">Create embed