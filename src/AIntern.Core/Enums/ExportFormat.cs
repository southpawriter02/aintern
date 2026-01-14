namespace AIntern.Core.Enums;

/// <summary>
/// Specifies the format for exporting conversations.
/// </summary>
/// <remarks>
/// <para>
/// Each format has an associated file extension and MIME type:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Markdown"/>: .md, text/markdown</description></item>
///   <item><description><see cref="Json"/>: .json, application/json</description></item>
///   <item><description><see cref="PlainText"/>: .txt, text/plain</description></item>
///   <item><description><see cref="Html"/>: .html, text/html</description></item>
/// </list>
/// <para>Added in v0.2.5c.</para>
/// </remarks>
public enum ExportFormat
{
    /// <summary>
    /// Markdown format with headers, bold role labels, and separators.
    /// </summary>
    /// <remarks>
    /// <para>File extension: .md</para>
    /// <para>MIME type: text/markdown</para>
    /// </remarks>
    Markdown,

    /// <summary>
    /// JSON format with structured messages array.
    /// </summary>
    /// <remarks>
    /// <para>File extension: .json</para>
    /// <para>MIME type: application/json</para>
    /// </remarks>
    Json,

    /// <summary>
    /// Plain text format with title underline and timestamps.
    /// </summary>
    /// <remarks>
    /// <para>File extension: .txt</para>
    /// <para>MIME type: text/plain</para>
    /// </remarks>
    PlainText,

    /// <summary>
    /// HTML format with embedded dark theme CSS.
    /// </summary>
    /// <remarks>
    /// <para>File extension: .html</para>
    /// <para>MIME type: text/html</para>
    /// <para>Produces a standalone, responsive HTML file with dark theme styling.</para>
    /// </remarks>
    Html
}
