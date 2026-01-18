// ============================================================================
// File: TerminalSearchState.cs
// Path: src/AIntern.Core/Models/Terminal/TerminalSearchState.cs
// Description: Immutable record maintaining the current state of terminal search
//              including query, options, results, and navigation position.
//              Designed for use with MVVM patterns and functional state updates.
// Created: 2026-01-18
// AI Intern v0.5.5a - Terminal Search Models
// ============================================================================

namespace AIntern.Core.Models.Terminal;

using System.Collections.Immutable;

/// <summary>
/// Maintains the current state of terminal search including query,
/// options, results, and navigation position.
/// </summary>
/// <remarks>
/// <para>
/// This is an immutable record type designed for functional state management.
/// All modifications return a new state instance via <c>with</c> expressions
/// or factory methods, making it safe for use in reactive UI patterns.
/// </para>
/// <para>
/// State Transitions:
/// <code>
/// Initial State (empty)
///     ↓ User types query
/// Searching State (IsSearching = true)
///     ↓ Search completes
/// Results State (HasResults = true) OR No Results State
///     ↓ Navigation (Next/Previous)
/// Updated Results State (new CurrentResultIndex)
///     ↓ Clear search
/// Back to Initial State
/// </code>
/// </para>
/// <para>
/// Example Usage:
/// <code>
/// // Create new state with results
/// var state = TerminalSearchState.Empty with { Query = "error" };
/// state = state.WithResults(results);
/// 
/// // Navigate through results
/// state = state.NavigateNext();
/// state = state.NavigatePrevious();
/// 
/// // Check navigation availability
/// if (state.CanNavigateNext) { ... }
/// </code>
/// </para>
/// </remarks>
public sealed record TerminalSearchState
{
    #region Query Properties

    /// <summary>
    /// Gets the current search query string.
    /// </summary>
    /// <remarks>
    /// An empty or null query indicates no active search.
    /// The query is preserved across state transitions.
    /// </remarks>
    public string Query { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether the search should be case-sensitive.
    /// </summary>
    /// <remarks>
    /// When false (default), searches match regardless of case.
    /// When true, "Error" will not match "error".
    /// </remarks>
    public bool CaseSensitive { get; init; }

    /// <summary>
    /// Gets whether to interpret the query as a regular expression.
    /// </summary>
    /// <remarks>
    /// When true, the query is compiled as a .NET Regex pattern.
    /// Invalid patterns will result in an <see cref="ErrorMessage"/>.
    /// </remarks>
    public bool UseRegex { get; init; }

    /// <summary>
    /// Gets whether to wrap around when navigating past the last/first result.
    /// </summary>
    /// <remarks>
    /// When true (default), navigating past the last result returns to the first.
    /// When false, navigation stops at boundaries.
    /// </remarks>
    public bool WrapAround { get; init; } = true;

    /// <summary>
    /// Gets whether to search in scrollback buffer or only visible content.
    /// </summary>
    /// <remarks>
    /// When true (default), the entire buffer including scrollback is searched.
    /// When false, only currently visible lines are searched.
    /// </remarks>
    public bool IncludeScrollback { get; init; } = true;

    /// <summary>
    /// Gets the search direction for navigation.
    /// </summary>
    /// <remarks>
    /// Affects which result is selected first and how Next/Previous navigate.
    /// Forward searches from top to bottom; Backward from bottom to top.
    /// </remarks>
    public SearchDirection Direction { get; init; } = SearchDirection.Forward;

    #endregion

    #region Results Properties

    /// <summary>
    /// Gets all found search results.
    /// </summary>
    /// <remarks>
    /// Results are ordered by position in the buffer (line index, then column).
    /// Empty if no search has been performed or no matches found.
    /// </remarks>
    public IReadOnlyList<TerminalSearchResult> Results { get; init; } =
        Array.Empty<TerminalSearchResult>();

    /// <summary>
    /// Gets the index of the currently selected result (-1 if none).
    /// </summary>
    /// <remarks>
    /// Valid range is 0 to Results.Count - 1, or -1 if no result is selected.
    /// Updated by navigation methods.
    /// </remarks>
    public int CurrentResultIndex { get; init; } = -1;

    #endregion

    #region Status Properties

    /// <summary>
    /// Gets whether a search is currently in progress.
    /// </summary>
    /// <remarks>
    /// True while the search service is actively searching.
    /// Used to display loading indicators in the UI.
    /// </remarks>
    public bool IsSearching { get; init; }

    /// <summary>
    /// Gets the error message if search failed (e.g., invalid regex).
    /// </summary>
    /// <remarks>
    /// Null if no error occurred. Set when regex compilation fails
    /// or other search errors occur.
    /// </remarks>
    public string? ErrorMessage { get; init; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the currently selected result, or null if none selected.
    /// </summary>
    /// <remarks>
    /// Returns null if:
    /// <list type="bullet">
    ///   <item><description>CurrentResultIndex is -1</description></item>
    ///   <item><description>CurrentResultIndex is out of range</description></item>
    ///   <item><description>Results collection is empty</description></item>
    /// </list>
    /// </remarks>
    public TerminalSearchResult? CurrentResult =>
        CurrentResultIndex >= 0 && CurrentResultIndex < Results.Count
            ? Results[CurrentResultIndex]
            : null;

    /// <summary>
    /// Gets whether there are any results.
    /// </summary>
    public bool HasResults => Results.Count > 0;

    /// <summary>
    /// Gets whether the search query is valid and non-empty.
    /// </summary>
    public bool HasQuery => !string.IsNullOrEmpty(Query);

    /// <summary>
    /// Gets whether there is an error.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Gets the total number of results found.
    /// </summary>
    public int ResultCount => Results.Count;

    /// <summary>
    /// Gets a human-readable results summary (e.g., "3 of 15").
    /// </summary>
    /// <remarks>
    /// Returns:
    /// <list type="bullet">
    ///   <item><description>"X of Y" when results are found</description></item>
    ///   <item><description>"No results" when query exists but no matches</description></item>
    ///   <item><description>Empty string when no query or searching</description></item>
    /// </list>
    /// </remarks>
    public string ResultsSummary => HasResults
        ? $"{CurrentResultIndex + 1} of {Results.Count}"
        : HasQuery && !IsSearching
            ? "No results"
            : string.Empty;

    /// <summary>
    /// Gets whether navigation to the next result is possible.
    /// </summary>
    /// <remarks>
    /// True if there are results and either:
    /// <list type="bullet">
    ///   <item><description>WrapAround is enabled</description></item>
    ///   <item><description>Current index is not at the last result</description></item>
    /// </list>
    /// </remarks>
    public bool CanNavigateNext => HasResults &&
        (WrapAround || CurrentResultIndex < Results.Count - 1);

    /// <summary>
    /// Gets whether navigation to the previous result is possible.
    /// </summary>
    /// <remarks>
    /// True if there are results and either:
    /// <list type="bullet">
    ///   <item><description>WrapAround is enabled</description></item>
    ///   <item><description>Current index is not at the first result</description></item>
    /// </list>
    /// </remarks>
    public bool CanNavigatePrevious => HasResults &&
        (WrapAround || CurrentResultIndex > 0);

    /// <summary>
    /// Gets whether a valid result is currently selected.
    /// </summary>
    public bool HasCurrentResult => CurrentResult != null;

    /// <summary>
    /// Gets the 1-based display index for the current result.
    /// </summary>
    /// <remarks>
    /// Returns 0 if no result is selected, otherwise CurrentResultIndex + 1.
    /// Suitable for display in UI (e.g., "Result 3 of 15").
    /// </remarks>
    public int CurrentDisplayIndex => HasCurrentResult ? CurrentResultIndex + 1 : 0;

    #endregion

    #region Factory Methods

    /// <summary>
    /// Gets an empty search state with no query or results.
    /// </summary>
    public static TerminalSearchState Empty => new();

    /// <summary>
    /// Creates a new state with the specified query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <returns>A new state with the query set.</returns>
    public static TerminalSearchState ForQuery(string query)
    {
        return new TerminalSearchState { Query = query };
    }

    /// <summary>
    /// Creates a new state indicating a search is in progress.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="caseSensitive">Whether case-sensitive.</param>
    /// <param name="useRegex">Whether using regex.</param>
    /// <returns>A new searching state.</returns>
    public static TerminalSearchState Searching(
        string query,
        bool caseSensitive = false,
        bool useRegex = false)
    {
        return new TerminalSearchState
        {
            Query = query,
            CaseSensitive = caseSensitive,
            UseRegex = useRegex,
            IsSearching = true
        };
    }

    #endregion

    #region State Mutation Methods

    /// <summary>
    /// Creates a new state with updated results.
    /// </summary>
    /// <param name="results">The search results.</param>
    /// <returns>A new state with results and IsSearching set to false.</returns>
    /// <remarks>
    /// Automatically sets CurrentResultIndex to 0 if results are found,
    /// or -1 if no results. Also clears any previous error message.
    /// </remarks>
    public TerminalSearchState WithResults(IReadOnlyList<TerminalSearchResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return this with
        {
            Results = results,
            CurrentResultIndex = results.Count > 0 ? 0 : -1,
            IsSearching = false,
            ErrorMessage = null
        };
    }

    /// <summary>
    /// Creates a new state with an error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A new state with the error set and IsSearching set to false.</returns>
    public TerminalSearchState WithError(string error)
    {
        return this with
        {
            ErrorMessage = error,
            IsSearching = false,
            Results = Array.Empty<TerminalSearchResult>(),
            CurrentResultIndex = -1
        };
    }

    /// <summary>
    /// Creates a new state navigated to the next result.
    /// </summary>
    /// <returns>A new state with updated CurrentResultIndex.</returns>
    /// <remarks>
    /// Respects the WrapAround setting. If at the last result and WrapAround
    /// is true, navigates to the first result. Otherwise stays at the last.
    /// </remarks>
    public TerminalSearchState NavigateNext()
    {
        if (!HasResults)
        {
            return this;
        }

        var newIndex = CurrentResultIndex + 1;
        if (newIndex >= Results.Count)
        {
            newIndex = WrapAround ? 0 : Results.Count - 1;
        }

        return this with { CurrentResultIndex = newIndex };
    }

    /// <summary>
    /// Creates a new state navigated to the previous result.
    /// </summary>
    /// <returns>A new state with updated CurrentResultIndex.</returns>
    /// <remarks>
    /// Respects the WrapAround setting. If at the first result and WrapAround
    /// is true, navigates to the last result. Otherwise stays at the first.
    /// </remarks>
    public TerminalSearchState NavigatePrevious()
    {
        if (!HasResults)
        {
            return this;
        }

        var newIndex = CurrentResultIndex - 1;
        if (newIndex < 0)
        {
            newIndex = WrapAround ? Results.Count - 1 : 0;
        }

        return this with { CurrentResultIndex = newIndex };
    }

    /// <summary>
    /// Creates a new state navigated to a specific index.
    /// </summary>
    /// <param name="index">Target result index.</param>
    /// <returns>A new state with CurrentResultIndex clamped to valid range.</returns>
    public TerminalSearchState NavigateToIndex(int index)
    {
        if (!HasResults)
        {
            return this;
        }

        var clampedIndex = Math.Clamp(index, 0, Results.Count - 1);
        return this with { CurrentResultIndex = clampedIndex };
    }

    /// <summary>
    /// Creates a new state navigated to the first result.
    /// </summary>
    /// <returns>A new state with CurrentResultIndex set to 0.</returns>
    public TerminalSearchState NavigateToFirst()
    {
        if (!HasResults)
        {
            return this;
        }

        return this with { CurrentResultIndex = 0 };
    }

    /// <summary>
    /// Creates a new state navigated to the last result.
    /// </summary>
    /// <returns>A new state with CurrentResultIndex set to the last index.</returns>
    public TerminalSearchState NavigateToLast()
    {
        if (!HasResults)
        {
            return this;
        }

        return this with { CurrentResultIndex = Results.Count - 1 };
    }

    /// <summary>
    /// Creates a new state with toggled case sensitivity.
    /// </summary>
    /// <returns>A new state with CaseSensitive inverted.</returns>
    public TerminalSearchState ToggleCaseSensitive()
    {
        return this with { CaseSensitive = !CaseSensitive };
    }

    /// <summary>
    /// Creates a new state with toggled regex mode.
    /// </summary>
    /// <returns>A new state with UseRegex inverted.</returns>
    public TerminalSearchState ToggleRegex()
    {
        return this with { UseRegex = !UseRegex };
    }

    /// <summary>
    /// Creates a new state with toggled wrap-around mode.
    /// </summary>
    /// <returns>A new state with WrapAround inverted.</returns>
    public TerminalSearchState ToggleWrapAround()
    {
        return this with { WrapAround = !WrapAround };
    }

    /// <summary>
    /// Creates a cleared state ready for a new search.
    /// </summary>
    /// <returns>An empty state preserving only search options.</returns>
    public TerminalSearchState Clear()
    {
        return new TerminalSearchState
        {
            CaseSensitive = CaseSensitive,
            UseRegex = UseRegex,
            WrapAround = WrapAround,
            IncludeScrollback = IncludeScrollback,
            Direction = Direction
        };
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Updates the IsCurrent flag on all results based on CurrentResultIndex.
    /// </summary>
    /// <returns>A new state with updated result flags.</returns>
    /// <remarks>
    /// This creates new result instances with the correct IsCurrent flag.
    /// Usually called after navigation to update rendering state.
    /// </remarks>
    public TerminalSearchState UpdateCurrentFlags()
    {
        if (!HasResults)
        {
            return this;
        }

        var updatedResults = Results.Select((r, index) =>
        {
            if (r.IsCurrent != (index == CurrentResultIndex))
            {
                return new TerminalSearchResult
                {
                    LineIndex = r.LineIndex,
                    StartColumn = r.StartColumn,
                    Length = r.Length,
                    MatchedText = r.MatchedText,
                    LineContent = r.LineContent,
                    IsCurrent = index == CurrentResultIndex
                };
            }
            return r;
        }).ToList();

        return this with { Results = updatedResults };
    }

    #endregion
}
