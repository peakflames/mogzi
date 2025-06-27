using Spectre.Console;
using Spectre.Console.Rendering;
using System.Threading.Tasks;

namespace UI.Tests;

public class TuiComponentBaseTests
{
    private class TestComponent : TuiComponentBase
    {
        public TuiState<int> Counter => UseState(0);
        public int EffectRunCount { get; private set; }

        public void RunEffect(object[] deps)
        {
            UseEffect(() =>
            {
                EffectRunCount++;
                return Task.CompletedTask;
            }, deps);
        }

        public override Task<IRenderable> RenderAsync(RenderContext context)
        {
            return Task.FromResult<IRenderable>(new Text(Counter.Value.ToString()));
        }
    }

    [Fact]
    public void UseState_InitializesAndRetrievesState()
    {
        // Arrange
        var component = new TestComponent();

        // Act
        var counter = component.Counter;

        // Assert
        Assert.NotNull(counter);
        Assert.Equal(0, counter.Value);
    }

    [Fact]
    public void UseState_TriggersStateChangeNotification()
    {
        // Arrange
        var component = new TestComponent();
        var notificationReceived = false;
        var componentId = component.ComponentId;
        StateChangeNotifier.AddListener(id =>
        {
            if (id == componentId)
                notificationReceived = true;
        });

        // Act
        component.Counter.Value = 1;

        // Assert
        Assert.True(notificationReceived);
    }

    [Fact]
    public void UseEffect_RunsWhenDependenciesChange()
    {
        // Arrange
        var component = new TestComponent();

        // Act
        component.RunEffect(new object[] { 1 });
        component.RunEffect(new object[] { 2 });

        // Assert
        Assert.Equal(2, component.EffectRunCount);
    }

    [Fact]
    public void UseEffect_DoesNotRun_WhenDependenciesAreSame()
    {
        // Arrange
        var component = new TestComponent();
        var deps = new object[] { 1 };

        // Act
        component.RunEffect(deps);
        component.RunEffect(deps);

        // Assert
        Assert.Equal(1, component.EffectRunCount);
    }
}
