namespace UI.Tests;

public class TuiStateTests
{
    [Fact]
    public void InitialState_IsSetCorrectly()
    {
        // Arrange
        var initialState = "initial";
        var state = new TuiState<string>(initialState);

        // Assert
        Assert.Equal(initialState, state.Value);
    }

    [Fact]
    public void Value_Set_UpdatesState()
    {
        // Arrange
        var state = new TuiState<string>("initial");
        var newState = "updated";

        // Act
        state.Value = newState;

        // Assert
        Assert.Equal(newState, state.Value);
    }

    [Fact]
    public void ValueChanged_IsTriggered_WhenStateChanges()
    {
        // Arrange
        var state = new TuiState<string>("initial");
        var newState = "updated";
        string? receivedState = null;
        state.ValueChanged += (_, newValue) => receivedState = newValue;

        // Act
        state.Value = newState;

        // Assert
        Assert.Equal(newState, receivedState);
    }

    [Fact]
    public void ValueChanged_IsNotTriggered_WhenStateIsSame()
    {
        // Arrange
        var state = new TuiState<string>("initial");
        var received = false;
        state.ValueChanged += (_, _) => received = true;

        // Act
        state.Value = "initial";

        // Assert
        Assert.False(received);
    }
}
