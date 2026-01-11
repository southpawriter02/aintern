namespace AIntern.Core.Events;

/// <summary>
/// Event arguments for when the conversation list changes.
/// </summary>
public sealed class ConversationListChangedEventArgs : EventArgs
{
    /// <summary>
    /// The type of change that occurred.
    /// </summary>
    public ConversationListChangeType ChangeType { get; }

    /// <summary>
    /// The ID of the affected conversation, if applicable.
    /// </summary>
    public Guid? ConversationId { get; }

    public ConversationListChangedEventArgs(ConversationListChangeType changeType, Guid? conversationId = null)
    {
        ChangeType = changeType;
        ConversationId = conversationId;
    }
}

/// <summary>
/// Types of changes that can occur to the conversation list.
/// </summary>
public enum ConversationListChangeType
{
    /// <summary>A new conversation was added.</summary>
    Added,

    /// <summary>A conversation was removed.</summary>
    Removed,

    /// <summary>A conversation was updated (title, message count, etc.).</summary>
    Updated,

    /// <summary>The entire list was refreshed.</summary>
    Refreshed
}
