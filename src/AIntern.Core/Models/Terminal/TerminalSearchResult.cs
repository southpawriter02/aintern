// ============================================================================
// File: TerminalSearchResult.cs
// Path: src/AIntern.Core/Models/Terminal/TerminalSearchResult.cs
// Description: Represents a single search result match within the terminal buffer.
//              Provides position information for highlighting and navigation,
//              along with context extraction helpers for preview display.
// Created: 2026-01-18
// AI Intern v0.5.5a - Terminal Search Models
// ============================================================================

namespace AIntern.Core.Models.Terminal;

/// <summary>
/// Represents a single search result match within the terminal buffer.
/// Provides position information for highlighting and navigation.
/// </summary>
/// <remarks>
/// <para>
/// This model is designed to work with the terminal search system, providing:
/// </para>
/// <list type="bullet">
///   <item><description>Precise positioning within the terminal buffer (line and column)</description></item>
///   <item><description>Context extraction for preview display</description></item>
///   <item><description>Current selection state for navigation</description></item>
/// </list>
/// <para>
/// Coordinate System:
/// <code>
/// Line 0: (scrollback)  "$ npm install"
/// Line 1: (scrollback)  "added 523 packages"
/// Line 2: (scrollback)  "$ npm test"
/// ...
/// Line 847:             "ERROR: Module not found: 'react'"
///                        ↑                     ↑
///                        │StartColumn=0        │StartColumn=24
///                        └─ Length=5 ("ERROR") └─ Length=5 ("react")
/// </code>
/// </para>
/// </remarks>
public sealed class TerminalSearchResult
{
    #region Constants

    /// <summary>
    /// Default context length in characters for preview generation.
    /// </summary>
    public const int DefaultContextLength = 20;

    /// <summary>
    /// Maximum context length to prevent excessive memory usage.
    /// </summary>
    public const int MaxContextLength = 100;

    /// <summary>
    /// Ellipsis string used when context is truncated.
    /// </summary>
    public const string Ellipsis = "...";

    #endregion

    #region Position Properties

    /// <summary>
    /// Gets the line index in the terminal buffer (0-based, including scrollback).
    /// </summary>
    /// <remarks>
    /// Line indices are zero-based and include scrollback lines. Line 0 is the
    /// oldest line in the scrollback buffer. To convert to a viewport-relative
    /// position, subtract the first visible line index.
    /// </remarks>
    public int LineIndex { get; init; }

    /// <summary>
    /// Gets the starting column of the match (0-based).
    /// </summary>
    /// <remarks>
    /// Column index is zero-based. For wide characters (e.g., CJK, emoji),
    /// each character occupies the cell count based on its display width.
    /// </remarks>
    public int StartColumn { get; init; }

    /// <summary>
    /// Gets the length of the matched text in characters.
    /// </summary>
    /// <remarks>
    /// Length is measured in characters, not cells. For wide characters,
    /// the actual display width may differ from the length value.
    /// </remarks>
    public int Length { get; init; }

    /// <summary>
    /// Gets the ending column (exclusive) calculated from StartColumn + Length.
    /// Useful for rendering the highlight range.
    /// </summary>
    /// <remarks>
    /// This is a computed property that returns the exclusive end position.
    /// To iterate over match columns: for (int col = StartColumn; col &lt; EndColumn; col++)
    /// </remarks>
    public int EndColumn => StartColumn + Length;

    #endregion

    #region Content Properties

    /// <summary>
    /// Gets the actual matched text content.
    /// </summary>
    /// <remarks>
    /// This is the exact text that was matched. For case-insensitive searches,
    /// this preserves the original casing from the buffer.
    /// </remarks>
    public string MatchedText { get; init; } = string.Empty;

    /// <summary>
    /// Gets the full line content for context display.
    /// </summary>
    /// <remarks>
    /// Contains the complete line text including the match and surrounding context.
    /// This is used for generating preview strings and context display.
    /// </remarks>
    public string LineContent { get; init; } = string.Empty;

    #endregion

    #region State Properties

    /// <summary>
    /// Gets or sets whether this result is the currently selected/focused result.
    /// This property is mutable to allow navigation state updates.
    /// </summary>
    /// <remarks>
    /// When rendering, the current result should be highlighted differently
    /// (e.g., brighter background, border) to indicate it is the focus of navigation.
    /// </remarks>
    public bool IsCurrent { get; set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets whether the match has valid content.
    /// A match is valid if it has positive length and non-empty matched text.
    /// </summary>
    public bool IsValid => Length > 0 && !string.IsNullOrEmpty(MatchedText);

    /// <summary>
    /// Gets whether this match is at the start of its line.
    /// </summary>
    public bool IsAtLineStart => StartColumn == 0;

    /// <summary>
    /// Gets whether this match is at the end of its line.
    /// </summary>
    public bool IsAtLineEnd => EndColumn >= LineContent.Length;

    /// <summary>
    /// Gets the character count available before this match on the line.
    /// </summary>
    public int AvailableContextBefore => StartColumn;

    /// <summary>
    /// Gets the character count available after this match on the line.
    /// </summary>
    public int AvailableContextAfter => 
        string.IsNullOrEmpty(LineContent) ? 0 : Math.Max(0, LineContent.Length - EndColumn);

    #endregion

    #region Context Methods

    /// <summary>
    /// Gets context text before the match (for preview display).
    /// </summary>
    /// <param name="maxLength">Maximum characters to include. Clamped to 1-100.</param>
    /// <returns>Text before the match, with ellipsis if truncated.</returns>
    /// <remarks>
    /// If the available context exceeds maxLength, the returned string is truncated
    /// from the beginning and prefixed with "..." to indicate omitted content.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = new TerminalSearchResult
    /// {
    ///     StartColumn = 25,
    ///     LineContent = "This is a long line with 'react' in it"
    /// };
    /// var before = result.GetContextBefore(10); // Returns "line with "
    /// </code>
    /// </example>
    public string GetContextBefore(int maxLength = DefaultContextLength)
    {
        // Validate and clamp input
        maxLength = Math.Clamp(maxLength, 1, MaxContextLength);

        // No context available if at line start or empty line
        if (StartColumn <= 0 || string.IsNullOrEmpty(LineContent))
        {
            return string.Empty;
        }

        // Clamp start column to line length for safety
        var effectiveStartColumn = Math.Min(StartColumn, LineContent.Length);

        // Calculate how much context we can get
        var start = Math.Max(0, effectiveStartColumn - maxLength);
        var length = effectiveStartColumn - start;

        // Extract the context substring
        var context = LineContent.Substring(start, length);

        // Add ellipsis if we truncated from the beginning
        return start > 0 ? Ellipsis + context : context;
    }

    /// <summary>
    /// Gets context text after the match (for preview display).
    /// </summary>
    /// <param name="maxLength">Maximum characters to include. Clamped to 1-100.</param>
    /// <returns>Text after the match, with ellipsis if truncated.</returns>
    /// <remarks>
    /// If the available context exceeds maxLength, the returned string is truncated
    /// at the end and suffixed with "..." to indicate omitted content.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = new TerminalSearchResult
    /// {
    ///     StartColumn = 10,
    ///     Length = 5,
    ///     LineContent = "This is a react component for testing"
    /// };
    /// var after = result.GetContextAfter(10); // Returns " component..."
    /// </code>
    /// </example>
    public string GetContextAfter(int maxLength = DefaultContextLength)
    {
        // Validate and clamp input
        maxLength = Math.Clamp(maxLength, 1, MaxContextLength);

        // No context available if at line end or empty line
        if (string.IsNullOrEmpty(LineContent) || EndColumn >= LineContent.Length)
        {
            return string.Empty;
        }

        // Calculate available context
        var available = LineContent.Length - EndColumn;
        var length = Math.Min(available, maxLength);

        // Extract the context substring
        var context = LineContent.Substring(EndColumn, length);

        // Add ellipsis if we truncated at the end
        return EndColumn + length < LineContent.Length ? context + Ellipsis : context;
    }

    /// <summary>
    /// Creates a formatted preview string showing the match in context.
    /// </summary>
    /// <param name="beforeLength">Characters to show before match. Default: 20.</param>
    /// <param name="afterLength">Characters to show after match. Default: 20.</param>
    /// <returns>Formatted preview string with match highlighted by brackets.</returns>
    /// <remarks>
    /// The returned format is: "{context before}[{matched text}]{context after}"
    /// where the matched text is enclosed in square brackets for visibility.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = new TerminalSearchResult
    /// {
    ///     StartColumn = 25,
    ///     Length = 5,
    ///     MatchedText = "react",
    ///     LineContent = "ERROR: Module not found: 'react' in node_modules"
    /// };
    /// var preview = result.ToPreviewString(10, 10);
    /// // Returns "not found: [react] in node_..."
    /// </code>
    /// </example>
    public string ToPreviewString(int beforeLength = DefaultContextLength, int afterLength = DefaultContextLength)
    {
        var before = GetContextBefore(beforeLength);
        var after = GetContextAfter(afterLength);
        return $"{before}[{MatchedText}]{after}";
    }

    /// <summary>
    /// Creates a compact preview string with configurable total length.
    /// </summary>
    /// <param name="maxTotalLength">Maximum total length of the preview string.</param>
    /// <returns>Compact preview string that fits within the specified length.</returns>
    public string ToCompactPreview(int maxTotalLength = 50)
    {
        // Reserve space for match and brackets
        var matchWithBrackets = $"[{MatchedText}]";
        var remaining = maxTotalLength - matchWithBrackets.Length;

        if (remaining <= 0)
        {
            // Not enough room, truncate the match itself
            var truncatedMatch = MatchedText.Length > maxTotalLength - 2
                ? MatchedText.Substring(0, maxTotalLength - 5) + Ellipsis
                : MatchedText;
            return $"[{truncatedMatch}]";
        }

        // Split remaining space between before and after
        var beforeLen = remaining / 2;
        var afterLen = remaining - beforeLen;

        return $"{GetContextBefore(beforeLen)}{matchWithBrackets}{GetContextAfter(afterLen)}";
    }

    #endregion

    #region Comparison Methods

    /// <summary>
    /// Checks if this result overlaps with another result.
    /// </summary>
    /// <param name="other">The other result to check.</param>
    /// <returns>True if the results overlap on the same line.</returns>
    public bool OverlapsWith(TerminalSearchResult other)
    {
        if (other is null || LineIndex != other.LineIndex)
        {
            return false;
        }

        return StartColumn < other.EndColumn && EndColumn > other.StartColumn;
    }

    /// <summary>
    /// Checks if this result comes before another result in document order.
    /// </summary>
    /// <param name="other">The other result to compare.</param>
    /// <returns>True if this result comes before the other.</returns>
    public bool IsBefore(TerminalSearchResult other)
    {
        if (other is null) return true;

        return LineIndex < other.LineIndex ||
               (LineIndex == other.LineIndex && StartColumn < other.StartColumn);
    }

    /// <summary>
    /// Checks if this result comes after another result in document order.
    /// </summary>
    /// <param name="other">The other result to compare.</param>
    /// <returns>True if this result comes after the other.</returns>
    public bool IsAfter(TerminalSearchResult other)
    {
        if (other is null) return false;

        return LineIndex > other.LineIndex ||
               (LineIndex == other.LineIndex && StartColumn > other.StartColumn);
    }

    #endregion

    #region Object Overrides

    /// <summary>
    /// Returns a string representation of this search result.
    /// </summary>
    /// <returns>A string in the format "Line {Index}, Col {Start}-{End}: \"{Text}\"".</returns>
    public override string ToString()
    {
        return $"Line {LineIndex}, Col {StartColumn}-{EndColumn}: \"{MatchedText}\"";
    }

    /// <summary>
    /// Determines whether the specified object is equal to this result.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>True if equal by value.</returns>
    public override bool Equals(object? obj)
    {
        return obj is TerminalSearchResult other &&
               LineIndex == other.LineIndex &&
               StartColumn == other.StartColumn &&
               Length == other.Length &&
               MatchedText == other.MatchedText;
    }

    /// <summary>
    /// Returns a hash code for this search result.
    /// </summary>
    /// <returns>Hash code based on position and matched text.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(LineIndex, StartColumn, Length, MatchedText);
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new search result with the specified properties.
    /// </summary>
    /// <param name="lineIndex">Line index in the buffer.</param>
    /// <param name="startColumn">Start column of the match.</param>
    /// <param name="length">Length of the match.</param>
    /// <param name="matchedText">The matched text.</param>
    /// <param name="lineContent">Full line content.</param>
    /// <returns>A new TerminalSearchResult instance.</returns>
    public static TerminalSearchResult Create(
        int lineIndex,
        int startColumn,
        int length,
        string matchedText,
        string lineContent)
    {
        return new TerminalSearchResult
        {
            LineIndex = lineIndex,
            StartColumn = startColumn,
            Length = length,
            MatchedText = matchedText,
            LineContent = lineContent
        };
    }

    /// <summary>
    /// Creates a search result from a regex match.
    /// </summary>
    /// <param name="lineIndex">Line index in the buffer.</param>
    /// <param name="match">The regex match.</param>
    /// <param name="lineContent">Full line content.</param>
    /// <returns>A new TerminalSearchResult instance.</returns>
    public static TerminalSearchResult FromRegexMatch(
        int lineIndex,
        System.Text.RegularExpressions.Match match,
        string lineContent)
    {
        ArgumentNullException.ThrowIfNull(match);

        return new TerminalSearchResult
        {
            LineIndex = lineIndex,
            StartColumn = match.Index,
            Length = match.Length,
            MatchedText = match.Value,
            LineContent = lineContent
        };
    }

    #endregion
}
