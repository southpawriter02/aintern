using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AIntern.Core.Entities;

namespace AIntern.Data.Configurations;

/// <summary>
/// EF Core configuration for ConversationEntity.
/// </summary>
public class ConversationConfiguration : IEntityTypeConfiguration<ConversationEntity>
{
    public void Configure(EntityTypeBuilder<ConversationEntity> builder)
    {
        builder.ToTable("Conversations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        builder.Property(c => c.ModelPath)
            .HasMaxLength(500);

        builder.Property(c => c.IsArchived)
            .HasDefaultValue(false);

        builder.Property(c => c.MessageCount)
            .HasDefaultValue(0);

        // Foreign key to SystemPrompt (optional)
        builder.HasOne(c => c.SystemPrompt)
            .WithMany(sp => sp.Conversations)
            .HasForeignKey(c => c.SystemPromptId)
            .OnDelete(DeleteBehavior.SetNull);

        // Index on UpdatedAt for sorting recent conversations
        builder.HasIndex(c => c.UpdatedAt)
            .IsDescending();

        // Index on IsArchived for filtering
        builder.HasIndex(c => c.IsArchived);
    }
}
