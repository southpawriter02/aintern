using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

/// <summary>
/// Service for exporting conversations to various formats.
/// </summary>
public interface IExportService
{
    /// <summary>Exports a conversation to the specified format.</summary>
    /// <param name="conversationId">The conversation ID to export.</param>
    /// <param name="options">Export configuration options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Export result with content and metadata.</returns>
    Task<ExportResult> ExportAsync(
        Guid conversationId,
        ExportOptions options,
        CancellationToken ct = default);

    /// <summary>Generates a preview of the export (truncated).</summary>
    /// <param name="conversationId">The conversation ID to preview.</param>
    /// <param name="options">Export configuration options.</param>
    /// <param name="maxLength">Maximum preview length in characters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Truncated preview of the export content.</returns>
    Task<string> GeneratePreviewAsync(
        Guid conversationId,
        ExportOptions options,
        int maxLength = 500,
        CancellationToken ct = default);

    /// <summary>Gets the file extension for the specified format.</summary>
    string GetFileExtension(ExportFormat format);

    /// <summary>Gets the MIME type for the specified format.</summary>
    string GetMimeType(ExportFormat format);
}
