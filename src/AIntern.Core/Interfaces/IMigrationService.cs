// -----------------------------------------------------------------------
// <copyright file="IMigrationService.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
// </copyright>
// <summary>
//     Interface for application migration operations.
//     Added in v0.2.5d.
// </summary>
// -----------------------------------------------------------------------

using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

/// <summary>
/// Provides application migration functionality for version upgrades.
/// </summary>
/// <remarks>
/// <para>
/// This service handles migration from legacy application versions to the current version.
/// It automatically detects when migration is needed, backs up legacy files, and performs
/// the migration steps.
/// </para>
/// <para>
/// The migration process:
/// </para>
/// <list type="bullet">
///   <item><description><b>Detection:</b> Checks for legacy settings.json file</description></item>
///   <item><description><b>Version check:</b> Compares database version to current version</description></item>
///   <item><description><b>Backup:</b> Creates backup of legacy files before migration</description></item>
///   <item><description><b>Migration:</b> Performs version-specific migration steps</description></item>
///   <item><description><b>Stamp:</b> Records new version in database</description></item>
/// </list>
/// <para>Added in v0.2.5d.</para>
/// </remarks>
/// <example>
/// Basic usage on application startup:
/// <code>
/// var migrationService = serviceProvider.GetRequiredService&lt;IMigrationService&gt;();
///
/// // Run migration if needed (safe to call multiple times)
/// var result = await migrationService.MigrateIfNeededAsync();
///
/// if (!result.Success)
/// {
///     logger.LogWarning("Migration failed: {Error}", result.ErrorMessage);
/// }
/// else if (result.MigrationSteps.Count &gt; 1)
/// {
///     logger.LogInformation("Migration completed: {Steps}",
///         string.Join(" → ", result.MigrationSteps));
/// }
/// </code>
/// </example>
public interface IMigrationService
{
    /// <summary>
    /// Performs migration if required, otherwise returns a no-op result.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="MigrationResult"/> indicating whether migration was performed,
    /// the version transition, steps completed, and any error that occurred.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is safe to call multiple times. If no migration is needed
    /// (already at current version), it returns a successful result with no steps.
    /// </para>
    /// <para>
    /// Migration is idempotent: if called after successful migration, it will
    /// detect that the database version is current and return immediately.
    /// </para>
    /// <para>
    /// If migration fails partway through, the result will contain:
    /// <list type="bullet">
    ///   <item><description>Success = false</description></item>
    ///   <item><description>The steps that were completed before failure</description></item>
    ///   <item><description>An error message describing the failure</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await migrationService.MigrateIfNeededAsync();
    ///
    /// if (result.Success)
    /// {
    ///     Console.WriteLine($"Version: {result.ToVersion}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Migration failed: {result.ErrorMessage}");
    /// }
    /// </code>
    /// </example>
    Task<MigrationResult> MigrateIfNeededAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current application version recorded in the database.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The current version from the database, or the legacy version (0.1.0)
    /// if no version record exists.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method queries the AppVersions table for the most recent version record.
    /// If no records exist, it assumes the database is from a legacy installation
    /// and returns version 0.1.0.
    /// </para>
    /// <para>
    /// The version is used to determine what migrations need to be applied.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var version = await migrationService.GetCurrentVersionAsync();
    /// Console.WriteLine($"Database version: {version}"); // e.g., "0.2.0"
    /// </code>
    /// </example>
    Task<Version> GetCurrentVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether migration is required based on legacy files and database version.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if migration is needed (legacy settings.json exists and
    /// database version is older than current); <c>false</c> otherwise.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Migration is required when both conditions are met:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>A legacy settings.json file exists in the app data directory</description></item>
    ///   <item><description>The database version is older than the current version (0.2.0)</description></item>
    /// </list>
    /// <para>
    /// If no settings.json exists, migration is not needed (fresh installation).
    /// If the database version is current, migration is not needed (already migrated).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// if (await migrationService.IsMigrationRequiredAsync())
    /// {
    ///     Console.WriteLine("Migration will be performed on startup.");
    /// }
    /// </code>
    /// </example>
    Task<bool> IsMigrationRequiredAsync(CancellationToken cancellationToken = default);
}
