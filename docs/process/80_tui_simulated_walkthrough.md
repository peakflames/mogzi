## Simulation: "Create a 'hello world' python script in a new tmp directory then run it then change it to say 'hello universe'"

### Initial State

- FlexColumnTuiApp starts in `ChatState.Input`
- `_inputContext.State = InputState.Normal`
- Dynamic content renders: `CreateFlexInputComponent()` showing blue prompt ">" with blinking cursor
- Footer shows current directory, model info, and token usage
- Static content shows welcome message with ASCII art "MOGZI"

### User Types: "create a 'hello world' python script in a new tmp directory then run it then change it to say 'hello universe'"

__Character Input Flow:__

1. Each character triggers `OnCharacterTyped()` → `InsertCharacter()`
2. `_inputContext.CurrentInput` builds character by character
3. `_inputContext.CursorPosition` increments with each character
4. `UpdateAutocompleteState()` called after each character
5. Dynamic content re-renders showing updated input with cursor position

__Autocomplete Detection:__

- No '@' or '/' triggers detected, so autocomplete remains inactive
- `_inputContext.State` stays `InputState.Normal`

### User Presses Enter

__Input Processing:__

1. `OnKeyPressed()` with `ConsoleKey.Enter` → `SubmitCurrentInput()`
2. Input validation: not empty, state is `ChatState.Input`
3. `AddToCommandHistory()` stores the command
4. `ClearCurrentInput()` resets input context
5. `ProcessUserInput()` begins

__Message Processing:__

1. Environment context generated via `EnvSystemPrompt.GetEnvPrompt()`
2. Full message created with system environment appended
3. `ChatMessage(ChatRole.User, fullUserMessage)` added to history
4. Display shows only original user input (stripped of env context)
5. Static content updated with user message rendering

__AI Processing Begins:__

1. `_currentState = ChatState.Thinking`
2. `_aiOperationStartTime = DateTime.Now`
3. Dynamic content switches to `CreateFlexThinkingComponent()`
4. Shows animated dots "..." with duration counter and "esc to cancel"
5. `_appService.ProcessChatMessageAsync()` called with chat history

### AI Response Stream Processing

__Tool Execution 1: Create Directory__

1. AI decides to use `execute_command` tool
2. `IsToolExecutionUpdate()` detects `FunctionCallContent`
3. `_currentState = ChatState.ToolExecution`
4. `ExtractToolNameFromUpdate()` sets `_currentToolName = "execute_command → mkdir..."`
5. Dynamic content switches to `CreateFlexToolExecutionComponent()`
6. Shows animated dots with "Executing execute_command → mkdir..."
7. `HandleToolExecutionResult()` processes `FunctionResultContent`
8. `ToolExecutionDisplay.CreateToolDisplay()` renders tool result
9. Static content shows tool execution with status and output

__Tool Execution 2: Write Python File__

1. AI uses `write_file` tool
2. Same flow as above but `_currentToolName = "write_file → hello.py"`
3. `IsWriteFileTool()` detected in `HandleToolExecutionResult()`
4. File content extracted and displayed
5. No diff generated for write operations

__Tool Execution 3: Execute Python Script__

1. AI uses `execute_command` again
2. `_currentToolName = "execute_command → python..."`
3. Command output "Hello, World!" captured and displayed

__Tool Execution 4: Edit Python File__

1. AI uses `replace_in_file` tool (EditTool)
2. `CapturePreEditContentForEditTool()` reads original file content
3. `_functionCallToPreEditContent[callId]` stores original content
4. `IsEditTool()` detected in `HandleToolExecutionResult()`
5. Diff generated between original and new content using `UnifiedDiffGenerator`
6. `DiffRenderer` creates visual diff display with +/- lines
7. Static content shows diff with syntax highlighting

__Tool Execution 5: Run Modified Script__

1. Final `execute_command` to run updated script
2. Output "Hello, Universe!" captured and displayed

### Final State

1. `_currentState` returns to `ChatState.Input`
2. `_toolProgress` and `_currentToolName` cleared
3. Dynamic content returns to `CreateFlexInputComponent()`
4. All tool executions and responses stored in chat history
5. Ready for next user input

### Key UI State Transitions

```javascript
Input → Thinking → ToolExecution → ToolExecution → ... → Input
  ↓         ↓            ↓              ↓                    ↓
Normal   Animated    Tool Progress   Tool Progress      Normal
Input    Dots        Display         Display            Input
```

### Dynamic Content Rendering Cycle

- `RenderDynamicContent()` called continuously by `_scrollbackTerminal.StartDynamicDisplayAsync()`
- Content switches based on `_currentState` and `_inputContext.State`
- Each state has dedicated rendering method
- Animation frames calculated using `DateTime.Now.Millisecond` </sim_results>

### 1. State Pattern Implementation

The TUI uses the State Pattern with three ITuiState implementations:

- `InputTuiState` - User input mode with autocomplete/selection support
- `ThinkingTuiState` - AI processing mode with cancellation
- `ToolExecutionTuiState` - Tool execution mode with progress display

Each state handles its own rendering and input through ITuiState interface.

### 2. Component-Mediated Architecture

The system uses coordinated component interactions:

- Keyboard events → TuiStateManager → Current ITuiState
- AI response streams → FlexColumnMediator → Component updates
- Tool execution → ProgressPanel + ToolExecutionDisplay
- State transitions → Component visibility management
- All coordination flows through ITuiMediator pattern

### 3. Separation of Static vs Dynamic Content

Clear separation between:

- __Static Content__: Chat history, tool results, messages (written once, persistent)
- __Dynamic Content__: Input area, progress indicators, animations (continuously updated)

### 4. Streaming Response Handling

The AI response processing follows a consistent pattern:

1. Detect response type (`IsToolExecutionUpdate()`)
2. Extract relevant information (`ExtractToolNameFromUpdate()`)
3. Update UI state accordingly
4. Process and display results (`HandleToolExecutionResult()`)

### 5. Tool Execution Lifecycle

Consistent tool execution pattern:

1. Function call detection → Progress display
2. Pre-execution capture (for edit tools)
3. Execution monitoring with progress updates
4. Result processing and specialized display
5. State cleanup and return to input

### 6. Context and Component Management

The `ITuiContext` serves as a unified service container while `TuiComponentManager` handles:

- Component lifecycle and visibility
- Layout composition via FlexColumnLayout
- Input event broadcasting to components
- Render context creation and coordination

### 7. Component Rendering Pattern

Modular rendering through specialized components:

- InputPanel handles input display with cursor positioning
- AutocompletePanel/UserSelectionPanel overlay when active
- ProgressPanel adapts content based on operation type
- ToolExecutionDisplay provides specialized tool result formatting
- All components receive IRenderContext for consistent styling

### 8. Error Handling and Cancellation

Robust cancellation support:

- Escape key cancels operations at any state
- Cancellation tokens properly propagated
- Graceful state recovery after interruption

> -- END OF DOC --
