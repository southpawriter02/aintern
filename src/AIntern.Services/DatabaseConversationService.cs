using AIntern.Core.Entities;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Services;

/// <summary>
/// Conversation service implementation with SQLite persistence.
/// Provides auto-save with debouncing and full conversation history management.
/// </summary>
public sealed class DatabaseConversationService : IConversationService, IDisposable
{
    private readonly IConversationRepository _conversationRepository;
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private readonly System.Timers.Timer _autoSaveTimer;

    private Conversation _currentConversation = new();
    private Guid? _currentConversationDbId;
    private bool _hasUnsavedChanges;
    private bool _disposed;

    private const int AutoSaveDelayMs = 500;

    public DatabaseConversationService(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));

        _autoSaveTimer = new System.Timers.Timer(AutoSaveDelayMs);
        _autoSaveTimer.AutoReset = false;
        _autoSaveTimer.Elapsed += async (_, _) => await SaveCurrentConversationInternalAsync();
    }

    public Conversation CurrentConversation => _currentConversation;

    public bool HasUnsavedChanges => _hasUnsavedChanges;

    public event EventHandler? ConversationChanged;
    public event EventHandler<ConversationListChangedEventArgs>? ConversationListChanged;

    public void AddMessage(ChatMessage message)
    {
        _currentConversation.Messages.Add(message);
        _currentConversation.UpdatedAt = DateTime.UtcNow;
        _hasUnsavedChanges = true;
        ScheduleAutoSave();
        ConversationChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateMessage(Guid messageId, Action<ChatMessage> updateAction)
    {
        var message = _currentConversation.Messages.FirstOrDefault(m => m.Id == messageId);
        if (message is not null)
        {
            updateAction(message);
            _currentConversation.UpdatedAt = DateTime.UtcNow;
            _hasUnsavedChanges = true;
            ScheduleAutoSave();
            ConversationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ClearConversation()
    {
        _currentConversation = new Conversation();
        _currentConversationDbId = null;
        _hasUnsavedChanges = false;
        ConversationChanged?.Invoke(this, EventArgs.Empty);
    }

    public IEnumerable<ChatMessage> GetMessages() => _currentConversation.Messages.AsReadOnly();

    public Conversation CreateNewConversation()
    {
        _currentConversation = new Conversation();
        _currentConversationDbId = null;
        _hasUnsavedChanges = false;
        ConversationChanged?.Invoke(this, EventArgs.Empty);
        return _currentConversation;
    }

    // ============ Persistence Methods ============

    public async Task<IReadOnlyList<ConversationSummary>> GetRecentConversationsAsync(int count = 50, CancellationToken ct = default)
    {
        var entities = await _conversationRepository.GetRecentAsync(count, ct);
        return entities.Select(MapToSummary).ToList();
    }

    public async Task<Conversation> LoadConversationAsync(Guid conversationId, CancellationToken ct = default)
    {
        var entity = await _conversationRepository.GetByIdWithMessagesAsync(conversationId, ct);

        if (entity is null)
        {
            throw new InvalidOperationException($"Conversation with ID {conversationId} not found.");
        }

        _currentConversation = MapToConversation(entity);
        _currentConversationDbId = entity.Id;
        _hasUnsavedChanges = false;

        ConversationChanged?.Invoke(this, EventArgs.Empty);
        return _currentConversation;
    }

    public async Task SaveCurrentConversationAsync(CancellationToken ct = default)
    {
        await SaveCurrentConversationInternalAsync(ct);
    }

    public async Task<Conversation> CreateNewConversationAsync(string? title = null, Guid? systemPromptId = null, CancellationToken ct = default)
    {
        var conversation = new Conversation
        {
            Title = title ?? "New Conversation"
        };

        var entity = new ConversationEntity
        {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            SystemPromptId = systemPromptId,
            IsArchived = false,
            MessageCount = 0
        };

        await _conversationRepository.CreateAsync(entity, ct);

        _currentConversation = conversation;
        _currentConversationDbId = entity.Id;
        _hasUnsavedChanges = false;

        ConversationChanged?.Invoke(this, EventArgs.Empty);
        ConversationListChanged?.Invoke(this, new ConversationListChangedEventArgs(
            ConversationListChangeType.Added, entity.Id));

        return conversation;
    }

    public async Task DeleteConversationAsync(Guid conversationId, CancellationToken ct = default)
    {
        await _conversationRepository.DeleteAsync(conversationId, ct);

        // If we deleted the current conversation, create a new one
        if (_currentConversationDbId == conversationId)
        {
            CreateNewConversation();
        }

        ConversationListChanged?.Invoke(this, new ConversationListChangedEventArgs(
            ConversationListChangeType.Removed, conversationId));
    }

    public async Task RenameConversationAsync(Guid conversationId, string newTitle, CancellationToken ct = default)
    {
        var entity = await _conversationRepository.GetByIdAsync(conversationId, ct);

        if (entity is null)
        {
            throw new InvalidOperationException($"Conversation with ID {conversationId} not found.");
        }

        entity.Title = newTitle;
        entity.UpdatedAt = DateTime.UtcNow;
        await _conversationRepository.UpdateAsync(entity, ct);

        // Update current conversation if it's the one being renamed
        if (_currentConversationDbId == conversationId)
        {
            _currentConversation.Title = newTitle;
            ConversationChanged?.Invoke(this, EventArgs.Empty);
        }

        ConversationListChanged?.Invoke(this, new ConversationListChangedEventArgs(
            ConversationListChangeType.Updated, conversationId));
    }

    public async Task<IReadOnlyList<ConversationSummary>> SearchConversationsAsync(string query, CancellationToken ct = default)
    {
        var entities = await _conversationRepository.SearchAsync(query, ct);
        return entities.Select(MapToSummary).ToList();
    }

    // ============ Private Methods ============

    private void ScheduleAutoSave()
    {
        _autoSaveTimer.Stop();
        _autoSaveTimer.Start();
    }

    private async Task SaveCurrentConversationInternalAsync(CancellationToken ct = default)
    {
        if (!_hasUnsavedChanges)
        {
            return;
        }

        await _saveLock.WaitAsync(ct);
        try
        {
            if (_currentConversationDbId is null)
            {
                // First save - create in database
                var entity = MapToEntity(_currentConversation);
                await _conversationRepository.CreateAsync(entity, ct);
                _currentConversationDbId = entity.Id;

                ConversationListChanged?.Invoke(this, new ConversationListChangedEventArgs(
                    ConversationListChangeType.Added, entity.Id));
            }
            else
            {
                // Update existing conversation
                var existingEntity = await _conversationRepository.GetByIdWithMessagesAsync(_currentConversationDbId.Value, ct);

                if (existingEntity is not null)
                {
                    // Update basic properties
                    existingEntity.Title = GenerateTitle();
                    existingEntity.UpdatedAt = DateTime.UtcNow;
                    existingEntity.ModelPath = _currentConversation.ModelPath;
                    existingEntity.MessageCount = _currentConversation.Messages.Count;

                    await _conversationRepository.UpdateAsync(existingEntity, ct);

                    // Add any new messages
                    var existingMessageIds = existingEntity.Messages.Select(m => m.Id).ToHashSet();
                    var newMessages = _currentConversation.Messages
                        .Where(m => !existingMessageIds.Contains(m.Id))
                        .ToList();

                    foreach (var message in newMessages)
                    {
                        var messageEntity = MapToMessageEntity(message, _currentConversationDbId.Value);
                        await _conversationRepository.AddMessageAsync(_currentConversationDbId.Value, messageEntity, ct);
                    }

                    // Update existing messages that may have changed
                    foreach (var message in _currentConversation.Messages)
                    {
                        if (existingMessageIds.Contains(message.Id))
                        {
                            var messageEntity = MapToMessageEntity(message, _currentConversationDbId.Value);
                            await _conversationRepository.UpdateMessageAsync(messageEntity, ct);
                        }
                    }

                    ConversationListChanged?.Invoke(this, new ConversationListChangedEventArgs(
                        ConversationListChangeType.Updated, _currentConversationDbId.Value));
                }
            }

            _hasUnsavedChanges = false;
        }
        finally
        {
            _saveLock.Release();
        }
    }

    private string GenerateTitle()
    {
        // Auto-generate title from first user message if still "New Conversation"
        if (_currentConversation.Title == "New Conversation")
        {
            var firstUserMessage = _currentConversation.Messages
                .FirstOrDefault(m => m.Role == MessageRole.User);

            if (firstUserMessage is not null)
            {
                var title = firstUserMessage.Content.Trim();
                // Truncate to reasonable length
                if (title.Length > 50)
                {
                    title = title[..47] + "...";
                }
                _currentConversation.Title = title;
            }
        }

        return _currentConversation.Title;
    }

    private static ConversationSummary MapToSummary(ConversationEntity entity)
    {
        var firstMessage = entity.Messages
            .Where(m => m.Role == MessageRole.User)
            .OrderBy(m => m.SequenceNumber)
            .FirstOrDefault();

        string? preview = null;
        if (firstMessage is not null)
        {
            preview = firstMessage.Content.Length > 100
                ? firstMessage.Content[..97] + "..."
                : firstMessage.Content;
        }

        return new ConversationSummary(
            entity.Id,
            entity.Title,
            entity.UpdatedAt,
            entity.MessageCount,
            preview);
    }

    private static Conversation MapToConversation(ConversationEntity entity)
    {
        var messages = entity.Messages
            .OrderBy(m => m.SequenceNumber)
            .Select(MapToMessage)
            .ToList();

        return new Conversation
        {
            Title = entity.Title,
            ModelPath = entity.ModelPath,
            Messages = { }
        };
    }

    private static ChatMessage MapToMessage(MessageEntity entity)
    {
        return new ChatMessage
        {
            Role = entity.Role,
            Content = entity.Content,
            TokenCount = entity.TokenCount,
            GenerationTime = entity.GenerationTimeMs.HasValue
                ? TimeSpan.FromMilliseconds(entity.GenerationTimeMs.Value)
                : null
        };
    }

    private ConversationEntity MapToEntity(Conversation conversation)
    {
        var entity = new ConversationEntity
        {
            Id = conversation.Id,
            Title = GenerateTitle(),
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            ModelPath = conversation.ModelPath,
            IsArchived = false,
            MessageCount = conversation.Messages.Count
        };

        int sequenceNumber = 0;
        foreach (var message in conversation.Messages)
        {
            entity.Messages.Add(MapToMessageEntity(message, entity.Id, sequenceNumber++));
        }

        return entity;
    }

    private static MessageEntity MapToMessageEntity(ChatMessage message, Guid conversationId, int sequenceNumber = 0)
    {
        return new MessageEntity
        {
            Id = message.Id,
            ConversationId = conversationId,
            Role = message.Role,
            Content = message.Content,
            Timestamp = message.Timestamp,
            TokenCount = message.TokenCount,
            GenerationTimeMs = message.GenerationTime.HasValue
                ? (int)message.GenerationTime.Value.TotalMilliseconds
                : null,
            SequenceNumber = sequenceNumber
        };
    }

    public void Dispose()
    {
        if (_disposed) return;

        _autoSaveTimer.Stop();
        _autoSaveTimer.Dispose();
        _saveLock.Dispose();
        _disposed = true;
    }
}
