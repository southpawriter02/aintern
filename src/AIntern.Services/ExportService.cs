namespace AIntern.Services;

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using AIntern.Core.Entities;
using AIntern.Core.Enums;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Data.Repositories;
using Microsoft.Extensions.Logging;

/// <summary>
/// Provides conversation export functionality in multiple formats.
/// </summary>
/// <remarks>
/// <para>
/// This service exports conversations to various formats:
/// </para>
/// <list type="bullet">
///   <item><description><b>Markdown:</b> Headers, bold roles, separators</description></item>
///   <item><description><b>JSON:</b> Structured data with messages array</description></item>
///   <item><description><b>PlainText:</b> Title with underline, timestamps</description></item>
///   <item><description><b>HTML:</b> Dark theme, responsive, embedded CSS</description></item>
/// </list>
/// <para>
/// The service uses <see cref="IConversationRepository"/> to load conversations
/// with their messages and system prompts.
/// </para>
/// <para>Added in v0.2.5c.</para>
/// </remarks>
public sealed partial class ExportService : IExportService
{
    #region Constants

    /// <summary>
    /// Application name used in export footers.
    /// </summary>
    private const string AppName = "AIntern";

    #endregion

    #region Fields

    private readonly IConversationRepository _conversationRepository;
    private readonly ILogger<ExportService> _logger;

    /// <summary>
    /// JSON serializer options for JSON export format.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportService"/> class.
    /// </summary>
    /// <param name="conversationRepository">Repository for loading conversations.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="conversationRepository"/> or <paramref name="logger"/> is null.
    /// </exception>
    public ExportService(
        IConversationRepository conversationRepository,
        ILogger<ExportService> logger)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("[INIT] ExportService created");
    }

    #endregion

    #region IExportService Implementation

    /// <inheritdoc />
    public async Task<ExportResult> ExportAsync(
        Guid conversationId,
        ExportOptions options,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug(
            "[ENTER] ExportAsync - ConversationId: {ConversationId}, {LogSummary}",
            conversationId,
            options.LogSummary);

        try
        {
            // Load conversation with messages.
            var conversation = await _conversationRepository.GetByIdWithMessagesAsync(
                conversationId,
                cancellationToken);

            if (conversation is null)
            {
                _logger.LogDebug(
                    "[SKIP] ExportAsync - Conversation not found: {ConversationId}",
                    conversationId);
                return ExportResult.Failed("Conversation not found");
            }

            // Export to requested format.
            var content = options.Format switch
            {
                ExportFormat.Markdown => ExportToMarkdown(conversation, options),
                ExportFormat.Json => ExportToJson(conversation, options),
                ExportFormat.PlainText => ExportToPlainText(conversation, options),
                ExportFormat.Html => ExportToHtml(conversation, options),
                _ => throw new ArgumentOutOfRangeException(nameof(options.Format), options.Format, "Unknown export format")
            };

            // Build result.
            var safeFileName = SanitizeFileName(conversation.Title);
            var extension = GetFileExtension(options.Format);
            var mimeType = GetMimeType(options.Format);

            stopwatch.Stop();
            _logger.LogInformation(
                "[EXIT] ExportAsync - Exported to {Format} ({Bytes} bytes) in {Ms}ms",
                options.Format,
                content.Length,
                stopwatch.ElapsedMilliseconds);

            return new ExportResult
            {
                Success = true,
                Content = content,
                SuggestedFileName = $"{safeFileName}{extension}",
                MimeType = mimeType
            };
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogDebug(
                "[EXIT] ExportAsync - Cancelled after {Ms}ms",
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex) when (ex is not ArgumentOutOfRangeException)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "[ERROR] ExportAsync - Failed after {Ms}ms: {Message}",
                stopwatch.ElapsedMilliseconds,
                ex.Message);
            return ExportResult.Failed(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<string> GeneratePreviewAsync(
        Guid conversationId,
        ExportOptions options,
        int maxLength = 500,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug(
            "[ENTER] GeneratePreviewAsync - ConversationId: {ConversationId}, MaxLength: {MaxLength}",
            conversationId,
            maxLength);

        var result = await ExportAsync(conversationId, options, cancellationToken);

        stopwatch.Stop();

        if (!result.Success)
        {
            _logger.LogDebug(
                "[EXIT] GeneratePreviewAsync - Error: {Error} in {Ms}ms",
                result.ErrorMessage,
                stopwatch.ElapsedMilliseconds);
            return $"Error: {result.ErrorMessage}";
        }

        if (result.Content.Length <= maxLength)
        {
            _logger.LogDebug(
                "[EXIT] GeneratePreviewAsync - Full content ({Length} chars) in {Ms}ms",
                result.Content.Length,
                stopwatch.ElapsedMilliseconds);
            return result.Content;
        }

        _logger.LogDebug(
            "[EXIT] GeneratePreviewAsync - Truncated to {MaxLength} of {Length} chars in {Ms}ms",
            maxLength,
            result.Content.Length,
            stopwatch.ElapsedMilliseconds);
        return result.Content[..maxLength] + "\n\n... (truncated)";
    }

    /// <inheritdoc />
    public string GetFileExtension(ExportFormat format) => format switch
    {
        ExportFormat.Markdown => ".md",
        ExportFormat.Json => ".json",
        ExportFormat.PlainText => ".txt",
        ExportFormat.Html => ".html",
        _ => ".txt"
    };

    /// <inheritdoc />
    public string GetMimeType(ExportFormat format) => format switch
    {
        ExportFormat.Markdown => "text/markdown",
        ExportFormat.Json => "application/json",
        ExportFormat.PlainText => "text/plain",
        ExportFormat.Html => "text/html",
        _ => "text/plain"
    };

    #endregion

    #region Format Exporters

    /// <summary>
    /// Exports a conversation to Markdown format.
    /// </summary>
    private static string ExportToMarkdown(ConversationEntity conversation, ExportOptions options)
    {
        var sb = new StringBuilder();

        // Title
        sb.AppendLine($"# {conversation.Title}");
        sb.AppendLine();

        // Metadata
        if (options.IncludeMetadata)
        {
            sb.AppendLine($"**Created:** {conversation.CreatedAt:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"**Last Updated:** {conversation.UpdatedAt:yyyy-MM-dd HH:mm}");
            sb.AppendLine();
        }

        // System prompt
        if (options.IncludeSystemPrompt && conversation.SystemPrompt is not null &&
            !string.IsNullOrWhiteSpace(conversation.SystemPrompt.Content))
        {
            sb.AppendLine("## System Prompt");
            sb.AppendLine();
            sb.AppendLine(conversation.SystemPrompt.Content);
            sb.AppendLine();
        }

        // Messages
        sb.AppendLine("## Conversation");
        sb.AppendLine();

        foreach (var message in conversation.Messages.OrderBy(m => m.SequenceNumber))
        {
            var roleLabel = GetRoleLabel(message.Role);

            if (options.IncludeTimestamps)
            {
                sb.AppendLine($"**{roleLabel}** ({message.Timestamp:HH:mm}):");
            }
            else
            {
                sb.AppendLine($"**{roleLabel}**:");
            }

            sb.AppendLine();
            sb.AppendLine(message.Content);

            if (options.IncludeTokenCounts && message.TokenCount.HasValue)
            {
                sb.AppendLine();
                sb.AppendLine($"*Tokens: {message.TokenCount}*");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        sb.AppendLine($"*Exported from {AppName}*");
        return sb.ToString();
    }

    /// <summary>
    /// Exports a conversation to JSON format.
    /// </summary>
    private static string ExportToJson(ConversationEntity conversation, ExportOptions options)
    {
        var export = new
        {
            title = conversation.Title,
            createdAt = conversation.CreatedAt,
            updatedAt = conversation.UpdatedAt,
            systemPrompt = options.IncludeSystemPrompt && conversation.SystemPrompt is not null
                ? conversation.SystemPrompt.Content
                : null,
            messages = conversation.Messages
                .OrderBy(m => m.SequenceNumber)
                .Select(m => new
                {
                    role = m.Role.ToString().ToLowerInvariant(),
                    content = m.Content,
                    timestamp = options.IncludeTimestamps ? m.Timestamp : (DateTime?)null,
                    tokenCount = options.IncludeTokenCounts ? m.TokenCount : null
                })
                .ToList(),
            exportedAt = DateTime.UtcNow,
            exportedBy = AppName
        };

        return JsonSerializer.Serialize(export, JsonOptions);
    }

    /// <summary>
    /// Exports a conversation to plain text format.
    /// </summary>
    private static string ExportToPlainText(ConversationEntity conversation, ExportOptions options)
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

        // System prompt
        if (options.IncludeSystemPrompt && conversation.SystemPrompt is not null &&
            !string.IsNullOrWhiteSpace(conversation.SystemPrompt.Content))
        {
            sb.AppendLine("System Prompt:");
            sb.AppendLine(conversation.SystemPrompt.Content);
            sb.AppendLine();
        }

        // Messages
        foreach (var message in conversation.Messages.OrderBy(m => m.SequenceNumber))
        {
            var roleLabel = message.Role.ToString().ToUpperInvariant();

            if (options.IncludeTimestamps)
            {
                sb.AppendLine($"[{message.Timestamp:HH:mm}] {roleLabel}:");
            }
            else
            {
                sb.AppendLine($"{roleLabel}:");
            }

            sb.AppendLine(message.Content);

            if (options.IncludeTokenCounts && message.TokenCount.HasValue)
            {
                sb.AppendLine($"(Tokens: {message.TokenCount})");
            }

            sb.AppendLine();
        }

        sb.AppendLine($"Exported from {AppName}");
        return sb.ToString();
    }

    /// <summary>
    /// Exports a conversation to HTML format with dark theme styling.
    /// </summary>
    private static string ExportToHtml(ConversationEntity conversation, ExportOptions options)
    {
        var sb = new StringBuilder();

        // HTML head
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"  <title>{HttpUtility.HtmlEncode(conversation.Title)}</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 800px; margin: 0 auto; padding: 20px; background: #1a1a2e; color: #e0e0e0; line-height: 1.6; }");
        sb.AppendLine("    h1 { color: #00d9ff; border-bottom: 2px solid #00d9ff; padding-bottom: 10px; margin-bottom: 20px; }");
        sb.AppendLine("    h2 { color: #7b68ee; margin-top: 30px; }");
        sb.AppendLine("    .metadata { color: #888; font-size: 0.9em; margin-bottom: 20px; }");
        sb.AppendLine("    .metadata p { margin: 5px 0; }");
        sb.AppendLine("    .system-prompt { background: #2d2d44; padding: 15px; border-radius: 8px; margin-bottom: 20px; border-left: 4px solid #ffd700; }");
        sb.AppendLine("    .system-prompt h3 { color: #ffd700; margin-top: 0; }");
        sb.AppendLine("    .message { margin-bottom: 20px; padding: 15px; border-radius: 8px; }");
        sb.AppendLine("    .message.user { background: #16213e; border-left: 4px solid #00d9ff; }");
        sb.AppendLine("    .message.assistant { background: #1f1f3a; border-left: 4px solid #7b68ee; }");
        sb.AppendLine("    .message.system { background: #2d2d44; border-left: 4px solid #ffd700; }");
        sb.AppendLine("    .role { font-weight: bold; margin-bottom: 8px; }");
        sb.AppendLine("    .role.user { color: #00d9ff; }");
        sb.AppendLine("    .role.assistant { color: #7b68ee; }");
        sb.AppendLine("    .role.system { color: #ffd700; }");
        sb.AppendLine("    .timestamp { color: #888; font-size: 0.85em; margin-left: 10px; }");
        sb.AppendLine("    .content { white-space: pre-wrap; word-wrap: break-word; }");
        sb.AppendLine("    .token-count { color: #666; font-size: 0.8em; margin-top: 10px; font-style: italic; }");
        sb.AppendLine("    .footer { margin-top: 40px; text-align: center; color: #666; font-size: 0.85em; border-top: 1px solid #333; padding-top: 20px; }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // Title
        sb.AppendLine($"  <h1>{HttpUtility.HtmlEncode(conversation.Title)}</h1>");

        // Metadata
        if (options.IncludeMetadata)
        {
            sb.AppendLine("  <div class=\"metadata\">");
            sb.AppendLine($"    <p><strong>Created:</strong> {conversation.CreatedAt:yyyy-MM-dd HH:mm}</p>");
            sb.AppendLine($"    <p><strong>Last Updated:</strong> {conversation.UpdatedAt:yyyy-MM-dd HH:mm}</p>");
            sb.AppendLine("  </div>");
        }

        // System prompt
        if (options.IncludeSystemPrompt && conversation.SystemPrompt is not null &&
            !string.IsNullOrWhiteSpace(conversation.SystemPrompt.Content))
        {
            sb.AppendLine("  <div class=\"system-prompt\">");
            sb.AppendLine("    <h3>System Prompt</h3>");
            sb.AppendLine($"    <div class=\"content\">{HttpUtility.HtmlEncode(conversation.SystemPrompt.Content)}</div>");
            sb.AppendLine("  </div>");
        }

        // Messages
        sb.AppendLine("  <h2>Conversation</h2>");

        foreach (var message in conversation.Messages.OrderBy(m => m.SequenceNumber))
        {
            var roleLower = message.Role.ToString().ToLowerInvariant();
            var roleLabel = GetRoleLabel(message.Role);

            sb.AppendLine($"  <div class=\"message {roleLower}\">");
            sb.Append($"    <div class=\"role {roleLower}\">{roleLabel}");

            if (options.IncludeTimestamps)
            {
                sb.Append($"<span class=\"timestamp\">{message.Timestamp:HH:mm}</span>");
            }

            sb.AppendLine("</div>");
            sb.AppendLine($"    <div class=\"content\">{HttpUtility.HtmlEncode(message.Content)}</div>");

            if (options.IncludeTokenCounts && message.TokenCount.HasValue)
            {
                sb.AppendLine($"    <div class=\"token-count\">Tokens: {message.TokenCount}</div>");
            }

            sb.AppendLine("  </div>");
        }

        // Footer
        sb.AppendLine($"  <div class=\"footer\">Exported from {AppName}</div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Gets the display label for a message role.
    /// </summary>
    private static string GetRoleLabel(MessageRole role) => role switch
    {
        MessageRole.User => "User",
        MessageRole.Assistant => "Assistant",
        MessageRole.System => "System",
        _ => "Unknown"
    };

    /// <summary>
    /// Sanitizes a filename for cross-platform file system safety.
    /// </summary>
    /// <param name="title">The conversation title to sanitize.</param>
    /// <returns>A safe lowercase filename without extension.</returns>
    /// <remarks>
    /// <para>
    /// The sanitization process:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Replaces non-alphanumeric characters (except hyphens, underscores, spaces) with hyphens</description></item>
    ///   <item><description>Replaces spaces with hyphens</description></item>
    ///   <item><description>Removes leading/trailing hyphens</description></item>
    ///   <item><description>Collapses multiple consecutive hyphens</description></item>
    ///   <item><description>Converts to lowercase</description></item>
    ///   <item><description>Returns "conversation" if result is empty</description></item>
    /// </list>
    /// </remarks>
    internal static string SanitizeFileName(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "conversation";
        }

        // Replace invalid characters with hyphens.
        var sanitized = InvalidFileNameCharsRegex().Replace(title, "-");

        // Replace spaces with hyphens.
        sanitized = sanitized.Replace(' ', '-');

        // Collapse multiple hyphens.
        sanitized = MultipleHyphensRegex().Replace(sanitized, "-");

        // Trim hyphens and convert to lowercase.
        sanitized = sanitized.Trim('-').ToLowerInvariant();

        return string.IsNullOrEmpty(sanitized) ? "conversation" : sanitized;
    }

    /// <summary>
    /// Regex pattern for invalid filename characters.
    /// </summary>
    [GeneratedRegex(@"[^a-zA-Z0-9\-_\s]")]
    private static partial Regex InvalidFileNameCharsRegex();

    /// <summary>
    /// Regex pattern for multiple consecutive hyphens.
    /// </summary>
    [GeneratedRegex(@"-+")]
    private static partial Regex MultipleHyphensRegex();

    #endregion
}
