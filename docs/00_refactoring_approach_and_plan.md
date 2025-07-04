## Recommended Refactoring Approach: Hybrid State + Mediator + Component Pattern

### Why This Combination?

For a TUI application like Mogzi, I recommend a __hybrid approach__ combining:

1. __State Pattern__ for core application states (Input, Thinking, ToolExecution)
2. __Mediator Pattern__ for coordinating between components and managing complex interactions
3. __Component Pattern__ for UI modularity and reusability
4. __Strategy Pattern__ for rendering different UI contexts

This combination is ideal for TUI applications because:

- __State Pattern__ naturally maps to the distinct UI modes
- __Mediator__ reduces coupling between the many interacting components
- __Component Pattern__ enables modular UI composition
- __Strategy Pattern__ handles the varied rendering requirements

### High-Level Phased Refactoring Plan

#### Phase 1: Foundation and State Extraction (2-3 weeks)

__Goal__: Extract state management and establish core interfaces

__Steps:__

1. __Create Core Interfaces__

   ```csharp
   // Core state interface
   public interface ITuiState
   {
       string Name { get; }
       IRenderable RenderDynamicContent(ITuiContext context);
       Task<bool> HandleKeyPress(KeyPressEventArgs e, ITuiContext context);
       Task<bool> HandleCharacterTyped(CharacterTypedEventArgs e, ITuiContext context);
       Task OnEnterAsync(ITuiContext context);
       Task OnExitAsync(ITuiContext context);
   }

   // Context interface for state access
   public interface ITuiContext
   {
       InputContext InputContext { get; }
       IServiceProvider ServiceProvider { get; }
       ILogger Logger { get; }
       // Other shared state
   }
   ```

2. __Extract State Classes__

   - `InputTuiState` - handles normal input, autocomplete, user selection
   - `ThinkingTuiState` - handles AI processing state
   - `ToolExecutionTuiState` - handles tool execution display

3. __Create State Manager__

   ```csharp
   public interface ITuiStateManager
   {
       ITuiState CurrentState { get; }
       Task TransitionToAsync(string stateName);
       Task<bool> HandleKeyPressAsync(KeyPressEventArgs e);
       Task<bool> HandleCharacterTypedAsync(CharacterTypedEventArgs e);
   }
   ```

4. __Refactor FlexColumnTuiApp__

   - Remove state-specific switch statements
   - Delegate to state manager
   - Keep only coordination logic

__Deliverables:__

- Working state pattern implementation
- Reduced FlexColumnTuiApp complexity by ~40%
- All existing functionality preserved

#### Phase 2: Component Extraction and Mediator Introduction (3-4 weeks)

__Goal__: Break down monolithic rendering into components and introduce mediator

__Steps:__

1. __Create Component Interfaces__

   ```csharp
   public interface ITuiComponent
   {
       string Name { get; }
       IRenderable Render(IRenderContext context);
       Task<bool> HandleInputAsync(InputEvent inputEvent);
       bool IsVisible { get; set; }
   }

   public interface ITuiPanel : ITuiComponent
   {
       void AddChild(ITuiComponent child);
       void RemoveChild(ITuiComponent child);
       IReadOnlyList<ITuiComponent> Children { get; }
   }
   ```

2. __Extract UI Components__

   - `InputPanel` - input area with cursor management
   - `AutocompletePanel` - suggestion display
   - `UserSelectionPanel` - interactive command selection
   - `ProgressPanel` - thinking/tool execution indicators
   - `FooterPanel` - status information
   - `WelcomePanel` - welcome message display

3. __Create Mediator__

   ```csharp
   public interface ITuiMediator
   {
       Task HandleUserInputAsync(string input);
       Task HandleKeyPressAsync(ConsoleKey key, ConsoleModifiers modifiers);
       Task HandleStateChangeAsync(string newState);
       Task HandleToolExecutionAsync(ToolExecutionEvent evt);
       Task NotifyComponentAsync(string componentName, object message);
   }
   ```

4. __Implement Component Manager__

   ```csharp
   public interface ITuiComponentManager
   {
       void RegisterComponent(ITuiComponent component);
       ITuiComponent GetComponent(string name);
       IRenderable RenderLayout(string layoutName);
       Task BroadcastInputAsync(InputEvent inputEvent);
   }
   ```

__Deliverables:__

- Modular component system
- Mediator coordinating all interactions
- Reduced coupling between UI elements
- FlexColumnTuiApp becomes primarily a coordinator

#### Phase 3: Rendering Strategy and Performance Optimization (2-3 weeks)

__Goal__: Optimize rendering and introduce strategy pattern for different contexts

__Steps:__

1. __Create Rendering Strategies__

   ```csharp
   public interface IRenderingStrategy
   {
       string Name { get; }
       IRenderable Render(IRenderContext context);
       bool CanRender(IRenderContext context);
   }

   // Strategies for different contexts
   public class NormalInputRenderingStrategy : IRenderingStrategy
   public class AutocompleteRenderingStrategy : IRenderingStrategy
   public class ToolProgressRenderingStrategy : IRenderingStrategy
   ```

2. __Implement Dirty Checking__

   ```csharp
   public interface IRenderCache
   {
       bool IsDirty(string componentName);
       void MarkDirty(string componentName);
       void MarkClean(string componentName);
       IRenderable GetCachedRender(string componentName);
       void CacheRender(string componentName, IRenderable renderable);
   }
   ```

3. __Optimize Animation System__

   - Extract animation logic into dedicated service
   - Implement frame-rate limiting
   - Add animation state management

4. __Performance Monitoring__

   - Add rendering performance metrics
   - Implement component render timing
   - Memory usage tracking

__Deliverables:__

- Optimized rendering performance
- Flexible rendering strategy system
- Reduced memory allocations
- Better animation control

#### Phase 4: Advanced Features and Polish (2-3 weeks)

__Goal__: Add advanced features and polish the architecture

__Steps:__

1. __Enhanced Input System__

   ```csharp
   public interface IInputProcessor
   {
       Task<InputResult> ProcessAsync(InputEvent inputEvent);
       void RegisterHandler(IInputHandler handler);
       void UnregisterHandler(IInputHandler handler);
   }
   ```

2. __Plugin Architecture__

   ```csharp
   public interface ITuiPlugin
   {
       string Name { get; }
       void Initialize(ITuiContext context);
       void RegisterComponents(ITuiComponentManager componentManager);
       void RegisterStates(ITuiStateManager stateManager);
   }
   ```

3. __Theme System__

   ```csharp
   public interface ITuiTheme
   {
       Color GetColor(string colorName);
       Style GetStyle(string styleName);
       string GetTemplate(string templateName);
   }
   ```

4. __Configuration System__

   - Externalize UI configuration
   - Runtime theme switching
   - Customizable key bindings

__Deliverables:__

- Plugin-ready architecture
- Themeable UI system
- Configurable behavior
- Production-ready codebase

### Implementation Guidelines

#### Dependency Injection Strategy

```csharp
// Service registration for new architecture
services.AddSingleton<ITuiStateManager, TuiStateManager>();
services.AddSingleton<ITuiMediator, FlexColumnMediator>();
services.AddSingleton<ITuiComponentManager, TuiComponentManager>();
services.AddSingleton<IRenderingEngine, SpectreTuiRenderingEngine>();

// Register states
services.AddTransient<ITuiState, InputTuiState>();
services.AddTransient<ITuiState, ThinkingTuiState>();
services.AddTransient<ITuiState, ToolExecutionTuiState>();

// Register components
services.AddTransient<ITuiComponent, InputPanel>();
services.AddTransient<ITuiComponent, AutocompletePanel>();
services.AddTransient<ITuiComponent, ProgressPanel>();
services.AddTransient<ITuiComponent, FooterPanel>();
```

#### Testing Strategy

- __Unit Tests__: Each component and state in isolation
- __Integration Tests__: State transitions and component interactions
- __UI Tests__: End-to-end user interaction scenarios
- __Performance Tests__: Rendering performance and memory usage

#### Migration Strategy

- __Backward Compatibility__: Maintain existing public APIs during transition
- __Feature Flags__: Enable/disable new architecture components
- __Gradual Migration__: Migrate one state/component at a time
- __Rollback Plan__: Ability to revert to original implementation

### Expected Benefits

#### Immediate (Phase 1-2)

- __Reduced Complexity__: 60-70% reduction in FlexColumnTuiApp size
- __Better Testability__: Individual components can be unit tested
- __Improved Maintainability__: Clear separation of concerns

#### Medium-term (Phase 3-4)

- __Performance Improvements__: 20-30% reduction in rendering overhead
- __Enhanced Extensibility__: Easy to add new UI components and states
- __Better User Experience__: Smoother animations and interactions

#### Long-term

- __Plugin Ecosystem__: Third-party UI extensions
- __Theme Marketplace__: Community-contributed themes
- __Advanced Features__: Multi-pane layouts, custom widgets, etc.

### Risk Mitigation

- __Incremental Approach__: Each phase delivers working software
- __Comprehensive Testing__: Maintain test coverage throughout refactoring
- __Performance Monitoring__: Continuous performance regression testing
- __User Feedback__: Beta testing with power users during each phase

This phased approach ensures that the refactoring delivers value incrementally while maintaining the stability and functionality that makes Mogzi effective.
