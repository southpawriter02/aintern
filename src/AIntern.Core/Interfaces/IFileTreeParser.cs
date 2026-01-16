using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ I FILE TREE PARSER (v0.4.4b)                                             │
// │ Contract for parsing multi-file proposals from LLM responses.            │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Parser for detecting and extracting multi-file proposals from LLM responses.
/// </summary>
/// <remarks>
/// <para>
/// The parser analyzes LLM response content to identify:
/// </para>
/// <list type="number">
///   <item>ASCII file tree structures (├└│── format)</item>
///   <item>Multiple code blocks with file path annotations</item>
///   <item>Contextual descriptions about the proposed structure</item>
/// </list>
/// <para>Added in v0.4.4b.</para>
/// </remarks>
public interface IFileTreeParser
{
    /// <summary>
    /// Parse a message to detect and extract multi-file proposals.
    /// </summary>
    /// <param name="content">The full message content from the LLM.</param>
    /// <param name="messageId">ID of the source message for linking.</param>
    /// <param name="codeBlocks">Pre-extracted code blocks from the message.</param>
    /// <returns>
    /// A FileTreeProposal if multi-file proposal detected (2+ files), null otherwise.
    /// </returns>
    /// <remarks>
    /// Detection requires at least 2 code blocks with TargetFilePath set.
    /// The parser correlates ASCII trees with code blocks when present.
    /// </remarks>
    FileTreeProposal? ParseProposal(
        string content,
        Guid messageId,
        IReadOnlyList<CodeBlock> codeBlocks);

    /// <summary>
    /// Try to parse a proposal with detailed result information.
    /// </summary>
    /// <param name="content">The full message content.</param>
    /// <param name="messageId">ID of the source message.</param>
    /// <param name="codeBlocks">Pre-extracted code blocks.</param>
    /// <param name="proposal">The parsed proposal if successful.</param>
    /// <param name="reason">Reason for failure if unsuccessful.</param>
    /// <returns>True if a valid proposal was parsed.</returns>
    bool TryParseProposal(
        string content,
        Guid messageId,
        IReadOnlyList<CodeBlock> codeBlocks,
        out FileTreeProposal? proposal,
        out string? reason);

    /// <summary>
    /// Detect if content contains a file tree structure.
    /// </summary>
    /// <param name="content">The content to analyze.</param>
    /// <returns>True if an ASCII file tree is detected.</returns>
    /// <remarks>
    /// Checks for both structure indicator phrases and tree-like patterns.
    /// Does not validate the tree structure itself.
    /// </remarks>
    bool ContainsFileTree(string content);

    /// <summary>
    /// Parse an ASCII file tree into a list of file paths.
    /// </summary>
    /// <param name="treeContent">The tree content (without code fences).</param>
    /// <returns>List of file paths extracted from the tree.</returns>
    /// <remarks>
    /// Handles multiple tree formats:
    /// - Standard: ├── └── │
    /// - Simple: indented paths
    /// - Mixed: combination of both
    /// Directories (ending in /) are excluded from the result.
    /// </remarks>
    IReadOnlyList<string> ParseAsciiTree(string treeContent);

    /// <summary>
    /// Parse an ASCII tree into a structured node tree.
    /// </summary>
    /// <param name="treeContent">The tree content.</param>
    /// <returns>Root node of the parsed tree structure.</returns>
    ParsedTreeNode? ParseAsciiTreeStructured(string treeContent);

    /// <summary>
    /// Extract description from surrounding text.
    /// </summary>
    /// <param name="content">The full message content.</param>
    /// <returns>
    /// Extracted description near structure indicators, or null if not found.
    /// </returns>
    /// <remarks>
    /// Looks for text near phrases like "project structure" or "files to create".
    /// Cleans markdown formatting from the extracted text.
    /// </remarks>
    string? ExtractDescription(string content);

    /// <summary>
    /// Find the common root path from a collection of paths.
    /// </summary>
    /// <param name="paths">Collection of file paths.</param>
    /// <returns>Common root directory, or empty string if none.</returns>
    string FindCommonRoot(IEnumerable<string> paths);

    /// <summary>
    /// Validate that a path is suitable for file operations.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>True if the path is valid for use.</returns>
    bool IsValidPath(string path);
}
