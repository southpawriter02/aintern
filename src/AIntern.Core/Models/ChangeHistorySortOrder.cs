namespace AIntern.Core.Models;

/// <summary>
/// Sort order for change history display (v0.4.5h).
/// </summary>
public enum ChangeHistorySortOrder
{
    /// <summary>
    /// Most recent changes first (default).
    /// </summary>
    NewestFirst,

    /// <summary>
    /// Oldest changes first.
    /// </summary>
    OldestFirst,

    /// <summary>
    /// Alphabetically by file name.
    /// </summary>
    FileName,

    /// <summary>
    /// Grouped by change type.
    /// </summary>
    ChangeType,

    /// <summary>
    /// Changes expiring soonest first.
    /// </summary>
    ExpiringSoonest
}
