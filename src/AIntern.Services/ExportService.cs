using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using AIntern.Core.Entities;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

using static AIntern.Core.Models.MessageRole;

namespace AIntern.Services;

/// <summary>
/// Service for exporting conversations to various formats.
/// </summary>
public sealed partial class ExportService : IExportService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ILogger<ExportService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExportService(
        IConversationRepository conversationRepository,
        ILogger<ExportService> logger)
    {
        _conversationRepository = conversationRepository;
        _logger = logger;
    }

    public async Task<ExportResult> ExportAsync(
        Guid conversationId,
        ExportOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        _logger.LogDebug("Exporting conversation {Id} as {Format}", conversationId, options.Format);

        var conversation = await _conversationRepository.GetByIdWithMessagesAsync(conversationId, ct);
        if (conversation is null)
        {
            _logger.LogWarning("Conversation {Id} not found for export", conversationId);
            return ExportResult.Failed("Conversation not found.");
        }

        try
        {
            var content = options.Format switch
            {
                ExportFormat.Markdown => ExportToMarkdown(conversation, options),
                ExportFormat.Json => ExportToJson(conversation, options),
                ExportFormat.PlainText => ExportToPlainText(conversation, options),
                ExportFormat.Html => ExportToHtml(conversation, options),
                _ => throw new ArgumentOutOfRangeException(nameof(options.Format))
            };

            var fileName = GenerateFileName(conversation.Title, options.Format);

            _logger.LogInformation(
                "Exported conversation {Id} as {Format}: {Length} chars",
                conversationId, options.Format, content.Length);

            return new ExportResult
            {
                Success = true,
                Content = content,
                SuggestedFileName = fileName,
                MimeType = GetMimeType(options.Format)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting conversation {Id}", conversationId);
            return ExportResult.Failed($"Export failed: {ex.Message}");
        }
    }

    public async Task<string> GeneratePreviewAsync(
        Guid conversationId,
        ExportOptions options,
        int maxLength = 500,
        CancellationToken ct = default)
    {
        var result = await ExportAsync(conversationId, options, ct);
        if (!result.Success)
            return $"[Preview unavailable: {result.ErrorMessage}]";

        return result.Content.Length <= maxLength
            ? result.Content
            : result.Content[..maxLength] + "\n\n... (truncated)";
    }

    public string GetFileExtension(ExportFormat format) => format switch
    {
        ExportFormat.Markdown => ".md",
        ExportFormat.Json => ".json",
        ExportFormat.PlainText => ".txt",
        ExportFormat.Html => ".html",
        _ => ".txt"
    };

    public string GetMimeType(ExportFormat format) => format switch
    {
        ExportFormat.Markdown => "text/markdown",
        ExportFormat.Json => "application/json",
        ExportFormat.PlainText => "text/plain",
        ExportFormat.Html => "text/html",
        _ => "text/plain"
    };

    #region Format Exporters

    private string ExportToMarkdown(ConversationEntity conversation, ExportOptions options)
    {
        var sb = new StringBuilder();

        // Title
        sb.AppendLine($"# {conversation.Title}");
        sb.AppendLine();

        // Metadata
        if (options.IncludeMetadata)
        {
            sb.AppendLine($"**Created:** {conversation.CreatedAt:yyyy-MM-dd HH:mm}  ");
            sb.AppendLine($"**Updated:** {conversation.UpdatedAt:yyyy-MM-dd HH:mm}");
            sb.AppendLine();
        }

        // System Prompt
        if (options.IncludeSystemPrompt && conversation.SystemPrompt?.Content is not null)
        {
            sb.AppendLine("## System Prompt");
            sb.AppendLine();
            sb.AppendLine(conversation.SystemPrompt.Content);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        // Messages
        sb.AppendLine("## Conversation");
        sb.AppendLine();

        var messages = conversation.Messages.OrderBy(m => m.Timestamp).ToList();
        foreach (var message in messages)
        {
            var roleLabel = message.Role == User ? "**User**" : "**Assistant**";
            var timestamp = options.IncludeTimestamps
                ? $" _{message.Timestamp:HH:mm}_"
                : "";
            var tokens = options.IncludeTokenCounts && message.TokenCount > 0
                ? $" `{message.TokenCount} tokens`"
                : "";

            sb.AppendLine($"{roleLabel}{timestamp}{tokens}");
            sb.AppendLine();
            sb.AppendLine(message.Content);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private string ExportToJson(ConversationEntity conversation, ExportOptions options)
    {
        var export = new Dictionary<string, object>
        {
            ["id"] = conversation.Id.ToString(),
            ["title"] = conversation.Title
        };

        if (options.IncludeMetadata)
        {
            export["createdAt"] = conversation.CreatedAt.ToString("O");
            export["updatedAt"] = conversation.UpdatedAt.ToString("O");
        }

        if (options.IncludeSystemPrompt && conversation.SystemPrompt?.Content is not null)
        {
            export["systemPrompt"] = conversation.SystemPrompt.Content;
        }

        var messages = conversation.Messages
            .OrderBy(m => m.Timestamp)
            .Select(m =>
            {
                var msg = new Dictionary<string, object>
                {
                    ["role"] = m.Role,
                    ["content"] = m.Content
                };

                if (options.IncludeTimestamps)
                    msg["timestamp"] = m.Timestamp.ToString("O");

                if (options.IncludeTokenCounts && m.TokenCount > 0)
                    msg["tokenCount"] = m.TokenCount;

                return msg;
            })
            .ToList();

        export["messages"] = messages;

        return JsonSerializer.Serialize(export, JsonOptions);
    }

    private string ExportToPlainText(ConversationEntity conversation, ExportOptions options)
    {
        var sb = new StringBuilder();

        // Title with underline
        sb.AppendLine(conversation.Title);
        sb.AppendLine(new string('=', conversation.Title.Length));
        sb.AppendLine();

        // Metadata
        if (options.IncludeMetadata)
        {
            sb.AppendLine($"Created: {conversation.CreatedAt:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"Updated: {conversation.UpdatedAt:yyyy-MM-dd HH:mm}");
            sb.AppendLine();
        }

        // System Prompt
        if (options.IncludeSystemPrompt && conversation.SystemPrompt?.Content is not null)
        {
            sb.AppendLine("SYSTEM PROMPT:");
            sb.AppendLine(conversation.SystemPrompt.Content);
            sb.AppendLine();
            sb.AppendLine(new string('-', 40));
            sb.AppendLine();
        }

        // Messages
        var messages = conversation.Messages.OrderBy(m => m.Timestamp).ToList();
        foreach (var message in messages)
        {
            var role = message.Role.ToString().ToUpperInvariant();
            var timestamp = options.IncludeTimestamps
                ? $"[{message.Timestamp:HH:mm}] "
                : "";
            var tokens = options.IncludeTokenCounts && message.TokenCount > 0
                ? $" ({message.TokenCount} tokens)"
                : "";

            sb.AppendLine($"{timestamp}{role}:{tokens}");
            sb.AppendLine(message.Content);
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private string ExportToHtml(ConversationEntity conversation, ExportOptions options)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"UTF-8\">");
        sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"    <title>{EscapeHtml(conversation.Title)}</title>");
        sb.AppendLine("    <style>");
        sb.AppendLine(GetHtmlStyles());
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // Header
        sb.AppendLine($"    <h1>{EscapeHtml(conversation.Title)}</h1>");

        // Metadata
        if (options.IncludeMetadata)
        {
            sb.AppendLine("    <div class=\"metadata\">");
            sb.AppendLine($"        <span>Created: {conversation.CreatedAt:yyyy-MM-dd HH:mm}</span>");
            sb.AppendLine($"        <span>Updated: {conversation.UpdatedAt:yyyy-MM-dd HH:mm}</span>");
            sb.AppendLine("    </div>");
        }

        // System Prompt
        if (options.IncludeSystemPrompt && conversation.SystemPrompt?.Content is not null)
        {
            sb.AppendLine("    <div class=\"system-prompt\">");
            sb.AppendLine("        <h2>System Prompt</h2>");
            sb.AppendLine($"        <pre>{EscapeHtml(conversation.SystemPrompt.Content)}</pre>");
            sb.AppendLine("    </div>");
        }

        // Messages
        sb.AppendLine("    <div class=\"conversation\">");

        var messages = conversation.Messages.OrderBy(m => m.Timestamp).ToList();
        foreach (var message in messages)
        {
            var roleClass = message.Role == User ? "user" : "assistant";
            var roleLabel = message.Role == User ? "User" : "Assistant";

            sb.AppendLine($"        <div class=\"message {roleClass}\">");
            sb.AppendLine("            <div class=\"message-header\">");
            sb.AppendLine($"                <span class=\"role\">{roleLabel}</span>");

            if (options.IncludeTimestamps)
            {
                sb.AppendLine($"                <span class=\"timestamp\">{message.Timestamp:HH:mm}</span>");
            }

            if (options.IncludeTokenCounts && message.TokenCount > 0)
            {
                sb.AppendLine($"                <span class=\"tokens\">{message.TokenCount} tokens</span>");
            }

            sb.AppendLine("            </div>");
            sb.AppendLine($"            <div class=\"content\">{EscapeHtml(message.Content).Replace("\n", "<br>")}</div>");
            sb.AppendLine("        </div>");
        }

        sb.AppendLine("    </div>");

        // Footer
        sb.AppendLine("    <div class=\"footer\">");
        sb.AppendLine($"        <p>Exported from AIntern on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</p>");
        sb.AppendLine("    </div>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    #endregion

    #region Helpers

    private static string GetHtmlStyles() => """
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
            background: #1a1a2e;
            color: #e0e0e0;
            max-width: 800px;
            margin: 0 auto;
            padding: 2rem;
            line-height: 1.6;
        }
        h1 {
            color: #00d9ff;
            border-bottom: 2px solid #00d9ff;
            padding-bottom: 0.5rem;
        }
        h2 {
            color: #7b68ee;
            font-size: 1.2rem;
        }
        .metadata {
            display: flex;
            gap: 2rem;
            color: #888;
            font-size: 0.9rem;
            margin-bottom: 1.5rem;
        }
        .system-prompt {
            background: #252545;
            border-radius: 8px;
            padding: 1rem;
            margin-bottom: 2rem;
        }
        .system-prompt pre {
            white-space: pre-wrap;
            margin: 0;
            font-size: 0.9rem;
        }
        .conversation {
            display: flex;
            flex-direction: column;
            gap: 1rem;
        }
        .message {
            padding: 1rem;
            border-radius: 8px;
            background: #252545;
        }
        .message.user {
            border-left: 4px solid #00d9ff;
        }
        .message.assistant {
            border-left: 4px solid #7b68ee;
        }
        .message-header {
            display: flex;
            gap: 1rem;
            margin-bottom: 0.5rem;
            font-size: 0.85rem;
        }
        .role {
            font-weight: bold;
        }
        .message.user .role { color: #00d9ff; }
        .message.assistant .role { color: #7b68ee; }
        .timestamp, .tokens {
            color: #666;
        }
        .content {
            white-space: pre-wrap;
        }
        .footer {
            margin-top: 3rem;
            padding-top: 1rem;
            border-top: 1px solid #333;
            color: #666;
            font-size: 0.8rem;
            text-align: center;
        }
""";

    private string GenerateFileName(string title, ExportFormat format)
    {
        var sanitized = SanitizeFileName(title);
        var extension = GetFileExtension(format);
        return $"{sanitized}{extension}";
    }

    private static string SanitizeFileName(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "conversation";

        // Remove invalid filename characters, keep alphanumeric, spaces, hyphens, underscores
        var sanitized = InvalidFileNameCharsRegex().Replace(title, "-");

        // Collapse multiple hyphens
        sanitized = MultipleHyphensRegex().Replace(sanitized, "-");

        // Trim, lowercase, limit length
        sanitized = sanitized.Trim('-', ' ').ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(sanitized))
            return "conversation";

        // Limit to reasonable filename length
        return sanitized.Length > 50 ? sanitized[..50].TrimEnd('-') : sanitized;
    }

    private static string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    [GeneratedRegex(@"[^a-zA-Z0-9\-_ ]")]
    private static partial Regex InvalidFileNameCharsRegex();

    [GeneratedRegex(@"-+")]
    private static partial Regex MultipleHyphensRegex();

    #endregion
}
