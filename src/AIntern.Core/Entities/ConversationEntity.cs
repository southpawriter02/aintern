namespace AIntern.Core.Entities;

/// <summary>
/// Entity class for persisting conversations to the database.
/// </summary>
public sealed class ConversationEntity
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "New Conversation";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? SystemPromptId { get; set; }
    public string? ModelPath { get; set; }
    public bool IsArchived { get; set; }
    public int MessageCount { get; set; }

    // Navigation properties
    public SystemPromptEntity? SystemPrompt { get; set; }
    public ICollection<MessageEntity> Messages { get; set; } = new List<MessageEntity>();
}
