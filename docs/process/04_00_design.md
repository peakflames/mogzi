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

> **📋 Detailed Documentation**: [Tool Implementation Design](04_03_tool_implementation_design.md)

### Terminal User Interface
Sophisticated TUI providing real-time interaction with AI assistance:

- **IScrollbackTerminal**: Core terminal abstraction for content management
- **FlexColumnTuiApp**: Main application with dependency injection
- **State Management**: Centralized application state with event-driven updates
- **Input Handling**: Advanced keyboard processing with autocomplete integration

> **📋 Detailed Documentation**: [Terminal Interface & TUI Design](04_04_terminal_tui_design.md)

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

> **📋 Detailed Documentation**: [Terminal Interface & TUI Design](04_04_terminal_tui_design.md)

### Diff/Patch System
Sophisticated code modification capabilities with intelligent conflict resolution:

- **Unified Diff Model**: Standard-compliant diff representation
- **Fuzzy Matching**: Intelligent patch application with multiple strategies
- **Conflict Resolution**: Comprehensive error handling and recovery
- **Tool Integration**: AI-accessible diff generation and application

> **📋 Detailed Documentation**: [Diff/Patch System Design](04_05_diff_patch_design.md)

### Cross-Platform & Performance
Mogzi is designed for optimal performance across all supported platforms:

- **Platform Detection**: Automatic shell and command selection
- **AOT Compilation**: Native compilation support for improved startup
- **Memory Efficiency**: Streaming responses and efficient resource management
- **Async Operations**: Non-blocking I/O throughout the application

> **📋 Detailed Documentation**: [Cross-Platform & Performance Design](04_06_platform_performance_design.md)

### Tool Execution Flow
Comprehensive workflow management for AI tool interactions:

- **Execution Pipeline**: Structured tool invocation with validation
- **Security Enforcement**: Working directory boundary checks
- **Response Processing**: Structured XML parsing and error handling
- **State Management**: Tool execution tracking and session persistence

> **📋 Detailed Documentation**: [Tool Execution Flow Design](04_07_tool_execution_flow_design.md)

### Modular System Prompt Architecture
Dynamic, model-specific prompt generation system:

- **Model Family Detection**: Automatic detection of AI model types (Claude, Gemini, OpenAI)
- **Component Assembly**: Modular prompt construction with reusable components
- **Path Resolution**: Absolute working directory path enforcement
- **Extensible Design**: Clean separation of model-specific and shared prompt components

> **📋 Detailed Documentation**: [Modular System Prompt Design](04_08_modular_system_prompt_design.md)

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
