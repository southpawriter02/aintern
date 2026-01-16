namespace AIntern.Core.Models;

using System.Text;

/// <summary>
/// Represents a code block that is currently being parsed during streaming (v0.4.1f).
/// </summary>
public sealed class PartialCodeBlock
{
    /// <summary>
    /// Unique identifier for this code block.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// ID of the message containing this code block.
    /// </summary>
    public Guid MessageId { get; init; }

    /// <summary>
    /// Zero-based sequence number within the message.
    /// First code block is 0, second is 1, etc.
    /// </summary>
    public int SequenceNumber { get; init; }

    /// <summary>
    /// Detected or specified programming language.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Display name for the language (e.g., "C#" for "csharp").
    /// </summary>
    public string? DisplayLanguage { get; set; }

    /// <summary>
    /// Target file path if specified in fence line.
    /// </summary>
    public string? TargetFilePath { get; set; }

    /// <summary>
    /// Accumulated code content.
    /// </summary>
    public StringBuilder Content { get; } = new();

    /// <summary>
    /// Character position where the code block starts (at the opening fence).
    /// </summary>
    public int StartPosition { get; init; }

    /// <summary>
    /// Timestamp when parsing started for this block.
    /// </summary>
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Type of fence delimiter (backtick or tilde).
    /// </summary>
    public FenceType FenceType { get; init; } = FenceType.Backtick;

    /// <summary>
    /// Number of fence characters (3 for ```, 4 for ````, etc.).
    /// </summary>
    public int FenceLength { get; init; } = 3;

    /// <summary>
    /// Current line count in the content.
    /// </summary>
    public int LineCount => Content.Length == 0 ? 0 : Content.ToString().Split('\n').Length;

    /// <summary>
    /// Raw fence line content (language + optional path).
    /// </summary>
    public string? FenceLine { get; set; }

    /// <summary>
    /// Convert this partial block to a completed CodeBlock.
    /// </summary>
    /// <param name="endPosition">Character position at end of block.</param>
    /// <returns>Completed CodeBlock model.</returns>
    public CodeBlock ToCodeBlock(int endPosition)
    {
        var content = Content.ToString();

        // Trim trailing newline if present (before closing fence)
        if (content.EndsWith('\n'))
            content = content[..^1];

        return new CodeBlock
        {
            Id = Id,
            MessageId = MessageId,
            SequenceNumber = SequenceNumber,
            Language = Language,
            DisplayLanguage = DisplayLanguage,
            TargetFilePath = TargetFilePath,
            Content = content.Trim(),
            SourceRange = new TextRange(StartPosition, endPosition),
            // BlockType will be set by classifier after conversion
        };
    }

    /// <summary>
    /// Append content to this block.
    /// </summary>
    public void AppendContent(string text)
    {
        Content.Append(text);
    }

    /// <summary>
    /// Append a single character to this block.
    /// </summary>
    public void AppendContent(char ch)
    {
        Content.Append(ch);
    }

    /// <summary>
    /// Remove characters from the end of content.
    /// Used to strip closing fence if detected within content.
    /// </summary>
    public void RemoveFromEnd(int count)
    {
        if (count > 0 && count <= Content.Length)
        {
            Content.Remove(Content.Length - count, count);
        }
    }

    /// <summary>
    /// Check if content ends with the specified string.
    /// </summary>
    public bool ContentEndsWith(string suffix)
    {
        if (string.IsNullOrEmpty(suffix) || Content.Length < suffix.Length)
            return false;

        for (int i = 0; i < suffix.Length; i++)
        {
            if (Content[Content.Length - suffix.Length + i] != suffix[i])
                return false;
        }
        return true;
    }

    public override string ToString() =>
        $"PartialCodeBlock[{SequenceNumber}] Lang={Language} Lines={LineCount} Path={TargetFilePath}";
}
