using UI.State;

namespace UI.Tests;

public class StateManagerTests
{
    [Fact]
    public void NotifyStateChanged_AddsComponentToPendingChanges()
    {
        // Arrange
        using var stateManager = new StateManager();
        var componentId = "test-component";

        // Act
        stateManager.NotifyStateChanged(componentId);

        // Assert
        Assert.True(stateManager.HasPendingChangesFor(componentId));
        Assert.Equal(1, stateManager.PendingChangesCount);
    }

    [Fact]
    public async Task StateChangesReady_IsTriggered_AfterDebouncePeriod()
    {
        // Arrange
        using var stateManager = new StateManager(debounceMs: 10);
        var componentId = "test-component";
        var eventTriggered = new TaskCompletionSource<bool>();
        stateManager.StateChangesReady += () => eventTriggered.TrySetResult(true);

        // Act
        stateManager.NotifyStateChanged(componentId);

        // Assert
        var completedTask = await Task.WhenAny(eventTriggered.Task, Task.Delay(200));
        Assert.Same(eventTriggered.Task, completedTask);
        var eventWasTriggered = await eventTriggered.Task;
        Assert.True(eventWasTriggered);
    }

    [Fact]
    public void FlushPendingChanges_TriggersStateChangesReady_Immediately()
    {
        // Arrange
        using var stateManager = new StateManager(debounceMs: 1000); // Long debounce
        var componentId = "test-component";
        var eventTriggered = false;
        stateManager.StateChangesReady += () => eventTriggered = true;

        // Act
        stateManager.NotifyStateChanged(componentId);
        stateManager.FlushPendingChanges();

        // Assert
        Assert.True(eventTriggered);
    }

    [Fact]
    public void ClearPendingChanges_RemovesAllPendingChanges()
    {
        // Arrange
        using var stateManager = new StateManager();
        stateManager.NotifyStateChanged("comp1");
        stateManager.NotifyStateChanged("comp2");

        // Act
        stateManager.ClearPendingChanges();

        // Assert
        Assert.False(stateManager.HasPendingChanges);
        Assert.Equal(0, stateManager.PendingChangesCount);
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        // Arrange
        using var stateManager = new StateManager();
        stateManager.NotifyStateChanged("comp1");

        // Act
        stateManager.Reset();

        // Assert
        Assert.False(stateManager.HasPendingChanges);
        Assert.Null(stateManager.GetLastChangeTime("comp1"));
    }
}
