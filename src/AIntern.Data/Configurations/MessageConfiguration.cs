using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AIntern.Core.Entities;

namespace AIntern.Data.Configurations;

/// <summary>
/// EF Core configuration for MessageEntity.
/// </summary>
public class MessageConfiguration : IEntityTypeConfiguration<MessageEntity>
{
    public void Configure(EntityTypeBuilder<MessageEntity> builder)
    {
        builder.ToTable("Messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ConversationId)
            .IsRequired();

        builder.Property(m => m.Role)
            .IsRequired();

        builder.Property(m => m.Content)
            .IsRequired();

        builder.Property(m => m.Timestamp)
            .IsRequired();

        builder.Property(m => m.SequenceNumber)
            .IsRequired();

        // Foreign key to Conversation with cascade delete
        builder.HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Composite index for efficient message ordering within conversation
        builder.HasIndex(m => new { m.ConversationId, m.SequenceNumber });
    }
}
