namespace AIntern.Core.Models;

/// <summary>
/// Represents a single code block extracted from an LLM response (v0.4.1a).
/// </summary>
public sealed class CodeBlock
{
    /// <summary>
    /// Unique identifier for this code block.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Raw content of the code block (without markdown fences).
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Detected or specified programming language identifier.
    /// Examples: "csharp", "python", "javascript", "bash"
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Normalized language name for display.
    /// Examples: "C#", "Python", "JavaScript", "Bash"
    /// </summary>
    public string? DisplayLanguage { get; init; }

    /// <summary>
    /// Inferred or specified target file path (relative to workspace).
    /// May be set by fence hint (```csharp:src/Foo.cs) or inferred from context.
    /// </summary>
    public string? TargetFilePath { get; set; }

    /// <summary>
    /// Classification of this code block's purpose.
    /// </summary>
    public CodeBlockType BlockType { get; init; } = CodeBlockType.Snippet;

    /// <summary>
    /// Line range in original file this block should replace (for snippets).
    /// Null for complete file replacements or unknown targets.
    /// </summary>
    public LineRange? ReplacementRange { get; set; }

    /// <summary>
    /// Source message ID that contains this code block.
    /// </summary>
    public Guid MessageId { get; init; }

    /// <summary>
    /// Position in message (0-based index for multiple code blocks).
    /// </summary>
    public int SequenceNumber { get; init; }

    /// <summary>
    /// Start and end character positions in the original message content.
    /// </summary>
    public TextRange SourceRange { get; init; }

    /// <summary>
    /// Confidence level for inferred properties (0.0 to 1.0).
    /// Lower confidence may indicate uncertain language or path inference.
    /// </summary>
    public float ConfidenceScore { get; set; } = 1.0f;

    /// <summary>
    /// Timestamp when this code block was extracted.
    /// </summary>
    public DateTime ExtractedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Current status of this code block in the apply workflow.
    /// </summary>
    public CodeBlockStatus Status { get; set; } = CodeBlockStatus.Pending;

    /// <summary>
    /// Whether this block can be applied (has target path and is applicable type).
    /// </summary>
    public bool IsApplicable =>
        BlockType is CodeBlockType.CompleteFile or CodeBlockType.Snippet or CodeBlockType.Config
        && !string.IsNullOrEmpty(TargetFilePath);

    /// <summary>
    /// Line count of the code content.
    /// </summary>
    public int LineCount => Content.Split('\n').Length;

    /// <summary>
    /// Creates a copy of this CodeBlock with the specified modifications.
    /// </summary>
    public CodeBlock With(
        string? content = null,
        string? language = null,
        string? targetFilePath = null,
        CodeBlockType? blockType = null,
        CodeBlockStatus? status = null,
        float? confidenceScore = null)
    {
        return new CodeBlock
        {
            Id = Id,
            Content = content ?? Content,
            Language = language ?? Language,
            DisplayLanguage = DisplayLanguage,
            TargetFilePath = targetFilePath ?? TargetFilePath,
            BlockType = blockType ?? BlockType,
            ReplacementRange = ReplacementRange,
            MessageId = MessageId,
            SequenceNumber = SequenceNumber,
            SourceRange = SourceRange,
            ConfidenceScore = confidenceScore ?? ConfidenceScore,
            ExtractedAt = ExtractedAt,
            Status = status ?? Status
        };
    }
}
