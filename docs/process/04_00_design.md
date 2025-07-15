# Mogzi CLI Design Overview

This document provides a high-level overview of Mogzi's design architecture. For detailed implementation specifics, refer to the specialized design appendices.

## Design Philosophy

Mogzi follows modern .NET design patterns emphasizing:
- **Security-First**: All operations validated within working directory boundaries
- **Async-First**: Non-blocking operations throughout the application
- **Dependency Injection**: Clean separation of concerns and testability
- **Cross-Platform**: Consistent behavior across Windows, macOS, and Linux
- **Performance**: AOT compilation support and efficient resource management

## Core Architecture Components

### Service Layer
The service layer provides the primary application logic through clean abstractions:

- **IAppService**: Main application service interface for chat processing
- **AppService**: Implementation handling AI interactions and session management
- **ChatHistoryService**: Persistent storage for chat sessions
- **ChatClient**: AI client factory with configuration management

> **📋 Detailed Documentation**: [Service Layer & Domain Design](04_02_service_domain_design.md)

### Tool System
Mogzi's tool system enables AI function calling through a comprehensive set of secure, validated tools:

- **Base Tool Pattern**: Consistent implementation across all tools
- **Security Implementation**: Working directory validation and approval system
- **Tool Categories**: File operations, system interactions, diff/patch operations
- **Response Format**: Structured XML responses with comprehensive error handling

> **📋 Detailed Documentation**: [Tool Implementation Design](04_03_ai_tool_implementation_design.md)

### Terminal User Interface
Sophisticated TUI providing real-time interaction with AI assistance:

- **IScrollbackTerminal**: Core terminal abstraction for content management
- **FlexColumnTuiApp**: Main application with dependency injection
- **State Pattern**: Dedicated state classes (InputTuiState, ThinkingTuiState, ToolExecutionTuiState)
- **Component Architecture**: Modular UI components with mediator coordination
- **Input Handling**: Advanced keyboard processing with autocomplete integration

> **📋 Detailed Documentation**: [Terminal Interface & TUI Design](04_05_terminal_tui_design.md)

### Autocomplete System
Intelligent, context-aware completion for enhanced user experience:

- **Provider Pattern**: Extensible completion types (file paths, slash commands)
- **Trigger-Based Activation**: Context-aware completion using trigger characters
- **Security Integration**: File operations validated within working directory
- **Performance Optimization**: Async operations with result limiting

> **📋 Detailed Documentation**: [Autocomplete System Design](04_01_autocomplete_design.md)

### User Selection System
Interactive, non-blocking user selection for slash commands:

- **Provider Pattern**: Extensible selection providers for different commands
- **Event-Driven Activation**: Interactive UI triggered by events from the slash command processor
- **Integrated UI**: Renders as an overlay within the main TUI loop
- **State Management**: Coordinated through a dedicated `UserSelectionManager`

> **📋 Detailed Documentation**: [Terminal Interface & TUI Design](04_05_terminal_tui_design.md)

### Diff/Patch System
Sophisticated code modification capabilities with intelligent conflict resolution:

- **Unified Diff Model**: Standard-compliant diff representation
- **Fuzzy Matching**: Intelligent patch application with multiple strategies
- **Conflict Resolution**: Comprehensive error handling and recovery
- **Tool Integration**: AI-accessible diff generation and application

> **📋 Detailed Documentation**: [Diff/Patch System Design](04_04_ai_tool_diff_patch_design.md)

### Cross-Platform & Performance
Mogzi is designed for optimal performance across all supported platforms:

- **Platform Detection**: Automatic shell and command selection
- **AOT Compilation**: Native compilation support for improved startup
- **Memory Efficiency**: Streaming responses and efficient resource management
- **Async Operations**: Non-blocking I/O throughout the application

> **📋 Detailed Documentation**: [Platform Performance Design](04_07_platform_performance_design.md)

### Tool Execution Flow
Comprehensive workflow management for AI tool interactions:

- **Execution Pipeline**: Structured tool invocation with validation
- **Security Enforcement**: Working directory boundary checks
- **Response Processing**: Structured XML parsing and error handling
- **State Management**: Tool execution tracking and session persistence

> **📋 Detailed Documentation**: [Terminal TUI Tool Execution Flow Design](04_06_terminial_tui_tool_exe_flow_design.md)

### Modular System Prompt Architecture
Dynamic, model-specific prompt generation system:

- **Model Family Detection**: Automatic detection of AI model types (Claude, Gemini, OpenAI)
- **Component Assembly**: Modular prompt construction with reusable components
- **Path Resolution**: Absolute working directory path enforcement
- **Extensible Design**: Clean separation of model-specific and shared prompt components

> **📋 Detailed Documentation**: [Modular System Prompt Design](04_08_modular_system_prompt_design.md)

### Session Management
Comprehensive session persistence with attachment support and usage tracking:

- **Session Lifecycle**: Complete session creation, loading, and persistence
- **Attachment Handling**: Support for images, PDFs, and other file types within chat messages
- **Directory-Based Storage**: Each session gets its own directory with metadata and attachments
- **Usage Metrics**: Real-time token usage tracking with Cline-inspired display formatting
- **Tool Execution Replay**: Complete preservation of function calls and results for session restoration

> **📋 Detailed Documentation**: [Session Management Design](04_09_session_mgmt.md)

### Session Usage Metrics
Real-time token usage tracking with Cline-inspired display and per-session isolation:

- **Token Tracking**: Automatic capture of input/output tokens during AI interactions
- **Cline-Style Formatting**: Smart number abbreviations (345, 1.9k, 15k, 1.9m) for clean display
- **Context Window Monitoring**: Color-coded context utilization (Green/Yellow/Red)
- **Session Persistence**: Usage metrics automatically saved and restored with sessions
- **Footer Integration**: Enhanced footer display with token usage and context window status

> **📋 Detailed Documentation**: [Session Usage Metrics Design](04_10_session_usage_metrics_design.md)

## System Prompt Design

**Dynamic Prompt Generation:**
Mogzi uses a computed property system for real-time prompt generation:

```csharp
public string SystemPrompt => Promptinator.GetSystemPrompt(
    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
    OperatingSystem.ToString(),
    DefaultShell,
    Username,
    Hostname,
    Directory.GetCurrentDirectory(),
    Config,
    _mode);
```

**Key Features:**
- **Real-Time Context**: Current timestamp and environment state
- **Security Awareness**: Working directory boundaries emphasized
- **Configuration Integration**: Tool approval settings and debug mode
- **Mode-Specific Behavior**: Different prompts for oneshot vs chat modes
- **Model-Specific Adaptation**: Tailored prompts for different AI model families

## Error Handling Strategy

**Functional Error Handling:**
- **Result<T> Pattern**: Functional error handling without exceptions
- **Graceful Degradation**: Application continues operation when possible
- **Security-Aware Messages**: Generic error messages prevent information disclosure
- **Debug Mode Integration**: Detailed error information when enabled

**Application Resilience:**
- **Exception Boundaries**: Try-catch blocks around all external operations
- **Resource Cleanup**: Proper disposal of streams, processes, and services
- **State Recovery**: Graceful handling of corrupted or missing data

## Design Patterns

### Primary Patterns Used
- **Factory Pattern**: ChatClient creation with comprehensive validation
- **Repository Pattern**: ChatHistoryService for session persistence
- **Strategy Pattern**: Fuzzy matching strategies and working directory providers
- **State Pattern**: Application state management with dedicated state classes
- **Component Pattern**: Modular UI components with lifecycle management
- **Mediator Pattern**: Coordinated component communication
- **Observer Pattern**: State management with event-driven updates
- **Provider Pattern**: Extensible autocomplete and tool systems

### Dependency Injection
- **Service Lifetime Management**: Singleton services for shared state
- **Interface Segregation**: Clean abstractions for testability
- **Constructor Injection**: Dependencies provided through constructors

## Configuration Management

**Hierarchical Configuration:**
```csharp
public class ApplicationConfiguration
{
    public string DefaultMode { get; set; } = "oneshot";
    public List<ApiProvider> ApiProviders { get; set; } = [];
    public List<Profile> Profiles { get; set; } = [];
    public string ToolApprovals { get; set; } = "readonly";
    public bool Debug { get; set; } = false;
}
```

**Features:**
- **JSON Serialization**: Source-generated contexts for AOT compatibility
- **Profile Management**: Multiple AI provider configurations
- **Security Controls**: Tool approval system integration
- **Environment Integration**: Debug mode and operational settings

This design provides Mogzi with a robust, secure, and performant foundation for AI-assisted development while maintaining clean architecture principles and excellent user experience.

## State Management Architecture

**State Pattern Implementation:**
```csharp
public interface ITuiState
{
    string Name { get; }
    IRenderable RenderDynamicContent(ITuiContext context);
    Task HandleKeyPressAsync(ITuiContext context, KeyPressEventArgs e);
    Task HandleCharacterTypedAsync(ITuiContext context, CharacterTypedEventArgs e);
    Task OnEnterAsync(ITuiContext context, ITuiState? previousState);
    Task OnExitAsync(ITuiContext context, ITuiState? nextState);
}
```

**State Manager Coordination:**
```csharp
public interface ITuiStateManager
{
    ITuiState? CurrentState { get; }
    ChatState CurrentStateType { get; }
    Task TransitionToStateAsync(ChatState newStateType);
    Task HandleKeyPressAsync(KeyPressEventArgs e);
    Task HandleCharacterTypedAsync(CharacterTypedEventArgs e);
    void RegisterState(ChatState stateType, Func<ITuiState> stateFactory);
}
```

**Key Features:**
- **State Isolation**: Each application state (Input, Thinking, ToolExecution) handled by a separate class
- **Lifecycle Management**: OnEnterAsync/OnExitAsync methods for state setup and teardown
- **Input Delegation**: Keyboard events delegated to the current active state
- **Dynamic Rendering**: Each state provides its own rendering logic
- **Factory Registration**: States registered with factory functions for lazy instantiation

**State Transition Flow:**
- InputTuiState → ThinkingTuiState (when user submits input)
- ThinkingTuiState → ToolExecutionTuiState (when tool call detected)
- ToolExecutionTuiState → ThinkingTuiState (when tool execution completes)
- ThinkingTuiState → InputTuiState (when AI response completes)
- Any State → InputTuiState (on cancellation)

## Component Architecture

**Component System Design:**
```csharp
public interface ITuiComponent
{
    string Name { get; }
    bool IsVisible { get; set; }
    IRenderable Render(IRenderContext context);
    Task<bool> HandleInputAsync(IRenderContext context, object inputEvent);
    Task InitializeAsync(IRenderContext context);
    Task DisposeAsync();
}
```

**Component Manager:**
```csharp
public interface ITuiComponentManager
{
    IReadOnlyDictionary<string, ITuiComponent> Components { get; }
    ITuiLayout? CurrentLayout { get; set; }
    void RegisterComponent(ITuiComponent component);
    IRenderable RenderLayout(IRenderContext context);
    Task<bool> BroadcastInputAsync(object inputEvent, IRenderContext context);
    void UpdateComponentVisibility(ChatState currentState, IRenderContext context);
}
```

**AI Processing Coordinator:**
```csharp
public interface IAiProcessingCoordinator
{
    string Name { get; }
    Task StartAiProcessingWorkflow(ITuiContext context);
    Task NotifyHistoryChangedAsync();
}
```

**Key Features:**
- **Component Modularity**: Each UI element is a self-contained component
- **Lifecycle Management**: Components have initialization and disposal phases
- **Layout System**: Flexible component arrangement through layout interfaces
- **Mediator Coordination**: Reduced coupling through centralized communication
- **Event Broadcasting**: Input events distributed to relevant components
- **State-Based Visibility**: Component visibility managed based on application state

**Core Components:**
- InputPanel: User input field with cursor positioning
- AutocompletePanel: Suggestion display with keyboard navigation
- UserSelectionPanel: Interactive selection lists for commands
- FooterPanel: Status information (directory, model, token usage)
- WelcomePanel: Application welcome screen and branding
- ProgressPanel: AI processing and tool execution progress
