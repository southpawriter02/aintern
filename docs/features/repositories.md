# Repository Layer Feature Documentation

**Version:** v0.2.1d
**Location:** `src/AIntern.Data/Repositories/`

---

## Overview

The Repository Layer provides a clean abstraction over Entity Framework Core operations, encapsulating all database access logic for the AIntern application. This layer separates data access concerns from business logic, enabling better testability and maintainability.

### Key Features

- **Three Repository Interfaces**: Abstractions for conversations, system prompts, and inference presets
- **Sealed Implementations**: Thread-safe, non-inheritable repository classes
- **Comprehensive Logging**: Debug-level logging for all operations
- **Built-in Protection**: Prevents deletion of built-in system prompts and inference presets
- **Soft Delete Support**: System prompts support soft delete with restore capability
- **Automatic Sequencing**: Messages receive automatic sequence numbers

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Service Layer                            │
│     (ConversationService, PromptService, InferenceService)     │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                               │ depends on
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Repository Interfaces                        │
│  ┌──────────────────────┐ ┌───────────────────┐ ┌─────────────┐│
│  │IConversationRepository│ │ISystemPromptRepo  │ │IInferenceRepo│
│  │   (17 methods)        │ │   (13 methods)    │ │ (11 methods) ││
│  └──────────────────────┘ └───────────────────┘ └─────────────┘│
└──────────────────────────────┬──────────────────────────────────┘
                               │
                               │ implements
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Repository Implementations                     │
│  ┌──────────────────────┐ ┌───────────────────┐ ┌─────────────┐│
│  │ConversationRepository │ │SystemPromptRepo   │ │InferenceRepo ││
│  │   (~350 lines)        │ │   (~280 lines)    │ │ (~250 lines) ││
│  └──────────────────────┘ └───────────────────┘ └─────────────┘│
└──────────────────────────────┬──────────────────────────────────┘
                               │
                               │ uses
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                       AInternDbContext                          │
│              (Entity Framework Core DbContext)                   │
└─────────────────────────────────────────────────────────────────┘
```

---

## Repository Interfaces

### IConversationRepository

**Location:** `src/AIntern.Data/Repositories/IConversationRepository.cs`

Manages conversations and their associated messages.

| Category | Method | Description |
|----------|--------|-------------|
| **Read** | `GetByIdAsync` | Get conversation by ID |
| **Read** | `GetByIdWithMessagesAsync` | Get conversation with all messages |
| **Read** | `GetRecentAsync` | Get paginated recent conversations |
| **Read** | `SearchAsync` | Search by title |
| **Read** | `ExistsAsync` | Check if conversation exists |
| **Write** | `CreateAsync` | Create new conversation |
| **Write** | `UpdateAsync` | Update existing conversation |
| **Write** | `DeleteAsync` | Delete conversation and messages |
| **Flags** | `ArchiveAsync` | Archive conversation |
| **Flags** | `UnarchiveAsync` | Unarchive conversation |
| **Flags** | `PinAsync` | Pin conversation |
| **Flags** | `UnpinAsync` | Unpin conversation |
| **Messages** | `AddMessageAsync` | Add message with auto sequence |
| **Messages** | `UpdateMessageAsync` | Update message content |
| **Messages** | `GetMessagesAsync` | Get paginated messages |
| **Messages** | `GetMessageCountAsync` | Get message count |
| **Messages** | `DeleteMessageAsync` | Delete specific message |

---

### ISystemPromptRepository

**Location:** `src/AIntern.Data/Repositories/ISystemPromptRepository.cs`

Manages reusable system prompt templates.

| Category | Method | Description |
|----------|--------|-------------|
| **Read** | `GetByIdAsync` | Get prompt by ID |
| **Read** | `GetDefaultAsync` | Get default prompt |
| **Read** | `GetAllActiveAsync` | Get all active prompts |
| **Read** | `GetByCategoryAsync` | Filter by category |
| **Read** | `GetCategoriesAsync` | Get distinct categories |
| **Read** | `SearchAsync` | Search by name/description |
| **Read** | `NameExistsAsync` | Check name uniqueness |
| **Write** | `CreateAsync` | Create new prompt |
| **Write** | `UpdateAsync` | Update existing prompt |
| **Write** | `DeleteAsync` | Soft delete (IsActive=false) |
| **Write** | `HardDeleteAsync` | Permanent delete (user-created only) |
| **Actions** | `SetAsDefaultAsync` | Set as default prompt |
| **Actions** | `IncrementUsageCountAsync` | Increment usage counter |
| **Actions** | `RestoreAsync` | Restore soft-deleted prompt |

---

### IInferencePresetRepository

**Location:** `src/AIntern.Data/Repositories/IInferencePresetRepository.cs`

Manages inference parameter presets.

| Category | Method | Description |
|----------|--------|-------------|
| **Read** | `GetByIdAsync` | Get preset by ID |
| **Read** | `GetDefaultAsync` | Get default preset |
| **Read** | `GetAllAsync` | Get all presets |
| **Read** | `GetBuiltInAsync` | Get built-in presets |
| **Read** | `GetUserCreatedAsync` | Get user-created presets |
| **Read** | `NameExistsAsync` | Check name uniqueness |
| **Write** | `CreateAsync` | Create new preset |
| **Write** | `UpdateAsync` | Update existing preset |
| **Write** | `DeleteAsync` | Delete (user-created only) |
| **Write** | `SetAsDefaultAsync` | Set as default preset |
| **Write** | `DuplicateAsync` | Duplicate with new name |

---

## Usage with Dependency Injection

### Registration

```csharp
// In Program.cs or Startup.cs
services.AddScoped<IConversationRepository, ConversationRepository>();
services.AddScoped<ISystemPromptRepository, SystemPromptRepository>();
services.AddScoped<IInferencePresetRepository, InferencePresetRepository>();
```

### Injection into Services

```csharp
public class ConversationService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ISystemPromptRepository _promptRepository;

    public ConversationService(
        IConversationRepository conversationRepository,
        ISystemPromptRepository promptRepository)
    {
        _conversationRepository = conversationRepository;
        _promptRepository = promptRepository;
    }

    public async Task<ConversationEntity> StartConversationAsync(string title)
    {
        var defaultPrompt = await _promptRepository.GetDefaultAsync();

        var conversation = new ConversationEntity
        {
            Title = title,
            SystemPromptId = defaultPrompt?.Id
        };

        return await _conversationRepository.CreateAsync(conversation);
    }
}
```

---

## Key Implementation Patterns

### Constructor with Optional Logger

```csharp
public ConversationRepository(
    AInternDbContext context,
    ILogger<ConversationRepository>? logger = null)
{
    _context = context ?? throw new ArgumentNullException(nameof(context));
    _logger = logger ?? NullLogger<ConversationRepository>.Instance;
}
```

### ExecuteUpdateAsync for Flag Operations

```csharp
public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
{
    await _context.Conversations
        .Where(c => c.Id == id)
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(c => c.IsArchived, true)
            .SetProperty(c => c.UpdatedAt, DateTime.UtcNow),
            cancellationToken);
}
```

### Built-in Protection

```csharp
public async Task HardDeleteAsync(Guid id, CancellationToken cancellationToken = default)
{
    var prompt = await _context.SystemPrompts.FindAsync([id], cancellationToken);

    if (prompt?.IsBuiltIn == true)
    {
        _logger.LogWarning(
            "Cannot hard-delete built-in system prompt {PromptId}: {Name}",
            id, prompt.Name);
        return;
    }

    // Proceed with delete...
}
```

### Automatic Sequence Assignment

```csharp
public async Task<MessageEntity> AddMessageAsync(Guid conversationId, MessageEntity message, ...)
{
    var maxSequence = await _context.Messages
        .Where(m => m.ConversationId == conversationId)
        .MaxAsync(m => (int?)m.SequenceNumber) ?? 0;

    message.SequenceNumber = maxSequence + 1;
    // ...
}
```

---

## Logging Strategy

| Level | Operations |
|-------|------------|
| Debug | All CRUD operations, flag changes, message operations, searches |
| Warning | Hard-delete of built-in entities (protection triggered) |

### Structured Log Properties

- `ConversationId`, `MessageId`, `PromptId`, `PresetId` - Entity identifiers
- `Title`, `Name` - Display names
- `Count`, `Skip`, `Take` - Pagination parameters
- `AffectedRows` - ExecuteUpdate results
- `SequenceNumber` - Message ordering

---

## Design Decisions

### 1. Sealed Classes

**Decision:** All repository implementations are `sealed`.
**Rationale:** Prevents inheritance-related issues; repositories are designed for composition.

### 2. Optional Logger Parameter

**Decision:** Logger is optional with `NullLogger<T>` fallback.
**Rationale:** Enables testing without logger setup while supporting production logging.

### 3. IReadOnlyList Return Types

**Decision:** Collection methods return `IReadOnlyList<T>` instead of `List<T>`.
**Rationale:** Expresses immutability intent; prevents accidental mutation.

### 4. Soft Delete for System Prompts

**Decision:** `DeleteAsync` performs soft delete; `HardDeleteAsync` for permanent removal.
**Rationale:** Allows recovery of accidentally deleted prompts; protects built-in prompts.

### 5. Automatic Default Reassignment

**Decision:** Deleting a default preset automatically assigns a new default.
**Rationale:** Ensures there's always a default available for new conversations.

---

## Testing

### Test Infrastructure

```csharp
public class ConversationRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AInternDbContext _context;
    private readonly ConversationRepository _repository;

    public ConversationRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AInternDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AInternDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new ConversationRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
```

### Test Categories

| Repository | Tests | Categories |
|------------|-------|------------|
| ConversationRepository | 12 | Constructor, CRUD, Archive/Pin, Messages, Search |
| SystemPromptRepository | 8 | CRUD, Soft Delete, Hard Delete, Categories |
| InferencePresetRepository | 6 | CRUD, Default Handling, Duplication |

---

## Related Files

- [AInternDbContext](../../src/AIntern.Data/AInternDbContext.cs)
- [ConversationEntity](../../src/AIntern.Core/Entities/ConversationEntity.cs)
- [MessageEntity](../../src/AIntern.Core/Entities/MessageEntity.cs)
- [SystemPromptEntity](../../src/AIntern.Core/Entities/SystemPromptEntity.cs)
- [InferencePresetEntity](../../src/AIntern.Core/Entities/InferencePresetEntity.cs)

---

## Version History

| Version | Changes |
|---------|---------|
| v0.2.1d | Initial implementation with 3 interfaces, 3 implementations, 41 methods, 26 tests |
