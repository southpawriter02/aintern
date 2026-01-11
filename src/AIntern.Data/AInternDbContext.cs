using Microsoft.EntityFrameworkCore;
using AIntern.Core.Entities;

namespace AIntern.Data;

/// <summary>
/// Entity Framework Core database context for the AIntern application.
/// </summary>
public class AInternDbContext : DbContext
{
    public DbSet<ConversationEntity> Conversations => Set<ConversationEntity>();
    public DbSet<MessageEntity> Messages => Set<MessageEntity>();
    public DbSet<SystemPromptEntity> SystemPrompts => Set<SystemPromptEntity>();
    public DbSet<InferencePresetEntity> InferencePresets => Set<InferencePresetEntity>();

    public AInternDbContext(DbContextOptions<AInternDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AInternDbContext).Assembly);
    }
}
