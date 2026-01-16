namespace AIntern.Core.Interfaces;

using AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ INLINE DIFF SERVICE INTERFACE (v0.4.2c)                                  │
// │ Service for computing character-level inline diffs.                      │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Service for computing character-level inline diffs.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.2c.</para>
/// <para>
/// This service enables precise highlighting of character changes within modified lines,
/// allowing users to see exactly which characters were added, removed, or remained unchanged.
/// </para>
/// </remarks>
public interface IInlineDiffService
{
    /// <summary>
    /// Compute inline changes between two lines.
    /// </summary>
    /// <param name="originalLine">The original line content.</param>
    /// <param name="proposedLine">The proposed (modified) line content.</param>
    /// <returns>
    /// A list of inline changes representing character-level modifications.
    /// Returns an empty list if lines are identical.
    /// </returns>
    /// <remarks>
    /// The returned changes include:
    /// <list type="bullet">
    /// <item><description>Unchanged segments (for context)</description></item>
    /// <item><description>Removed segments (characters in original but not in proposed)</description></item>
    /// <item><description>Added segments (characters in proposed but not in original)</description></item>
    /// </list>
    /// </remarks>
    IReadOnlyList<InlineChange> ComputeInlineChanges(
        string originalLine,
        string proposedLine);

    /// <summary>
    /// Split a line into segments with change information for rendering.
    /// </summary>
    /// <param name="content">The line content to segment.</param>
    /// <param name="changes">The inline changes computed for this line pair.</param>
    /// <param name="side">Which side of the diff (Original or Proposed).</param>
    /// <returns>
    /// Segments for rendering, filtered appropriately for the specified side.
    /// Original side excludes Added segments; Proposed side excludes Removed segments.
    /// </returns>
    IReadOnlyList<InlineSegment> GetInlineSegments(
        string content,
        IReadOnlyList<InlineChange> changes,
        DiffSide side);
}
