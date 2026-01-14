namespace AIntern.Core.Models;

/// <summary>
/// Represents the result of a conversation export operation.
/// </summary>
/// <remarks>
/// <para>
/// This class encapsulates the result of an export operation, including:
/// </para>
/// <list type="bullet">
///   <item><description><b>Success:</b> Whether the export completed successfully</description></item>
///   <item><description><b>Content:</b> The exported content (empty on failure)</description></item>
///   <item><description><b>SuggestedFileName:</b> A safe filename for saving</description></item>
///   <item><description><b>MimeType:</b> The content's MIME type</description></item>
///   <item><description><b>ErrorMessage:</b> Error details if export failed</description></item>
/// </list>
/// <para>
/// Use <see cref="Failed(string)"/> to create a failed result with an error message.
/// </para>
/// <para>Added in v0.2.5c.</para>
/// </remarks>
/// <example>
/// Handling an export result:
/// <code>
/// var result = await exportService.ExportAsync(conversationId, options);
///
/// if (result.Success)
/// {
///     await File.WriteAllTextAsync(result.SuggestedFileName, result.Content);
/// }
/// else
/// {
///     Console.WriteLine($"Export failed: {result.ErrorMessage}");
/// }
/// </code>
/// </example>
public sealed class ExportResult
{
    /// <summary>
    /// Gets whether the export operation succeeded.
    /// </summary>
    /// <remarks>
    /// <para>When <c>true</c>, <see cref="Content"/> contains the exported data.</para>
    /// <para>When <c>false</c>, <see cref="ErrorMessage"/> contains the failure reason.</para>
    /// </remarks>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the exported content.
    /// </summary>
    /// <remarks>
    /// <para>Contains the full exported content when <see cref="Success"/> is <c>true</c>.</para>
    /// <para>Empty string when <see cref="Success"/> is <c>false</c>.</para>
    /// </remarks>
    public required string Content { get; init; }

    /// <summary>
    /// Gets the suggested filename for saving the export.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The filename is sanitized for cross-platform compatibility:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Lowercase</description></item>
    ///   <item><description>Special characters replaced with hyphens</description></item>
    ///   <item><description>Includes the appropriate file extension</description></item>
    /// </list>
    /// <para>Example: "my-conversation.md"</para>
    /// </remarks>
    public required string SuggestedFileName { get; init; }

    /// <summary>
    /// Gets the MIME type of the exported content.
    /// </summary>
    /// <remarks>
    /// <para>Standard MIME types by format:</para>
    /// <list type="bullet">
    ///   <item><description>Markdown: text/markdown</description></item>
    ///   <item><description>JSON: application/json</description></item>
    ///   <item><description>PlainText: text/plain</description></item>
    ///   <item><description>HTML: text/html</description></item>
    /// </list>
    /// </remarks>
    public required string MimeType { get; init; }

    /// <summary>
    /// Gets the error message if the export failed.
    /// </summary>
    /// <remarks>
    /// <para>Contains the error description when <see cref="Success"/> is <c>false</c>.</para>
    /// <para><c>null</c> when <see cref="Success"/> is <c>true</c>.</para>
    /// </remarks>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a failed export result with the specified error message.
    /// </summary>
    /// <param name="error">The error message describing why the export failed.</param>
    /// <returns>An <see cref="ExportResult"/> with <see cref="Success"/> set to <c>false</c>.</returns>
    /// <remarks>
    /// <para>
    /// This factory method creates a consistent failure result with:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Success: false</description></item>
    ///   <item><description>Content: empty string</description></item>
    ///   <item><description>SuggestedFileName: empty string</description></item>
    ///   <item><description>MimeType: empty string</description></item>
    ///   <item><description>ErrorMessage: the provided error</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// if (conversation is null)
    ///     return ExportResult.Failed("Conversation not found");
    /// </code>
    /// </example>
    public static ExportResult Failed(string error) => new()
    {
        Success = false,
        Content = string.Empty,
        SuggestedFileName = string.Empty,
        MimeType = string.Empty,
        ErrorMessage = error
    };
}
