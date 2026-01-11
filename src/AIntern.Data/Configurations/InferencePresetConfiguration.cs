using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AIntern.Core.Entities;

namespace AIntern.Data.Configurations;

/// <summary>
/// EF Core configuration for InferencePresetEntity.
/// </summary>
public class InferencePresetConfiguration : IEntityTypeConfiguration<InferencePresetEntity>
{
    public void Configure(EntityTypeBuilder<InferencePresetEntity> builder)
    {
        builder.ToTable("InferencePresets");

        builder.HasKey(ip => ip.Id);

        builder.Property(ip => ip.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ip => ip.Temperature)
            .IsRequired();

        builder.Property(ip => ip.TopP)
            .IsRequired();

        builder.Property(ip => ip.MaxTokens)
            .IsRequired();

        builder.Property(ip => ip.ContextSize)
            .IsRequired();

        builder.Property(ip => ip.IsDefault)
            .HasDefaultValue(false);

        builder.Property(ip => ip.IsBuiltIn)
            .HasDefaultValue(false);

        // Unique index on Name
        builder.HasIndex(ip => ip.Name)
            .IsUnique();
    }
}
