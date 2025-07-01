# Modular System Prompt Design

## Overview

The modular system prompt architecture provides model-specific prompt generation for different AI model families while maintaining consistent tool usage instructions and environment information.

## Architecture

### Component Structure

```mermaid
graph TD
    A[Promptinator.GetSystemPrompt] --> B[SystemPromptComponentFactory]
    B --> C[Model Family Detection]
    C --> D{ModelFamily}
    
    D -->|Claude| E[ClaudeSystemPrompts]
    D -->|Gemini| F[GeminiSystemPrompts]
    D -->|OpenAI| G[OpenAISystemPrompts]
    D -->|Other| H[Default to Claude]
    
    A --> I[ToolUsageSystemPrompt]
    A --> J[EnvSystemPrompt]
    A --> K[UserCustomSystemPrompt]
    
    E --> L[Prelude + Epilogue]
    F --> M[Prelude + Epilogue]
    G --> N[Prelude + Epilogue]
    H --> O[Prelude + Epilogue]
    
    L --> P[Final System Prompt]
    M --> P
    N --> P
    O --> P
    I --> P
    J --> P
    K --> P
```

### Prompt Assembly Flow

```mermaid
sequenceDiagram
    participant C as ChatClient
    participant P as Promptinator
    participant F as SystemPromptComponentFactory
    participant MS as Model-Specific Prompts
    participant TS as ToolUsageSystemPrompt
    participant ES as EnvSystemPrompt
    participant US as UserCustomSystemPrompt
    
    C->>P: GetSystemPrompt(config, env)
    P->>P: Ensure absolute working directory
    P->>F: GetModelFamily(modelId)
    F-->>P: ModelFamily enum
    
    P->>F: GetModelSpecificPreludeSystemPrompt(family)
    F->>MS: GetPreludePrompt()
    MS-->>F: Prelude text
    F-->>P: Prelude text
    
    P->>TS: GetToolUsagePrompt()
    TS-->>P: Tool usage text
    
    P->>ES: GetEnvPrompt(env details)
    ES-->>P: Environment text
    
    P->>US: GetUserCustomPrompt(config)
    US-->>P: Custom text (empty)
    
    P->>F: GetModelSpecificEpilogSystemPrompt(family)
    F->>MS: GetEpilogPrompt()
    MS-->>F: Epilogue text
    F-->>P: Epilogue text
    
    P-->>C: Complete system prompt
```

## Model Family Detection

### Detection Logic

```csharp
public static ModelFamily GetModelFamily(string modelId)
{
    var lowerModelId = modelId.ToLowerInvariant();

    if (lowerModelId.Contains("claude"))
        return ModelFamily.Claude;
    
    if (lowerModelId.Contains("gemini"))
        return ModelFamily.Gemini;
    
    if (lowerModelId.Contains("gpt") || lowerModelId.Contains("openai"))
        return ModelFamily.OpenAI;
    
    return ModelFamily.Other;
}
```

### Model Family Characteristics

| Family | Detection Pattern | Personality | Tone |
|--------|------------------|-------------|------|
| Claude | "claude" | Warm, empathetic | Natural, conversational |
| Gemini | "gemini" | Direct, helpful | Methodical, precise |
| OpenAI | "gpt", "openai" | Professional | Efficient, practical |
| Other | Default fallback | Uses Claude prompts | Compatibility mode |

## File Organization

### Class Structure

```
src/MaxBot/Prompts/
├── ModelFamily.cs                 # Enum definition
├── SystemPrompt.cs               # Main orchestrator
├── SystemPromptComponents.cs     # Factory class
├── ClaudeSystemPrompts.cs        # Claude-specific prompts
├── GeminiSystemPrompts.cs        # Gemini-specific prompts
├── OpenAISystemPrompts.cs        # OpenAI-specific prompts
├── ToolUsageSystemPrompt.cs      # Model-agnostic tools
├── EnvSystemPrompt.cs            # Environment information
└── UserCustomSystemPrompt.cs    # User customization
```

### Responsibility Matrix

| Class | Responsibility | Scope |
|-------|---------------|-------|
| `Promptinator` | Orchestrates prompt assembly | Main entry point |
| `SystemPromptComponentFactory` | Model family detection and routing | Factory pattern |
| `ClaudeSystemPrompts` | Claude prelude/epilogue | Model-specific |
| `GeminiSystemPrompts` | Gemini prelude/epilogue | Model-specific |
| `OpenAISystemPrompts` | OpenAI prelude/epilogue | Model-specific |
| `ToolUsageSystemPrompt` | Tool usage instructions | Model-agnostic |
| `EnvSystemPrompt` | Environment details | Model-agnostic |
| `UserCustomSystemPrompt` | Custom user prompts | Configuration-based |

## Working Directory Path Resolution

### Path Handling Flow

```mermaid
graph LR
    A[Input Path] --> B{Is Absolute?}
    B -->|Yes| C[Use As-Is]
    B -->|No| D[Path.GetFullPath]
    C --> E[Absolute Path]
    D --> E
    E --> F[Include in Environment Prompt]
```

### Implementation

```csharp
// In Promptinator.GetSystemPrompt()
var absoluteWorkingDirectory = Path.IsPathRooted(currentWorkingDirectory) 
    ? currentWorkingDirectory 
    : Path.GetFullPath(currentWorkingDirectory);

// In DefaultWorkingDirectoryProvider
public string GetCurrentDirectory()
{
    var currentDirectory = Directory.GetCurrentDirectory();
    return Path.IsPathRooted(currentDirectory)
        ? currentDirectory 
        : Path.GetFullPath(currentDirectory);
}
```

## Prompt Assembly Structure

### Final Prompt Format

```
[Model-Specific Prelude]

[Tool Usage Instructions]

[Environment Information]

[User Custom Prompts] (if any)

[Model-Specific Epilogue]
```

### Component Ordering

1. **Model-Specific Prelude** - Sets personality and context
2. **Tool Usage Instructions** - Universal tool guidelines
3. **Environment Information** - System context and settings
4. **User Custom Prompts** - Optional user-defined content
5. **Model-Specific Epilogue** - Final instructions and reminders

## Interface Contracts

### Core Interfaces

```csharp
// Model-specific prompt providers
internal static class ClaudeSystemPrompts
{
    public static string GetPreludePrompt();
    public static string GetEpilogPrompt();
}

// Factory methods
public static class SystemPromptComponentFactory
{
    public static ModelFamily GetModelFamily(string modelId);
    public static string GetModelSpecificPreludeSystemPrompt(ModelFamily family);
    public static string GetModelSpecificEpilogSystemPrompt(ModelFamily family);
}

// Component providers
internal static class ToolUsageSystemPrompt
{
    public static string GetToolUsagePrompt();
}

internal static class EnvSystemPrompt
{
    public static string GetEnvPrompt(string dateTime, string os, string shell, 
                                     string username, string hostname, 
                                     string workingDir, string mode, string approvals);
}
```

## Testing Strategy

### Test Coverage Areas

- Model family detection accuracy
- Absolute path resolution
- Prompt component assembly
- Model-specific content inclusion
- Environment information formatting

### Key Test Cases

```csharp
[Fact] GetModelFamily_ShouldReturnClaude_WhenModelIdContainsClaude()
[Fact] GetModelFamily_ShouldReturnGemini_WhenModelIdContainsGemini()
[Fact] GetModelFamily_ShouldReturnOpenAI_WhenModelIdContainsGpt()
[Fact] GetSystemPrompt_ShouldIncludeAbsolutePath_WhenRelativePathProvided()
[Fact] GetSystemPrompt_ShouldContainModelSpecificContent_ForEachFamily()
