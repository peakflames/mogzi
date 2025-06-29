# MaxBot CLI Architecture

## Core Architecture

**Technology Stack:**
- .NET 9 with C# for cross-platform development
- Spectre.Console for rich terminal UI rendering
- Microsoft.Extensions.AI for AI model integration
- Dependency injection with Microsoft.Extensions.DependencyInjection

**Component Hierarchy:**
```mermaid
graph TD
    A[Program.cs] --> B[FlexColumnTuiApp]
    B --> C[AdvancedKeyboardHandler]
    B --> D[SlashCommandProcessor]
    B --> E[HistoryManager]
    B --> F[StateManager]
    B --> G[IScrollbackTerminal]
    
    H[IAppService] --> I[ChatClient]
    I --> J[FileSystemTools]
    I --> K[DiffPatchTools]
    I --> L[SystemTools]
    
    B --> H
```

**Data Flow Architecture:**
```mermaid
sequenceDiagram
    participant User
    participant FlexColumnTuiApp
    participant IAppService
    participant ChatClient
    participant AIModel
    participant Tools

    User->>FlexColumnTuiApp: Types message
    FlexColumnTuiApp->>IAppService: ProcessChatMessageAsync
    IAppService->>ChatClient: GetStreamingResponseAsync
    ChatClient->>AIModel: Send request
    
    loop Streaming Response
        AIModel-->>ChatClient: Content chunk
        ChatClient-->>FlexColumnTuiApp: Update UI
        
        alt Tool Call Required
            ChatClient->>Tools: Execute tool
            Tools-->>ChatClient: Tool result
            ChatClient->>AIModel: Continue with result
        end
    end
    
    ChatClient-->>FlexColumnTuiApp: Final response
```

**State Management:**
- ChatState enum for UI state (Input, Thinking, ToolExecution)
- HistoryManager for chat message persistence
- StateManager for application state coordination
- Event-driven architecture with custom keyboard handler

**Key Architectural Patterns:**
- **Dependency Injection**: Service-oriented design with DI container
- **Streaming Architecture**: IAsyncEnumerable for real-time updates
- **Event-Driven Input**: Custom AdvancedKeyboardHandler for keyboard events
- **Service Layer**: Clean separation between UI and business logic
- **Tool Integration**: Microsoft.Extensions.AI function calling system
