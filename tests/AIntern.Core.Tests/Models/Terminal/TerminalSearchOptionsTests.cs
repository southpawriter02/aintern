using Xunit;
using AIntern.Core.Models.Terminal;

namespace AIntern.Core.Tests.Models.Terminal;

/// <summary>
/// Unit tests for <see cref="TerminalSearchOptions"/>.
/// </summary>
/// <remarks>Added in v0.5.5a.</remarks>
public sealed class TerminalSearchOptionsTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Default Preset Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Default_HasExpectedValues()
    {
        // Act
        var options = TerminalSearchOptions.Default;

        // Assert
        Assert.Equal(10000, options.MaxResults);
        Assert.Equal(150, options.DebounceDelayMs);
        Assert.Equal(1, options.MinQueryLength);
        Assert.Equal(5000, options.RegexTimeoutMs);
        Assert.False(options.DefaultCaseSensitive);
        Assert.False(options.DefaultUseRegex);
        Assert.True(options.DefaultIncludeScrollback);
        Assert.True(options.DefaultWrapAround);
        Assert.True(options.HighlightAllMatches);
        Assert.True(options.AutoScrollToMatch);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // LargeBuffer Preset Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void LargeBuffer_HasExpectedValues()
    {
        // Act
        var options = TerminalSearchOptions.LargeBuffer;

        // Assert
        Assert.Equal(50000, options.MaxResults);
        Assert.Equal(300, options.DebounceDelayMs);
        Assert.Equal(2, options.MinQueryLength);
        Assert.Equal(10000, options.RegexTimeoutMs);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // QuickFind Preset Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void QuickFind_HasExpectedValues()
    {
        // Act
        var options = TerminalSearchOptions.QuickFind;

        // Assert
        Assert.Equal(1000, options.MaxResults);
        Assert.Equal(50, options.DebounceDelayMs);
        Assert.Equal(1, options.MinQueryLength);
        Assert.Equal(2000, options.RegexTimeoutMs);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // RegexFocused Preset Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void RegexFocused_HasExpectedValues()
    {
        // Act
        var options = TerminalSearchOptions.RegexFocused;

        // Assert
        Assert.True(options.DefaultUseRegex);
        Assert.Equal(10000, options.RegexTimeoutMs);
        Assert.Equal(300, options.DebounceDelayMs);
        Assert.Equal(2, options.MinQueryLength);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Property Clamping Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void DebounceDelayMs_ClampsToValidRange()
    {
        // Arrange
        var options = new TerminalSearchOptions();

        // Act - Try to set below minimum
        options.DebounceDelayMs = -100;

        // Assert
        Assert.Equal(0, options.DebounceDelayMs);
    }

    [Fact]
    public void DebounceDelayMs_ClampsToMaximum()
    {
        // Arrange
        var options = new TerminalSearchOptions();

        // Act - Try to set above maximum
        options.DebounceDelayMs = 10000;

        // Assert
        Assert.Equal(2000, options.DebounceDelayMs);
    }

    [Fact]
    public void RegexTimeoutMs_ClampsToValidRange()
    {
        // Arrange
        var options = new TerminalSearchOptions();

        // Act - Try to set below minimum
        options.RegexTimeoutMs = 10;

        // Assert
        Assert.Equal(100, options.RegexTimeoutMs);
    }

    [Fact]
    public void RegexTimeoutMs_ClampsToMaximum()
    {
        // Arrange
        var options = new TerminalSearchOptions();

        // Act - Try to set above maximum
        options.RegexTimeoutMs = 100000;

        // Assert
        Assert.Equal(30000, options.RegexTimeoutMs);
    }

    [Fact]
    public void MinQueryLength_ClampsToMinimumOfOne()
    {
        // Arrange
        var options = new TerminalSearchOptions();

        // Act
        options.MinQueryLength = -5;

        // Assert
        Assert.Equal(1, options.MinQueryLength);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // RegexTimeout Property Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void RegexTimeout_ReturnsCorrectTimeSpan()
    {
        // Arrange
        var options = new TerminalSearchOptions { RegexTimeoutMs = 5000 };

        // Act
        var timeout = options.RegexTimeout;

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(5000), timeout);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Clone Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new TerminalSearchOptions
        {
            MaxResults = 5000,
            DebounceDelayMs = 200,
            MinQueryLength = 3,
            DefaultCaseSensitive = true,
            DefaultUseRegex = true,
            RegexTimeoutMs = 3000,
            DefaultIncludeScrollback = false,
            DefaultWrapAround = false,
            HighlightAllMatches = false,
            AutoScrollToMatch = false,
            ContextLines = 5
        };

        // Act
        var clone = original.Clone();

        // Assert - All values copied
        Assert.Equal(original.MaxResults, clone.MaxResults);
        Assert.Equal(original.DebounceDelayMs, clone.DebounceDelayMs);
        Assert.Equal(original.MinQueryLength, clone.MinQueryLength);
        Assert.Equal(original.DefaultCaseSensitive, clone.DefaultCaseSensitive);
        Assert.Equal(original.DefaultUseRegex, clone.DefaultUseRegex);
        Assert.Equal(original.RegexTimeoutMs, clone.RegexTimeoutMs);
        Assert.Equal(original.DefaultIncludeScrollback, clone.DefaultIncludeScrollback);
        Assert.Equal(original.DefaultWrapAround, clone.DefaultWrapAround);
        Assert.Equal(original.HighlightAllMatches, clone.HighlightAllMatches);
        Assert.Equal(original.AutoScrollToMatch, clone.AutoScrollToMatch);
        Assert.Equal(original.ContextLines, clone.ContextLines);
    }

    [Fact]
    public void Clone_ModifyingCloneDoesNotAffectOriginal()
    {
        // Arrange
        var original = new TerminalSearchOptions { MaxResults = 5000 };

        // Act
        var clone = original.Clone();
        clone.MaxResults = 10000;

        // Assert
        Assert.Equal(5000, original.MaxResults);
        Assert.Equal(10000, clone.MaxResults);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CreateInitialState Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CreateInitialState_UsesDefaultOptions()
    {
        // Arrange
        var options = new TerminalSearchOptions
        {
            DefaultCaseSensitive = true,
            DefaultUseRegex = true,
            DefaultWrapAround = false,
            DefaultIncludeScrollback = false,
            DefaultDirection = SearchDirection.Backward
        };

        // Act
        var state = options.CreateInitialState();

        // Assert
        Assert.True(state.CaseSensitive);
        Assert.True(state.UseRegex);
        Assert.False(state.WrapAround);
        Assert.False(state.IncludeScrollback);
        Assert.Equal(SearchDirection.Backward, state.Direction);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Validation Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Validate_ReturnsEmptyForValidOptions()
    {
        // Arrange
        var options = TerminalSearchOptions.Default;

        // Act
        var issues = options.Validate();

        // Assert
        Assert.Empty(issues);
        Assert.True(options.IsValid);
    }

    [Fact]
    public void Validate_ReturnsIssueForNegativeMaxResults()
    {
        // Arrange
        var options = new TerminalSearchOptions { MaxResults = -1 };

        // Act
        var issues = options.Validate();

        // Assert
        Assert.Single(issues);
        Assert.Contains("MaxResults", issues[0]);
        Assert.False(options.IsValid);
    }

    [Fact]
    public void Validate_ReturnsIssueForNegativeContextLines()
    {
        // Arrange
        var options = new TerminalSearchOptions { ContextLines = -1 };

        // Act
        var issues = options.Validate();

        // Assert
        Assert.Single(issues);
        Assert.Contains("ContextLines", issues[0]);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ToString Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var options = TerminalSearchOptions.Default;

        // Act
        var str = options.ToString();

        // Assert
        Assert.Contains("TerminalSearchOptions", str);
        Assert.Contains("Max=10000", str);
        Assert.Contains("Debounce=150ms", str);
    }
}
