using Xunit;
using AIntern.Core.Models.Terminal;

namespace AIntern.Core.Tests.Models.Terminal;

/// <summary>
/// Unit tests for <see cref="TerminalSearchExtensions"/>.
/// </summary>
/// <remarks>Added in v0.5.5a.</remarks>
public sealed class TerminalSearchExtensionsTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // IsVisible Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void IsVisible_ReturnsTrueWhenResultInViewport()
    {
        // Arrange
        var result = new TerminalSearchResult { LineIndex = 50 };

        // Act - Viewport from line 40 with 25 visible lines (40-64)
        var isVisible = result.IsVisible(40, 25);

        // Assert
        Assert.True(isVisible);
    }

    [Fact]
    public void IsVisible_ReturnsFalseWhenResultAboveViewport()
    {
        // Arrange
        var result = new TerminalSearchResult { LineIndex = 30 };

        // Act - Viewport from line 40 with 25 visible lines
        var isVisible = result.IsVisible(40, 25);

        // Assert
        Assert.False(isVisible);
    }

    [Fact]
    public void IsVisible_ReturnsFalseWhenResultBelowViewport()
    {
        // Arrange
        var result = new TerminalSearchResult { LineIndex = 100 };

        // Act - Viewport from line 40 with 25 visible lines (40-64)
        var isVisible = result.IsVisible(40, 25);

        // Assert
        Assert.False(isVisible);
    }

    [Fact]
    public void IsVisible_ReturnsTrueAtViewportStart()
    {
        // Arrange
        var result = new TerminalSearchResult { LineIndex = 40 };

        // Act
        var isVisible = result.IsVisible(40, 25);

        // Assert
        Assert.True(isVisible);
    }

    [Fact]
    public void IsVisible_ReturnsTrueAtViewportEnd()
    {
        // Arrange - Last visible line is 64 (40 + 25 - 1)
        var result = new TerminalSearchResult { LineIndex = 64 };

        // Act
        var isVisible = result.IsVisible(40, 25);

        // Assert
        Assert.True(isVisible);
    }

    [Fact]
    public void IsVisible_ReturnsFalseJustBelowViewport()
    {
        // Arrange - Line 65 is just below viewport ending at 64
        var result = new TerminalSearchResult { LineIndex = 65 };

        // Act
        var isVisible = result.IsVisible(40, 25);

        // Assert
        Assert.False(isVisible);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // IsAboveViewport/IsBelowViewport Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void IsAboveViewport_ReturnsTrueWhenAbove()
    {
        // Arrange
        var result = new TerminalSearchResult { LineIndex = 10 };

        // Act
        var isAbove = result.IsAboveViewport(20);

        // Assert
        Assert.True(isAbove);
    }

    [Fact]
    public void IsBelowViewport_ReturnsTrueWhenBelow()
    {
        // Arrange
        var result = new TerminalSearchResult { LineIndex = 100 };

        // Act
        var isBelow = result.IsBelowViewport(40, 25);

        // Assert
        Assert.True(isBelow);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetVisibleResults Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetVisibleResults_FiltersCorrectly()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult { LineIndex = 10 },  // Above
            new TerminalSearchResult { LineIndex = 45 },  // Visible
            new TerminalSearchResult { LineIndex = 50 },  // Visible
            new TerminalSearchResult { LineIndex = 100 }  // Below
        };
        var state = TerminalSearchState.Empty.WithResults(results);

        // Act
        var visible = state.GetVisibleResults(40, 25).ToList();

        // Assert
        Assert.Equal(2, visible.Count);
        Assert.Equal(45, visible[0].LineIndex);
        Assert.Equal(50, visible[1].LineIndex);
    }

    [Fact]
    public void GetVisibleResultCount_ReturnsCorrectCount()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult { LineIndex = 10 },
            new TerminalSearchResult { LineIndex = 45 },
            new TerminalSearchResult { LineIndex = 50 },
            new TerminalSearchResult { LineIndex = 100 }
        };
        var state = TerminalSearchState.Empty.WithResults(results);

        // Act
        var count = state.GetVisibleResultCount(40, 25);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public void GetResultsAboveCount_ReturnsCorrectCount()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult { LineIndex = 10 },
            new TerminalSearchResult { LineIndex = 20 },
            new TerminalSearchResult { LineIndex = 50 }
        };
        var state = TerminalSearchState.Empty.WithResults(results);

        // Act
        var count = state.GetResultsAboveCount(40);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public void GetResultsBelowCount_ReturnsCorrectCount()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult { LineIndex = 50 },
            new TerminalSearchResult { LineIndex = 100 },
            new TerminalSearchResult { LineIndex = 150 }
        };
        var state = TerminalSearchState.Empty.WithResults(results);

        // Act
        var count = state.GetResultsBelowCount(40, 25); // Below line 65

        // Assert
        Assert.Equal(2, count);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetMatchingLineIndices Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetMatchingLineIndices_ReturnsUniqueIndices()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult { LineIndex = 10 },
            new TerminalSearchResult { LineIndex = 10 },  // Duplicate line
            new TerminalSearchResult { LineIndex = 20 },
            new TerminalSearchResult { LineIndex = 30 }
        };
        var state = TerminalSearchState.Empty.WithResults(results);

        // Act
        var lineIndices = state.GetMatchingLineIndices();

        // Assert
        Assert.Equal(3, lineIndices.Count);
        Assert.Contains(10, lineIndices);
        Assert.Contains(20, lineIndices);
        Assert.Contains(30, lineIndices);
    }

    [Fact]
    public void GetMatchCountPerLine_ReturnsCorrectCounts()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult { LineIndex = 10 },
            new TerminalSearchResult { LineIndex = 10 },
            new TerminalSearchResult { LineIndex = 10 },
            new TerminalSearchResult { LineIndex = 20 }
        };
        var state = TerminalSearchState.Empty.WithResults(results);

        // Act
        var counts = state.GetMatchCountPerLine();

        // Assert
        Assert.Equal(3, counts[10]);
        Assert.Equal(1, counts[20]);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FindNearestResultIndex Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void FindNearestResultIndex_FindsExactMatch()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult { LineIndex = 10 },
            new TerminalSearchResult { LineIndex = 50 },
            new TerminalSearchResult { LineIndex = 100 }
        };
        var state = TerminalSearchState.Empty.WithResults(results);

        // Act
        var index = state.FindNearestResultIndex(50);

        // Assert
        Assert.Equal(1, index);
    }

    [Fact]
    public void FindNearestResultIndex_FindsNearestForward()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult { LineIndex = 10 },
            new TerminalSearchResult { LineIndex = 50 },
            new TerminalSearchResult { LineIndex = 100 }
        };
        var state = TerminalSearchState.Empty.WithResults(results);

        // Act
        var index = state.FindNearestResultIndex(40, SearchDirection.Forward);

        // Assert - Nearest forward from 40 is line 50 (index 1)
        Assert.Equal(1, index);
    }

    [Fact]
    public void FindNearestResultIndex_ReturnsMinusOneWhenNoResults()
    {
        // Arrange
        var state = TerminalSearchState.Empty;

        // Act
        var index = state.FindNearestResultIndex(50);

        // Assert
        Assert.Equal(-1, index);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FindFirstResultAtOrAfter/FindLastResultAtOrBefore Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void FindFirstResultAtOrAfter_FindsCorrectIndex()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult { LineIndex = 10 },
            new TerminalSearchResult { LineIndex = 50 },
            new TerminalSearchResult { LineIndex = 100 }
        };
        var state = TerminalSearchState.Empty.WithResults(results);

        // Act
        var index = state.FindFirstResultAtOrAfter(40);

        // Assert - First result at or after 40 is line 50 (index 1)
        Assert.Equal(1, index);
    }

    [Fact]
    public void FindFirstResultAtOrAfter_ReturnsMinusOneWhenNoneFound()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult { LineIndex = 10 },
            new TerminalSearchResult { LineIndex = 20 }
        };
        var state = TerminalSearchState.Empty.WithResults(results);

        // Act
        var index = state.FindFirstResultAtOrAfter(50);

        // Assert
        Assert.Equal(-1, index);
    }

    [Fact]
    public void FindLastResultAtOrBefore_FindsCorrectIndex()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult { LineIndex = 10 },
            new TerminalSearchResult { LineIndex = 50 },
            new TerminalSearchResult { LineIndex = 100 }
        };
        var state = TerminalSearchState.Empty.WithResults(results);

        // Act
        var index = state.FindLastResultAtOrBefore(60);

        // Assert - Last result at or before 60 is line 50 (index 1)
        Assert.Equal(1, index);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetResultsInRange Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetResultsInRange_ReturnsMatchesInRange()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult { LineIndex = 10 },
            new TerminalSearchResult { LineIndex = 50 },
            new TerminalSearchResult { LineIndex = 100 },
            new TerminalSearchResult { LineIndex = 150 }
        };
        var state = TerminalSearchState.Empty.WithResults(results);

        // Act
        var inRange = state.GetResultsInRange(40, 110).ToList();

        // Assert
        Assert.Equal(2, inRange.Count);
        Assert.Equal(50, inRange[0].LineIndex);
        Assert.Equal(100, inRange[1].LineIndex);
    }

    [Fact]
    public void GetResultsOnLine_ReturnsMatchesOnSpecificLine()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult { LineIndex = 50, StartColumn = 0 },
            new TerminalSearchResult { LineIndex = 50, StartColumn = 20 },
            new TerminalSearchResult { LineIndex = 100 }
        };
        var state = TerminalSearchState.Empty.WithResults(results);

        // Act
        var onLine50 = state.GetResultsOnLine(50).ToList();

        // Assert
        Assert.Equal(2, onLine50.Count);
    }

    [Fact]
    public void HasMatchOnLine_ReturnsTrueWhenMatchExists()
    {
        // Arrange
        var results = new[] { new TerminalSearchResult { LineIndex = 50 } };
        var state = TerminalSearchState.Empty.WithResults(results);

        // Act & Assert
        Assert.True(state.HasMatchOnLine(50));
        Assert.False(state.HasMatchOnLine(100));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Current Result Utilities Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void UpdateCurrentFlags_SetsCorrectFlag()
    {
        // Arrange
        var results = new List<TerminalSearchResult>
        {
            new() { LineIndex = 10 },
            new() { LineIndex = 20 },
            new() { LineIndex = 30 }
        };

        // Act
        results.UpdateCurrentFlags(1);

        // Assert
        Assert.False(results[0].IsCurrent);
        Assert.True(results[1].IsCurrent);
        Assert.False(results[2].IsCurrent);
    }

    [Fact]
    public void ClearCurrentFlags_ClearsAllFlags()
    {
        // Arrange
        var results = new List<TerminalSearchResult>
        {
            new() { IsCurrent = true },
            new() { IsCurrent = true }
        };

        // Act
        results.ClearCurrentFlags();

        // Assert
        Assert.All(results, r => Assert.False(r.IsCurrent));
    }

    [Fact]
    public void GetCurrentIndex_ReturnsIndexOfCurrentResult()
    {
        // Arrange
        var results = new List<TerminalSearchResult>
        {
            new(),
            new() { IsCurrent = true },
            new()
        };

        // Act
        var index = results.GetCurrentIndex();

        // Assert
        Assert.Equal(1, index);
    }

    [Fact]
    public void GetCurrentIndex_ReturnsMinusOneWhenNoCurrent()
    {
        // Arrange
        var results = new List<TerminalSearchResult>
        {
            new(),
            new()
        };

        // Act
        var index = results.GetCurrentIndex();

        // Assert
        Assert.Equal(-1, index);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Position Calculation Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CalculateCenteredScrollPosition_CentersResult()
    {
        // Arrange
        var result = new TerminalSearchResult { LineIndex = 100 };

        // Act - 25 visible lines, total 1000 lines
        var scrollPos = result.CalculateCenteredScrollPosition(25, 1000);

        // Assert - Should center line 100, so first line should be ~88
        Assert.Equal(88, scrollPos);
    }

    [Fact]
    public void CalculateCenteredScrollPosition_ClampsToValidRange()
    {
        // Arrange
        var result = new TerminalSearchResult { LineIndex = 5 };

        // Act - Centering line 5 with 25 visible would put first line at negative
        var scrollPos = result.CalculateCenteredScrollPosition(25, 1000);

        // Assert - Should clamp to 0
        Assert.Equal(0, scrollPos);
    }

    [Fact]
    public void GetRelativePosition_ReturnsCorrectValue()
    {
        // Arrange
        var result = new TerminalSearchResult { LineIndex = 50 };

        // Act - Total 101 lines (0-100)
        var relPos = result.GetRelativePosition(101);

        // Assert - Line 50 out of 100 = 0.5
        Assert.Equal(0.5, relPos);
    }

    [Fact]
    public void GetRelativePosition_ReturnsZeroForSingleLine()
    {
        // Arrange
        var result = new TerminalSearchResult { LineIndex = 0 };

        // Act
        var relPos = result.GetRelativePosition(1);

        // Assert
        Assert.Equal(0.0, relPos);
    }
}
