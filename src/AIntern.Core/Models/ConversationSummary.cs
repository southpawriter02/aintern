namespace AIntern.Core.Models;

/// <summary>
/// Lightweight summary of a conversation for display in conversation lists.
/// </summary>
/// <param name="Id">The unique identifier of the conversation.</param>
/// <param name="Title">The display title of the conversation.</param>
/// <param name="UpdatedAt">When the conversation was last updated.</param>
/// <param name="MessageCount">The number of messages in the conversation.</param>
/// <param name="FirstMessagePreview">Optional preview text from the first user message.</param>
public record ConversationSummary(
    Guid Id,
    string Title,
    DateTime UpdatedAt,
    int MessageCount,
    string? FirstMessagePreview);
