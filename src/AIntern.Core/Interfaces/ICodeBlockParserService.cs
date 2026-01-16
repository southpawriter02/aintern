namespace AIntern.Core.Interfaces;

using AIntern.Core.Models;

/// <summary>
/// Service for parsing code blocks from LLM response content (v0.4.1b).
/// </summary>
public interface ICodeBlockParserService
{
    /// <summary>
    /// Extract all code blocks from a complete message.
    /// </summary>
    /// <param name="content">The full message content (markdown).</param>
    /// <param name="messageId">ID of the source message.</param>
    /// <returns>List of extracted code blocks.</returns>
    IReadOnlyList<CodeBlock> ParseMessage(string content, Guid messageId);

    /// <summary>
    /// Create a CodeProposal from extracted code blocks with context-aware inference.
    /// </summary>
    /// <param name="content">The full message content.</param>
    /// <param name="messageId">ID of the source message.</param>
    /// <param name="attachedFilePaths">File paths attached to the conversation for inference.</param>
    /// <returns>A proposal containing all code blocks with inferred metadata.</returns>
    CodeProposal CreateProposal(
        string content,
        Guid messageId,
        IReadOnlyList<string>? attachedFilePaths = null);

    /// <summary>
    /// Parse a single code block from raw fence content.
    /// </summary>
    /// <param name="fenceContent">Content including the opening fence (```lang).</param>
    /// <param name="messageId">ID of the source message.</param>
    /// <param name="sequenceNumber">Position in message.</param>
    /// <returns>Parsed code block or null if invalid.</returns>
    CodeBlock? ParseSingleBlock(string fenceContent, Guid messageId, int sequenceNumber);

    /// <summary>
    /// Check if content contains any code blocks.
    /// </summary>
    /// <param name="content">The content to check.</param>
    /// <returns>True if at least one code block is present.</returns>
    bool ContainsCodeBlocks(string content);

    /// <summary>
    /// Get the count of code blocks without fully parsing.
    /// </summary>
    /// <param name="content">The content to check.</param>
    /// <returns>Number of code blocks found.</returns>
    int CountCodeBlocks(string content);
}
