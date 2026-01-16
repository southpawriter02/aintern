namespace AIntern.Desktop.Messages;

using CommunityToolkit.Mvvm.Messaging.Messages;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ CLIPBOARD MESSAGES                                                       │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Request to copy text to clipboard (v0.4.1g).
/// </summary>
public sealed class CopyToClipboardRequestMessage : ValueChangedMessage<string>
{
    /// <summary>
    /// Description of the source for notification.
    /// </summary>
    public string? SourceDescription { get; init; }

    public CopyToClipboardRequestMessage(string content) : base(content) { }
}

/// <summary>
/// Notification that text was copied to clipboard (v0.4.1g).
/// </summary>
public sealed class ClipboardCopiedMessage : ValueChangedMessage<string>
{
    /// <summary>
    /// Description of what was copied.
    /// </summary>
    public string? SourceDescription { get; init; }

    public ClipboardCopiedMessage(string content) : base(content) { }
}

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ CODE BLOCK ACTION MESSAGES                                               │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Request to show diff view for a code block (v0.4.1g).
/// </summary>
public sealed class ShowDiffRequestMessage : ValueChangedMessage<CodeBlockViewModel>
{
    /// <summary>
    /// The message containing this code block.
    /// </summary>
    public Guid MessageId { get; init; }

    public ShowDiffRequestMessage(CodeBlockViewModel block) : base(block)
    {
        MessageId = block.MessageId;
    }
}

/// <summary>
/// Request to apply changes from a code block (v0.4.1g).
/// </summary>
public sealed class ApplyChangesRequestMessage : ValueChangedMessage<CodeBlockViewModel>
{
    /// <summary>
    /// Whether to skip the diff preview and apply directly.
    /// </summary>
    public bool SkipDiffPreview { get; init; }

    public ApplyChangesRequestMessage(CodeBlockViewModel block) : base(block) { }
}

/// <summary>
/// Request to apply all code blocks from a message (v0.4.1g).
/// </summary>
public sealed class ApplyAllChangesRequestMessage : ValueChangedMessage<ChatMessageViewModel>
{
    /// <summary>
    /// Whether to skip the diff preview.
    /// </summary>
    public bool SkipDiffPreview { get; init; }

    public ApplyAllChangesRequestMessage(ChatMessageViewModel message) : base(message) { }
}

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ CODE BLOCK STATUS MESSAGES                                               │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Notification that a code block's status changed (v0.4.1g).
/// </summary>
public sealed class CodeBlockStatusChangedMessage
{
    /// <summary>
    /// ID of the code block.
    /// </summary>
    public required Guid BlockId { get; init; }

    /// <summary>
    /// ID of the message containing the block.
    /// </summary>
    public required Guid MessageId { get; init; }

    /// <summary>
    /// Previous status.
    /// </summary>
    public required CodeBlockStatus OldStatus { get; init; }

    /// <summary>
    /// New status.
    /// </summary>
    public required CodeBlockStatus NewStatus { get; init; }

    /// <summary>
    /// Error message if status is Error.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ PATH INFERENCE MESSAGES                                                  │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Request to select a file path for an ambiguous code block (v0.4.1g).
/// </summary>
public sealed class SelectFilePathRequestMessage : AsyncRequestMessage<string?>
{
    /// <summary>
    /// The code block that needs path selection.
    /// </summary>
    public required CodeBlockViewModel Block { get; init; }

    /// <summary>
    /// List of possible paths to choose from.
    /// </summary>
    public required IReadOnlyList<string> PossiblePaths { get; init; }
}

/// <summary>
/// Notification that path inference completed for a block (v0.4.1g).
/// </summary>
public sealed class PathInferenceCompletedMessage
{
    /// <summary>
    /// ID of the code block.
    /// </summary>
    public required Guid BlockId { get; init; }

    /// <summary>
    /// The inferred path, or null if failed.
    /// </summary>
    public required string? InferredPath { get; init; }

    /// <summary>
    /// Confidence score (0.0 to 1.0).
    /// </summary>
    public required float Confidence { get; init; }

    /// <summary>
    /// Whether multiple paths matched.
    /// </summary>
    public required bool IsAmbiguous { get; init; }

    /// <summary>
    /// Alternative paths if ambiguous.
    /// </summary>
    public IReadOnlyList<string>? AlternativePaths { get; init; }
}
