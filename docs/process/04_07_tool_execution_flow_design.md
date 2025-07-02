# Tool Execution Flow Design

## Overview

This document describes the complex flow of tool execution in Mogzi's TUI, from the initial user input through AI model processing to the final display in the scrollback terminal. The system involves multiple components working together to provide enhanced tool feedback with visual diff displays.

## Architecture Components

### Core Components
- **FlexColumnTuiApp**: Main TUI application orchestrating the flow
- **AppService**: Handles AI model communication and streaming responses
- **ToolResponseParser**: Parses tool responses and extracts display information
- **UnifiedDiffGenerator**: Generates diffs for file modifications
- **DiffRenderer**: Renders diff visualizations
- **ToolExecutionDisplay**: Creates enhanced tool display components
- **ScrollbackTerminal**: Manages terminal output and display

## Tool Execution Flow

### High-Level Flow

```mermaid
graph TD
    A[User Input] --> B[FlexColumnTuiApp.ProcessUserInput]
    B --> C[AppService.ProcessChatMessageAsync]
    C --> D[AI Model Processing]
    D --> E[Streaming Response Updates]
    E --> F{Tool Execution Detected?}
    F -->|Yes| G[HandleToolExecutionResult]
    F -->|No| H[Regular Text Display]
    G --> I[ToolResponseParser.ParseToolResponse]
    I --> J{Tool Type?}
    J -->|Edit/Replace| K[Generate Diff]
    J -->|Write File| L[Extract Content]
    J -->|Other| M[Extract Summary]
    K --> N[ToolExecutionDisplay.CreateToolDisplay]
    L --> N
    M --> N
    N --> O[ScrollbackTerminal.WriteStatic]
    H --> O
    O --> P[Display to User]
```

### Detailed Tool Execution Flow

```mermaid
sequenceDiagram
    participant User
    participant TuiApp as FlexColumnTuiApp
    participant AppSvc as AppService
    participant AI as AI Model
    participant Parser as ToolResponseParser
    participant DiffGen as UnifiedDiffGenerator
    participant Display as ToolExecutionDisplay
    participant Terminal as ScrollbackTerminal

    User->>TuiApp: Submit Input
    TuiApp->>AppSvc: ProcessChatMessageAsync()
    AppSvc->>AI: Send Chat Request
    
    loop Streaming Response
        AI->>AppSvc: ChatResponseUpdate
        AppSvc->>TuiApp: Response Stream
        
        alt Tool Execution Detected
            TuiApp->>TuiApp: IsToolExecutionUpdate()
            Note over TuiApp: Checks for FunctionCallContent<br/>or XML tool responses
            
            alt Function Call (Tool Starting)
                TuiApp->>TuiApp: ExtractToolNameFromUpdate()
                TuiApp->>TuiApp: CapturePreEditContentForEditTool()
                Note over TuiApp: Store call ID mappings<br/>Capture original file content
            end
            
            alt Function Result (Tool Completed)
                TuiApp->>TuiApp: HandleToolExecutionResult()
                TuiApp->>Parser: ParseToolResponse()
                
                Parser->>Parser: ParseXmlResponse()
                Note over Parser: Extract status, file path,<br/>content, error info
                
                alt Edit/Replace Tool
                    Parser->>DiffGen: GenerateDiff()
                    DiffGen->>DiffGen: IdentifyChanges()
                    DiffGen->>DiffGen: GenerateHunks()
                    DiffGen->>Parser: UnifiedDiff
                end
                
                Parser->>TuiApp: ToolResponseInfo
                TuiApp->>Display: CreateToolDisplay()
                Display->>Terminal: Enhanced Tool Display
            end
        else Regular Text
            TuiApp->>Terminal: WriteStatic()
        end
    end
    
    Terminal->>User: Display Output
```

### Tool Response Processing Detail

```mermaid
graph TD
    A[Tool Response Received] --> B{Response Type?}
    
    B -->|FunctionCallContent| C[Tool Starting]
    B -->|FunctionResultContent| D[Tool Completed]
    B -->|XML in Text| E[XML Tool Response]
    
    C --> C1[Store Call ID → Tool Name Mapping]
    C1 --> C2{Edit Tool?}
    C2 -->|Yes| C3[Capture Pre-Edit File Content]
    C2 -->|No| C4[Continue]
    C3 --> C4
    
    D --> D1[Retrieve Tool Name from Call ID]
    D1 --> D2[Extract Result Content]
    D2 --> F[Parse Tool Response]
    
    E --> E1[Extract Tool Name from XML]
    E1 --> E2[Extract Result Content]
    E2 --> F
    
    F --> G[ToolResponseParser.ParseToolResponse]
    G --> H[Extract File Path, Status, Content]
    H --> I{Tool Type Classification}
    
    I -->|WriteFileTool| J[Extract Content for Display]
    I -->|EditTool| K[Generate Diff from Pre/Post Content]
    I -->|DiffPatchTool| L[Extract or Generate Patch Diff]
    I -->|Other| M[Use Summary Text]
    
    J --> N[Create Tool Display]
    K --> K1[UnifiedDiffGenerator.GenerateDiff]
    K1 --> K2[DiffRenderer.RenderDiff]
    K2 --> N
    L --> N
    M --> N
    
    N --> O[ToolExecutionDisplay.CreateToolDisplay]
    O --> P[ScrollbackTerminal.WriteStatic]
    P --> Q[User Sees Enhanced Tool Output]
```

### Diff Generation Process

```mermaid
graph TD
    A[Diff Generation Request] --> B[UnifiedDiffGenerator.GenerateDiff]
    B --> C[Split Content into Lines]
    C --> D[LongestCommonSubsequence.FindLcs]
    D --> E[IdentifyChanges]
    E --> F[Group Changes into Hunks]
    F --> G[Generate Context Lines]
    G --> H[Create DiffHunk Objects]
    H --> I[Return UnifiedDiff]
    
    I --> J[DiffRenderer.RenderDiff]
    J --> K{Empty Diff?}
    K -->|Yes| L[Display "No changes detected"]
    K -->|No| M[Render Diff Lines]
    
    M --> M1[Context Lines: dim gray]
    M --> M2[Added Lines: green with +]
    M --> M3[Removed Lines: red with -]
    
    M1 --> N[Formatted Diff Display]
    M2 --> N
    M3 --> N
    N --> O[Include in Tool Display Panel]
```

## State Management

### Tool Execution State Tracking

```mermaid
stateDiagram-v2
    [*] --> Input
    Input --> Thinking : User submits input
    Thinking --> ToolExecution : Tool detected
    Thinking --> Input : Response complete (no tools)
    ToolExecution --> ToolExecution : Multiple tools
    ToolExecution --> Input : All tools complete
    
    state ToolExecution {
        [*] --> FunctionCall
        FunctionCall --> FunctionResult : Tool executes
        FunctionResult --> Display : Parse & render
        Display --> [*]
    }
```

### Data Flow Tracking

```mermaid
graph LR
    A[Function Call ID] --> B[Tool Name Mapping]
    A --> C[Pre-Edit Content Mapping]
    
    B --> D[Tool Result Processing]
    C --> D
    
    D --> E[Enhanced Display Generation]
    E --> F[Scrollback Terminal]
    
    subgraph "Persistent Storage"
        B
        C
    end
    
    subgraph "Processing Pipeline"
        D
        E
    end
    
    subgraph "User Interface"
        F
    end
```

## Component Interactions

### Key Method Interactions

```mermaid
graph TD
    A[ProcessUserInput] --> B[IsToolExecutionUpdate]
    B --> C[ExtractToolNameFromUpdate]
    B --> D[CapturePreEditContentForEditTool]
    B --> E[HandleToolExecutionResult]
    
    E --> F[ToolResponseParser.ParseToolResponse]
    F --> G[ToolResponseParser.ExtractFileDiff]
    G --> H[UnifiedDiffGenerator.GenerateDiff]
    
    E --> I[ToolExecutionDisplay.CreateToolDisplay]
    I --> J[DiffRenderer.RenderDiff]
    
    I --> K[ScrollbackTerminal.WriteStatic]
    
    subgraph "FlexColumnTuiApp"
        A
        B
        C
        D
        E
    end
    
    subgraph "Tool Response Processing"
        F
        G
        H
    end
    
    subgraph "Display Generation"
        I
        J
        K
    end
```

## Error Handling and Edge Cases

### Error Flow

```mermaid
graph TD
    A[Tool Execution] --> B{Success?}
    B -->|Yes| C[Normal Processing]
    B -->|No| D[Error Detected]
    
    D --> E[Extract Error Message]
    E --> F[Set Status to Failed]
    F --> G[Create Error Display]
    G --> H[Show Error in Scrollback]
    
    C --> I[Parse Tool Response]
    I --> J{Valid Response?}
    J -->|Yes| K[Generate Display]
    J -->|No| L[Fallback Display]
    
    K --> M[Enhanced Tool Display]
    L --> N[Basic Tool Display]
    
    M --> O[Scrollback Terminal]
    N --> O
    H --> O
```

### Edge Case Handling

```mermaid
graph TD
    A[Tool Response Processing] --> B{Content Available?}
    B -->|No| C[Use Fallback Summary]
    B -->|Yes| D{File Path Available?}
    D -->|No| E[Skip Diff Generation]
    D -->|Yes| F{Original Content Available?}
    F -->|No| G[Skip Diff for Edit Tools]
    F -->|Yes| H[Generate Diff]
    
    C --> I[Basic Display]
    E --> I
    G --> I
    H --> J[Enhanced Display with Diff]
    
    I --> K[Scrollback Output]
    J --> K
```

## Performance Considerations

### Async Processing

```mermaid
graph TD
    A[Streaming Response] --> B[Async Processing]
    B --> C[Non-blocking UI Updates]
    C --> D[Real-time Tool Feedback]
    
    B --> E[Background Diff Generation]
    E --> F[Cached Results]
    F --> G[Fast Display Updates]
    
    subgraph "UI Thread"
        A
        C
        D
    end
    
    subgraph "Background Processing"
        E
        F
    end
```

## Configuration and Extensibility

### Tool Type Detection

```mermaid
graph TD
    A[Tool Name] --> B[Normalize to Lowercase]
    B --> C{Tool Type?}
    
    C -->|write_file, writefile, write_to_file| D[WriteFileTool]
    C -->|replace_in_file, edit_file, editfile, edit| E[EditTool]
    C -->|apply_code_patch, generate_code_patch, preview_patch_application| F[DiffPatchTool]
    C -->|Other| G[GenericTool]
    
    D --> H[Show Content Directly]
    E --> I[Generate Diff Display]
    F --> J[Extract/Generate Patch]
    G --> K[Show Summary]
    
    H --> L[Tool Display Panel]
    I --> L
    J --> L
    K --> L
```

## Future Enhancements

### Planned Improvements

1. **Visual Refinements**
   - Replace heavy bordered panels with subtle rounded rectangles
   - Implement compact tool execution boxes with dark background
   - Add line numbers to diff visualization
   - Improve diff color scheme and syntax highlighting

2. **Enhanced Tool Support**
   - Support for more tool types
   - Better content extraction for different response formats
   - Improved error handling and recovery

3. **Performance Optimizations**
   - Diff caching for repeated operations
   - Lazy loading of large file content
   - Optimized rendering for large diffs

## Conclusion

The tool execution flow in Mogzi's TUI is a sophisticated system that provides enhanced feedback for AI tool operations. The integration of multiple components enables real-time tool execution tracking, intelligent diff generation, and rich visual displays that help users understand what tools are doing and what changes they're making.

The system's modular design allows for easy extension and customization while maintaining clean separation of concerns between UI management, tool response processing, and display generation.
