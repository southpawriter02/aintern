using AIntern.Core.Enums;
using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

/// <summary>
/// Provides conversation export functionality in multiple formats.
/// </summary>
/// <remarks>
/// <para>
/// This service enables exporting conversations to various formats:
/// </para>
/// <list type="bullet">
///   <item><description><b>Markdown:</b> Headers, bold roles, separators</description></item>
///   <item><description><b>JSON:</b> Structured data with messages array</description></item>
///   <item><description><b>PlainText:</b> Title with underline, [HH:mm] timestamps</description></item>
///   <item><description><b>HTML:</b> Dark theme, responsive, embedded CSS</description></item>
/// </list>
/// <para>
/// Export options can be customized to include/exclude timestamps, system prompts,
/// metadata, and token counts. See <see cref="ExportOptions"/> for details.
/// </para>
/// <para>Added in v0.2.5c.</para>
/// </remarks>
/// <example>
/// Basic usage:
/// <code>
/// var exportService = serviceProvider.GetRequiredService&lt;IExportService&gt;();
///
/// // Export to Markdown with default options
/// var result = await exportService.ExportAsync(conversationId, ExportOptions.Default);
///
/// if (result.Success)
/// {
///     await File.WriteAllTextAsync(result.SuggestedFileName, result.Content);
/// }
///
/// // Generate a preview for UI display
/// var preview = await exportService.GeneratePreviewAsync(conversationId, options, maxLength: 500);
/// </code>
/// </example>
public interface IExportService
{
    /// <summary>
    /// Exports a conversation to the specified format.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation to export.</param>
    /// <param name="options">The export options specifying format and content inclusion.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="ExportResult"/> containing the exported content and metadata,
    /// or a failed result if the conversation was not found or an error occurred.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The method loads the conversation with all messages and exports according
    /// to the specified options. The result includes:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>The exported content in the requested format</description></item>
    ///   <item><description>A sanitized filename suggestion based on the conversation title</description></item>
    ///   <item><description>The appropriate MIME type for the format</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = new ExportOptions
    /// {
    ///     Format = ExportFormat.Html,
    ///     IncludeTimestamps = true,
    ///     IncludeSystemPrompt = true
    /// };
    /// var result = await exportService.ExportAsync(conversationId, options);
    /// </code>
    /// </example>
    Task<ExportResult> ExportAsync(
        Guid conversationId,
        ExportOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a preview of the export, truncated to the specified length.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation to preview.</param>
    /// <param name="options">The export options specifying format and content inclusion.</param>
    /// <param name="maxLength">The maximum length of the preview in characters. Default: 500.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A truncated preview of the export content, or an error message if the export failed.
    /// If the content is shorter than <paramref name="maxLength"/>, the full content is returned.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is useful for showing a preview in the UI before the user
    /// commits to saving the file. The preview is truncated with "... (truncated)"
    /// appended if it exceeds <paramref name="maxLength"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var preview = await exportService.GeneratePreviewAsync(
    ///     conversationId,
    ///     ExportOptions.Default,
    ///     maxLength: 500);
    /// previewTextBox.Text = preview;
    /// </code>
    /// </example>
    Task<string> GeneratePreviewAsync(
        Guid conversationId,
        ExportOptions options,
        int maxLength = 500,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the file extension for the specified export format.
    /// </summary>
    /// <param name="format">The export format.</param>
    /// <returns>
    /// The file extension including the leading period:
    /// ".md", ".json", ".txt", or ".html".
    /// </returns>
    /// <remarks>
    /// <para>
    /// File extensions by format:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="ExportFormat.Markdown"/>: ".md"</description></item>
    ///   <item><description><see cref="ExportFormat.Json"/>: ".json"</description></item>
    ///   <item><description><see cref="ExportFormat.PlainText"/>: ".txt"</description></item>
    ///   <item><description><see cref="ExportFormat.Html"/>: ".html"</description></item>
    /// </list>
    /// </remarks>
    string GetFileExtension(ExportFormat format);

    /// <summary>
    /// Gets the MIME type for the specified export format.
    /// </summary>
    /// <param name="format">The export format.</param>
    /// <returns>
    /// The standard MIME type for the format:
    /// "text/markdown", "application/json", "text/plain", or "text/html".
    /// </returns>
    /// <remarks>
    /// <para>
    /// MIME types by format:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="ExportFormat.Markdown"/>: "text/markdown"</description></item>
    ///   <item><description><see cref="ExportFormat.Json"/>: "application/json"</description></item>
    ///   <item><description><see cref="ExportFormat.PlainText"/>: "text/plain"</description></item>
    ///   <item><description><see cref="ExportFormat.Html"/>: "text/html"</description></item>
    /// </list>
    /// </remarks>
    string GetMimeType(ExportFormat format);
}
