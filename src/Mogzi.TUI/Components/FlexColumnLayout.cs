namespace Mogzi.TUI.Components;

/// <summary>
/// Main application layout that arranges components in a flexible column structure.
/// Handles dynamic content area management and supports responsive layout adjustments.
/// </summary>
public class FlexColumnLayout : ITuiLayout
{
    public string Name => "FlexColumnLayout";

    public IEnumerable<string> GetRequiredComponents()
    {
        return
        [
            "InputPanel",
            "AutocompletePanel",
            "UserSelectionPanel",
            "ProgressPanel",
            "FooterPanel",
            "WelcomePanel"
        ];
    }

    public IRenderable Compose(IReadOnlyDictionary<string, ITuiComponent> components, IRenderContext context)
    {
        var currentState = context.CurrentState;

        // Determine which components to show based on current state and input context
        var contentComponents = new List<IRenderable>
        {
            // Add spacing at the top
            new Text("")
        };

        // Show welcome panel if no chat history
        var chatHistory = context.TuiContext.HistoryManager.GetCurrentChatHistory();
        if (!chatHistory.Any())
        {
            if (components.TryGetValue("WelcomePanel", out var welcomePanel) && welcomePanel.IsVisible)
            {
                contentComponents.Add(welcomePanel.Render(context));
            }
        }

        // Show progress panel for thinking and tool execution states
        if (currentState is ChatState.Thinking or ChatState.ToolExecution)
        {
            if (components.TryGetValue("ProgressPanel", out var progressPanel) && progressPanel.IsVisible)
            {
                contentComponents.Add(progressPanel.Render(context));
            }
        }

        // Show input panel for input state
        if (currentState == ChatState.Input)
        {
            // Determine which input-related panels to show
            var inputPanel = GetInputPanelContent(components, context);
            if (inputPanel != null)
            {
                contentComponents.Add(inputPanel);
            }
            else
            {
                context.Logger.LogWarning("FlexColumnLayout: Input panel content is null");
            }
        }

        // Add spacing before footer
        contentComponents.Add(new Text(""));

        // Always show footer panel
        if (components.TryGetValue("FooterPanel", out var footerPanel) && footerPanel.IsVisible)
        {
            contentComponents.Add(footerPanel.Render(context));
        }

        return new Rows(contentComponents);
    }

    public bool ValidateComponents(IReadOnlyDictionary<string, ITuiComponent> availableComponents)
    {
        var requiredComponents = GetRequiredComponents();
        return requiredComponents.All(availableComponents.ContainsKey);
    }

    private IRenderable? GetInputPanelContent(IReadOnlyDictionary<string, ITuiComponent> components, IRenderContext context)
    {
        var inputContext = context.TuiContext.InputContext;
        var inputComponents = new List<IRenderable>();


        // Always show the main input panel
        if (components.TryGetValue("InputPanel", out var inputPanel) && inputPanel.IsVisible)
        {
            var renderedInput = inputPanel.Render(context);
            inputComponents.Add(renderedInput);
        }
        else
        {
            context.Logger.LogWarning("GetInputPanelContent: InputPanel not found or not visible. Found: {Found}, Visible: {Visible}",
                components.ContainsKey("InputPanel"), inputPanel?.IsVisible);
        }

        // Show additional panels based on input state
#pragma warning disable IDE0010 // Add missing cases
        switch (inputContext.State)
        {
            case InputState.UserSelection:
                if (components.TryGetValue("UserSelectionPanel", out var userSelectionPanel) && userSelectionPanel.IsVisible)
                {
                    var selectionContent = userSelectionPanel.Render(context);
                    if (selectionContent is not Text text || !string.IsNullOrEmpty(text.ToString()))
                    {
                        inputComponents.Add(selectionContent);
                    }
                }
                break;

            case InputState.Autocomplete when inputContext.ShowSuggestions:
                if (components.TryGetValue("AutocompletePanel", out var autocompletePanel) && autocompletePanel.IsVisible)
                {
                    var autocompleteContent = autocompletePanel.Render(context);
                    if (autocompleteContent is not Text text || !string.IsNullOrEmpty(text.ToString()))
                    {
                        inputComponents.Add(autocompleteContent);
                    }
                }
                break;
        }
#pragma warning restore IDE0010 // Add missing cases

        return inputComponents.Count > 0 ? new Rows(inputComponents) : null;
    }
}
