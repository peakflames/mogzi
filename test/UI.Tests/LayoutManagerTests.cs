using UI.Layout;
using UI.Core;

namespace UI.Tests;

public class LayoutManagerTests
{
    [Fact]
    public void CalculateLayout_WithDefaultConfiguration_ReturnsValidLayout()
    {
        // Arrange
        var layoutManager = new LayoutManager();
        var terminalSize = new TerminalSize(100, 50);

        // Act
        var result = layoutManager.CalculateLayout(terminalSize);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(terminalSize, result.TerminalSize);
        Assert.True(result.StaticZoneConstraints.Height > 0);
        Assert.True(result.DynamicZoneConstraints.Height > 0);
        Assert.Equal(98, result.AvailableWidth); // 100 - 2 (left + right margin)
        Assert.Equal(48, result.AvailableHeight); // 50 - 2 (top + bottom margin)
    }

    [Fact]
    public void CalculateLayout_WithMinimumTerminalSize_RespectsMinimums()
    {
        // Arrange
        var layoutManager = new LayoutManager();
        var terminalSize = new TerminalSize(10, 10); // Very small terminal

        // Act
        var result = layoutManager.CalculateLayout(terminalSize);

        // Assert
        Assert.Equal(60, result.TerminalSize.Width); // Should be enforced to minimum
        Assert.Equal(20, result.TerminalSize.Height); // Should be enforced to minimum
        Assert.True(result.StaticZoneConstraints.Height >= 1);
        Assert.True(result.DynamicZoneConstraints.Height >= 1);
    }

    [Fact]
    public void CalculateLayout_WithCustomConfiguration_UsesCustomValues()
    {
        // Arrange
        var config = new LayoutConfiguration(
            StaticZoneRatio: 0.8,
            DynamicZoneRatio: 0.2,
            MinimumStaticHeight: 15,
            MinimumDynamicHeight: 8,
            TopMargin: 2,
            BottomMargin: 2,
            LeftMargin: 3,
            RightMargin: 3
        );
        var layoutManager = new LayoutManager(config);
        var terminalSize = new TerminalSize(100, 50);

        // Act
        var result = layoutManager.CalculateLayout(terminalSize);

        // Assert
        Assert.Equal(94, result.AvailableWidth); // 100 - 6 (left + right margin)
        Assert.Equal(46, result.AvailableHeight); // 50 - 4 (top + bottom margin)
        
        // Static zone should get ~80% of available height
        var expectedStaticHeight = (int)(46 * 0.8);
        Assert.True(Math.Abs(result.StaticZoneConstraints.Height - expectedStaticHeight) <= 2);
    }

    [Fact]
    public void UpdateConfiguration_WithNewConfiguration_UpdatesLayout()
    {
        // Arrange
        var layoutManager = new LayoutManager();
        var terminalSize = new TerminalSize(100, 50);
        var originalResult = layoutManager.CalculateLayout(terminalSize);

        var newConfig = new LayoutConfiguration(
            StaticZoneRatio: 0.3,
            DynamicZoneRatio: 0.7
        );

        // Act
        layoutManager.UpdateConfiguration(newConfig);
        var newResult = layoutManager.CalculateLayout(terminalSize);

        // Assert
        Assert.NotEqual(originalResult.StaticZoneConstraints.Height, newResult.StaticZoneConstraints.Height);
        Assert.NotEqual(originalResult.DynamicZoneConstraints.Height, newResult.DynamicZoneConstraints.Height);
        
        // Dynamic zone should now be larger than static zone
        Assert.True(newResult.DynamicZoneConstraints.Height > newResult.StaticZoneConstraints.Height);
    }

    [Fact]
    public void GetConfiguration_ReturnsCurrentConfiguration()
    {
        // Arrange
        var config = new LayoutConfiguration(StaticZoneRatio: 0.75);
        var layoutManager = new LayoutManager(config);

        // Act
        var retrievedConfig = layoutManager.GetConfiguration();

        // Assert
        Assert.Equal(0.75, retrievedConfig.StaticZoneRatio);
    }

    [Fact]
    public void CalculateOptimalLayout_ChatFocused_PrioritizesStaticZone()
    {
        // Arrange
        var layoutManager = new LayoutManager();
        var terminalSize = new TerminalSize(100, 50);

        // Act
        var result = layoutManager.CalculateOptimalLayout(terminalSize, LayoutUseCase.ChatFocused);

        // Assert
        Assert.True(result.StaticZoneConstraints.Height > result.DynamicZoneConstraints.Height);
        
        // Should be approximately 70/30 split
        var totalHeight = result.StaticZoneConstraints.Height + result.DynamicZoneConstraints.Height;
        var staticRatio = (double)result.StaticZoneConstraints.Height / totalHeight;
        Assert.True(staticRatio > 0.6); // Should be around 0.7
    }

    [Fact]
    public void CalculateOptimalLayout_ToolExecutionFocused_PrioritizesDynamicZone()
    {
        // Arrange
        var layoutManager = new LayoutManager();
        var terminalSize = new TerminalSize(100, 50);

        // Act
        var result = layoutManager.CalculateOptimalLayout(terminalSize, LayoutUseCase.ToolExecutionFocused);

        // Assert
        Assert.True(result.DynamicZoneConstraints.Height > result.StaticZoneConstraints.Height);
        
        // Should be approximately 30/70 split
        var totalHeight = result.StaticZoneConstraints.Height + result.DynamicZoneConstraints.Height;
        var dynamicRatio = (double)result.DynamicZoneConstraints.Height / totalHeight;
        Assert.True(dynamicRatio > 0.6); // Should be around 0.7
    }

    [Fact]
    public void CalculateOptimalLayout_DoesNotPermanentlyChangeConfiguration()
    {
        // Arrange
        var layoutManager = new LayoutManager();
        var terminalSize = new TerminalSize(100, 50);
        var originalConfig = layoutManager.GetConfiguration();

        // Act
        layoutManager.CalculateOptimalLayout(terminalSize, LayoutUseCase.ChatFocused);
        var configAfterOptimal = layoutManager.GetConfiguration();

        // Assert
        Assert.Equal(originalConfig, configAfterOptimal);
    }

    [Theory]
    [InlineData(80, 30)]
    [InlineData(120, 40)]
    [InlineData(200, 60)]
    public void CalculateLayout_WithVariousTerminalSizes_ProducesValidLayouts(int width, int height)
    {
        // Arrange
        var layoutManager = new LayoutManager();
        var terminalSize = new TerminalSize(width, height);

        // Act
        var result = layoutManager.CalculateLayout(terminalSize);

        // Assert
        Assert.True(result.StaticZoneConstraints.Height > 0);
        Assert.True(result.DynamicZoneConstraints.Height > 0);
        Assert.True(result.AvailableWidth > 0);
        Assert.True(result.AvailableHeight > 0);
        Assert.Equal(result.TotalUsedHeight, result.AvailableHeight);
    }

    [Fact]
    public void LayoutResult_SpaceEfficiency_CalculatesCorrectly()
    {
        // Arrange
        var terminalSize = new TerminalSize(100, 50);
        var staticConstraints = new LayoutConstraints(20, 98, 1, 1);
        var dynamicConstraints = new LayoutConstraints(28, 98, 1, 21);
        
        // Act
        var result = new LayoutResult(terminalSize, staticConstraints, dynamicConstraints, 98, 48);

        // Assert
        Assert.Equal(48, result.TotalUsedHeight);
        Assert.Equal(0.96, result.SpaceEfficiency, 2); // 48/50 = 0.96
    }

    [Fact]
    public void LayoutResult_IsOptimal_ReturnsTrueForGoodLayouts()
    {
        // Arrange
        var terminalSize = new TerminalSize(100, 50);
        var staticConstraints = new LayoutConstraints(20, 98, 1, 1);
        var dynamicConstraints = new LayoutConstraints(25, 98, 1, 21);
        
        // Act
        var result = new LayoutResult(terminalSize, staticConstraints, dynamicConstraints, 98, 45);

        // Assert
        Assert.True(result.IsOptimal); // Good space efficiency (90%) and adequate heights
    }

    [Fact]
    public void LayoutConfiguration_Default_HasValidValues()
    {
        // Act
        var config = LayoutConfiguration.Default;

        // Assert
        Assert.True(config.IsValid);
        Assert.Equal(0.6, config.StaticZoneRatio);
        Assert.Equal(0.4, config.DynamicZoneRatio);
        Assert.True(config.MinimumStaticHeight > 0);
        Assert.True(config.MinimumDynamicHeight > 0);
    }

    [Fact]
    public void LayoutConfiguration_Compact_HasValidValues()
    {
        // Act
        var config = LayoutConfiguration.Compact;

        // Assert
        Assert.True(config.IsValid);
        Assert.Equal(0, config.TopMargin);
        Assert.Equal(0, config.BottomMargin);
        Assert.True(config.MinimumWidth < LayoutConfiguration.Default.MinimumWidth);
    }

    [Fact]
    public void LayoutConfiguration_IsValid_ReturnsFalseForInvalidConfigurations()
    {
        // Arrange & Act & Assert
        var invalidConfig1 = new LayoutConfiguration(StaticZoneRatio: 0); // Invalid ratio
        Assert.False(invalidConfig1.IsValid);

        var invalidConfig2 = new LayoutConfiguration(MinimumStaticHeight: 0); // Invalid height
        Assert.False(invalidConfig2.IsValid);

        var invalidConfig3 = new LayoutConfiguration(
            MinimumStaticHeight: 20,
            MaximumStaticHeight: 10); // Max < Min
        Assert.False(invalidConfig3.IsValid);
    }
}
