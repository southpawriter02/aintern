namespace AIntern.Core.Events;

using AIntern.Core.Models;

/// <summary>
/// Event args for when a code block is extracted (v0.4.1b).
/// </summary>
public sealed class CodeBlockExtractedEventArgs : EventArgs
{
    /// <summary>
    /// The extracted code block.
    /// </summary>
    public required CodeBlock CodeBlock { get; init; }

    /// <summary>
    /// ID of the message containing this block.
    /// </summary>
    public required Guid MessageId { get; init; }

    /// <summary>
    /// Total number of blocks found in the message.
    /// </summary>
    public required int TotalBlocksInMessage { get; init; }
}

/// <summary>
/// Event args for when a proposal is created (v0.4.1b).
/// </summary>
public sealed class CodeProposalCreatedEventArgs : EventArgs
{
    /// <summary>
    /// The created proposal.
    /// </summary>
    public required CodeProposal Proposal { get; init; }
}

/// <summary>
/// Event args for parsing errors (v0.4.1b).
/// </summary>
public sealed class CodeBlockParseErrorEventArgs : EventArgs
{
    /// <summary>
    /// The content that failed to parse.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Description of the error.
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Location in the original content where the error occurred.
    /// </summary>
    public required TextRange Location { get; init; }
}

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ STREAMING PARSER EVENTS (v0.4.1f)                                        │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Event args raised when a code block starts during streaming (v0.4.1f).
/// </summary>
public sealed class CodeBlockStartedEventArgs : EventArgs
{
    /// <summary>
    /// The partial code block that just started.
    /// </summary>
    public required PartialCodeBlock Block { get; init; }

    /// <summary>
    /// ID of the message containing this block.
    /// </summary>
    public required Guid MessageId { get; init; }

    /// <summary>
    /// Sequence number of this block within the message (0-based).
    /// </summary>
    public int SequenceNumber => Block.SequenceNumber;

    /// <summary>
    /// Language detected from fence line (may be null).
    /// </summary>
    public string? Language => Block.Language;

    /// <summary>
    /// Target file path if specified in fence line.
    /// </summary>
    public string? TargetFilePath => Block.TargetFilePath;
}

/// <summary>
/// Event args raised when content is added to a streaming code block (v0.4.1f).
/// </summary>
public sealed class CodeBlockContentEventArgs : EventArgs
{
    /// <summary>
    /// The content that was just added.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// The partial code block receiving content.
    /// </summary>
    public required PartialCodeBlock Block { get; init; }

    /// <summary>
    /// Whether this content includes a newline.
    /// </summary>
    public bool ContainsNewline => Content.Contains('\n');

    /// <summary>
    /// Total content accumulated so far.
    /// </summary>
    public string TotalContent => Block.Content.ToString();

    /// <summary>
    /// Current line count in the block.
    /// </summary>
    public int LineCount => Block.LineCount;
}

/// <summary>
/// Event args raised when a code block is completed during streaming (v0.4.1f).
/// </summary>
public sealed class CodeBlockCompletedEventArgs : EventArgs
{
    /// <summary>
    /// The completed code block.
    /// </summary>
    public required CodeBlock Block { get; init; }

    /// <summary>
    /// ID of the message containing this block.
    /// </summary>
    public required Guid MessageId { get; init; }

    /// <summary>
    /// Time taken to stream this code block.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Sequence number of this block within the message (0-based).
    /// </summary>
    public int SequenceNumber => Block.SequenceNumber;

    /// <summary>
    /// Number of lines in the completed block.
    /// </summary>
    public int LineCount => Block.LineCount;

    /// <summary>
    /// Whether the block was truncated (incomplete fence at EOF).
    /// </summary>
    public bool WasTruncated { get; init; }
}

/// <summary>
/// Event args raised when a streaming parsing error occurs (v0.4.1f).
/// </summary>
public sealed class StreamingParseErrorEventArgs : EventArgs
{
    /// <summary>
    /// Description of the error.
    /// </summary>
    public required string Error { get; init; }

    /// <summary>
    /// Position in the stream where the error occurred.
    /// </summary>
    public int Position { get; init; }

    /// <summary>
    /// The partial block that was being parsed, if any.
    /// </summary>
    public PartialCodeBlock? Block { get; init; }

    /// <summary>
    /// ID of the message being parsed.
    /// </summary>
    public Guid MessageId { get; init; }
}
