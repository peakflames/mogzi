# UI Test Case Specification

## 1. Introduction

This document defines comprehensive test cases for the MaxBot Component-Based User Interface (UI). The test cases are organized by component and functionality to ensure thorough validation of all UI features and requirements.

## 2. Test Strategy Overview

### 2.1. Test Categories

**Unit Tests:** Component-level testing with isolated dependencies
**Integration Tests:** Component interaction and service integration testing
**Performance Tests:** Rendering performance, memory usage, and responsiveness
**User Acceptance Tests:** End-to-end workflow validation
**Accessibility Tests:** Keyboard navigation and visual accessibility

### 2.2. Test Environment

**Target Platforms:**
- Windows 10/11 (PowerShell, Command Prompt, Windows Terminal)
- macOS (Terminal.app, iTerm2)
- Linux (GNOME Terminal, Konsole, xterm)

**Terminal Sizes:**
- Minimum: 80x24 columns
- Standard: 120x30 columns
- Large: 200x50 columns

## 3. Core Infrastructure Tests

### 3.1. TuiApp Component Tests

#### TC-UI-APP-001: Application Initialization
**Objective:** Verify TuiApp initializes correctly with all dependencies
**Prerequisites:** Clean system state, valid configuration
**Test Steps:**
1. Launch TuiApp with default configuration
2. Verify dependency injection container is configured
3. Verify all required services are registered
4. Verify MaxBot core service connection
**Expected Results:**
- Application starts within 500ms
- No initialization errors logged
- All services are available and functional
- UI displays welcome screen

#### TC-UI-APP-002: Graceful Shutdown
**Objective:** Verify application shuts down cleanly
**Prerequisites:** Running TuiApp instance
**Test Steps:**
1. Send SIGTERM signal to application
2. Verify shutdown sequence initiates
3. Verify resources are cleaned up
4. Verify user preferences are persisted
**Expected Results:**
- Shutdown completes within 2 seconds
- No resource leaks detected
- Configuration saved successfully
- Exit code is 0

#### TC-UI-APP-003: Error Handling
**Objective:** Verify application handles unhandled exceptions gracefully
**Prerequisites:** Running TuiApp instance
**Test Steps:**
1. Trigger unhandled exception in component
2. Verify error boundary catches exception
3. Verify error message is displayed to user
4. Verify application continues running
**Expected Results:**
- Exception is caught and logged
- User sees meaningful error message
- Application remains stable
- Component can recover or degrade gracefully

### 3.2. Component System Tests

#### TC-UI-COMP-001: Component Registration
**Objective:** Verify components are registered and resolved correctly
**Prerequisites:** Initialized dependency injection container
**Test Steps:**
1. Register test component with container
2. Resolve component instance
3. Verify component dependencies are injected
4. Verify component lifecycle methods are called
**Expected Results:**
- Component resolves successfully
- Dependencies are properly injected
- Lifecycle methods execute in correct order
- Component is ready for rendering

#### TC-UI-COMP-002: State Management
**Objective:** Verify TuiState<T> provides reactive state management
**Prerequisites:** Component with TuiState<T> usage
**Test Steps:**
1. Create TuiState<string> with initial value
2. Subscribe to PropertyChanged events
3. Update state value
4. Verify event is fired
5. Verify StateManager is notified
**Expected Results:**
- PropertyChanged event fires on value change
- StateManager receives notification
- UI re-render is scheduled
- State change is reflected in UI

#### TC-UI-COMP-003: Component Rendering
**Objective:** Verify components render correctly to IRenderable
**Prerequisites:** Test component with known output
**Test Steps:**
1. Create test component with fixed content
2. Call RenderAsync with test context
3. Verify returned IRenderable matches expected output
4. Verify rendering completes within performance target
**Expected Results:**
- IRenderable output matches expected content
- Rendering completes within 16ms
- No exceptions during rendering
- Output is valid Spectre.Console object

## 4. Layout Component Tests

### 4.1. AppComponent Tests

#### TC-UI-APPCOMP-001: Layout Composition
**Objective:** Verify AppComponent composes child components correctly
**Prerequisites:** All child components available
**Test Steps:**
1. Initialize AppComponent with all child components
2. Call RenderAsync with full terminal context
3. Verify all child components are rendered
4. Verify layout structure is correct
**Expected Results:**
- All child components appear in output
- Layout follows specified structure (header, history, dynamic, input, footer)
- Height distribution is appropriate
- No layout overlaps or gaps

#### TC-UI-APPCOMP-002: Terminal Resize Handling
**Objective:** Verify AppComponent handles terminal resize events
**Prerequisites:** Running AppComponent instance
**Test Steps:**
1. Render AppComponent at 120x30 terminal size
2. Trigger terminal resize to 80x24
3. Verify layout adapts to new size
4. Trigger resize to 200x50
5. Verify layout utilizes additional space
**Expected Results:**
- Layout adapts smoothly to size changes
- No visual artifacts during resize
- Content remains readable at all sizes
- Performance remains within targets

#### TC-UI-APPCOMP-003: User Input Processing
**Objective:** Verify AppComponent processes user input correctly
**Prerequisites:** AppComponent with MaxBot service integration
**Test Steps:**
1. Submit user input through InputComponent
2. Verify input is added to history
3. Verify MaxBot service is called
4. Verify streaming response is handled
5. Verify final response is added to history
**Expected Results:**
- User input appears in history immediately
- Processing indicator is shown
- Streaming content updates in real-time
- Final response is properly formatted
- Processing indicator is cleared

### 4.2. HeaderComponent Tests

#### TC-UI-HEADER-001: Status Display
**Objective:** Verify HeaderComponent displays status information correctly
**Prerequisites:** HeaderComponent with status data
**Test Steps:**
1. Set application status to "Processing"
2. Render HeaderComponent
3. Verify status is displayed
4. Update status to "Error"
5. Verify status update is reflected
**Expected Results:**
- Status information is clearly visible
- Status updates appear within 100ms
- Visual indicators match status type
- Status text is readable and informative

#### TC-UI-HEADER-002: Responsive Design
**Objective:** Verify HeaderComponent adapts to different terminal widths
**Prerequisites:** HeaderComponent with full status information
**Test Steps:**
1. Render at 200-column width
2. Verify all information is displayed
3. Render at 120-column width
4. Verify important information is preserved
5. Render at 80-column width
6. Verify critical information remains visible
**Expected Results:**
- All information visible at wide widths
- Graceful truncation at medium widths
- Critical information preserved at narrow widths
- No text overflow or wrapping issues

### 4.3. StaticHistoryComponent Tests

#### TC-UI-HISTORY-001: Content Display
**Objective:** Verify StaticHistoryComponent displays conversation history
**Prerequisites:** HistoryManager with test conversation data
**Test Steps:**
1. Add multiple history items of different types
2. Render StaticHistoryComponent
3. Verify all items are displayed in chronological order
4. Verify formatting is preserved
5. Verify visual hierarchy is clear
**Expected Results:**
- All history items are visible
- Chronological order is maintained
- Message types are visually distinct
- Formatting (markdown, code) is preserved
- Visual hierarchy is clear and consistent

#### TC-UI-HISTORY-002: Scrolling Behavior
**Objective:** Verify scrolling works correctly with large histories
**Prerequisites:** HistoryManager with 100+ history items
**Test Steps:**
1. Render component with limited height
2. Verify scrolling indicators are shown
3. Scroll to top of history
4. Verify oldest items are visible
5. Scroll to bottom
6. Verify newest items are visible
**Expected Results:**
- Scrolling indicators are clear
- Scroll position is maintained accurately
- Content scrolls smoothly
- Performance remains responsive
- Scroll position persists during updates

#### TC-UI-HISTORY-003: Performance with Large Histories
**Objective:** Verify performance remains acceptable with large conversation histories
**Prerequisites:** HistoryManager with 1000+ history items
**Test Steps:**
1. Load 1000 history items
2. Measure initial render time
3. Scroll through history
4. Measure scroll performance
5. Add new items
6. Measure update performance
**Expected Results:**
- Initial render completes within 100ms
- Scrolling maintains 60 FPS
- New item addition doesn't affect performance
- Memory usage remains stable
- No performance degradation over time

### 4.4. DynamicContentComponent Tests

#### TC-UI-DYNAMIC-001: Real-time Updates
**Objective:** Verify DynamicContentComponent handles real-time updates
**Prerequisites:** DynamicContentComponent with streaming content
**Test Steps:**
1. Start streaming content update
2. Verify content appears immediately
3. Continue streaming updates
4. Verify smooth content updates
5. Complete streaming
6. Verify final content is correct
**Expected Results:**
- Content updates appear within 50ms
- No flickering during updates
- Smooth animation of content changes
- Final content matches expected output
- Performance remains stable during streaming

#### TC-UI-DYNAMIC-002: Multiple Concurrent Operations
**Objective:** Verify handling of multiple concurrent operations
**Prerequisites:** Multiple tool executions running simultaneously
**Test Steps:**
1. Start 3 concurrent tool executions
2. Verify all operations are displayed
3. Update status of individual tools
4. Verify updates are reflected correctly
5. Complete operations in different order
6. Verify completion handling is correct
**Expected Results:**
- All operations are clearly visible
- Individual status updates are accurate
- No interference between operations
- Completion order doesn't affect display
- Visual organization remains clear

### 4.5. InputComponent Tests

#### TC-UI-INPUT-001: Text Input Handling
**Objective:** Verify InputComponent handles text input correctly
**Prerequisites:** InputComponent in active state
**Test Steps:**
1. Type single-line text input
2. Verify text appears immediately
3. Type multi-line text input
4. Verify word wrapping works correctly
5. Paste large text block
6. Verify paste handling is correct
**Expected Results:**
- Text appears within 50ms of typing
- Multi-line input wraps correctly
- Large paste operations complete successfully
- Input validation provides immediate feedback
- Special characters are handled correctly

#### TC-UI-INPUT-002: Command History Navigation
**Objective:** Verify command history navigation works correctly
**Prerequisites:** InputComponent with command history
**Test Steps:**
1. Submit several commands
2. Press Up arrow key
3. Verify previous command is recalled
4. Press Up arrow multiple times
5. Verify navigation through history
6. Press Down arrow
7. Verify forward navigation works
**Expected Results:**
- Previous commands are recalled accurately
- Navigation is smooth and responsive
- History order is maintained correctly
- Edge cases (beginning/end) are handled
- Current input is preserved when navigating

#### TC-UI-INPUT-003: State Management
**Objective:** Verify InputComponent state changes work correctly
**Prerequisites:** InputComponent with state management
**Test Steps:**
1. Verify input is enabled in idle state
2. Start processing operation
3. Verify input is disabled
4. Complete processing
5. Verify input is re-enabled
6. Trigger confirmation prompt
7. Verify confirmation state is handled
**Expected Results:**
- Input state changes are immediate
- Visual feedback is clear
- State transitions are smooth
- User cannot input during disabled state
- Confirmation prompts work correctly

## 5. Content Component Tests

### 5.1. HistoryItemComponent Tests

#### TC-UI-HISTITEM-001: Message Type Rendering
**Objective:** Verify different message types render with distinct styling
**Prerequisites:** HistoryItemComponent with various message types
**Test Steps:**
1. Render UserHistoryItem
2. Verify user message styling
3. Render GeminiHistoryItem
4. Verify AI response styling
5. Render InfoHistoryItem
6. Verify system message styling
**Expected Results:**
- Each message type has distinct visual styling
- User messages are clearly attributed
- AI responses are properly formatted
- System messages are visually distinct
- Styling is consistent across instances

#### TC-UI-HISTITEM-002: Content Formatting
**Objective:** Verify rich content formatting works correctly
**Prerequisites:** HistoryItemComponent with formatted content
**Test Steps:**
1. Render message with markdown content
2. Verify markdown is rendered correctly
3. Render message with code blocks
4. Verify syntax highlighting works
5. Render message with tables
6. Verify table formatting is preserved
**Expected Results:**
- Markdown renders with proper formatting
- Code blocks have syntax highlighting
- Tables are properly aligned
- Lists and other structures are formatted
- Formatting is consistent and readable

#### TC-UI-HISTITEM-003: Interactive Features
**Objective:** Verify interactive features work correctly
**Prerequisites:** HistoryItemComponent with long content
**Test Steps:**
1. Render long message that exceeds height limit
2. Verify content is truncated with expand option
3. Trigger expand action
4. Verify full content is displayed
5. Trigger collapse action
6. Verify content returns to truncated state
**Expected Results:**
- Long content is appropriately truncated
- Expand/collapse controls are clear
- Transitions are smooth
- Full content is accessible
- Performance remains good with large content

### 5.2. ToolGroupComponent Tests

#### TC-UI-TOOLGROUP-001: Group Visualization
**Objective:** Verify tool groups are displayed with clear visual boundaries
**Prerequisites:** ToolGroupComponent with multiple tools
**Test Steps:**
1. Render tool group with 3 tools
2. Verify group container is visible
3. Verify group header shows tool count
4. Verify individual tools are contained within group
5. Verify group status is indicated
**Expected Results:**
- Group container has clear visual boundaries
- Group header is informative
- Individual tools are properly contained
- Group status is clearly indicated
- Visual hierarchy is maintained

#### TC-UI-TOOLGROUP-002: Status Tracking
**Objective:** Verify tool status updates are reflected in real-time
**Prerequisites:** ToolGroupComponent with tools in various states
**Test Steps:**
1. Start with all tools in pending state
2. Update first tool to executing
3. Verify status change is reflected
4. Update tool to success
5. Verify completion is shown
6. Update another tool to error
7. Verify error state is displayed
**Expected Results:**
- Status changes appear within 100ms
- Visual indicators are clear and distinct
- Group status reflects individual tool states
- Error states are prominently displayed
- Status transitions are smooth

#### TC-UI-TOOLGROUP-003: Interactive Features
**Objective:** Verify tool confirmation and interaction features
**Prerequisites:** ToolGroupComponent with tools requiring confirmation
**Test Steps:**
1. Trigger tool requiring confirmation
2. Verify confirmation prompt is displayed
3. Confirm tool execution
4. Verify tool proceeds to execution
5. Trigger tool cancellation
6. Verify tool is marked as cancelled
**Expected Results:**
- Confirmation prompts are clear and actionable
- User choices are processed correctly
- Tool state reflects user decisions
- Cancellation is handled gracefully
- Interactive elements are responsive

### 5.3. ToolComponent Tests

#### TC-UI-TOOL-001: Status Visualization
**Objective:** Verify individual tool status is displayed clearly
**Prerequisites:** ToolComponent with various status states
**Test Steps:**
1. Render tool in pending state
2. Verify pending indicator is shown
3. Update to executing state
4. Verify executing animation/indicator
5. Update to success state
6. Verify success indicator
7. Update to error state
8. Verify error indicator and message
**Expected Results:**
- Each status has distinct visual indicator
- Status transitions are smooth
- Executing state shows activity
- Success state is clearly positive
- Error state is prominently displayed

#### TC-UI-TOOL-002: Output Display
**Objective:** Verify tool output is displayed with appropriate formatting
**Prerequisites:** ToolComponent with various output types
**Test Steps:**
1. Display tool with text output
2. Verify text is properly formatted
3. Display tool with structured data output
4. Verify structure is preserved
5. Display tool with code output
6. Verify syntax highlighting is applied
**Expected Results:**
- Text output is readable and formatted
- Structured data maintains organization
- Code output has appropriate highlighting
- Long output is handled with truncation
- Output formatting is consistent

#### TC-UI-TOOL-003: Interactive Features
**Objective:** Verify tool interaction features work correctly
**Prerequisites:** ToolComponent with interactive capabilities
**Test Steps:**
1. Display tool with long output
2. Verify output is truncated with expand option
3. Expand output
4. Verify full output is displayed
5. Copy output to clipboard
6. Verify copy operation succeeds
**Expected Results:**
- Output truncation is appropriate
- Expand/collapse works smoothly
- Copy operation completes successfully
- Interactive elements are discoverable
- Performance remains good with large output

## 6. Performance Tests

### 6.1. Rendering Performance Tests

#### TC-UI-PERF-001: Startup Performance
**Objective:** Verify application startup meets performance targets
**Prerequisites:** Clean system state
**Test Steps:**
1. Measure time from launch to ready state
2. Repeat test 10 times
3. Calculate average startup time
4. Verify no performance regression
**Expected Results:**
- Average startup time < 500ms
- 95th percentile < 750ms
- No memory leaks during startup
- Consistent performance across runs

#### TC-UI-PERF-002: Rendering Latency
**Objective:** Verify rendering performance meets 60 FPS target
**Prerequisites:** UI with moderate content load
**Test Steps:**
1. Measure frame rendering time over 1000 frames
2. Calculate average and 95th percentile
3. Verify no frames exceed 16ms target
4. Test with various content types
**Expected Results:**
- Average frame time < 8ms
- 95th percentile < 16ms
- No dropped frames during normal operation
- Consistent performance across content types

#### TC-UI-PERF-003: Memory Usage
**Objective:** Verify memory usage remains within acceptable limits
**Prerequisites:** Extended UI session
**Test Steps:**
1. Monitor memory usage during 1-hour session
2. Add 1000+ history items
3. Perform various UI operations
4. Verify no memory leaks
5. Measure peak memory usage
**Expected Results:**
- Memory usage < 100MB during normal operation
- No memory leaks detected
- Memory usage scales linearly with content
- Garbage collection doesn't impact performance

### 6.2. Responsiveness Tests

#### TC-UI-RESP-001: Input Responsiveness
**Objective:** Verify user input is processed within latency targets
**Prerequisites:** UI in ready state
**Test Steps:**
1. Measure keystroke to display latency
2. Test with various input types
3. Measure command submission latency
4. Test during high system load
**Expected Results:**
- Keystroke latency < 50ms
- Command submission < 100ms
- Performance maintained under load
- No input lag or dropped keystrokes

#### TC-UI-RESP-002: State Update Responsiveness
**Objective:** Verify UI state updates appear within targets
**Prerequisites:** UI with active operations
**Test Steps:**
1. Trigger state change
2. Measure time to visual update
3. Test with various state change types
4. Verify update consistency
**Expected Results:**
- State updates appear within 100ms
- Visual feedback is immediate
- No delayed or missed updates
- Consistent performance across state types

## 7. Integration Tests

### 7.1. MaxBot Service Integration Tests

#### TC-UI-INT-001: Chat Flow Integration
**Objective:** Verify complete chat flow works end-to-end
**Prerequisites:** UI connected to MaxBot services
**Test Steps:**
1. Submit user message
2. Verify message appears in history
3. Verify AI response is streamed
4. Verify final response is complete
5. Verify conversation state is maintained
**Expected Results:**
- Complete chat flow works without errors
- All messages are properly recorded
- Streaming works smoothly
- Conversation state is consistent
- Performance meets targets throughout

#### TC-UI-INT-002: Tool Execution Integration
**Objective:** Verify tool execution integration works correctly
**Prerequisites:** UI with tool execution capabilities
**Test Steps:**
1. Submit request requiring tool execution
2. Verify tool group is created
3. Verify individual tools execute
4. Verify tool output is displayed
5. Verify completion handling
**Expected Results:**
- Tool execution flow is seamless
- Tool status updates are accurate
- Tool output is properly formatted
- Error handling works correctly
- Performance remains acceptable

#### TC-UI-INT-003: Error Handling Integration
**Objective:** Verify error scenarios are handled gracefully
**Prerequisites:** UI with error simulation capabilities
**Test Steps:**
1. Simulate MaxBot service disconnection
2. Verify error indication is shown
3. Simulate tool execution failure
4. Verify error is displayed appropriately
5. Verify recovery when service returns
**Expected Results:**
- Service errors are clearly communicated
- Tool errors don't crash the UI
- Recovery is automatic when possible
- User is informed of error states
- Graceful degradation is maintained

## 8. User Acceptance Tests

### 8.1. Workflow Tests

#### TC-UI-UAT-001: Basic Chat Workflow
**Objective:** Verify basic chat workflow meets user expectations
**Prerequisites:** Clean UI state
**Test Steps:**
1. User starts application
2. User submits simple question
3. User reviews AI response
4. User submits follow-up question
5. User reviews conversation history
**Expected Results:**
- Workflow is intuitive and natural
- Responses are clearly formatted
- History is easy to navigate
- User can accomplish tasks efficiently

#### TC-UI-UAT-002: File Operation Workflow
**Objective:** Verify file operation workflow is user-friendly
**Prerequisites:** UI with file operation capabilities
**Test Steps:**
1. User requests file modification
2. User reviews proposed changes
3. User confirms tool execution
4. User reviews execution results
5. User verifies file changes
**Expected Results:**
- File operations are clearly presented
- Confirmation prompts are understandable
- Results are easy to interpret
- User maintains control throughout process

#### TC-UI-UAT-003: Error Recovery Workflow
**Objective:** Verify users can recover from error situations
**Prerequisites:** UI with error simulation
**Test Steps:**
1. User encounters tool execution error
2. User reads error message
3. User attempts recovery action
4. User continues with workflow
**Expected Results:**
- Error messages are helpful and actionable
- Recovery options are clear
- User can continue after errors
- Workflow remains productive

### 8.2. Usability Tests

#### TC-UI-USAB-001: Discoverability
**Objective:** Verify UI features are discoverable
**Prerequisites:** New user scenario
**Test Steps:**
1. Present UI to new user
2. Ask user to perform common tasks
3. Observe user behavior
4. Note any confusion or difficulty
**Expected Results:**
- Common features are easily discovered
- UI provides helpful guidance
- User can accomplish tasks without training
- Interface is self-explanatory

#### TC-UI-USAB-002: Efficiency
**Objective:** Verify experienced users can work efficiently
**Prerequisites:** User familiar with UI
**Test Steps:**
1. Time user performing routine tasks
2. Measure keystrokes and actions required
3. Compare to baseline expectations
4. Identify efficiency improvements
**Expected Results:**
- Routine tasks can be completed quickly
- Keyboard shortcuts are effective
- UI doesn't impede user productivity
- Workflow is optimized for common use cases

## 9. Accessibility Tests

### 9.1. Keyboard Navigation Tests

#### TC-UI-ACC-001: Keyboard Navigation
**Objective:** Verify all UI functions are accessible via keyboard
**Prerequisites:** UI without mouse input
**Test Steps:**
1. Navigate through all UI components using Tab
2. Activate controls using Enter/Space
3. Navigate history using Page Up/Down
4. Test all keyboard shortcuts
**Expected Results:**
- All components are keyboard accessible
- Navigation order is logical
- Keyboard shortcuts work consistently
- No functionality requires mouse

#### TC-UI-ACC-002: Focus Management
**Objective:** Verify focus management is appropriate
**Prerequisites:** UI with multiple interactive elements
**Test Steps:**
1. Tab through interactive elements
2. Verify focus indicators are visible
3. Test focus trapping in modal dialogs
4. Verify focus restoration after operations
**Expected Results:**
- Focus indicators are clearly visible
- Focus order is logical and predictable
- Focus is properly managed in all contexts
- Users always know where focus is

### 9.2. Visual Accessibility Tests

#### TC-UI-VIS-001: Color Accessibility
**Objective:** Verify UI works for colorblind users
**Prerequisites:** UI with color-based information
**Test Steps:**
1. Test UI with colorblind simulation
2. Verify information is conveyed beyond color
3. Test high contrast mode
4. Verify readability is maintained
**Expected Results:**
- Information is accessible without color
- High contrast mode is functional
- Text remains readable in all modes
- Status indicators work for colorblind users

#### TC-UI-VIS-002: Typography and Layout
**Objective:** Verify text is readable and layout is clear
**Prerequisites:** UI with various text content
**Test Steps:**
1. Verify text contrast meets standards
2. Test readability at various terminal sizes
3. Verify layout clarity and organization
4. Test with different font settings
**Expected Results:**
- Text contrast meets accessibility standards
- Content is readable at all supported sizes
- Layout is clear and well-organized
- UI adapts to different font settings

## 10. Test Automation

### 10.1. Automated Test Framework

```csharp
[TestFixture]
public class ComponentTestBase
{
    protected IServiceProvider ServiceProvider { get; private set; }
    protected TestConsole TestConsole { get; private set; }
    
    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        ConfigureTestServices(services);
        ServiceProvider = services.BuildServiceProvider();
        TestConsole = new TestConsole();
    }
    
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
        services.AddSingleton<IAnsiConsole>(TestConsole);
        services.AddSingleton<StateManager>();
        services.AddSingleton<HistoryManager>();
    }
}

[TestFixture]
public class ToolGroupComponentTests : ComponentTestBase
{
    [Test]
    public async Task RenderAsync_WithMultipleTools_DisplaysAllTools()
    {
        // Arrange
        var tools = CreateTestTools(3);
        var component = new ToolGroupComponent(tools);
        var context = CreateTestRenderContext();
        
        // Act
        var result = await component.RenderAsync(context);
        
        // Assert
        Assert.That(result, Is.InstanceOf<Panel>());
        var panel = (Panel)result;
        Assert.That(panel.Header.Text, Contains.Substring("3 tools"));
    }
}
```

### 10.2. Performance Test Framework

```csharp
[TestFixture]
public class PerformanceTests
{
    [Test]
    [Performance(MaxExecutionTime = 500)] // milliseconds
    public async Task ApplicationStartup_CompletesWithinTarget()
    {
        var stopwatch = Stopwatch.StartNew();
        var app = new TuiApp();
        await app.InitializeAsync();
        stopwatch.Stop();
        
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(500));
    }
    
    [Test]
    [Performance(MaxMemoryUsage = 100 * 1024 * 1024)] // 100MB
    public async Task ExtendedSession_StaysWithinMemoryLimits()
    {
        var app = CreateTestApp();
        await SimulateExtendedSession(app, TimeSpan.FromMinutes(30));
        
        var memoryUsage = GC.GetTotalMemory(true);
        Assert.That(memoryUsage, Is.LessThan(100 * 1024 * 1024));
    }
}
```

---

*These comprehensive test cases ensure the MaxBot UI meets all functional, performance, and accessibility requirements while providing a robust foundation for continuous quality assurance.*
