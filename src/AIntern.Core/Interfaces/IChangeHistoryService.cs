using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ CHANGE HISTORY SERVICE INTERFACE (v0.4.5h)                               │
// │ Service for querying and managing change history.                        │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Service for querying and managing change history.
/// </summary>
public interface IChangeHistoryService
{
    // ═══════════════════════════════════════════════════════════════════════
    // Query Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets change history matching the filter.
    /// </summary>
    /// <param name="filter">Optional filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching change records.</returns>
    Task<IReadOnlyList<FileChangeRecord>> GetHistoryAsync(
        ChangeHistoryFilter? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aggregated statistics for the filtered history.
    /// </summary>
    /// <param name="filter">Optional filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated statistics.</returns>
    Task<ChangeHistoryStats> GetStatsAsync(
        ChangeHistoryFilter? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of history items.
    /// </summary>
    /// <param name="filter">Optional filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Count of matching records.</returns>
    Task<int> GetCountAsync(
        ChangeHistoryFilter? filter = null,
        CancellationToken cancellationToken = default);

    // ═══════════════════════════════════════════════════════════════════════
    // Grouped Query Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets history grouped by file.
    /// </summary>
    /// <param name="filter">Optional filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Groups of records by file path.</returns>
    Task<IReadOnlyList<ChangeHistoryGroup>> GetGroupedByFileAsync(
        ChangeHistoryFilter? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets history grouped by time period.
    /// </summary>
    /// <param name="filter">Optional filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Groups of records by time period (Today, Yesterday, etc.).</returns>
    Task<IReadOnlyList<ChangeHistoryGroup>> GetGroupedByTimePeriodAsync(
        ChangeHistoryFilter? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets history grouped by change type.
    /// </summary>
    /// <param name="filter">Optional filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Groups of records by change type.</returns>
    Task<IReadOnlyList<ChangeHistoryGroup>> GetGroupedByChangeTypeAsync(
        ChangeHistoryFilter? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets history grouped by directory.
    /// </summary>
    /// <param name="filter">Optional filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Groups of records by directory.</returns>
    Task<IReadOnlyList<ChangeHistoryGroup>> GetGroupedByDirectoryAsync(
        ChangeHistoryFilter? filter = null,
        CancellationToken cancellationToken = default);

    // ═══════════════════════════════════════════════════════════════════════
    // Search
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Searches history by text.
    /// </summary>
    /// <param name="searchText">Text to search for in file paths.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching records.</returns>
    Task<IReadOnlyList<FileChangeRecord>> SearchAsync(
        string searchText,
        CancellationToken cancellationToken = default);

    // ═══════════════════════════════════════════════════════════════════════
    // Export
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Exports history to a stream in the specified format.
    /// </summary>
    /// <param name="format">Export format.</param>
    /// <param name="filter">Optional filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Stream containing the exported data.</returns>
    Task<Stream> ExportAsync(
        ChangeHistoryExportFormat format,
        ChangeHistoryFilter? filter = null,
        CancellationToken cancellationToken = default);

    // ═══════════════════════════════════════════════════════════════════════
    // Management
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Clears history older than the specified date.
    /// </summary>
    /// <param name="olderThan">Clear records older than this date. Null clears all.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of records cleared.</returns>
    Task<int> ClearHistoryAsync(
        DateTime? olderThan = null,
        CancellationToken cancellationToken = default);

    // ═══════════════════════════════════════════════════════════════════════
    // Events
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Raised when new history is recorded.
    /// </summary>
    event EventHandler<FileChangeRecord>? HistoryRecorded;

    /// <summary>
    /// Raised when history is cleared.
    /// </summary>
    event EventHandler<int>? HistoryCleared;
}
