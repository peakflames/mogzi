# TUI Architecture and Design

## 1. Introduction

This document provides the architectural and design specification for the MaxBot Terminal User Interface (TUI). It details the proposed architecture, component design, and technology stack required to implement the features and requirements defined in the `concept_of_operations.md` and `tool_requirements.md`.

## 2. Proposed Architecture

The TUI will be built upon a **strictly decoupled, event-driven architecture**. The core `MaxBot` library will contain **zero UI dependencies** and will be completely unaware of any user interface.

The `TUI` project will contain its own UI-specific service (`TuiAppService`) that wraps the core `IAppService`. This service will be responsible for orchestrating calls to the core logic and publishing UI-specific events.

This approach ensures a clean separation of concerns, making the `MaxBot` library reusable across different frontends (TUI, Web API, Blazor, etc.) and enhancing testability.

### 2.1. Architectural Diagram

```mermaid
graph TD
    subgraph "Application Core (MaxBot)"
        A[IAppService]
        C[ChatClient] --> A;
    end

    subgraph "User Interface (TUI Project)"
        D[TuiAppService] -- "Wraps" --> A;
        D --> B{TuiEventBus};
        E[ConsoleRenderer] -- "Subscribes to" --> B;
        E --> F[ITuiCard];
        G[Program.cs] --> E;
    end

    subgraph "TUI Cards"
        direction TB
        F --> H[TextCard];
        F --> I[CommandCard];
        F --> J[DiffCard];
        F --> K[...];
    end

    B -- "Publishes ITuiEvent notifications" --> E;
```

### 2.2. Data Flow

1.  The `TuiAppService` in the `TUI` project calls the core `IAppService.ProcessChatMessageAsync` method.
2.  The `TuiAppService` iterates over the `IAsyncEnumerable<ChatResponseUpdate>` stream returned by the core service.
3.  For each update received from the core, the `TuiAppService` creates and publishes a specific, strongly-typed TUI event (e.g., `TextReceivedEvent`, `ToolCallEvent`) to the `TuiEventBus`.
4.  The `ConsoleRenderer`, also in the `TUI` project, subscribes to these events.
5.  Upon receiving an event, the `ConsoleRenderer`'s handler instantiates the appropriate `ITuiCard` (e.g., `TextCard`, `CommandCard`) and passes the event data to it.
6.  The card uses the data to render itself to the console using `Spectre.Console` components. This flow ensures the `MaxBot` core remains completely decoupled from the UI.

## 3. Component Design

### 3.1. Core Components

-   **`ConsoleRenderer`**: A singleton class responsible for managing the overall TUI layout. It initializes `Spectre.Console`, sets up the main layout regions (e.g., conversation history, input prompt), and handles the subscription to and processing of UI events.
-   **`TuiEventBus`**: A lightweight, in-memory event bus that provides type-safe pub/sub functionality. It uses synchronous event handling to ensure immediate UI updates and optimal responsiveness.
-   **`ITuiEvent`**: A base interface that all TUI events must implement, providing common properties like timestamp and ensuring type safety across the event system.
-   **`ITuiCard`**: An interface that defines the contract for all renderable TUI cards. It will have a single method, `Render(IAnsiConsole console)`, which contains the logic for drawing the card to the screen.
-   **Card Implementations**: A series of classes that implement `ITuiCard`. These cards are designed for reusability. For instance, a single `FileCard` could be responsible for rendering the output of `read_file`, `list_files`, and `write_to_file`, adapting its presentation (e.g., showing a text preview vs. a file tree) based on the specific event data it receives. This approach promotes code reuse while maintaining presentation flexibility.

### 3.2. Event System Design

The event system uses a simple, type-safe approach that avoids reflection and maintains AOT compatibility:

```csharp
public interface ITuiEvent
{
    DateTime Timestamp { get; }
}

public interface ITuiEventBus
{
    void Publish<T>(T eventData) where T : ITuiEvent;
    void Subscribe<T>(Action<T> handler) where T : ITuiEvent;
}

// Example event implementation
public class FileReadEvent : ITuiEvent
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string FilePath { get; init; }
    public string Content { get; init; }
    public int LineCount { get; init; }
}
```

This design ensures:
- **Type Safety**: All events implement `ITuiEvent` and handlers are strongly typed
- **Performance**: Direct method calls without reflection overhead
- **AOT Compatibility**: No dynamic type discovery or runtime code generation
- **Simplicity**: Minimal API surface that's easy to understand and maintain

### 3.3. Class Diagram

```mermaid
classDiagram
    class IAppService {
        <<interface>>
        +ProcessChatMessageAsync()
    }
    class TuiAppService {
        -IAppService appService
        +ProcessChatMessageAsync()
        -PublishEvent(event)
    }
    class TuiEventBus {
        +Publish~T~(event)
        +Subscribe~T~(handler)
    }
    class ITuiEvent {
        <<interface>>
    }
    class ConsoleRenderer {
        +Render()
        -SubscribeToEvents()
    }
    class ITuiCard {
        <<interface>>
        +GetRenderable()
    }
    class TextCard {
        +GetRenderable()
    }
    class TextReceivedEvent {
        +string Text
    }

    TuiAppService --> IAppService : Wraps
    TuiAppService --> TuiEventBus : Publishes events
    ConsoleRenderer --> TuiEventBus : Subscribes to events
    ConsoleRenderer o-- ITuiCard
    ITuiCard <|-- TextCard
    ITuiEvent <|-- TextReceivedEvent
    TuiEventBus ..> ITuiEvent : Uses
```

## 4. Technology Stack

-   **Primary TUI Library**: **`Spectre.Console`** will be used for its rich set of components, including layouts, tables, trees, and live displays. This will form the foundation of the TUI.
-   **Markdown Rendering**: **`Spectre.Console.Markdig`** will be integrated to parse and render markdown-formatted text received from the LLM, allowing for richly formatted chat messages.
-   **Syntax Highlighting**: **`Spectre.Console.Extensions.Markup`** will be used to render code blocks with language-appropriate syntax highlighting, improving the readability of file contents and code snippets.

This combination of technologies will enable the rapid development of a robust, feature-rich, and visually appealing TUI that meets all the specified requirements.
