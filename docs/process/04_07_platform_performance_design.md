# Mogzi Cross-Platform & Performance Design

## Cross-Platform Support

### Platform Detection
```csharp
OperatingSystem = Environment.OSVersion.Platform;
DefaultShell = OperatingSystem switch
{
    PlatformID.Win32NT => "powershell",
    PlatformID.MacOSX => "zsh",
    _ => "bash"
};
```

### Shell Command Execution
```csharp
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    fileName = "cmd.exe";
    arguments = $"/C \"{command}\"";
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
{
    fileName = "/bin/zsh";
    arguments = $"-c \"{command}\"";
}
else // Linux and Unix-like systems
{
    fileName = "/bin/bash";
    arguments = $"-c \"{command}\"";
}
```

### Path Handling
```csharp
private bool PathsEqual(string path1, string path2)
{
    var comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
        ? StringComparison.OrdinalIgnoreCase 
        : StringComparison.Ordinal;
        
    return string.Equals(Path.GetFullPath(path1), Path.GetFullPath(path2), comparison);
}
```

## Performance Optimizations

### AOT Compilation Support
- **Source Generation**: JsonSerializerContext for reflection-free JSON
- **PublishAot**: Enabled in project configuration for native compilation
- **Minimal Dependencies**: Careful selection of AOT-compatible libraries

### Memory Management
- **Streaming Responses**: IAsyncEnumerable for memory-efficient AI responses
- **Efficient Rendering**: Minimal redraws and content updates in terminal
- **Resource Disposal**: Proper cleanup of streams, processes, and services

### Async Operations
```csharp
public async Task<string> ReadFileAsync(string path)
{
    using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
    using var reader = new StreamReader(fileStream, detectEncodingFromByteOrderMarks: true);
    
    return await reader.ReadToEndAsync();
}
```

### Token Optimization
- **Token Counting**: SharpToken integration for accurate token metrics
- **System Prompt Optimization**: Dynamic generation to include only current context
- **History Management**: Efficient storage and retrieval of chat sessions

### Caching Strategy
- **Configuration Caching**: Configuration loaded once during startup
- **System Information**: Environment data cached during initialization
- **Tool Instance Reuse**: Tools instantiated once and reused

This design ensures Mogzi runs efficiently across Windows, macOS, and Linux while maintaining optimal performance through careful resource management and platform-specific optimizations.
