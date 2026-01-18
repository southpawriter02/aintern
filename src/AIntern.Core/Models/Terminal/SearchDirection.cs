// ============================================================================
// File: SearchDirection.cs
// Path: src/AIntern.Core/Models/Terminal/SearchDirection.cs
// Description: Enumeration defining search direction for terminal find operations.
//              Used by TerminalSearchState and search services for navigation.
// Created: 2026-01-18
// AI Intern v0.5.5a - Terminal Search Models
// ============================================================================

namespace AIntern.Core.Models.Terminal;

/// <summary>
/// Specifies the direction for incremental search operations within the terminal buffer.
/// </summary>
/// <remarks>
/// <para>
/// The search direction affects:
/// </para>
/// <list type="bullet">
///   <item><description>Which result is selected first when starting a search</description></item>
///   <item><description>How navigation between results progresses</description></item>
///   <item><description>Where the search starts when FindNearestResultIndex is called</description></item>
/// </list>
/// <para>
/// In a terminal context:
/// <list type="bullet">
///   <item><description>Forward searches from older lines toward newer lines (top to bottom)</description></item>
///   <item><description>Backward searches from newer lines toward older lines (bottom to top)</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var state = new TerminalSearchState
/// {
///     Query = "error",
///     Direction = SearchDirection.Backward // Start from bottom, search upward
/// };
/// </code>
/// </example>
public enum SearchDirection
{
    /// <summary>
    /// Search forward from current position.
    /// </summary>
    /// <remarks>
    /// Forward direction:
    /// <list type="bullet">
    ///   <item><description>Searches from older lines to newer lines</description></item>
    ///   <item><description>Navigates from top of buffer toward bottom</description></item>
    ///   <item><description>First result is typically the earliest match</description></item>
    /// </list>
    /// This is the default search direction.
    /// </remarks>
    Forward = 0,

    /// <summary>
    /// Search backward from current position.
    /// </summary>
    /// <remarks>
    /// Backward direction:
    /// <list type="bullet">
    ///   <item><description>Searches from newer lines to older lines</description></item>
    ///   <item><description>Navigates from bottom of buffer toward top</description></item>
    ///   <item><description>First result is typically the most recent match</description></item>
    /// </list>
    /// Useful when searching for recent output or errors.
    /// </remarks>
    Backward = 1
}

/// <summary>
/// Extension methods for <see cref="SearchDirection"/> enumeration.
/// </summary>
public static class SearchDirectionExtensions
{
    /// <summary>
    /// Gets the opposite search direction.
    /// </summary>
    /// <param name="direction">The current search direction.</param>
    /// <returns>The opposite direction.</returns>
    /// <example>
    /// <code>
    /// var direction = SearchDirection.Forward;
    /// var opposite = direction.Opposite(); // Returns SearchDirection.Backward
    /// </code>
    /// </example>
    public static SearchDirection Opposite(this SearchDirection direction)
    {
        return direction == SearchDirection.Forward
            ? SearchDirection.Backward
            : SearchDirection.Forward;
    }

    /// <summary>
    /// Gets a display-friendly description of the search direction.
    /// </summary>
    /// <param name="direction">The search direction.</param>
    /// <returns>A human-readable description.</returns>
    public static string ToDescription(this SearchDirection direction)
    {
        return direction switch
        {
            SearchDirection.Forward => "Forward (top to bottom)",
            SearchDirection.Backward => "Backward (bottom to top)",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Gets the multiplier for index navigation based on direction.
    /// </summary>
    /// <param name="direction">The search direction.</param>
    /// <returns>+1 for Forward, -1 for Backward.</returns>
    /// <remarks>
    /// Useful for calculating next/previous indices:
    /// <code>
    /// newIndex = currentIndex + direction.NavigationStep();
    /// </code>
    /// </remarks>
    public static int NavigationStep(this SearchDirection direction)
    {
        return direction == SearchDirection.Forward ? 1 : -1;
    }

    /// <summary>
    /// Checks if this direction searches toward newer content.
    /// </summary>
    /// <param name="direction">The search direction.</param>
    /// <returns>True if searching toward newer (more recent) content.</returns>
    public static bool IsTowardNewer(this SearchDirection direction)
    {
        return direction == SearchDirection.Forward;
    }

    /// <summary>
    /// Checks if this direction searches toward older content.
    /// </summary>
    /// <param name="direction">The search direction.</param>
    /// <returns>True if searching toward older (scrollback) content.</returns>
    public static bool IsTowardOlder(this SearchDirection direction)
    {
        return direction == SearchDirection.Backward;
    }
}
