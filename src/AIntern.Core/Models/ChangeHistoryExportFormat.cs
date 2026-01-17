namespace AIntern.Core.Models;

/// <summary>
/// Export format options for change history (v0.4.5h).
/// </summary>
public enum ChangeHistoryExportFormat
{
    /// <summary>
    /// JSON format with structured data.
    /// </summary>
    Json,

    /// <summary>
    /// CSV format for spreadsheet compatibility.
    /// </summary>
    Csv,

    /// <summary>
    /// Markdown format for documentation.
    /// </summary>
    Markdown,

    /// <summary>
    /// Unified diff format for git compatibility.
    /// </summary>
    UnifiedDiff
}
