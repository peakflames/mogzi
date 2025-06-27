using Xunit;
using UI.Components;
using Spectre.Console.Rendering;
using System.Reflection;
using System.Collections.Generic;

namespace UI.Tests;

public class AppComponentTests
{
    [Fact]
    public async Task AppComponent_RendersAllChildComponents()
    {
        // Arrange
        var header = new HeaderComponent();
        var staticHistory = new StaticHistoryComponent();
        var dynamicContent = new DynamicContentComponent();
        var input = new InputComponent();
        var footer = new FooterComponent();

        var appComponent = new AppComponent(header, staticHistory, dynamicContent, input, footer);
        var terminalSize = new TerminalSize(80, 24);
        var layoutConstraints = new LayoutConstraints(24, 80);
        var renderContext = new RenderContext(layoutConstraints, terminalSize, false);

        // Act
        var result = await appComponent.RenderAsync(renderContext);

        // Assert
        var rows = Assert.IsType<Rows>(result);
        var childrenField = typeof(Rows).GetField("_children", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(childrenField);
        var children = childrenField.GetValue(rows) as IReadOnlyList<IRenderable>;
        Assert.NotNull(children);
        Assert.Equal(5, children.Count);
    }
}
