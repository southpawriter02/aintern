// -----------------------------------------------------------------------
// <copyright file="AppVersionEntity.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
// </copyright>
// <summary>
//     Entity for tracking application version and migration history in the database.
//     Added in v0.2.5d.
// </summary>
// -----------------------------------------------------------------------

namespace AIntern.Core.Entities;

/// <summary>
/// Represents an application version record in the database.
/// </summary>
/// <remarks>
/// <para>
/// This entity tracks version history for the application, recording each migration
/// that has been performed. The most recent record represents the current database version.
/// </para>
/// <para>
/// Version numbers follow semantic versioning (Major.Minor.Patch). The <see cref="ToVersion"/>
/// method converts the stored components to a <see cref="System.Version"/> object.
/// </para>
/// </remarks>
public sealed class AppVersionEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this version record.
    /// </summary>
    /// <value>
    /// The auto-generated primary key.
    /// </value>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the major version number.
    /// </summary>
    /// <value>
    /// The major version component (e.g., 0 in 0.2.0).
    /// </value>
    public int Major { get; set; }

    /// <summary>
    /// Gets or sets the minor version number.
    /// </summary>
    /// <value>
    /// The minor version component (e.g., 2 in 0.2.0).
    /// </value>
    public int Minor { get; set; }

    /// <summary>
    /// Gets or sets the patch version number.
    /// </summary>
    /// <value>
    /// The patch version component (e.g., 0 in 0.2.0).
    /// </value>
    public int Patch { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this migration was performed.
    /// </summary>
    /// <value>
    /// The UTC timestamp of the migration.
    /// </value>
    public DateTime MigratedAt { get; set; }

    /// <summary>
    /// Converts this entity to a <see cref="Version"/> object.
    /// </summary>
    /// <returns>
    /// A <see cref="Version"/> object with the stored Major, Minor, and Patch values.
    /// </returns>
    public Version ToVersion() => new(Major, Minor, Patch);
}
