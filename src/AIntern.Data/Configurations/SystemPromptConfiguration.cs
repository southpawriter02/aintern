using AIntern.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIntern.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="SystemPromptEntity"/> entity.
/// Defines table mapping, column constraints, indexes, and relationships.
/// </summary>
/// <remarks>
/// <para>
/// This configuration establishes the "SystemPrompts" table schema with the following key features:
/// </para>
/// <list type="bullet">
///   <item><description>Primary key on Id (GUID)</description></item>
///   <item><description>Unique constraint on Name column</description></item>
///   <item><description>Default category of "General"</description></item>
///   <item><description>IsActive flag for soft-delete functionality</description></item>
///   <item><description>Six indexes for query optimization</description></item>
/// </list>
/// <para>
/// The composite index <c>IX_SystemPrompts_ActiveList</c> supports the typical prompt selection
/// query: active prompts grouped by category, sorted by most recent.
/// </para>
/// </remarks>
/// <example>
/// The configuration is automatically discovered and applied by the DbContext:
/// <code>
/// protected override void OnModelCreating(ModelBuilder modelBuilder)
/// {
///     modelBuilder.ApplyConfigurationsFromAssembly(typeof(SystemPromptConfiguration).Assembly);
/// }
/// </code>
/// </example>
public sealed class SystemPromptConfiguration : IEntityTypeConfiguration<SystemPromptEntity>
{
    #region Constants

    /// <summary>
    /// The database table name for system prompts.
    /// </summary>
    public const string TableName = "SystemPrompts";

    /// <summary>
    /// Maximum length for the Name column.
    /// </summary>
    public const int NameMaxLength = 100;

    /// <summary>
    /// Maximum length for the Description column.
    /// </summary>
    public const int DescriptionMaxLength = 500;

    /// <summary>
    /// Maximum length for the Category column.
    /// </summary>
    public const int CategoryMaxLength = 50;

    /// <summary>
    /// Default value for the Category column.
    /// </summary>
    public const string DefaultCategory = "General";

    #endregion

    #region Index Names

    /// <summary>
    /// Index name for the unique Name constraint.
    /// </summary>
    private const string IndexName = "IX_SystemPrompts_Name";

    /// <summary>
    /// Index name for IsDefault column.
    /// </summary>
    private const string IndexIsDefault = "IX_SystemPrompts_IsDefault";

    /// <summary>
    /// Index name for Category column.
    /// </summary>
    private const string IndexCategory = "IX_SystemPrompts_Category";

    /// <summary>
    /// Index name for IsActive column.
    /// </summary>
    private const string IndexIsActive = "IX_SystemPrompts_IsActive";

    /// <summary>
    /// Index name for UsageCount column.
    /// </summary>
    private const string IndexUsageCount = "IX_SystemPrompts_UsageCount";

    /// <summary>
    /// Index name for the composite list query index.
    /// </summary>
    private const string IndexActiveList = "IX_SystemPrompts_ActiveList";

    #endregion

    #region Configure Method

    /// <summary>
    /// Configures the entity mapping for <see cref="SystemPromptEntity"/>.
    /// </summary>
    /// <param name="builder">The entity type builder used to configure the entity.</param>
    /// <remarks>
    /// <para>Configuration includes:</para>
    /// <list type="bullet">
    ///   <item><description>Table name and comment</description></item>
    ///   <item><description>Primary key configuration</description></item>
    ///   <item><description>Unique constraint on Name</description></item>
    ///   <item><description>Column constraints (required, max length, defaults)</description></item>
    ///   <item><description>Six indexes for query optimization</description></item>
    /// </list>
    /// </remarks>
    public void Configure(EntityTypeBuilder<SystemPromptEntity> builder)
    {
        // Table configuration
        builder.ToTable(TableName, table =>
        {
            table.HasComment("Stores reusable system prompt templates with categories and usage tracking.");
        });

        // Primary key
        builder.HasKey(sp => sp.Id);

        // Name column - required and unique
        builder.Property(sp => sp.Name)
            .IsRequired()
            .HasMaxLength(NameMaxLength)
            .HasComment("Unique display name for the system prompt.");

        // Description column - optional
        builder.Property(sp => sp.Description)
            .HasMaxLength(DescriptionMaxLength)
            .HasComment("Optional description explaining the prompt's purpose and use case.");

        // Content column - required, unlimited text
        builder.Property(sp => sp.Content)
            .IsRequired()
            .HasComment("The actual system prompt text sent to the model.");

        // Category column
        builder.Property(sp => sp.Category)
            .IsRequired()
            .HasMaxLength(CategoryMaxLength)
            .HasDefaultValue(DefaultCategory)
            .HasComment("Category for organizing prompts: General, Code, Creative, Technical.");

        // Timestamp columns
        builder.Property(sp => sp.CreatedAt)
            .IsRequired()
            .HasComment("UTC timestamp when the prompt was created.");

        builder.Property(sp => sp.UpdatedAt)
            .IsRequired()
            .HasComment("UTC timestamp when the prompt was last modified.");

        // Status flags
        builder.Property(sp => sp.IsDefault)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Whether this is the default prompt for new conversations.");

        builder.Property(sp => sp.IsBuiltIn)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Whether this is a built-in prompt that cannot be deleted.");

        builder.Property(sp => sp.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Whether this prompt is visible and selectable (soft-delete flag).");

        // Usage statistics
        builder.Property(sp => sp.UsageCount)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Number of conversations that have used this prompt.");

        // Indexes
        ConfigureIndexes(builder);

        // Note: Relationship to Conversations is configured in ConversationConfiguration
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Configures indexes for optimized query performance.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    private static void ConfigureIndexes(EntityTypeBuilder<SystemPromptEntity> builder)
    {
        // Unique index on Name
        builder.HasIndex(sp => sp.Name)
            .HasDatabaseName(IndexName)
            .IsUnique();

        // Index for finding default prompt
        builder.HasIndex(sp => sp.IsDefault)
            .HasDatabaseName(IndexIsDefault);

        // Index for category filtering
        builder.HasIndex(sp => sp.Category)
            .HasDatabaseName(IndexCategory);

        // Index for active/inactive filtering
        builder.HasIndex(sp => sp.IsActive)
            .HasDatabaseName(IndexIsActive);

        // Index for usage count sorting
        builder.HasIndex(sp => sp.UsageCount)
            .HasDatabaseName(IndexUsageCount);

        // Composite index for prompt list query:
        // WHERE IsActive = 1 ORDER BY Category, UpdatedAt DESC
        builder.HasIndex(sp => new { sp.IsActive, sp.Category, sp.UpdatedAt })
            .HasDatabaseName(IndexActiveList)
            .IsDescending(false, false, true);
    }

    #endregion
}
