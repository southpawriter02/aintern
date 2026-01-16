namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TREE PARSE RESULT (v0.4.4b)                                              │
// │ Result of parsing an ASCII tree structure.                               │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Result of parsing an ASCII tree structure.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4b.</para>
/// </remarks>
public sealed record TreeParseResult
{
    /// <summary>
    /// Extracted file paths (directories excluded).
    /// </summary>
    public IReadOnlyList<string> Paths { get; init; } = Array.Empty<string>();

    /// <summary>
    /// All paths including directories.
    /// </summary>
    public IReadOnlyList<string> AllPaths { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Directory paths only.
    /// </summary>
    public IReadOnlyList<string> Directories { get; init; } = Array.Empty<string>();

    /// <summary>
    /// The original raw tree text.
    /// </summary>
    public string RawTreeText { get; init; } = string.Empty;

    /// <summary>
    /// Detected tree format.
    /// </summary>
    public TreeFormat Format { get; init; }

    /// <summary>
    /// Root directory if detected from the tree.
    /// </summary>
    public string? RootDirectory { get; init; }

    /// <summary>
    /// Maximum depth of nesting in the tree.
    /// </summary>
    public int MaxDepth { get; init; }

    /// <summary>
    /// Any comments extracted from the tree.
    /// </summary>
    public IReadOnlyDictionary<string, string> Comments { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Whether parsing was successful.
    /// </summary>
    public bool Success => Paths.Count > 0;

    /// <summary>
    /// Error message if parsing failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Total number of files.
    /// </summary>
    public int FileCount => Paths.Count;

    /// <summary>
    /// Total number of directories.
    /// </summary>
    public int DirectoryCount => Directories.Count;

    // ═══════════════════════════════════════════════════════════════════════
    // Factory Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Create an empty result (no paths found).
    /// </summary>
    public static TreeParseResult Empty(string rawText = "") => new()
    {
        RawTreeText = rawText,
        Format = TreeFormat.Unknown
    };

    /// <summary>
    /// Create a failed result with error message.
    /// </summary>
    public static TreeParseResult Failed(string errorMessage, string rawText = "") => new()
    {
        RawTreeText = rawText,
        ErrorMessage = errorMessage,
        Format = TreeFormat.Unknown
    };
}
