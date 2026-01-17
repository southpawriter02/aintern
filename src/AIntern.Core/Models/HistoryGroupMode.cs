namespace AIntern.Core.Models;

/// <summary>
/// Grouping modes for change history display (v0.4.5h).
/// </summary>
public enum HistoryGroupMode
{
    /// <summary>
    /// No grouping - flat list.
    /// </summary>
    None,

    /// <summary>
    /// Group by file path.
    /// </summary>
    ByFile,

    /// <summary>
    /// Group by parent directory.
    /// </summary>
    ByDirectory,

    /// <summary>
    /// Group by change type.
    /// </summary>
    ByChangeType,

    /// <summary>
    /// Group by time period (Today, Yesterday, This Week, etc.).
    /// </summary>
    ByTimePeriod,

    /// <summary>
    /// Group by session/conversation.
    /// </summary>
    BySession
}
