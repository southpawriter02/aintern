using AIntern.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIntern.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="InferencePresetEntity"/> entity.
/// Defines table mapping, column constraints, indexes, and default values.
/// </summary>
/// <remarks>
/// <para>
/// This configuration establishes the "InferencePresets" table schema with the following key features:
/// </para>
/// <list type="bullet">
///   <item><description>Primary key on Id (GUID)</description></item>
///   <item><description>Unique constraint on Name column</description></item>
///   <item><description>Default values for all sampling parameters matching LLamaSharp defaults</description></item>
///   <item><description>Three indexes for query optimization</description></item>
/// </list>
/// <para>
/// Default parameter values are chosen to work well with most LLM models:
/// Temperature=0.7, TopP=0.9, TopK=40, RepeatPenalty=1.1, MaxTokens=2048, ContextSize=4096.
/// </para>
/// </remarks>
/// <example>
/// The configuration is automatically discovered and applied by the DbContext:
/// <code>
/// protected override void OnModelCreating(ModelBuilder modelBuilder)
/// {
///     modelBuilder.ApplyConfigurationsFromAssembly(typeof(InferencePresetConfiguration).Assembly);
/// }
/// </code>
/// </example>
public sealed class InferencePresetConfiguration : IEntityTypeConfiguration<InferencePresetEntity>
{
    #region Constants

    /// <summary>
    /// The database table name for inference presets.
    /// </summary>
    public const string TableName = "InferencePresets";

    /// <summary>
    /// Maximum length for the Name column.
    /// </summary>
    public const int NameMaxLength = 100;

    /// <summary>
    /// Maximum length for the Description column.
    /// </summary>
    public const int DescriptionMaxLength = 500;

    #endregion

    #region Default Values

    /// <summary>
    /// Default value for Temperature parameter.
    /// </summary>
    /// <remarks>
    /// Controls randomness in generation. Lower values (0.1-0.4) produce more
    /// focused/deterministic output; higher values (0.8-1.2) increase creativity.
    /// </remarks>
    public const float DefaultTemperature = 0.7f;

    /// <summary>
    /// Default value for TopP (nucleus sampling) parameter.
    /// </summary>
    /// <remarks>
    /// Cumulative probability threshold for token selection. Only tokens with
    /// cumulative probability up to TopP are considered.
    /// </remarks>
    public const float DefaultTopP = 0.9f;

    /// <summary>
    /// Default value for TopK parameter.
    /// </summary>
    /// <remarks>
    /// Limits token selection to the K most probable tokens. Lower values
    /// increase coherence; higher values increase diversity.
    /// </remarks>
    public const int DefaultTopK = 40;

    /// <summary>
    /// Default value for RepeatPenalty parameter.
    /// </summary>
    /// <remarks>
    /// Penalty applied to repeated tokens. Values > 1.0 discourage repetition;
    /// 1.0 disables the penalty.
    /// </remarks>
    public const float DefaultRepeatPenalty = 1.1f;

    /// <summary>
    /// Default value for MaxTokens parameter.
    /// </summary>
    /// <remarks>
    /// Maximum number of tokens to generate in a response. Affects response
    /// length and generation time.
    /// </remarks>
    public const int DefaultMaxTokens = 2048;

    /// <summary>
    /// Default value for ContextSize parameter.
    /// </summary>
    /// <remarks>
    /// Size of the context window in tokens. Determines how much conversation
    /// history the model can consider.
    /// </remarks>
    public const int DefaultContextSize = 4096;

    #endregion

    #region Index Names

    /// <summary>
    /// Index name for the unique Name constraint.
    /// </summary>
    private const string IndexName = "IX_InferencePresets_Name";

    /// <summary>
    /// Index name for IsDefault column.
    /// </summary>
    private const string IndexIsDefault = "IX_InferencePresets_IsDefault";

    /// <summary>
    /// Index name for the composite list query index.
    /// </summary>
    private const string IndexList = "IX_InferencePresets_List";

    #endregion

    #region Configure Method

    /// <summary>
    /// Configures the entity mapping for <see cref="InferencePresetEntity"/>.
    /// </summary>
    /// <param name="builder">The entity type builder used to configure the entity.</param>
    /// <remarks>
    /// <para>Configuration includes:</para>
    /// <list type="bullet">
    ///   <item><description>Table name and comment</description></item>
    ///   <item><description>Primary key configuration</description></item>
    ///   <item><description>Unique constraint on Name</description></item>
    ///   <item><description>Column constraints with default values for all parameters</description></item>
    ///   <item><description>Three indexes for query optimization</description></item>
    /// </list>
    /// </remarks>
    public void Configure(EntityTypeBuilder<InferencePresetEntity> builder)
    {
        // Table configuration
        builder.ToTable(TableName, table =>
        {
            table.HasComment("Stores saved inference parameter configurations for model generation.");
        });

        // Primary key
        builder.HasKey(ip => ip.Id);

        // Name column - required and unique
        builder.Property(ip => ip.Name)
            .IsRequired()
            .HasMaxLength(NameMaxLength)
            .HasComment("Unique display name for the preset.");

        // Description column - optional
        builder.Property(ip => ip.Description)
            .HasMaxLength(DescriptionMaxLength)
            .HasComment("Optional description explaining the preset's use case.");

        // Sampling parameters with defaults
        builder.Property(ip => ip.Temperature)
            .IsRequired()
            .HasDefaultValue(DefaultTemperature)
            .HasComment("Randomness control (0.0-2.0). Lower = more focused, higher = more creative.");

        builder.Property(ip => ip.TopP)
            .IsRequired()
            .HasDefaultValue(DefaultTopP)
            .HasComment("Nucleus sampling threshold (0.0-1.0). Cumulative probability for token selection.");

        builder.Property(ip => ip.TopK)
            .IsRequired()
            .HasDefaultValue(DefaultTopK)
            .HasComment("Token selection limit (1-100). Only top K tokens considered.");

        builder.Property(ip => ip.RepeatPenalty)
            .IsRequired()
            .HasDefaultValue(DefaultRepeatPenalty)
            .HasComment("Repetition penalty (1.0-2.0). Values > 1.0 discourage repetition.");

        builder.Property(ip => ip.MaxTokens)
            .IsRequired()
            .HasDefaultValue(DefaultMaxTokens)
            .HasComment("Maximum response length in tokens (1-32768).");

        builder.Property(ip => ip.ContextSize)
            .IsRequired()
            .HasDefaultValue(DefaultContextSize)
            .HasComment("Context window size in tokens (512-131072).");

        // Status flags
        builder.Property(ip => ip.IsDefault)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Whether this is the default preset for new conversations.");

        builder.Property(ip => ip.IsBuiltIn)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Whether this is a built-in preset that cannot be deleted.");

        // Timestamp columns
        builder.Property(ip => ip.CreatedAt)
            .IsRequired()
            .HasComment("UTC timestamp when the preset was created.");

        builder.Property(ip => ip.UpdatedAt)
            .IsRequired()
            .HasComment("UTC timestamp when the preset was last modified.");

        // Indexes
        ConfigureIndexes(builder);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Configures indexes for optimized query performance.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    private static void ConfigureIndexes(EntityTypeBuilder<InferencePresetEntity> builder)
    {
        // Unique index on Name
        builder.HasIndex(ip => ip.Name)
            .HasDatabaseName(IndexName)
            .IsUnique();

        // Index for finding default preset
        builder.HasIndex(ip => ip.IsDefault)
            .HasDatabaseName(IndexIsDefault);

        // Composite index for preset list query:
        // ORDER BY IsBuiltIn DESC, UpdatedAt DESC (built-in presets first, then by recency)
        builder.HasIndex(ip => new { ip.IsBuiltIn, ip.UpdatedAt })
            .HasDatabaseName(IndexList)
            .IsDescending(true, true);
    }

    #endregion
}
