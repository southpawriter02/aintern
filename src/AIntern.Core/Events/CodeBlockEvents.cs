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
