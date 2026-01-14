// -----------------------------------------------------------------------
// <copyright file="MigrationResult.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
// </copyright>
// <summary>
//     Result record for migration operations tracking success, versions, and steps.
//     Added in v0.2.5d.
// </summary>
// -----------------------------------------------------------------------

namespace AIntern.Core.Models;

/// <summary>
/// Represents the result of a migration operation.
/// </summary>
/// <remarks>
/// <para>
/// This immutable record captures the outcome of a migration attempt, including
/// whether it succeeded, the version transition, steps performed, and any error message.
/// </para>
/// <para>
/// Use the static factory methods to create instances:
/// <list type="bullet">
///     <item><see cref="NoMigrationNeeded"/> - When already at current version</item>
///     <item><see cref="Succeeded"/> - When migration completed successfully</item>
///     <item><see cref="Failed"/> - When migration encountered an error</item>
/// </list>
/// </para>
/// </remarks>
public sealed record MigrationResult
{
    /// <summary>
    /// Gets a value indicating whether the migration operation succeeded.
    /// </summary>
    /// <value>
    /// <c>true</c> if the migration completed successfully or no migration was needed;
    /// <c>false</c> if an error occurred during migration.
    /// </value>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the version before migration was attempted.
    /// </summary>
    /// <value>
    /// The source version, or the current version if no migration was needed.
    /// </value>
    public required Version FromVersion { get; init; }

    /// <summary>
    /// Gets the version after migration completed.
    /// </summary>
    /// <value>
    /// The target version, or the current version if no migration was needed.
    /// </value>
    public required Version ToVersion { get; init; }

    /// <summary>
    /// Gets the list of migration steps that were performed.
    /// </summary>
    /// <value>
    /// A read-only list of step descriptions. Empty if no migration was needed.
    /// Contains at least one entry if migration was attempted.
    /// </value>
    public required IReadOnlyList<string> MigrationSteps { get; init; }

    /// <summary>
    /// Gets the error message if the migration failed.
    /// </summary>
    /// <value>
    /// The error message describing the failure, or <c>null</c> if successful.
    /// </value>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a result indicating no migration was needed.
    /// </summary>
    /// <param name="currentVersion">The current application version.</param>
    /// <returns>A successful <see cref="MigrationResult"/> with no steps performed.</returns>
    public static MigrationResult NoMigrationNeeded(Version currentVersion) => new()
    {
        Success = true,
        FromVersion = currentVersion,
        ToVersion = currentVersion,
        MigrationSteps = ["No migration required"],
        ErrorMessage = null,
    };

    /// <summary>
    /// Creates a result indicating successful migration.
    /// </summary>
    /// <param name="fromVersion">The version before migration.</param>
    /// <param name="toVersion">The version after migration.</param>
    /// <param name="steps">The list of steps that were performed.</param>
    /// <returns>A successful <see cref="MigrationResult"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="fromVersion"/>, <paramref name="toVersion"/>,
    /// or <paramref name="steps"/> is <c>null</c>.
    /// </exception>
    public static MigrationResult Succeeded(
        Version fromVersion,
        Version toVersion,
        IReadOnlyList<string> steps)
    {
        ArgumentNullException.ThrowIfNull(fromVersion);
        ArgumentNullException.ThrowIfNull(toVersion);
        ArgumentNullException.ThrowIfNull(steps);

        return new MigrationResult
        {
            Success = true,
            FromVersion = fromVersion,
            ToVersion = toVersion,
            MigrationSteps = steps,
            ErrorMessage = null,
        };
    }

    /// <summary>
    /// Creates a result indicating migration failure.
    /// </summary>
    /// <param name="fromVersion">The version before migration was attempted.</param>
    /// <param name="toVersion">The target version that was not reached.</param>
    /// <param name="steps">The steps that were performed before failure.</param>
    /// <param name="errorMessage">A description of the error that occurred.</param>
    /// <returns>A failed <see cref="MigrationResult"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="fromVersion"/>, <paramref name="toVersion"/>,
    /// <paramref name="steps"/>, or <paramref name="errorMessage"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="errorMessage"/> is empty or whitespace.
    /// </exception>
    public static MigrationResult Failed(
        Version fromVersion,
        Version toVersion,
        IReadOnlyList<string> steps,
        string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(fromVersion);
        ArgumentNullException.ThrowIfNull(toVersion);
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return new MigrationResult
        {
            Success = false,
            FromVersion = fromVersion,
            ToVersion = toVersion,
            MigrationSteps = steps,
            ErrorMessage = errorMessage,
        };
    }
}
