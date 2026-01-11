namespace AIntern.Core.Models;

/// <summary>
/// Result of a conversation export operation.
/// </summary>
public sealed class ExportResult
{
    /// <summary>Whether the export completed successfully.</summary>
    public required bool Success { get; init; }

    /// <summary>The exported content in the requested format.</summary>
    public required string Content { get; init; }

    /// <summary>Suggested filename including extension.</summary>
    public required string SuggestedFileName { get; init; }

    /// <summary>MIME type for the exported content.</summary>
    public required string MimeType { get; init; }

    /// <summary>Error message if the export failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Creates a failed export result with an error message.</summary>
    public static ExportResult Failed(string error) => new()
    {
        Success = false,
        Content = string.Empty,
        SuggestedFileName = string.Empty,
        MimeType = string.Empty,
        ErrorMessage = error
    };
}
