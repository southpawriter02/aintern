using Xunit;
using AIntern.Core.Models.Terminal;

namespace AIntern.Core.Tests.Models.Terminal;

/// <summary>
/// Unit tests for <see cref="SearchHighlightStyle"/>.
/// </summary>
/// <remarks>Added in v0.5.5a.</remarks>
public sealed class SearchHighlightStyleTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Default Preset Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Default_HasExpectedValues()
    {
        // Act
        var style = SearchHighlightStyle.Default;

        // Assert
        Assert.Equal("#FFFF00", style.MatchBackground);
        Assert.Equal("#000000", style.MatchForeground);
        Assert.Equal("#FF8C00", style.CurrentMatchBackground);
        Assert.Equal("#000000", style.CurrentMatchForeground);
        Assert.Equal("#FF4500", style.CurrentMatchBorder);
        Assert.Equal(1, style.CurrentMatchBorderThickness);
        Assert.Equal(0.7, style.MatchOpacity);
        Assert.False(style.UseOutlineStyle);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Dark Preset Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Dark_HasExpectedValues()
    {
        // Act
        var style = SearchHighlightStyle.Dark;

        // Assert
        Assert.Equal("#3A3D41", style.MatchBackground);
        Assert.Equal("#FFD700", style.MatchForeground);
        Assert.Equal("#515C6B", style.CurrentMatchBackground);
        Assert.Equal("#FFFFFF", style.CurrentMatchForeground);
        Assert.Equal("#007ACC", style.CurrentMatchBorder);
        Assert.Equal(0.9, style.MatchOpacity);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Light Preset Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Light_HasExpectedValues()
    {
        // Act
        var style = SearchHighlightStyle.Light;

        // Assert
        Assert.Equal("#FFFACD", style.MatchBackground);
        Assert.Equal("#000000", style.MatchForeground);
        Assert.Equal("#FFD700", style.CurrentMatchBackground);
        Assert.Equal("#000000", style.CurrentMatchForeground);
        Assert.Equal("#FF8C00", style.CurrentMatchBorder);
        Assert.Equal(0.8, style.MatchOpacity);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HighContrast Preset Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void HighContrast_HasExpectedValues()
    {
        // Act
        var style = SearchHighlightStyle.HighContrast;

        // Assert
        Assert.Equal("#00FF00", style.MatchBackground);
        Assert.Equal("#000000", style.MatchForeground);
        Assert.Equal("#FF00FF", style.CurrentMatchBackground);
        Assert.Equal("#FFFFFF", style.CurrentMatchForeground);
        Assert.Equal("#FFFFFF", style.CurrentMatchBorder);
        Assert.Equal(2, style.CurrentMatchBorderThickness);
        Assert.Equal(1.0, style.MatchOpacity);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Subtle Preset Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Subtle_HasExpectedValues()
    {
        // Act
        var style = SearchHighlightStyle.Subtle;

        // Assert
        Assert.True(style.UseOutlineStyle);
        Assert.True(style.UnderlineMatches);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ForTheme Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ForTheme_ReturnsDarkWhenIsDarkThemeTrue()
    {
        // Act
        var style = SearchHighlightStyle.ForTheme(isDarkTheme: true);

        // Assert
        Assert.Equal(SearchHighlightStyle.Dark.MatchBackground, style.MatchBackground);
    }

    [Fact]
    public void ForTheme_ReturnsLightWhenIsDarkThemeFalse()
    {
        // Act
        var style = SearchHighlightStyle.ForTheme(isDarkTheme: false);

        // Assert
        Assert.Equal(SearchHighlightStyle.Light.MatchBackground, style.MatchBackground);
    }

    [Theory]
    [InlineData("dark", "#3A3D41")]
    [InlineData("light", "#FFFACD")]
    [InlineData("highcontrast", "#00FF00")]
    [InlineData("high-contrast", "#00FF00")]
    [InlineData("subtle", "#FFFFFF00")]
    [InlineData("solarized-dark", "#073642")]
    [InlineData("solarized-light", "#EEE8D5")]
    [InlineData("unknown", "#FFFF00")] // Default
    public void ForTheme_ByName_ReturnsCorrectStyle(string themeName, string expectedBackground)
    {
        // Act
        var style = SearchHighlightStyle.ForTheme(themeName);

        // Assert
        Assert.Equal(expectedBackground, style.MatchBackground);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Property Clamping Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CurrentMatchBorderThickness_ClampsToValidRange()
    {
        // Arrange
        var style = new SearchHighlightStyle();

        // Act - Try to set below minimum
        style.CurrentMatchBorderThickness = -1;

        // Assert
        Assert.Equal(0, style.CurrentMatchBorderThickness);
    }

    [Fact]
    public void CurrentMatchBorderThickness_ClampsToMaximum()
    {
        // Arrange
        var style = new SearchHighlightStyle();

        // Act - Try to set above maximum
        style.CurrentMatchBorderThickness = 10;

        // Assert
        Assert.Equal(5, style.CurrentMatchBorderThickness);
    }

    [Fact]
    public void MatchOpacity_ClampsToValidRange()
    {
        // Arrange
        var style = new SearchHighlightStyle();

        // Act - Try to set below minimum
        style.MatchOpacity = -0.5;

        // Assert
        Assert.Equal(0.0, style.MatchOpacity);
    }

    [Fact]
    public void MatchOpacity_ClampsToMaximum()
    {
        // Arrange
        var style = new SearchHighlightStyle();

        // Act - Try to set above maximum
        style.MatchOpacity = 1.5;

        // Assert
        Assert.Equal(1.0, style.MatchOpacity);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Validation Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("#FFF", true)]
    [InlineData("#FFFFFF", true)]
    [InlineData("#FFFFFFFF", true)]
    [InlineData("#fff", true)]
    [InlineData("#ffffff", true)]
    [InlineData("#GGG", false)]
    [InlineData("FFFFFF", false)]
    [InlineData("#FFFFF", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidHexColor_ValidatesCorrectly(string? hexColor, bool expected)
    {
        // Act & Assert
        Assert.Equal(expected, SearchHighlightStyle.IsValidHexColor(hexColor));
    }

    [Fact]
    public void ValidateColors_ReturnsEmptyForValidStyle()
    {
        // Arrange
        var style = SearchHighlightStyle.Default;

        // Act
        var invalid = style.ValidateColors();

        // Assert
        Assert.Empty(invalid);
        Assert.True(style.AreColorsValid);
    }

    [Fact]
    public void ValidateColors_ReturnsInvalidPropertyNames()
    {
        // Arrange
        var style = new SearchHighlightStyle
        {
            MatchBackground = "invalid"
        };

        // Act
        var invalid = style.ValidateColors();

        // Assert
        Assert.Contains("MatchBackground", invalid);
        Assert.False(style.AreColorsValid);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Clone Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new SearchHighlightStyle
        {
            MatchBackground = "#123456",
            MatchForeground = "#654321",
            CurrentMatchBackground = "#ABCDEF",
            CurrentMatchForeground = "#FEDCBA",
            CurrentMatchBorder = "#999999",
            CurrentMatchBorderThickness = 3,
            MatchOpacity = 0.5,
            UseOutlineStyle = true,
            UnderlineMatches = true,
            BoldMatches = true
        };

        // Act
        var clone = original.Clone();

        // Assert - All values copied
        Assert.Equal(original.MatchBackground, clone.MatchBackground);
        Assert.Equal(original.MatchForeground, clone.MatchForeground);
        Assert.Equal(original.CurrentMatchBackground, clone.CurrentMatchBackground);
        Assert.Equal(original.CurrentMatchForeground, clone.CurrentMatchForeground);
        Assert.Equal(original.CurrentMatchBorder, clone.CurrentMatchBorder);
        Assert.Equal(original.CurrentMatchBorderThickness, clone.CurrentMatchBorderThickness);
        Assert.Equal(original.MatchOpacity, clone.MatchOpacity);
        Assert.Equal(original.UseOutlineStyle, clone.UseOutlineStyle);
        Assert.Equal(original.UnderlineMatches, clone.UnderlineMatches);
        Assert.Equal(original.BoldMatches, clone.BoldMatches);
    }

    [Fact]
    public void Clone_ModifyingCloneDoesNotAffectOriginal()
    {
        // Arrange
        var original = new SearchHighlightStyle { MatchBackground = "#FFFFFF" };

        // Act
        var clone = original.Clone();
        clone.MatchBackground = "#000000";

        // Assert
        Assert.Equal("#FFFFFF", original.MatchBackground);
        Assert.Equal("#000000", clone.MatchBackground);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Color Manipulation Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Darken_ReducesColorBrightness()
    {
        // Arrange
        var style = new SearchHighlightStyle { MatchBackground = "#FFFFFF" };

        // Act
        var darkened = style.Darken(0.5);

        // Assert - White darkened by 50% should be gray
        Assert.Equal("#7F7F7F", darkened.MatchBackground);
    }

    [Fact]
    public void Lighten_IncreasesColorBrightness()
    {
        // Arrange
        var style = new SearchHighlightStyle { MatchBackground = "#000000" };

        // Act
        var lightened = style.Lighten(0.5);

        // Assert - Black lightened by 50% should be gray
        Assert.Equal("#7F7F7F", lightened.MatchBackground);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ToString Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var style = SearchHighlightStyle.Default;

        // Act
        var str = style.ToString();

        // Assert
        Assert.Contains("SearchHighlightStyle", str);
        Assert.Contains("Match=", str);
        Assert.Contains("Current=", str);
        Assert.Contains("Opacity=", str);
    }
}
