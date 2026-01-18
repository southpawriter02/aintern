// ============================================================================
// File: TerminalSearchExtensions.cs
// Path: src/AIntern.Core/Models/Terminal/TerminalSearchExtensions.cs
// Description: Extension methods for terminal search functionality providing
//              viewport filtering, line index queries, nearest result finding,
//              and other utility operations on search results and state.
// Created: 2026-01-18
// AI Intern v0.5.5a - Terminal Search Models
// ============================================================================

namespace AIntern.Core.Models.Terminal;

/// <summary>
/// Extension methods for terminal search functionality.
/// </summary>
/// <remarks>
/// <para>
/// This class provides utility methods for working with terminal search results:
/// </para>
/// <list type="bullet">
///   <item><description>Viewport filtering - determine which results are visible</description></item>
///   <item><description>Line indexing - get unique line indices for scrollbar markers</description></item>
///   <item><description>Nearest finding - locate results closest to a position</description></item>
///   <item><description>Range queries - filter results within line ranges</description></item>
/// </list>
/// <para>
/// Example Usage:
/// <code>
/// // Filter to visible results
/// var visible = state.GetVisibleResults(firstVisibleLine, visibleLineCount);
/// 
/// // Get lines with matches for scrollbar markers
/// var matchLines = state.GetMatchingLineIndices();
/// 
/// // Find nearest result to current scroll position
/// var nearestIdx = state.FindNearestResultIndex(currentLine, SearchDirection.Forward);
/// </code>
/// </para>
/// </remarks>
public static class TerminalSearchExtensions
{
    #region Result Visibility

    /// <summary>
    /// Checks if a result is within the visible viewport.
    /// </summary>
    /// <param name="result">The search result to check.</param>
    /// <param name="firstVisibleLine">First visible line index in the viewport.</param>
    /// <param name="visibleLineCount">Number of visible lines in the viewport.</param>
    /// <returns>True if the result is visible in the current viewport.</returns>
    /// <remarks>
    /// A result is considered visible if its line index falls within the range
    /// [firstVisibleLine, firstVisibleLine + visibleLineCount).
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when result is null.</exception>
    /// <example>
    /// <code>
    /// var result = new TerminalSearchResult { LineIndex = 50 };
    /// bool isVisible = result.IsVisible(40, 25); // true if lines 40-64 are visible
    /// </code>
    /// </example>
    public static bool IsVisible(
        this TerminalSearchResult result,
        int firstVisibleLine,
        int visibleLineCount)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.LineIndex >= firstVisibleLine &&
               result.LineIndex < firstVisibleLine + visibleLineCount;
    }

    /// <summary>
    /// Checks if a result is above the visible viewport (in scrollback).
    /// </summary>
    /// <param name="result">The search result to check.</param>
    /// <param name="firstVisibleLine">First visible line index in the viewport.</param>
    /// <returns>True if the result is above (before) the visible viewport.</returns>
    public static bool IsAboveViewport(
        this TerminalSearchResult result,
        int firstVisibleLine)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.LineIndex < firstVisibleLine;
    }

    /// <summary>
    /// Checks if a result is below the visible viewport.
    /// </summary>
    /// <param name="result">The search result to check.</param>
    /// <param name="firstVisibleLine">First visible line index in the viewport.</param>
    /// <param name="visibleLineCount">Number of visible lines in the viewport.</param>
    /// <returns>True if the result is below (after) the visible viewport.</returns>
    public static bool IsBelowViewport(
        this TerminalSearchResult result,
        int firstVisibleLine,
        int visibleLineCount)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.LineIndex >= firstVisibleLine + visibleLineCount;
    }

    #endregion

    #region State Visibility Filtering

    /// <summary>
    /// Filters results to only those in the visible viewport.
    /// </summary>
    /// <param name="state">The search state containing results.</param>
    /// <param name="firstVisibleLine">First visible line index.</param>
    /// <param name="visibleLineCount">Number of visible lines.</param>
    /// <returns>Enumerable of results that are currently visible.</returns>
    /// <remarks>
    /// Use this to optimize rendering by only processing visible matches.
    /// Results maintain their original order.
    /// </remarks>
    public static IEnumerable<TerminalSearchResult> GetVisibleResults(
        this TerminalSearchState state,
        int firstVisibleLine,
        int visibleLineCount)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state.Results.Where(r => r.IsVisible(firstVisibleLine, visibleLineCount));
    }

    /// <summary>
    /// Gets the count of results in the visible viewport.
    /// </summary>
    /// <param name="state">The search state containing results.</param>
    /// <param name="firstVisibleLine">First visible line index.</param>
    /// <param name="visibleLineCount">Number of visible lines.</param>
    /// <returns>Number of visible results.</returns>
    public static int GetVisibleResultCount(
        this TerminalSearchState state,
        int firstVisibleLine,
        int visibleLineCount)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state.Results.Count(r => r.IsVisible(firstVisibleLine, visibleLineCount));
    }

    /// <summary>
    /// Gets the count of results above the visible viewport.
    /// </summary>
    /// <param name="state">The search state containing results.</param>
    /// <param name="firstVisibleLine">First visible line index.</param>
    /// <returns>Number of results above the viewport.</returns>
    public static int GetResultsAboveCount(
        this TerminalSearchState state,
        int firstVisibleLine)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state.Results.Count(r => r.IsAboveViewport(firstVisibleLine));
    }

    /// <summary>
    /// Gets the count of results below the visible viewport.
    /// </summary>
    /// <param name="state">The search state containing results.</param>
    /// <param name="firstVisibleLine">First visible line index.</param>
    /// <param name="visibleLineCount">Number of visible lines.</param>
    /// <returns>Number of results below the viewport.</returns>
    public static int GetResultsBelowCount(
        this TerminalSearchState state,
        int firstVisibleLine,
        int visibleLineCount)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state.Results.Count(r => r.IsBelowViewport(firstVisibleLine, visibleLineCount));
    }

    #endregion

    #region Line Index Queries

    /// <summary>
    /// Gets the line indices that contain matches (for scrollbar markers).
    /// </summary>
    /// <param name="state">The search state containing results.</param>
    /// <returns>Set of unique line indices that have matches.</returns>
    /// <remarks>
    /// Use this to render match indicators on the scrollbar.
    /// Each line index appears at most once regardless of match count.
    /// </remarks>
    public static IReadOnlySet<int> GetMatchingLineIndices(this TerminalSearchState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state.Results.Select(r => r.LineIndex).ToHashSet();
    }

    /// <summary>
    /// Gets the number of matches per line for lines that have matches.
    /// </summary>
    /// <param name="state">The search state containing results.</param>
    /// <returns>Dictionary mapping line indices to match counts.</returns>
    /// <remarks>
    /// Useful for displaying match density indicators.
    /// </remarks>
    public static IReadOnlyDictionary<int, int> GetMatchCountPerLine(
        this TerminalSearchState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state.Results
            .GroupBy(r => r.LineIndex)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Gets results grouped by line.
    /// </summary>
    /// <param name="state">The search state containing results.</param>
    /// <returns>Lookup of results grouped by line index.</returns>
    public static ILookup<int, TerminalSearchResult> GetResultsByLine(
        this TerminalSearchState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state.Results.ToLookup(r => r.LineIndex);
    }

    #endregion

    #region Nearest Result Finding

    /// <summary>
    /// Finds the nearest result to a given line.
    /// </summary>
    /// <param name="state">The search state containing results.</param>
    /// <param name="lineIndex">The line index to search from.</param>
    /// <param name="direction">Preferred direction if no exact match.</param>
    /// <returns>Index of the nearest result, or -1 if no results.</returns>
    /// <remarks>
    /// <para>
    /// Search priority:
    /// <list type="number">
    ///   <item><description>Exact match on lineIndex</description></item>
    ///   <item><description>Nearest in preferred direction</description></item>
    ///   <item><description>Nearest in opposite direction</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static int FindNearestResultIndex(
        this TerminalSearchState state,
        int lineIndex,
        SearchDirection direction = SearchDirection.Forward)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!state.HasResults)
        {
            return -1;
        }

        // Try to find exact match first
        for (int i = 0; i < state.Results.Count; i++)
        {
            if (state.Results[i].LineIndex == lineIndex)
            {
                return i;
            }
        }

        // Find nearest in preferred direction
        int nearestIndex = -1;
        int minDistance = int.MaxValue;

        for (int i = 0; i < state.Results.Count; i++)
        {
            var result = state.Results[i];
            var distance = Math.Abs(result.LineIndex - lineIndex);

            // Check direction preference
            bool isPreferredDirection = direction == SearchDirection.Forward
                ? result.LineIndex >= lineIndex
                : result.LineIndex <= lineIndex;

            if (distance < minDistance ||
                (distance == minDistance && isPreferredDirection))
            {
                minDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    /// <summary>
    /// Finds the first result at or after a given line.
    /// </summary>
    /// <param name="state">The search state containing results.</param>
    /// <param name="lineIndex">The minimum line index.</param>
    /// <returns>Index of the first matching result, or -1 if none.</returns>
    public static int FindFirstResultAtOrAfter(
        this TerminalSearchState state,
        int lineIndex)
    {
        ArgumentNullException.ThrowIfNull(state);

        for (int i = 0; i < state.Results.Count; i++)
        {
            if (state.Results[i].LineIndex >= lineIndex)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Finds the last result at or before a given line.
    /// </summary>
    /// <param name="state">The search state containing results.</param>
    /// <param name="lineIndex">The maximum line index.</param>
    /// <returns>Index of the last matching result, or -1 if none.</returns>
    public static int FindLastResultAtOrBefore(
        this TerminalSearchState state,
        int lineIndex)
    {
        ArgumentNullException.ThrowIfNull(state);

        for (int i = state.Results.Count - 1; i >= 0; i--)
        {
            if (state.Results[i].LineIndex <= lineIndex)
            {
                return i;
            }
        }

        return -1;
    }

    #endregion

    #region Range Queries

    /// <summary>
    /// Gets results within a line range.
    /// </summary>
    /// <param name="state">The search state containing results.</param>
    /// <param name="startLine">Start line index (inclusive).</param>
    /// <param name="endLine">End line index (inclusive).</param>
    /// <returns>Enumerable of results within the range.</returns>
    public static IEnumerable<TerminalSearchResult> GetResultsInRange(
        this TerminalSearchState state,
        int startLine,
        int endLine)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state.Results.Where(r =>
            r.LineIndex >= startLine && r.LineIndex <= endLine);
    }

    /// <summary>
    /// Gets results on a specific line.
    /// </summary>
    /// <param name="state">The search state containing results.</param>
    /// <param name="lineIndex">The line index to filter by.</param>
    /// <returns>Enumerable of results on the specified line.</returns>
    public static IEnumerable<TerminalSearchResult> GetResultsOnLine(
        this TerminalSearchState state,
        int lineIndex)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state.Results.Where(r => r.LineIndex == lineIndex);
    }

    /// <summary>
    /// Checks if a specific line has any matches.
    /// </summary>
    /// <param name="state">The search state containing results.</param>
    /// <param name="lineIndex">The line index to check.</param>
    /// <returns>True if the line has at least one match.</returns>
    public static bool HasMatchOnLine(
        this TerminalSearchState state,
        int lineIndex)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state.Results.Any(r => r.LineIndex == lineIndex);
    }

    #endregion

    #region Current Result Utilities

    /// <summary>
    /// Updates the IsCurrent flag on all results based on a target index.
    /// </summary>
    /// <param name="results">The results to update.</param>
    /// <param name="currentIndex">The index that should be marked as current.</param>
    /// <remarks>
    /// This modifies the IsCurrent property on each result in the list.
    /// Use when results need to reflect a new current selection.
    /// </remarks>
    public static void UpdateCurrentFlags(
        this IReadOnlyList<TerminalSearchResult> results,
        int currentIndex)
    {
        ArgumentNullException.ThrowIfNull(results);

        for (int i = 0; i < results.Count; i++)
        {
            results[i].IsCurrent = (i == currentIndex);
        }
    }

    /// <summary>
    /// Clears the IsCurrent flag on all results.
    /// </summary>
    /// <param name="results">The results to update.</param>
    public static void ClearCurrentFlags(
        this IReadOnlyList<TerminalSearchResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        foreach (var result in results)
        {
            result.IsCurrent = false;
        }
    }

    /// <summary>
    /// Gets the index of the result marked as current.
    /// </summary>
    /// <param name="results">The results to search.</param>
    /// <returns>Index of the current result, or -1 if none is marked.</returns>
    public static int GetCurrentIndex(
        this IReadOnlyList<TerminalSearchResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].IsCurrent)
            {
                return i;
            }
        }

        return -1;
    }

    #endregion

    #region Position Calculations

    /// <summary>
    /// Calculates the scroll position needed to center a result in the viewport.
    /// </summary>
    /// <param name="result">The result to scroll to.</param>
    /// <param name="visibleLineCount">Number of visible lines in the viewport.</param>
    /// <param name="totalLineCount">Total number of lines in the buffer.</param>
    /// <returns>The first visible line index to center the result.</returns>
    public static int CalculateCenteredScrollPosition(
        this TerminalSearchResult result,
        int visibleLineCount,
        int totalLineCount)
    {
        ArgumentNullException.ThrowIfNull(result);

        // Calculate the line that should be at the top to center the result
        var targetFirstLine = result.LineIndex - (visibleLineCount / 2);

        // Clamp to valid range
        var maxFirstLine = Math.Max(0, totalLineCount - visibleLineCount);
        return Math.Clamp(targetFirstLine, 0, maxFirstLine);
    }

    /// <summary>
    /// Gets the relative position of a result within the buffer (0.0-1.0).
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="totalLineCount">Total number of lines in the buffer.</param>
    /// <returns>Relative position (0.0 = top, 1.0 = bottom).</returns>
    /// <remarks>
    /// Useful for positioning scrollbar markers.
    /// </remarks>
    public static double GetRelativePosition(
        this TerminalSearchResult result,
        int totalLineCount)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (totalLineCount <= 1)
        {
            return 0.0;
        }

        return (double)result.LineIndex / (totalLineCount - 1);
    }

    #endregion
}
