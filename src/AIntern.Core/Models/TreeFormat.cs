namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TREE FORMAT (v0.4.4b)                                                    │
// │ Format of the ASCII tree structure.                                      │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Format of the ASCII tree structure.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4b.</para>
/// </remarks>
public enum TreeFormat
{
    /// <summary>
    /// Standard Unicode box-drawing characters (├└│──).
    /// </summary>
    Standard,

    /// <summary>
    /// ASCII-only characters (+|`-).
    /// </summary>
    AsciiOnly,

    /// <summary>
    /// Simple indented listing.
    /// </summary>
    Indented,

    /// <summary>
    /// Mixed format (combination of styles).
    /// </summary>
    Mixed,

    /// <summary>
    /// Unknown or unrecognized format.
    /// </summary>
    Unknown
}
