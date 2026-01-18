using Xunit;
using AIntern.Core.Models.Terminal;

namespace AIntern.Core.Tests.Models.Terminal;

/// <summary>
/// Unit tests for <see cref="TerminalSearchState"/>.
/// </summary>
/// <remarks>Added in v0.5.5a.</remarks>
public sealed class TerminalSearchStateTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Factory Methods Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Empty_CreatesValidEmptyState()
    {
        // Act
        var state = TerminalSearchState.Empty;

        // Assert
        Assert.Equal(string.Empty, state.Query);
        Assert.Empty(state.Results);
        Assert.Equal(-1, state.CurrentResultIndex);
        Assert.False(state.IsSearching);
        Assert.Null(state.ErrorMessage);
        Assert.False(state.HasResults);
        Assert.False(state.HasQuery);
    }

    [Fact]
    public void ForQuery_CreatesStateWithQuery()
    {
        // Act
        var state = TerminalSearchState.ForQuery("test");

        // Assert
        Assert.Equal("test", state.Query);
        Assert.True(state.HasQuery);
    }

    [Fact]
    public void Searching_CreatesSearchingState()
    {
        // Act
        var state = TerminalSearchState.Searching("error", caseSensitive: true, useRegex: true);

        // Assert
        Assert.Equal("error", state.Query);
        Assert.True(state.CaseSensitive);
        Assert.True(state.UseRegex);
        Assert.True(state.IsSearching);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CurrentResult_ReturnsNullWhenNoResults()
    {
        // Arrange
        var state = TerminalSearchState.Empty;

        // Act & Assert
        Assert.Null(state.CurrentResult);
    }

    [Fact]
    public void CurrentResult_ReturnsCorrectResultWhenValidIndex()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult { LineIndex = 0, MatchedText = "first" },
            new TerminalSearchResult { LineIndex = 5, MatchedText = "second" },
            new TerminalSearchResult { LineIndex = 10, MatchedText = "third" }
        };
        var state = TerminalSearchState.Empty.WithResults(results);

        // Act
        var current = state.CurrentResult;

        // Assert
        Assert.NotNull(current);
        Assert.Equal("first", current.MatchedText);
    }

    [Fact]
    public void HasResults_ReturnsFalseWhenEmpty()
    {
        // Arrange
        var state = TerminalSearchState.Empty;

        // Act & Assert
        Assert.False(state.HasResults);
    }

    [Fact]
    public void HasResults_ReturnsTrueWhenResultsExist()
    {
        // Arrange
        var state = TerminalSearchState.Empty.WithResults(new[]
        {
            new TerminalSearchResult { MatchedText = "test" }
        });

        // Act & Assert
        Assert.True(state.HasResults);
    }

    [Fact]
    public void HasQuery_ReturnsFalseForEmptyQuery()
    {
        // Arrange
        var state = TerminalSearchState.Empty;

        // Act & Assert
        Assert.False(state.HasQuery);
    }

    [Fact]
    public void HasQuery_ReturnsTrueForNonEmptyQuery()
    {
        // Arrange
        var state = TerminalSearchState.ForQuery("test");

        // Act & Assert
        Assert.True(state.HasQuery);
    }

    [Fact]
    public void HasError_ReturnsFalseWhenNoError()
    {
        // Arrange
        var state = TerminalSearchState.Empty;

        // Act & Assert
        Assert.False(state.HasError);
    }

    [Fact]
    public void HasError_ReturnsTrueWhenErrorExists()
    {
        // Arrange
        var state = TerminalSearchState.Empty.WithError("Invalid regex");

        // Act & Assert
        Assert.True(state.HasError);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ResultsSummary Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ResultsSummary_FormatsCorrectlyWithResults()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult(),
            new TerminalSearchResult(),
            new TerminalSearchResult()
        };
        var state = TerminalSearchState.ForQuery("test").WithResults(results);

        // Act
        var summary = state.ResultsSummary;

        // Assert
        Assert.Equal("1 of 3", summary);
    }

    [Fact]
    public void ResultsSummary_ShowsNoResultsWhenQueryExistsButNoMatches()
    {
        // Arrange
        var state = TerminalSearchState.ForQuery("test").WithResults(Array.Empty<TerminalSearchResult>());

        // Act
        var summary = state.ResultsSummary;

        // Assert
        Assert.Equal("No results", summary);
    }

    [Fact]
    public void ResultsSummary_ReturnsEmptyWhenNoQuery()
    {
        // Arrange
        var state = TerminalSearchState.Empty;

        // Act
        var summary = state.ResultsSummary;

        // Assert
        Assert.Equal(string.Empty, summary);
    }

    [Fact]
    public void ResultsSummary_ReturnsEmptyWhileSearching()
    {
        // Arrange
        var state = TerminalSearchState.Searching("test");

        // Act
        var summary = state.ResultsSummary;

        // Assert
        Assert.Equal(string.Empty, summary);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Navigation Availability Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CanNavigateNext_ReturnsFalseWhenNoResults()
    {
        // Arrange
        var state = TerminalSearchState.Empty;

        // Act & Assert
        Assert.False(state.CanNavigateNext);
    }

    [Fact]
    public void CanNavigateNext_ReturnsTrueWithResultsAndWrapAround()
    {
        // Arrange
        var state = TerminalSearchState.Empty with { WrapAround = true };
        state = state.WithResults(new[] { new TerminalSearchResult() });

        // Act & Assert
        Assert.True(state.CanNavigateNext);
    }

    [Fact]
    public void CanNavigateNext_ReturnsFalseAtLastResultWithoutWrapAround()
    {
        // Arrange
        var results = new[] { new TerminalSearchResult() };
        var state = TerminalSearchState.Empty with { WrapAround = false };
        state = state.WithResults(results);

        // Act & Assert - Already at last (and only) result
        Assert.False(state.CanNavigateNext);
    }

    [Fact]
    public void CanNavigatePrevious_ReturnsFalseAtFirstResultWithoutWrapAround()
    {
        // Arrange
        var results = new[] { new TerminalSearchResult() };
        var state = TerminalSearchState.Empty with { WrapAround = false };
        state = state.WithResults(results);

        // Act & Assert - Already at first result
        Assert.False(state.CanNavigatePrevious);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // WithResults Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void WithResults_SetsResultsAndFirstIndex()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult { MatchedText = "first" },
            new TerminalSearchResult { MatchedText = "second" }
        };
        var state = TerminalSearchState.ForQuery("test");

        // Act
        var newState = state.WithResults(results);

        // Assert
        Assert.Equal(2, newState.Results.Count);
        Assert.Equal(0, newState.CurrentResultIndex);
        Assert.False(newState.IsSearching);
        Assert.Null(newState.ErrorMessage);
    }

    [Fact]
    public void WithResults_SetsNegativeOneIndexWhenEmpty()
    {
        // Arrange
        var state = TerminalSearchState.ForQuery("test");

        // Act
        var newState = state.WithResults(Array.Empty<TerminalSearchResult>());

        // Assert
        Assert.Equal(-1, newState.CurrentResultIndex);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // WithError Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void WithError_SetsErrorMessage()
    {
        // Arrange
        var state = TerminalSearchState.ForQuery("test");

        // Act
        var newState = state.WithError("Invalid regex pattern");

        // Assert
        Assert.Equal("Invalid regex pattern", newState.ErrorMessage);
        Assert.False(newState.IsSearching);
        Assert.Empty(newState.Results);
        Assert.Equal(-1, newState.CurrentResultIndex);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NavigateNext Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void NavigateNext_IncrementsIndex()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult(),
            new TerminalSearchResult(),
            new TerminalSearchResult()
        };
        var state = TerminalSearchState.Empty.WithResults(results);

        // Act
        var newState = state.NavigateNext();

        // Assert
        Assert.Equal(1, newState.CurrentResultIndex);
    }

    [Fact]
    public void NavigateNext_WrapsAroundWhenEnabled()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult(),
            new TerminalSearchResult()
        };
        var state = TerminalSearchState.Empty with { WrapAround = true };
        state = state.WithResults(results).NavigateNext(); // Now at index 1

        // Act
        var newState = state.NavigateNext(); // Should wrap to 0

        // Assert
        Assert.Equal(0, newState.CurrentResultIndex);
    }

    [Fact]
    public void NavigateNext_ClampsWhenWrapAroundDisabled()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult(),
            new TerminalSearchResult()
        };
        var state = TerminalSearchState.Empty with { WrapAround = false };
        state = state.WithResults(results).NavigateNext(); // Now at index 1

        // Act
        var newState = state.NavigateNext(); // Should stay at 1

        // Assert
        Assert.Equal(1, newState.CurrentResultIndex);
    }

    [Fact]
    public void NavigateNext_ReturnsSameStateWhenNoResults()
    {
        // Arrange
        var state = TerminalSearchState.Empty;

        // Act
        var newState = state.NavigateNext();

        // Assert
        Assert.Same(state, newState);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NavigatePrevious Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void NavigatePrevious_DecrementsIndex()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult(),
            new TerminalSearchResult(),
            new TerminalSearchResult()
        };
        var state = TerminalSearchState.Empty.WithResults(results).NavigateNext(); // At index 1

        // Act
        var newState = state.NavigatePrevious();

        // Assert
        Assert.Equal(0, newState.CurrentResultIndex);
    }

    [Fact]
    public void NavigatePrevious_WrapsAroundWhenEnabled()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult(),
            new TerminalSearchResult()
        };
        var state = TerminalSearchState.Empty with { WrapAround = true };
        state = state.WithResults(results); // At index 0

        // Act
        var newState = state.NavigatePrevious(); // Should wrap to 1

        // Assert
        Assert.Equal(1, newState.CurrentResultIndex);
    }

    [Fact]
    public void NavigatePrevious_ClampsWhenWrapAroundDisabled()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult(),
            new TerminalSearchResult()
        };
        var state = TerminalSearchState.Empty with { WrapAround = false };
        state = state.WithResults(results); // At index 0

        // Act
        var newState = state.NavigatePrevious(); // Should stay at 0

        // Assert
        Assert.Equal(0, newState.CurrentResultIndex);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NavigateToIndex Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void NavigateToIndex_SetsCorrectIndex()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult(),
            new TerminalSearchResult(),
            new TerminalSearchResult()
        };
        var state = TerminalSearchState.Empty.WithResults(results);

        // Act
        var newState = state.NavigateToIndex(2);

        // Assert
        Assert.Equal(2, newState.CurrentResultIndex);
    }

    [Fact]
    public void NavigateToIndex_ClampsToValidRange()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult(),
            new TerminalSearchResult()
        };
        var state = TerminalSearchState.Empty.WithResults(results);

        // Act
        var newState = state.NavigateToIndex(100);

        // Assert
        Assert.Equal(1, newState.CurrentResultIndex); // Clamped to max
    }

    [Fact]
    public void NavigateToIndex_ClampsNegativeToZero()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult(),
            new TerminalSearchResult()
        };
        var state = TerminalSearchState.Empty.WithResults(results);

        // Act
        var newState = state.NavigateToIndex(-5);

        // Assert
        Assert.Equal(0, newState.CurrentResultIndex);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NavigateToFirst/Last Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void NavigateToFirst_SetsIndexToZero()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult(),
            new TerminalSearchResult(),
            new TerminalSearchResult()
        };
        var state = TerminalSearchState.Empty.WithResults(results).NavigateToIndex(2);

        // Act
        var newState = state.NavigateToFirst();

        // Assert
        Assert.Equal(0, newState.CurrentResultIndex);
    }

    [Fact]
    public void NavigateToLast_SetsIndexToLastResult()
    {
        // Arrange
        var results = new[]
        {
            new TerminalSearchResult(),
            new TerminalSearchResult(),
            new TerminalSearchResult()
        };
        var state = TerminalSearchState.Empty.WithResults(results);

        // Act
        var newState = state.NavigateToLast();

        // Assert
        Assert.Equal(2, newState.CurrentResultIndex);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Toggle Methods Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ToggleCaseSensitive_InvertsValue()
    {
        // Arrange
        var state = TerminalSearchState.Empty with { CaseSensitive = false };

        // Act
        var toggled = state.ToggleCaseSensitive();

        // Assert
        Assert.True(toggled.CaseSensitive);
    }

    [Fact]
    public void ToggleRegex_InvertsValue()
    {
        // Arrange
        var state = TerminalSearchState.Empty with { UseRegex = false };

        // Act
        var toggled = state.ToggleRegex();

        // Assert
        Assert.True(toggled.UseRegex);
    }

    [Fact]
    public void ToggleWrapAround_InvertsValue()
    {
        // Arrange
        var state = TerminalSearchState.Empty; // WrapAround defaults to true

        // Act
        var toggled = state.ToggleWrapAround();

        // Assert
        Assert.False(toggled.WrapAround);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Clear Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Clear_PreservesSearchOptions()
    {
        // Arrange
        var state = TerminalSearchState.Empty with
        {
            Query = "test",
            CaseSensitive = true,
            UseRegex = true,
            WrapAround = false,
            Direction = SearchDirection.Backward
        };
        state = state.WithResults(new[] { new TerminalSearchResult() });

        // Act
        var cleared = state.Clear();

        // Assert
        Assert.Equal(string.Empty, cleared.Query);
        Assert.Empty(cleared.Results);
        Assert.True(cleared.CaseSensitive);
        Assert.True(cleared.UseRegex);
        Assert.False(cleared.WrapAround);
        Assert.Equal(SearchDirection.Backward, cleared.Direction);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Record Equality Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void RecordEquality_WorksCorrectly()
    {
        // Arrange
        var state1 = TerminalSearchState.ForQuery("test");
        var state2 = TerminalSearchState.ForQuery("test");

        // Act & Assert
        Assert.Equal(state1, state2);
    }

    [Fact]
    public void RecordWith_CreatesNewInstance()
    {
        // Arrange
        var original = TerminalSearchState.ForQuery("original");

        // Act
        var modified = original with { Query = "modified" };

        // Assert
        Assert.NotEqual(original.Query, modified.Query);
        Assert.Equal("original", original.Query);
        Assert.Equal("modified", modified.Query);
    }
}
