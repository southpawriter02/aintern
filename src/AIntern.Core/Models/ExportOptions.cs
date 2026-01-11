namespace AIntern.Core.Models;

/// <summary>
/// Configuration options for conversation export.
/// </summary>
public sealed class ExportOptions
{
    /// <summary>The output format for the export.</summary>
    public ExportFormat Format { get; set; } = ExportFormat.Markdown;

    /// <summary>Whether to include timestamps for each message.</summary>
    public bool IncludeTimestamps { get; set; } = true;

    /// <summary>Whether to include the system prompt if present.</summary>
    public bool IncludeSystemPrompt { get; set; } = true;

    /// <summary>Whether to include token counts for messages.</summary>
    public bool IncludeTokenCounts { get; set; } = false;

    /// <summary>Whether to include conversation metadata (created/updated dates).</summary>
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>Default export options with all features enabled except token counts.</summary>
    public static ExportOptions Default => new();

    /// <summary>Minimal export options with only essential content.</summary>
    public static ExportOptions Minimal => new()
    {
        IncludeTimestamps = false,
        IncludeSystemPrompt = false,
        IncludeTokenCounts = false,
        IncludeMetadata = false
    };
}

/// <summary>
/// Supported export formats.
/// </summary>
public enum ExportFormat
{
    /// <summary>Markdown format with headers and formatting.</summary>
    Markdown,

    /// <summary>Structured JSON format.</summary>
    Json,

    /// <summary>Plain text format for maximum compatibility.</summary>
    PlainText,

    /// <summary>HTML format with embedded dark-theme styling.</summary>
    Html
}
