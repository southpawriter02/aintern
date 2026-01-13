# AInternDbContext Feature Documentation

**Version:** v0.2.1c
**Location:** `src/AIntern.Data/AInternDbContext.cs`

---

## Overview

`AInternDbContext` is the Entity Framework Core DbContext for the AIntern application. It provides access to all database entities and implements automatic timestamp management for consistent data tracking.

### Key Features

- **Four DbSet Properties**: Conversations, Messages, SystemPrompts, InferencePresets
- **Automatic Timestamps**: CreatedAt/UpdatedAt managed automatically on save
- **Configuration Auto-Discovery**: Entity configurations loaded via `ApplyConfigurationsFromAssembly`
- **Comprehensive Logging**: Debug, Information, Warning, and Error level logging
- **Dual Constructors**: Support for dependency injection and design-time tools

---

## DbSet Properties

| Property | Entity | Description |
|----------|--------|-------------|
| `Conversations` | `ConversationEntity` | Chat sessions containing messages |
| `Messages` | `MessageEntity` | Individual messages within conversations |
| `SystemPrompts` | `SystemPromptEntity` | Reusable system prompt templates |
| `InferencePresets` | `InferencePresetEntity` | Saved inference parameter configurations |

### Expression-Bodied DbSets

DbSets use expression-bodied syntax for cleaner code:

```csharp
public DbSet<ConversationEntity> Conversations => Set<ConversationEntity>();
public DbSet<MessageEntity> Messages => Set<MessageEntity>();
public DbSet<SystemPromptEntity> SystemPrompts => Set<SystemPromptEntity>();
public DbSet<InferencePresetEntity> InferencePresets => Set<InferencePresetEntity>();
```

---

## Automatic Timestamp Management

The DbContext automatically manages timestamps when entities are saved:

### On Entity Addition (EntityState.Added)

- `CreatedAt` is set to `DateTime.UtcNow` if default
- `UpdatedAt` is set to match `CreatedAt`
- For `MessageEntity`, `Timestamp` is set instead

### On Entity Modification (EntityState.Modified)

- `UpdatedAt` is set to `DateTime.UtcNow`

### Example

```csharp
// CreatedAt and UpdatedAt are automatically set
var conversation = new ConversationEntity
{
    Id = Guid.NewGuid(),
    Title = "New Chat"
};
context.Conversations.Add(conversation);
await context.SaveChangesAsync();

Console.WriteLine(conversation.CreatedAt);  // 2026-01-12T10:30:00Z
Console.WriteLine(conversation.UpdatedAt);  // 2026-01-12T10:30:00Z

// UpdatedAt is automatically updated on modification
conversation.Title = "Renamed Chat";
await context.SaveChangesAsync();

Console.WriteLine(conversation.UpdatedAt);  // 2026-01-12T10:35:00Z (updated)
```

---

## Entity Configurations

Entity configurations are automatically discovered from the `AIntern.Data.Configurations` namespace.

### Configuration Classes

| Class | Entity | Table Name |
|-------|--------|------------|
| `ConversationConfiguration` | `ConversationEntity` | Conversations |
| `MessageConfiguration` | `MessageEntity` | Messages |
| `SystemPromptConfiguration` | `SystemPromptEntity` | SystemPrompts |
| `InferencePresetConfiguration` | `InferencePresetEntity` | InferencePresets |

### Auto-Discovery

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(
        typeof(ConversationConfiguration).Assembly);
}
```

---

## Index Strategy

The DbContext creates 19 indexes across all tables for optimized query performance:

### Conversations Table (6 indexes)

| Index Name | Columns | Purpose |
|------------|---------|---------|
| `IX_Conversations_UpdatedAt` | UpdatedAt DESC | Recent-first sorting |
| `IX_Conversations_IsArchived` | IsArchived | Archive filtering |
| `IX_Conversations_IsPinned` | IsPinned | Pin filtering |
| `IX_Conversations_SystemPromptId` | SystemPromptId | FK lookup |
| `IX_Conversations_CreatedAt` | CreatedAt | Creation date sorting |
| `IX_Conversations_List` | IsArchived, IsPinned DESC, UpdatedAt DESC | Main list query |

### Messages Table (4 indexes)

| Index Name | Columns | Purpose |
|------------|---------|---------|
| `IX_Messages_ConversationId` | ConversationId | FK lookup |
| `IX_Messages_Timestamp` | Timestamp | Time-based queries |
| `IX_Messages_Role` | Role | Role filtering |
| `IX_Messages_ConversationId_SequenceNumber` | ConversationId, SequenceNumber | **UNIQUE** ordering |

### SystemPrompts Table (6 indexes)

| Index Name | Columns | Purpose |
|------------|---------|---------|
| `IX_SystemPrompts_Name` | Name | **UNIQUE** constraint |
| `IX_SystemPrompts_IsDefault` | IsDefault | Default lookup |
| `IX_SystemPrompts_Category` | Category | Category filtering |
| `IX_SystemPrompts_IsActive` | IsActive | Active filtering |
| `IX_SystemPrompts_UsageCount` | UsageCount | Usage sorting |
| `IX_SystemPrompts_ActiveList` | IsActive, Category, UpdatedAt DESC | Active list query |

### InferencePresets Table (3 indexes)

| Index Name | Columns | Purpose |
|------------|---------|---------|
| `IX_InferencePresets_Name` | Name | **UNIQUE** constraint |
| `IX_InferencePresets_IsDefault` | IsDefault | Default lookup |
| `IX_InferencePresets_List` | IsBuiltIn DESC, UpdatedAt DESC | Preset list query |

---

## Relationships

### Entity Relationship Diagram

```
SystemPromptEntity (1) ──────┐
                             │ optional FK (SetNull on delete)
                             ▼
ConversationEntity (1) ◄──── SystemPromptId
        │
        │ 1:N (Cascade delete)
        ▼
MessageEntity (N)
        │
        └── Role: MessageRole enum (System=0, User=1, Assistant=2)


InferencePresetEntity (standalone - no relationships)
```

### Delete Behaviors

| Relationship | Delete Behavior | Effect |
|--------------|-----------------|--------|
| SystemPrompt → Conversations | SetNull | When a SystemPrompt is deleted, `SystemPromptId` in related Conversations is set to null |
| Conversation → Messages | Cascade | When a Conversation is deleted, all its Messages are deleted |

---

## Usage with Dependency Injection

### Registration

```csharp
// In Program.cs or Startup.cs
services.AddDbContext<AInternDbContext>(options =>
{
    var resolver = new DatabasePathResolver();
    options.UseSqlite(resolver.ConnectionString);
});
```

### Injection

```csharp
public class ConversationService
{
    private readonly AInternDbContext _context;

    public ConversationService(AInternDbContext context)
    {
        _context = context;
    }

    public async Task<List<ConversationEntity>> GetRecentAsync()
    {
        return await _context.Conversations
            .Where(c => !c.IsArchived)
            .OrderByDescending(c => c.IsPinned)
            .ThenByDescending(c => c.UpdatedAt)
            .Take(20)
            .ToListAsync();
    }
}
```

---

## Design-Time Usage

The parameterless constructor enables EF Core design-time tools:

```bash
# Create a migration
dotnet ef migrations add InitialCreate --project src/AIntern.Data

# Apply migrations
dotnet ef database update --project src/AIntern.Data

# Generate SQL script
dotnet ef migrations script --project src/AIntern.Data --output schema.sql
```

---

## Logging

The DbContext logs at various levels:

| Level | Events |
|-------|--------|
| Debug | Configuration loading, entity state changes, timestamp updates |
| Information | Configuration applied, table names |
| Warning | Design-time fallback constructor used |
| Error | Save failures (via EF Core) |

### Log Messages

```
Debug: AInternDbContext instance created with options
Debug: Applying entity configurations from assembly
Debug: Updated timestamps for 1 added and 0 modified entities
Information: Entity configurations applied for tables: Conversations, Messages, SystemPrompts, InferencePresets
Warning: AInternDbContext created with parameterless constructor (design-time fallback)
```

---

## Testing

Use SQLite in-memory for unit tests:

```csharp
public AInternDbContext CreateTestContext()
{
    var connection = new SqliteConnection("DataSource=:memory:");
    connection.Open();

    var options = new DbContextOptionsBuilder<AInternDbContext>()
        .UseSqlite(connection)
        .Options;

    var context = new AInternDbContext(options);
    context.Database.EnsureCreated();

    return context;
}
```

---

## Related Files

- [ConversationEntity](../src/AIntern.Core/Entities/ConversationEntity.cs)
- [MessageEntity](../src/AIntern.Core/Entities/MessageEntity.cs)
- [SystemPromptEntity](../src/AIntern.Core/Entities/SystemPromptEntity.cs)
- [InferencePresetEntity](../src/AIntern.Core/Entities/InferencePresetEntity.cs)
- [DatabasePathResolver](../src/AIntern.Data/DatabasePathResolver.cs)

---

## Version History

| Version | Changes |
|---------|---------|
| v0.2.1c | Initial implementation with 4 DbSets, auto-timestamps, 19 indexes |
