namespace AIntern.Services;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

/// <summary>
/// Formats file contexts for LLM prompts and UI display.
/// </summary>
public sealed class ContextFormatter : IContextFormatter
{
    private const int MaxDisplayLines = 10;

    /// <inheritdoc />
    public string FormatForPrompt(IEnumerable<FileContext> contexts)
    {
        var contextList = contexts.ToList();
        if (contextList.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();

        // Header indicating context is provided
        sb.AppendLine("I'm providing you with the following code context:");
        sb.AppendLine();

        foreach (var context in contextList)
        {
            sb.Append(FormatSingleContext(context));
            sb.AppendLine();
        }

        sb.AppendLine("Please consider this context when responding to my question below.");
        sb.AppendLine();

        return sb.ToString();
    }

    /// <inheritdoc />
    public string FormatSingleContext(FileContext context)
    {
        var sb = new StringBuilder();

        // File header
        sb.Append(FormatContextHeader(context));

        // Code block with syntax highlighting
        sb.Append(FormatCodeBlock(context.Content, context.Language));

        return sb.ToString();
    }

    /// <inheritdoc />
    public string FormatForDisplay(IEnumerable<FileContext> contexts, bool expanded = false)
    {
        var contextList = contexts.ToList();
        if (contextList.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();

        foreach (var context in contextList)
        {
            sb.AppendLine($"**{context.FileName}**");

            if (context.IsPartialContent)
            {
                sb.AppendLine($"_Lines {context.StartLine}-{context.EndLine}_");
            }

            if (expanded)
            {
                sb.Append(FormatCodeBlock(context.Content, context.Language));
            }
            else
            {
                // Truncated preview
                var preview = GetPreview(context.Content, MaxDisplayLines);
                sb.Append(FormatCodeBlock(preview, context.Language));
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public string FormatCodeBlock(string content, string? language)
    {
        var sb = new StringBuilder();

        sb.Append("```");
        sb.AppendLine(language ?? string.Empty);
        sb.AppendLine(content.TrimEnd());
        sb.AppendLine("```");

        return sb.ToString();
    }

    /// <inheritdoc />
    public string FormatContextHeader(FileContext context)
    {
        var sb = new StringBuilder();

        sb.Append($"### File: `{context.FileName}`");

        if (!string.IsNullOrEmpty(context.Language))
        {
            sb.Append($" ({context.Language})");
        }

        sb.AppendLine();

        // Add line range if it's a selection
        if (context.IsPartialContent)
        {
            sb.AppendLine($"**Lines {context.StartLine}-{context.EndLine}**");
        }

        // Add relative path if available and different from filename
        if (!string.IsNullOrEmpty(context.FilePath))
        {
            var relativePath = GetDisplayPath(context.FilePath);
            if (relativePath != context.FileName)
            {
                sb.AppendLine($"_Path: {relativePath}_");
            }
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public string FormatForStorage(IEnumerable<FileContext> contexts)
    {
        var storageItems = contexts.Select(c => new
        {
            c.Id,
            c.FilePath,
            c.FileName,
            c.Language,
            c.StartLine,
            c.EndLine,
            c.EstimatedTokens,
            c.AttachedAt,
            ContentHash = ComputeHash(c.Content),
            ContentLength = c.Content.Length
        });

        return JsonSerializer.Serialize(storageItems, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }

    #region Private Helpers

    /// <summary>
    /// Creates a truncated preview of content.
    /// </summary>
    private static string GetPreview(string content, int maxLines)
    {
        var lines = content.Split('\n');
        if (lines.Length <= maxLines)
            return content;

        var preview = string.Join('\n', lines.Take(maxLines));
        var remaining = lines.Length - maxLines;
        return $"{preview}\n// ... ({remaining} more lines)";
    }

    /// <summary>
    /// Extracts a meaningful display path from a full path.
    /// </summary>
    public static string GetDisplayPath(string fullPath)
    {
        // Extract meaningful path portion
        // e.g., "src/Services/MyService.cs" instead of full path
        var parts = fullPath.Replace('\\', '/').Split('/');

        // Take last 3 parts at most
        var meaningfulParts = parts.TakeLast(3);
        return string.Join('/', meaningfulParts);
    }

    /// <summary>
    /// Computes a SHA256 hash prefix for content.
    /// </summary>
    public static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash)[..16]; // First 16 chars of hash
    }

    #endregion
}
