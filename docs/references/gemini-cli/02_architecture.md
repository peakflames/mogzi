# Gemini-CLI Architecture

## Core Architecture

**Technology Stack:**
- React/Ink for terminal UI rendering
- TypeScript for type safety
- Node.js runtime environment
- Streaming API integration with Google Gemini

**Component Hierarchy:**
```mermaid
graph TD
    A[App.tsx] --> B[InputPrompt]
    A --> C[HistoryItemDisplay]
    A --> D[ToolGroupMessage]
    A --> E[LoadingIndicator]
    A --> F[Footer]
    
    B --> G[SuggestionsDisplay]
    B --> H[TextBuffer]
    
    D --> I[ToolMessage]
    D --> J[ToolConfirmationMessage]
    
    C --> K[GeminiMessage]
    C --> L[UserMessage]
    C --> M[ErrorMessage]
```

**Data Flow Architecture (Interactive TUI):**
```mermaid
sequenceDiagram
    participant User
    participant InputPrompt
    participant useGeminiStream
    participant GeminiAPI
    participant ToolSystem
    participant UI

    User->>InputPrompt: Types message
    InputPrompt->>useGeminiStream: Submit query
    useGeminiStream->>GeminiAPI: Stream request
    
    loop Streaming Response
        GeminiAPI-->>useGeminiStream: Content chunk
        useGeminiStream-->>UI: Update display
        
        alt Tool Call Required
            useGeminiStream->>ToolSystem: Execute tool
            ToolSystem-->>useGeminiStream: Tool result
            useGeminiStream->>GeminiAPI: Continue with result
        end
    end
    
    useGeminiStream-->>UI: Final response
```

**Data Flow Architecture (Non-Interactive CLI):**
```mermaid
sequenceDiagram
    participant User
    participant CLI
    participant GeminiAPI
    participant ToolSystem

    User->>CLI: Provides input via command line argument or stdin
    CLI->>GeminiAPI: Sends request
    GeminiAPI-->>CLI: Receives full response
    
    alt Tool Call Required
        CLI->>ToolSystem: Execute tool
        ToolSystem-->>CLI: Tool result
        CLI->>GeminiAPI: Continue with result
    end

    CLI-->>User: Prints final response to stdout
```

**State Management:**
- React Context for global state (SessionContext, StreamingContext)
- Custom hooks for feature-specific state (useGeminiStream, useInputHistory)
- Local component state for UI interactions

**Key Architectural Patterns:**
- **Streaming Architecture**: Real-time response updates via async generators
- **Event-Driven Input**: Ink's useInput hook for keyboard handling
- **Component Composition**: Modular UI components with clear responsibilities
- **Hook-Based Logic**: Custom hooks encapsulate complex business logic
- **Static/Dynamic Rendering**: Ink's Static component for history, dynamic for live updates
