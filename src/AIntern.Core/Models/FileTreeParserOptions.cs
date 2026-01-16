namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ FILE TREE PARSER OPTIONS (v0.4.4b)                                       │
// │ Configuration options for the file tree parser.                          │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Configuration options for the file tree parser.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4b.</para>
/// </remarks>
public sealed record FileTreeParserOptions
{
    /// <summary>
    /// Minimum number of files required to create a proposal.
    /// </summary>
    /// <remarks>
    /// A single file doesn't constitute a "multi-file" proposal.
    /// Default is 2.
    /// </remarks>
    public int MinimumFilesForProposal { get; init; } = 2;

    /// <summary>
    /// Maximum tree depth to parse.
    /// </summary>
    /// <remarks>
    /// Prevents stack overflow on malformed input.
    /// Default is 20.
    /// </remarks>
    public int MaxTreeDepth { get; init; } = 20;

    /// <summary>
    /// Maximum number of files to include in a proposal.
    /// </summary>
    /// <remarks>
    /// Very large proposals may be impractical.
    /// Default is 100.
    /// </remarks>
    public int MaxFilesInProposal { get; init; } = 100;

    /// <summary>
    /// Phrases that indicate a file structure follows.
    /// </summary>
    public IReadOnlyList<string> StructureIndicators { get; init; } = new[]
    {
        "project structure",
        "file structure",
        "folder structure",
        "directory structure",
        "here's the structure",
        "create these files",
        "create the following",
        "files to create",
        "following files",
        "file layout"
    };

    /// <summary>
    /// Whether to enable simple indented listing parsing.
    /// </summary>
    public bool EnableSimpleListing { get; init; } = true;

    /// <summary>
    /// Whether to trim inline comments from paths.
    /// </summary>
    public bool TrimComments { get; init; } = true;

    /// <summary>
    /// Whether to preserve the raw tree text in the proposal.
    /// </summary>
    public bool PreserveRawTreeText { get; init; } = true;

    /// <summary>
    /// Maximum description length to extract.
    /// </summary>
    public int MaxDescriptionLength { get; init; } = 200;

    /// <summary>
    /// Minimum description length to accept.
    /// </summary>
    public int MinDescriptionLength { get; init; } = 10;

    /// <summary>
    /// Whether to require structure indicators for tree detection.
    /// </summary>
    public bool RequireStructureIndicator { get; init; } = true;

    /// <summary>
    /// Code block types to include in proposals.
    /// </summary>
    public IReadOnlyList<CodeBlockType> IncludedBlockTypes { get; init; } = new[]
    {
        CodeBlockType.CompleteFile,
        CodeBlockType.Snippet
    };

    // ═══════════════════════════════════════════════════════════════════════
    // Static Instances
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Default options.
    /// </summary>
    public static FileTreeParserOptions Default { get; } = new();

    /// <summary>
    /// Lenient options (fewer restrictions).
    /// </summary>
    public static FileTreeParserOptions Lenient { get; } = new()
    {
        MinimumFilesForProposal = 1,
        RequireStructureIndicator = false,
        MaxFilesInProposal = 500
    };

    /// <summary>
    /// Strict options (more restrictions).
    /// </summary>
    public static FileTreeParserOptions Strict { get; } = new()
    {
        MinimumFilesForProposal = 3,
        RequireStructureIndicator = true,
        MaxFilesInProposal = 50
    };
}
