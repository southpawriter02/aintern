// -----------------------------------------------------------------------
// <copyright file="MigrationService.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
// </copyright>
// <summary>
//     Service for handling application version migrations.
//     Added in v0.2.5d.
// </summary>
// -----------------------------------------------------------------------

namespace AIntern.Services;

using System.Diagnostics;
using System.Text.Json;
using AIntern.Core.Entities;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
public sealed class MigrationService : IMigrationService
{
    #region Constants

    /// <summary>
    /// The current application version for migration tracking.
    /// </summary>
    internal static readonly Version CurrentVersion = new(0, 2, 0);

    /// <summary>
    /// The legacy application version (pre-database settings).
    /// </summary>
    internal static readonly Version LegacyVersion = new(0, 1, 0);

    /// <summary>
    /// The legacy settings file name.
    /// </summary>
    private const string LegacySettingsFileName = "settings.json";

    /// <summary>
    /// The backup file name for legacy settings.
    /// </summary>
    private const string LegacySettingsBackupFileName = "settings.v1.json.bak";

    #endregion

    #region Fields

    private readonly AInternDbContext _dbContext;
    private readonly DatabasePathResolver _pathResolver;
    private readonly ILogger<MigrationService> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationService"/> class.
    /// </summary>
    /// <param name="dbContext">The database context for migration operations.</param>
    /// <param name="pathResolver">The path resolver for file system paths.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="dbContext"/>, <paramref name="pathResolver"/>,
    /// or <paramref name="logger"/> is null.
    /// </exception>
    public MigrationService(
        AInternDbContext dbContext,
        DatabasePathResolver pathResolver,
        ILogger<MigrationService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _pathResolver = pathResolver ?? throw new ArgumentNullException(nameof(pathResolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("[INIT] MigrationService created");
    }

    #endregion

    #region IMigrationService Implementation

    /// <inheritdoc />
    public async Task<MigrationResult> MigrateIfNeededAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] MigrateIfNeededAsync");

        var steps = new List<string>();

        try
        {
            // Check if migration is required.
            var migrationRequired = await IsMigrationRequiredAsync(cancellationToken);

            if (!migrationRequired)
            {
                var currentVersion = await GetCurrentVersionAsync(cancellationToken);

                stopwatch.Stop();
                _logger.LogDebug(
                    "[SKIP] MigrateIfNeededAsync - No migration required, current version: {Version} in {Ms}ms",
                    currentVersion,
                    stopwatch.ElapsedMilliseconds);

                return MigrationResult.NoMigrationNeeded(currentVersion);
            }

            var fromVersion = await GetCurrentVersionAsync(cancellationToken);
            steps.Add($"Starting migration from {fromVersion}");

            _logger.LogInformation(
                "[INFO] MigrateIfNeededAsync - Migration required from {From} to {To}",
                fromVersion,
                CurrentVersion);

            // Step 1: Backup legacy settings.
            var settingsPath = GetLegacySettingsPath();

            if (File.Exists(settingsPath))
            {
                await BackupLegacySettingsAsync(settingsPath, cancellationToken);
                steps.Add("Backed up legacy settings.json");
            }

            // Step 2: Read legacy settings (for potential future use).
            var legacySettings = await ReadLegacySettingsAsync(settingsPath, cancellationToken);

            if (legacySettings is not null)
            {
                steps.Add("Read legacy settings");
                _logger.LogDebug(
                    "[INFO] MigrateIfNeededAsync - Legacy settings: LastModelPath={ModelPath}, Theme={Theme}",
                    legacySettings.LastModelPath ?? "(none)",
                    legacySettings.Theme);
            }

            // Step 3: Stamp the version in database.
            await MarkMigrationCompleteAsync(cancellationToken);
            steps.Add($"Stamped version {CurrentVersion} in database");

            stopwatch.Stop();
            _logger.LogInformation(
                "[EXIT] MigrateIfNeededAsync - Migrated from {From} to {To} in {Ms}ms. Steps: {StepCount}",
                fromVersion,
                CurrentVersion,
                stopwatch.ElapsedMilliseconds,
                steps.Count);

            return MigrationResult.Succeeded(fromVersion, CurrentVersion, steps);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogDebug(
                "[EXIT] MigrateIfNeededAsync - Cancelled after {Ms}ms",
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "[ERROR] MigrateIfNeededAsync - Failed after {Ms}ms: {Message}",
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            return MigrationResult.Failed(
                LegacyVersion,
                CurrentVersion,
                steps,
                ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Version> GetCurrentVersionAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] GetCurrentVersionAsync");

        try
        {
            // Get the most recent version record.
            var latestVersion = await _dbContext.AppVersions
                .OrderByDescending(v => v.MigratedAt)
                .FirstOrDefaultAsync(cancellationToken);

            stopwatch.Stop();

            if (latestVersion is not null)
            {
                var version = latestVersion.ToVersion();
                _logger.LogDebug(
                    "[EXIT] GetCurrentVersionAsync - Found version {Version} in {Ms}ms",
                    version,
                    stopwatch.ElapsedMilliseconds);
                return version;
            }

            // No version record found - assume legacy installation.
            _logger.LogDebug(
                "[EXIT] GetCurrentVersionAsync - No version found, returning legacy {Version} in {Ms}ms",
                LegacyVersion,
                stopwatch.ElapsedMilliseconds);
            return LegacyVersion;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogDebug(
                "[EXIT] GetCurrentVersionAsync - Cancelled after {Ms}ms",
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "[ERROR] GetCurrentVersionAsync - Failed after {Ms}ms: {Message}",
                stopwatch.ElapsedMilliseconds,
                ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsMigrationRequiredAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] IsMigrationRequiredAsync");

        try
        {
            var settingsPath = GetLegacySettingsPath();

            // No legacy settings file = no migration needed (fresh installation).
            if (!File.Exists(settingsPath))
            {
                stopwatch.Stop();
                _logger.LogDebug(
                    "[EXIT] IsMigrationRequiredAsync - No legacy settings file, returning false in {Ms}ms",
                    stopwatch.ElapsedMilliseconds);
                return false;
            }

            // Check database version.
            var currentVersion = await GetCurrentVersionAsync(cancellationToken);
            var migrationRequired = currentVersion < CurrentVersion;

            stopwatch.Stop();
            _logger.LogDebug(
                "[EXIT] IsMigrationRequiredAsync - CurrentVersion: {Current}, TargetVersion: {Target}, Required: {Required} in {Ms}ms",
                currentVersion,
                CurrentVersion,
                migrationRequired,
                stopwatch.ElapsedMilliseconds);

            return migrationRequired;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogDebug(
                "[EXIT] IsMigrationRequiredAsync - Cancelled after {Ms}ms",
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "[ERROR] IsMigrationRequiredAsync - Failed after {Ms}ms: {Message}",
                stopwatch.ElapsedMilliseconds,
                ex.Message);
            throw;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Gets the full path to the legacy settings file.
    /// </summary>
    /// <returns>The path to settings.json in the app data directory.</returns>
    private string GetLegacySettingsPath()
    {
        return Path.Combine(_pathResolver.AppDataDirectory, LegacySettingsFileName);
    }

    /// <summary>
    /// Gets the full path to the legacy settings backup file.
    /// </summary>
    /// <returns>The path to settings.v1.json.bak in the app data directory.</returns>
    private string GetLegacySettingsBackupPath()
    {
        return Path.Combine(_pathResolver.AppDataDirectory, LegacySettingsBackupFileName);
    }

    /// <summary>
    /// Backs up the legacy settings file.
    /// </summary>
    /// <param name="settingsPath">The path to the settings file.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    private async Task BackupLegacySettingsAsync(string settingsPath, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] BackupLegacySettingsAsync - Path: {Path}", settingsPath);

        try
        {
            var backupPath = GetLegacySettingsBackupPath();

            // Read and write using async file operations.
            var content = await File.ReadAllTextAsync(settingsPath, cancellationToken);
            await File.WriteAllTextAsync(backupPath, content, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "[EXIT] BackupLegacySettingsAsync - Created backup at {BackupPath} in {Ms}ms",
                backupPath,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                ex,
                "[ERROR] BackupLegacySettingsAsync - Failed to backup after {Ms}ms: {Message}",
                stopwatch.ElapsedMilliseconds,
                ex.Message);
            // Continue migration even if backup fails - don't throw.
        }
    }

    /// <summary>
    /// Reads legacy settings from the JSON file.
    /// </summary>
    /// <param name="settingsPath">The path to the settings file.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The parsed legacy settings, or null if the file doesn't exist or can't be parsed.</returns>
    private async Task<LegacySettings?> ReadLegacySettingsAsync(string settingsPath, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] ReadLegacySettingsAsync - Path: {Path}", settingsPath);

        try
        {
            if (!File.Exists(settingsPath))
            {
                stopwatch.Stop();
                _logger.LogDebug(
                    "[SKIP] ReadLegacySettingsAsync - File not found in {Ms}ms",
                    stopwatch.ElapsedMilliseconds);
                return null;
            }

            var json = await File.ReadAllTextAsync(settingsPath, cancellationToken);
            var settings = JsonSerializer.Deserialize<LegacySettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            stopwatch.Stop();
            _logger.LogDebug(
                "[EXIT] ReadLegacySettingsAsync - Parsed settings in {Ms}ms",
                stopwatch.ElapsedMilliseconds);

            return settings;
        }
        catch (JsonException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                ex,
                "[ERROR] ReadLegacySettingsAsync - Failed to parse JSON after {Ms}ms: {Message}",
                stopwatch.ElapsedMilliseconds,
                ex.Message);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                ex,
                "[ERROR] ReadLegacySettingsAsync - Failed to read file after {Ms}ms: {Message}",
                stopwatch.ElapsedMilliseconds,
                ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Stamps the current version in the database.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    private async Task MarkMigrationCompleteAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] MarkMigrationCompleteAsync - Version: {Version}", CurrentVersion);

        try
        {
            var versionEntity = new AppVersionEntity
            {
                Major = CurrentVersion.Major,
                Minor = CurrentVersion.Minor,
                Patch = CurrentVersion.Build >= 0 ? CurrentVersion.Build : 0,
                MigratedAt = DateTime.UtcNow
            };

            _dbContext.AppVersions.Add(versionEntity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "[EXIT] MarkMigrationCompleteAsync - Stamped version {Version} in {Ms}ms",
                CurrentVersion,
                stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogDebug(
                "[EXIT] MarkMigrationCompleteAsync - Cancelled after {Ms}ms",
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "[ERROR] MarkMigrationCompleteAsync - Failed after {Ms}ms: {Message}",
                stopwatch.ElapsedMilliseconds,
                ex.Message);
            throw;
        }
    }

    #endregion
}
