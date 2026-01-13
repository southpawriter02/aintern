# Entity Classes Specification

The `AIntern.Core` project contains domain entity classes that represent the database schema for conversations, messages, system prompts, and inference presets. These POCOs (Plain Old CLR Objects) are configured for Entity Framework Core persistence in the Data layer.

## Overview

This layer provides:
- **ConversationEntity**: Chat session with metadata, pinning, archiving, and statistics
- **MessageEntity**: Individual message with role, content, ordering, and generation statistics
- **SystemPromptEntity**: Reusable system prompts with categories and usage tracking
- **InferencePresetEntity**: Saved inference parameter configurations

---

## Entity Relationship Diagram

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

---

## ConversationEntity

Represents a chat session containing multiple messages.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Id` | `Guid` | - | Primary key |
| `Title` | `string` | "New Conversation" | Display title |
| `CreatedAt` | `DateTime` | - | Creation timestamp (UTC) |
| `UpdatedAt` | `DateTime` | - | Last modification (UTC) |
| `ModelPath` | `string?` | null | Path to model file |
| `ModelName` | `string?` | null | Human-readable model name |
| `SystemPromptId` | `Guid?` | null | FK to SystemPromptEntity |
| `IsArchived` | `bool` | false | Archive flag |
| `IsPinned` | `bool` | false | Pin flag |
| `MessageCount` | `int` | 0 | Denormalized message count |
| `TotalTokenCount` | `int` | 0 | Sum of all message tokens |

### Navigation Properties

| Property | Type | Description |
|----------|------|-------------|
| `SystemPrompt` | `SystemPromptEntity?` | Associated system prompt |
| `Messages` | `ICollection<MessageEntity>` | All messages in conversation |

### Usage Example

```csharp
var conversation = new ConversationEntity
{
    Id = Guid.NewGuid(),
    Title = "How to implement DI in C#",
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow,
    ModelName = "Llama 2 7B Chat"
};
```

---

## MessageEntity

Represents a single message within a conversation.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Id` | `Guid` | - | Primary key |
| `ConversationId` | `Guid` | - | FK to ConversationEntity |
| `Role` | `MessageRole` | - | System, User, or Assistant |
| `Content` | `string` | "" | Message text |
| `SequenceNumber` | `int` | - | Order within conversation |
| `Timestamp` | `DateTime` | - | Creation time (UTC) |
| `EditedAt` | `DateTime?` | null | Last edit time |
| `TokenCount` | `int?` | null | Token count |
| `GenerationTimeMs` | `int?` | null | Generation duration (ms) |
| `TokensPerSecond` | `float?` | null | Generation speed |
| `IsEdited` | `bool` | false | Edit flag |
| `IsComplete` | `bool` | true | Completion flag |

### Navigation Properties

| Property | Type | Description |
|----------|------|-------------|
| `Conversation` | `ConversationEntity` | Parent conversation (required) |

### Usage Example

```csharp
var message = new MessageEntity
{
    Id = Guid.NewGuid(),
    ConversationId = conversationId,
    Role = MessageRole.User,
    Content = "How do I implement dependency injection?",
    SequenceNumber = 1,
    Timestamp = DateTime.UtcNow
};
```

---

## SystemPromptEntity

Represents a reusable system prompt template.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Id` | `Guid` | - | Primary key |
| `Name` | `string` | "" | Unique display name |
| `Description` | `string?` | null | Optional description |
| `Content` | `string` | "" | The prompt text |
| `Category` | `string` | "General" | Organization category |
| `CreatedAt` | `DateTime` | - | Creation timestamp (UTC) |
| `UpdatedAt` | `DateTime` | - | Last modification (UTC) |
| `IsDefault` | `bool` | false | Default prompt flag |
| `IsBuiltIn` | `bool` | false | Built-in template flag |
| `IsActive` | `bool` | true | Visibility flag (soft-delete) |
| `UsageCount` | `int` | 0 | Usage statistics |

### Navigation Properties

| Property | Type | Description |
|----------|------|-------------|
| `Conversations` | `ICollection<ConversationEntity>` | Conversations using this prompt |

### Categories

| Category | Description |
|----------|-------------|
| General | General-purpose assistants |
| Code | Programming and development |
| Creative | Creative writing and brainstorming |
| Technical | Technical documentation |

### Usage Example

```csharp
var prompt = new SystemPromptEntity
{
    Id = Guid.NewGuid(),
    Name = "Code Expert",
    Description = "Specialized in C# development",
    Content = "You are a senior C# developer...",
    Category = "Code",
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};
```

---

## InferencePresetEntity

Represents saved inference parameter configurations.

### Properties

| Property | Type | Default | Range | Description |
|----------|------|---------|-------|-------------|
| `Id` | `Guid` | - | - | Primary key |
| `Name` | `string` | "" | 1-100 chars | Unique display name |
| `Description` | `string?` | null | 0-500 chars | Use case description |
| `Temperature` | `float` | 0.7 | 0.0-2.0 | Randomness control |
| `TopP` | `float` | 0.9 | 0.0-1.0 | Nucleus sampling |
| `TopK` | `int` | 40 | 1-100 | Token selection limit |
| `RepeatPenalty` | `float` | 1.1 | 1.0-2.0 | Repetition penalty |
| `MaxTokens` | `int` | 2048 | 1-32768 | Max response length |
| `ContextSize` | `int` | 4096 | 512-131072 | Context window |
| `IsDefault` | `bool` | false | - | Default preset flag |
| `IsBuiltIn` | `bool` | false | - | Built-in flag |
| `CreatedAt` | `DateTime` | - | - | Creation timestamp |
| `UpdatedAt` | `DateTime` | - | - | Last modification |

### Built-in Presets (seeded in v0.2.1e)

| Preset | Temp | TopP | TopK | MaxTokens | Context | Use Case |
|--------|------|------|------|-----------|---------|----------|
| Balanced | 0.7 | 0.9 | 40 | 2048 | 4096 | General conversation |
| Precise | 0.2 | 0.8 | 20 | 1024 | 4096 | Factual responses |
| Creative | 1.2 | 0.95 | 60 | 4096 | 8192 | Brainstorming |
| Long-form | 0.7 | 0.9 | 40 | 8192 | 16384 | Detailed explanations |

### Usage Example

```csharp
var preset = new InferencePresetEntity
{
    Id = Guid.NewGuid(),
    Name = "Creative",
    Description = "Higher temperature for creative writing",
    Temperature = 1.2f,
    TopP = 0.95f,
    MaxTokens = 4096,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};
```

---

## MessageRole Enum

Defined in `AIntern.Core.Models.ChatMessage` (existing).

| Value | Int | Description |
|-------|-----|-------------|
| System | 0 | Hidden instructions for the model |
| User | 1 | Human input |
| Assistant | 2 | AI-generated response |

---

## Directory Structure

```
src/AIntern.Core/
├── Entities/
│   ├── ConversationEntity.cs
│   ├── MessageEntity.cs
│   ├── SystemPromptEntity.cs
│   └── InferencePresetEntity.cs
├── Models/
│   └── ChatMessage.cs          # Contains MessageRole enum
└── ...

tests/AIntern.Core.Tests/
└── Entities/
    └── EntityTests.cs          # 26 unit tests
```

---

## Dependencies

These entities have no external dependencies beyond:
- `System` namespace for basic types
- `AIntern.Core.Models.MessageRole` enum (for MessageEntity)

Entity Framework Core configuration is added in v0.2.1c (Data layer).

---

## Design Decisions

### Entities in Core Project

Entities are placed in `AIntern.Core` rather than `AIntern.Data` to:
- Keep domain models independent of EF Core
- Allow Services to use entities without Data dependency
- Maintain clean separation of concerns

### Sealed Classes

All entity classes are `sealed` to:
- Prevent accidental inheritance
- Avoid EF Core proxy issues
- Clearly indicate they are final implementations

### GUID Primary Keys

GUIDs are used instead of integers to:
- Eliminate ID generation coordination
- Support disconnected scenarios
- Enable potential future sync features
- Improve security (harder to guess)

### Denormalized Counts

`MessageCount` and `TotalTokenCount` are stored on ConversationEntity to:
- Avoid COUNT queries for conversation lists
- Improve read performance
- Accept minor write overhead

### Soft Delete for System Prompts

`IsActive` flag enables soft-delete to:
- Preserve history for conversations using deleted prompts
- Allow recovery of accidentally deleted prompts
- Prevent deletion of built-in prompts
