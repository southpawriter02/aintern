using AIntern.Core.Models;

namespace AIntern.Core.Entities;

/// <summary>
/// Entity class for persisting chat messages to the database.
/// </summary>
public sealed class MessageEntity
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int? TokenCount { get; set; }
    public int? GenerationTimeMs { get; set; }
    public int SequenceNumber { get; set; }

    // Navigation property
    public ConversationEntity? Conversation { get; set; }
}
