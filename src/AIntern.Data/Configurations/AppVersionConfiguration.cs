// -----------------------------------------------------------------------
// <copyright file="AppVersionConfiguration.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
// </copyright>
// <summary>
//     Entity Framework Core configuration for the AppVersionEntity.
//     Defines table mapping, column constraints, and indexes.
//     Added in v0.2.5d.
// </summary>
// -----------------------------------------------------------------------

using AIntern.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIntern.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="AppVersionEntity"/> entity.
/// Defines table mapping, column constraints, and indexes.
/// </summary>
/// <remarks>
/// <para>
/// This configuration establishes the "AppVersions" table schema with the following key features:
/// </para>
/// <list type="bullet">
///   <item><description>Primary key on Id (auto-increment)</description></item>
///   <item><description>Version components (Major, Minor, Patch) as required columns</description></item>
///   <item><description>MigratedAt timestamp for tracking when migration occurred</description></item>
///   <item><description>Index on MigratedAt for retrieving the latest version</description></item>
/// </list>
/// </remarks>
/// <example>
/// The configuration is automatically discovered and applied by the DbContext:
/// <code>
/// protected override void OnModelCreating(ModelBuilder modelBuilder)
/// {
///     modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppVersionConfiguration).Assembly);
/// }
/// </code>
/// </example>
public sealed class AppVersionConfiguration : IEntityTypeConfiguration<AppVersionEntity>
{
    #region Constants

    /// <summary>
    /// The database table name for application versions.
    /// </summary>
    public const string TableName = "AppVersions";

    #endregion

    #region Index Names

    /// <summary>
    /// Index name for MigratedAt column (descending for retrieving latest).
    /// </summary>
    private const string IndexMigratedAt = "IX_AppVersions_MigratedAt";

    #endregion

    #region Configure Method

    /// <summary>
    /// Configures the entity mapping for <see cref="AppVersionEntity"/>.
    /// </summary>
    /// <param name="builder">The entity type builder used to configure the entity.</param>
    /// <remarks>
    /// <para>Configuration includes:</para>
    /// <list type="bullet">
    ///   <item><description>Table name and comment</description></item>
    ///   <item><description>Primary key configuration (auto-increment)</description></item>
    ///   <item><description>Version component columns (Major, Minor, Patch)</description></item>
    ///   <item><description>MigratedAt timestamp column</description></item>
    ///   <item><description>Index for efficient latest version lookup</description></item>
    /// </list>
    /// </remarks>
    public void Configure(EntityTypeBuilder<AppVersionEntity> builder)
    {
        // Table configuration
        builder.ToTable(TableName, table =>
        {
            table.HasComment("Tracks application version history and migration timestamps.");
        });

        // Primary key (auto-increment)
        builder.HasKey(av => av.Id);

        builder.Property(av => av.Id)
            .ValueGeneratedOnAdd()
            .HasComment("Auto-generated primary key.");

        // Version component columns
        builder.Property(av => av.Major)
            .IsRequired()
            .HasComment("Major version number (e.g., 0 in 0.2.0).");

        builder.Property(av => av.Minor)
            .IsRequired()
            .HasComment("Minor version number (e.g., 2 in 0.2.0).");

        builder.Property(av => av.Patch)
            .IsRequired()
            .HasComment("Patch version number (e.g., 0 in 0.2.0).");

        // Timestamp column
        builder.Property(av => av.MigratedAt)
            .IsRequired()
            .HasComment("UTC timestamp when this version migration was performed.");

        // Indexes
        ConfigureIndexes(builder);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Configures indexes for optimized query performance.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    private static void ConfigureIndexes(EntityTypeBuilder<AppVersionEntity> builder)
    {
        // Index on MigratedAt (descending) for efficient latest version lookup.
        // The migration service queries for the most recent version record
        // to determine the current database schema version.
        builder.HasIndex(av => av.MigratedAt)
            .HasDatabaseName(IndexMigratedAt)
            .IsDescending();
    }

    #endregion
}
