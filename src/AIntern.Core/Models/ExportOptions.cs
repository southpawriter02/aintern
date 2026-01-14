using AIntern.Core.Enums;

namespace AIntern.Core.Models;

/// <summary>
/// Configuration options for exporting conversations.
/// </summary>
/// <remarks>
/// <para>
/// This class provides configurable options for conversation export, including:
/// </para>
/// <list type="bullet">
///   <item><description><b>Format:</b> The output format (Markdown, JSON, PlainText, HTML)</description></item>
///   <item><description><b>Timestamps:</b> Whether to include message timestamps</description></item>
///   <item><description><b>System Prompt:</b> Whether to include the system prompt</description></item>
///   <item><description><b>Token Counts:</b> Whether to include per-message token counts</description></item>
///   <item><description><b>Metadata:</b> Whether to include conversation metadata (dates)</description></item>
/// </list>
/// <para>
/// Use <see cref="Default"/> for standard exports or <see cref="Minimal"/> for
/// content-only exports without extra information.
/// </para>
/// <para>Added in v0.2.5c.</para>
/// </remarks>
/// <example>
/// Creating export options:
/// <code>
/// // Default options (all features enabled except token counts)
/// var options = ExportOptions.Default;
///
/// // Minimal export (just the conversation content)
/// var minimal = ExportOptions.Minimal;
///
/// // Custom options
/// var custom = new ExportOptions
/// {
///     Format = ExportFormat.Html,
///     IncludeTimestamps = true,
///     IncludeSystemPrompt = false
/// };
/// </code>
/// </example>
public sealed class ExportOptions
{
    /// <summary>
    /// Gets or sets the export format.
    /// </summary>
    /// <remarks>
    /// Default value: <see cref="ExportFormat.Markdown"/>.
    /// </remarks>
    public ExportFormat Format { get; set; } = ExportFormat.Markdown;

    /// <summary>
    /// Gets or sets whether to include message timestamps in the export.
    /// </summary>
    /// <remarks>
    /// <para>Default value: <c>true</c>.</para>
    /// <para>When enabled, timestamps are formatted based on the export format:</para>
    /// <list type="bullet">
    ///   <item><description>Markdown: (HH:mm) after role label</description></item>
    ///   <item><description>PlainText: [HH:mm] before role label</description></item>
    ///   <item><description>HTML: Shown in timestamp span</description></item>
    ///   <item><description>JSON: Full ISO 8601 timestamp</description></item>
    /// </list>
    /// </remarks>
    public bool IncludeTimestamps { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include the system prompt in the export.
    /// </summary>
    /// <remarks>
    /// <para>Default value: <c>true</c>.</para>
    /// <para>When enabled and a system prompt exists, it is included as a
    /// separate section before the conversation messages.</para>
    /// </remarks>
    public bool IncludeSystemPrompt { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include token counts per message.
    /// </summary>
    /// <remarks>
    /// <para>Default value: <c>false</c>.</para>
    /// <para>When enabled, each message includes its token count (if available).
    /// This is primarily useful for debugging or analysis purposes.</para>
    /// </remarks>
    public bool IncludeTokenCounts { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to include conversation metadata.
    /// </summary>
    /// <remarks>
    /// <para>Default value: <c>true</c>.</para>
    /// <para>When enabled, includes metadata such as:</para>
    /// <list type="bullet">
    ///   <item><description>Created date</description></item>
    ///   <item><description>Last updated date</description></item>
    /// </list>
    /// </remarks>
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>
    /// Gets the default export options.
    /// </summary>
    /// <remarks>
    /// <para>Default options include:</para>
    /// <list type="bullet">
    ///   <item><description>Format: Markdown</description></item>
    ///   <item><description>Timestamps: Yes</description></item>
    ///   <item><description>System Prompt: Yes</description></item>
    ///   <item><description>Token Counts: No</description></item>
    ///   <item><description>Metadata: Yes</description></item>
    /// </list>
    /// </remarks>
    public static ExportOptions Default => new();

    /// <summary>
    /// Gets minimal export options with no extra information.
    /// </summary>
    /// <remarks>
    /// <para>Minimal options include:</para>
    /// <list type="bullet">
    ///   <item><description>Format: Markdown</description></item>
    ///   <item><description>Timestamps: No</description></item>
    ///   <item><description>System Prompt: No</description></item>
    ///   <item><description>Token Counts: No</description></item>
    ///   <item><description>Metadata: No</description></item>
    /// </list>
    /// </remarks>
    public static ExportOptions Minimal => new()
    {
        IncludeTimestamps = false,
        IncludeSystemPrompt = false,
        IncludeTokenCounts = false,
        IncludeMetadata = false
    };

    /// <summary>
    /// Gets a summary of the export options for logging purposes.
    /// </summary>
    /// <remarks>
    /// Returns a compact string representation of the enabled options.
    /// </remarks>
    public string LogSummary => $"Format={Format}, Timestamps={IncludeTimestamps}, SystemPrompt={IncludeSystemPrompt}, TokenCounts={IncludeTokenCounts}, Metadata={IncludeMetadata}";
}
