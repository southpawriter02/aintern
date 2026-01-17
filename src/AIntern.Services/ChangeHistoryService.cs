using System.Text;
using System.Text.Json;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using Microsoft.Extensions.Logging;

namespace AIntern.Services;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ CHANGE HISTORY SERVICE (v0.4.5h)                                         │
// │ Implements change history queries and management.                        │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Implements change history queries and management.
/// </summary>
public sealed class ChangeHistoryService : IChangeHistoryService
{
    private readonly IUndoManager _undoManager;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<ChangeHistoryService> _logger;

    // In-memory cache of all change records (including expired ones)
    private readonly List<FileChangeRecord> _historyCache = new();
    private readonly object _lock = new();

    // ═══════════════════════════════════════════════════════════════════════
    // Events
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public event EventHandler<FileChangeRecord>? HistoryRecorded;

    /// <inheritdoc/>
    public event EventHandler<int>? HistoryCleared;

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════

    public ChangeHistoryService(
        IUndoManager undoManager,
        ISettingsService settingsService,
        ILogger<ChangeHistoryService> logger)
    {
        _undoManager = undoManager;
        _settingsService = settingsService;
        _logger = logger;

        // Subscribe to undo manager events
        _undoManager.UndoAvailable += OnUndoAvailable;
        _undoManager.UndoCompleted += OnUndoCompleted;

        _logger.LogDebug("[ChangeHistoryService] Initialized");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Query Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public Task<IReadOnlyList<FileChangeRecord>> GetHistoryAsync(
        ChangeHistoryFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[ChangeHistoryService] GetHistoryAsync - Filter: {Filter}",
            filter?.SearchText ?? "none");

        lock (_lock)
        {
            var undoWindow = _undoManager.UndoWindow;
            var filtered = ApplyFilter(_historyCache, filter, undoWindow);
            var sorted = ApplySort(filtered, filter?.SortOrder ?? ChangeHistorySortOrder.NewestFirst);
            return Task.FromResult<IReadOnlyList<FileChangeRecord>>(sorted.ToList());
        }
    }

    /// <inheritdoc/>
    public Task<ChangeHistoryStats> GetStatsAsync(
        ChangeHistoryFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[ChangeHistoryService] GetStatsAsync");

        lock (_lock)
        {
            var undoWindow = _undoManager.UndoWindow;
            var filtered = ApplyFilter(_historyCache, filter, undoWindow);
            var stats = ChangeHistoryStats.FromRecords(filtered, undoWindow);
            return Task.FromResult(stats);
        }
    }

    /// <inheritdoc/>
    public Task<int> GetCountAsync(
        ChangeHistoryFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var undoWindow = _undoManager.UndoWindow;
            var count = ApplyFilter(_historyCache, filter, undoWindow).Count();
            return Task.FromResult(count);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Grouped Query Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ChangeHistoryGroup>> GetGroupedByFileAsync(
        ChangeHistoryFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[ChangeHistoryService] GetGroupedByFileAsync");

        var records = await GetHistoryAsync(filter, cancellationToken);

        return records
            .GroupBy(r => r.FilePath)
            .Select(g => new ChangeHistoryGroup(
                Key: g.Key,
                Label: Path.GetFileName(g.Key),
                Icon: GetFileIcon(g.Key),
                Items: g.OrderByDescending(r => r.ChangedAt).ToList()))
            .OrderByDescending(g => g.MostRecent)
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ChangeHistoryGroup>> GetGroupedByTimePeriodAsync(
        ChangeHistoryFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[ChangeHistoryService] GetGroupedByTimePeriodAsync");

        var records = await GetHistoryAsync(filter, cancellationToken);
        var now = DateTime.UtcNow;
        var today = now.Date;

        var groups = new List<(string Key, string Label, List<FileChangeRecord> Items)>
        {
            ("today", "Today", new List<FileChangeRecord>()),
            ("yesterday", "Yesterday", new List<FileChangeRecord>()),
            ("this_week", "This Week", new List<FileChangeRecord>()),
            ("this_month", "This Month", new List<FileChangeRecord>()),
            ("older", "Older", new List<FileChangeRecord>())
        };

        foreach (var record in records)
        {
            var recordDate = record.ChangedAt.Date;

            if (recordDate == today)
                groups[0].Items.Add(record);
            else if (recordDate == today.AddDays(-1))
                groups[1].Items.Add(record);
            else if (recordDate >= today.AddDays(-(int)today.DayOfWeek))
                groups[2].Items.Add(record);
            else if (recordDate >= today.AddMonths(-1))
                groups[3].Items.Add(record);
            else
                groups[4].Items.Add(record);
        }

        return groups
            .Where(g => g.Items.Count > 0)
            .Select(g => new ChangeHistoryGroup(
                Key: g.Key,
                Label: g.Label,
                Icon: "CalendarIcon",
                Items: g.Items.OrderByDescending(r => r.ChangedAt).ToList()))
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ChangeHistoryGroup>> GetGroupedByChangeTypeAsync(
        ChangeHistoryFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[ChangeHistoryService] GetGroupedByChangeTypeAsync");

        var records = await GetHistoryAsync(filter, cancellationToken);

        return records
            .GroupBy(r => r.ChangeType)
            .Select(g => new ChangeHistoryGroup(
                Key: g.Key.ToString(),
                Label: GetChangeTypeLabel(g.Key),
                Icon: GetChangeTypeIcon(g.Key),
                Items: g.OrderByDescending(r => r.ChangedAt).ToList()))
            .OrderBy(g => g.Key)
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ChangeHistoryGroup>> GetGroupedByDirectoryAsync(
        ChangeHistoryFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[ChangeHistoryService] GetGroupedByDirectoryAsync");

        var records = await GetHistoryAsync(filter, cancellationToken);

        return records
            .GroupBy(r => Path.GetDirectoryName(r.RelativePath) ?? "/")
            .Select(g => new ChangeHistoryGroup(
                Key: g.Key,
                Label: string.IsNullOrEmpty(g.Key) ? "Root" : g.Key,
                Icon: "FolderIcon",
                Items: g.OrderByDescending(r => r.ChangedAt).ToList()))
            .OrderBy(g => g.Key)
            .ToList();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Search
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public Task<IReadOnlyList<FileChangeRecord>> SearchAsync(
        string searchText,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[ChangeHistoryService] SearchAsync - Text: {Text}", searchText);

        var filter = new ChangeHistoryFilter { SearchText = searchText };
        return GetHistoryAsync(filter, cancellationToken);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Export
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<Stream> ExportAsync(
        ChangeHistoryExportFormat format,
        ChangeHistoryFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[ChangeHistoryService] ExportAsync - Format: {Format}", format);

        var records = await GetHistoryAsync(filter, cancellationToken);
        var stream = new MemoryStream();

        switch (format)
        {
            case ChangeHistoryExportFormat.Json:
                await ExportToJsonAsync(stream, records);
                break;
            case ChangeHistoryExportFormat.Csv:
                await ExportToCsvAsync(stream, records);
                break;
            case ChangeHistoryExportFormat.Markdown:
                await ExportToMarkdownAsync(stream, records);
                break;
            case ChangeHistoryExportFormat.UnifiedDiff:
                await ExportToUnifiedDiffAsync(stream, records);
                break;
        }

        stream.Position = 0;
        return stream;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Management
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public Task<int> ClearHistoryAsync(
        DateTime? olderThan = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[ChangeHistoryService] ClearHistoryAsync - OlderThan: {Date}",
            olderThan?.ToString("O") ?? "all");

        int clearedCount;
        lock (_lock)
        {
            if (olderThan.HasValue)
            {
                clearedCount = _historyCache.RemoveAll(r => r.ChangedAt < olderThan.Value);
            }
            else
            {
                clearedCount = _historyCache.Count;
                _historyCache.Clear();
            }
        }

        if (clearedCount > 0)
        {
            HistoryCleared?.Invoke(this, clearedCount);
        }

        _logger.LogInformation("[ChangeHistoryService] Cleared {Count} history records", clearedCount);
        return Task.FromResult(clearedCount);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Event Handlers
    // ═══════════════════════════════════════════════════════════════════════

    private void OnUndoAvailable(object? sender, UndoAvailableEventArgs e)
    {
        if (e.UndoState.ChangeRecord is not null)
        {
            lock (_lock)
            {
                _historyCache.Insert(0, e.UndoState.ChangeRecord);
                TrimHistory();
            }

            HistoryRecorded?.Invoke(this, e.UndoState.ChangeRecord);
            _logger.LogDebug("[ChangeHistoryService] Recorded change: {Path}", e.FilePath);
        }
    }

    private void OnUndoCompleted(object? sender, UndoCompletedEventArgs e)
    {
        lock (_lock)
        {
            var record = _historyCache.FirstOrDefault(r => r.Id == e.ChangeId);
            if (record is not null)
            {
                record.IsUndone = true;
                record.UndoneAt = DateTime.UtcNow;
            }
        }

        _logger.LogDebug("[ChangeHistoryService] Change undone: {Id}", e.ChangeId);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Private Helper Methods
    // ═══════════════════════════════════════════════════════════════════════

    private void TrimHistory()
    {
        var maxItems = _settingsService.CurrentSettings.MaxChangeHistoryItems;
        while (_historyCache.Count > maxItems)
        {
            _historyCache.RemoveAt(_historyCache.Count - 1);
        }
    }

    private static IEnumerable<FileChangeRecord> ApplyFilter(
        IEnumerable<FileChangeRecord> records,
        ChangeHistoryFilter? filter,
        TimeSpan undoWindow)
    {
        if (filter is null) return records;
        return records.Where(r => filter.Matches(r, undoWindow));
    }

    private static IOrderedEnumerable<FileChangeRecord> ApplySort(
        IEnumerable<FileChangeRecord> records,
        ChangeHistorySortOrder sortOrder)
    {
        return sortOrder switch
        {
            ChangeHistorySortOrder.NewestFirst =>
                records.OrderByDescending(r => r.ChangedAt),
            ChangeHistorySortOrder.OldestFirst =>
                records.OrderBy(r => r.ChangedAt),
            ChangeHistorySortOrder.FileName =>
                records.OrderBy(r => Path.GetFileName(r.FilePath)),
            ChangeHistorySortOrder.ChangeType =>
                records.OrderBy(r => r.ChangeType),
            ChangeHistorySortOrder.ExpiringSoonest =>
                records.OrderBy(r => r.IsUndone ? DateTime.MaxValue : r.ChangedAt),
            _ => records.OrderByDescending(r => r.ChangedAt)
        };
    }

    private static string GetFileIcon(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".cs" => "CSharpIcon",
            ".ts" or ".tsx" => "TypeScriptIcon",
            ".js" or ".jsx" => "JavaScriptIcon",
            ".json" => "JsonIcon",
            ".xml" or ".xaml" or ".axaml" => "XmlIcon",
            ".md" => "MarkdownIcon",
            ".css" or ".scss" or ".sass" => "CssIcon",
            ".html" => "HtmlIcon",
            _ => "FileIcon"
        };
    }

    private static string GetChangeTypeLabel(FileChangeType changeType) =>
        changeType switch
        {
            FileChangeType.Created => "Created",
            FileChangeType.Modified => "Modified",
            FileChangeType.Deleted => "Deleted",
            FileChangeType.Renamed => "Renamed",
            _ => "Changed"
        };

    private static string GetChangeTypeIcon(FileChangeType changeType) =>
        changeType switch
        {
            FileChangeType.Created => "PlusCircleIcon",
            FileChangeType.Modified => "EditIcon",
            FileChangeType.Deleted => "TrashIcon",
            FileChangeType.Renamed => "ArrowRightIcon",
            _ => "FileIcon"
        };

    // ═══════════════════════════════════════════════════════════════════════
    // Export Methods
    // ═══════════════════════════════════════════════════════════════════════

    private static async Task ExportToJsonAsync(
        Stream stream,
        IReadOnlyList<FileChangeRecord> records)
    {
        var exportData = records.Select(r => new
        {
            r.Id,
            r.FilePath,
            r.RelativePath,
            ChangeType = r.ChangeType.ToString(),
            r.ChangedAt,
            r.LinesAdded,
            r.LinesRemoved,
            r.IsUndone,
            r.Description
        });

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        await JsonSerializer.SerializeAsync(stream, exportData, options);
    }

    private static async Task ExportToCsvAsync(
        Stream stream,
        IReadOnlyList<FileChangeRecord> records)
    {
        await using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);

        // Header
        await writer.WriteLineAsync(
            "Id,FilePath,ChangeType,ChangedAt,LinesAdded,LinesRemoved,IsUndone");

        // Data rows
        foreach (var record in records)
        {
            await writer.WriteLineAsync(
                $"\"{record.Id}\"," +
                $"\"{record.FilePath.Replace("\"", "\"\"")}\"," +
                $"\"{record.ChangeType}\"," +
                $"\"{record.ChangedAt:O}\"," +
                $"{record.LinesAdded}," +
                $"{record.LinesRemoved}," +
                $"{record.IsUndone}");
        }
    }

    private static async Task ExportToMarkdownAsync(
        Stream stream,
        IReadOnlyList<FileChangeRecord> records)
    {
        await using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);

        await writer.WriteLineAsync("# Change History Export");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync($"Exported: {DateTime.Now:F}");
        await writer.WriteLineAsync($"Total Changes: {records.Count}");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("---");
        await writer.WriteLineAsync();

        foreach (var record in records)
        {
            await writer.WriteLineAsync($"## {record.FileName}");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync($"- **Path**: `{record.RelativePath}`");
            await writer.WriteLineAsync($"- **Type**: {record.ChangeType}");
            await writer.WriteLineAsync($"- **Time**: {record.ChangedAt:g}");
            await writer.WriteLineAsync($"- **Lines**: +{record.LinesAdded} -{record.LinesRemoved}");
            if (record.IsUndone)
            {
                await writer.WriteLineAsync("- **Status**: Undone");
            }
            await writer.WriteLineAsync();
        }
    }

    private static async Task ExportToUnifiedDiffAsync(
        Stream stream,
        IReadOnlyList<FileChangeRecord> records)
    {
        await using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);

        foreach (var record in records)
        {
            await writer.WriteLineAsync($"--- a/{record.RelativePath}");
            await writer.WriteLineAsync($"+++ b/{record.RelativePath}");
            await writer.WriteLineAsync($"@@ Change: {record.ChangeType} at {record.ChangedAt:O} @@");
            await writer.WriteLineAsync($"# +{record.LinesAdded} -{record.LinesRemoved} lines");
            await writer.WriteLineAsync();
        }
    }
}
