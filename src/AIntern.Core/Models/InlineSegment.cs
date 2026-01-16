namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ INLINE SEGMENT (v0.4.2c)                                                 │
// │ A segment of text with inline change information for rendering.         │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// A segment of text with inline change information for rendering.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.2c.</para>
/// <para>
/// InlineSegment is the rendering-focused counterpart to InlineChange.
/// While InlineChange tracks the diff computation results, InlineSegment
/// represents what should actually be displayed on a specific side of the diff.
/// </para>
/// </remarks>
public sealed class InlineSegment
{
    /// <summary>
    /// The text content of this segment.
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Whether this segment represents changed text.
    /// </summary>
    /// <remarks>
    /// When true, this segment should be visually highlighted in the diff view.
    /// The Type property indicates whether it was added or removed.
    /// </remarks>
    public bool IsChanged { get; init; }

    /// <summary>
    /// The type of change this segment represents.
    /// </summary>
    public InlineChangeType Type { get; init; }

    /// <summary>
    /// Character length of this segment.
    /// </summary>
    public int Length => Text.Length;

    /// <summary>
    /// Whether this segment is empty.
    /// </summary>
    public bool IsEmpty => string.IsNullOrEmpty(Text);

    // ═══════════════════════════════════════════════════════════════════════
    // Static Factory Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates an unchanged segment.
    /// </summary>
    public static InlineSegment Unchanged(string text) => new()
    {
        Text = text,
        IsChanged = false,
        Type = InlineChangeType.Unchanged
    };

    /// <summary>
    /// Creates an added segment.
    /// </summary>
    public static InlineSegment Added(string text) => new()
    {
        Text = text,
        IsChanged = true,
        Type = InlineChangeType.Added
    };

    /// <summary>
    /// Creates a removed segment.
    /// </summary>
    public static InlineSegment Removed(string text) => new()
    {
        Text = text,
        IsChanged = true,
        Type = InlineChangeType.Removed
    };
}
