namespace AIntern.Services;

using System.Text.RegularExpressions;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

/// <summary>
/// Service for estimating token counts using various methods.
/// </summary>
public sealed partial class TokenEstimationService : ITokenEstimationService
{
    // Default context limit - can be adjusted based on model
    private const int DefaultContextLimit = 8000;

    // Character-based estimation ratio (conservative for code)
    private const double CharsPerToken = 3.5;

    // Word-based estimation ratios
    private const double WordsPerToken = 0.75;
    private const double PunctuationWeight = 0.5;

    /// <inheritdoc />
    public int EstimateTokens(string content)
    {
        return EstimateTokens(content, TokenEstimationMethod.WordBased);
    }

    /// <inheritdoc />
    public int EstimateTokens(string content, TokenEstimationMethod method)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        return method switch
        {
            TokenEstimationMethod.CharacterBased => EstimateByCharacters(content),
            TokenEstimationMethod.WordBased => EstimateByWords(content),
            TokenEstimationMethod.BpeApproximate => EstimateByBpe(content),
            _ => EstimateByWords(content)
        };
    }

    /// <inheritdoc />
    public int GetRecommendedContextLimit()
    {
        return DefaultContextLimit;
    }

    /// <inheritdoc />
    public bool WouldExceedLimit(int currentTokens, string newContent)
    {
        var newTokens = EstimateTokens(newContent);
        return (currentTokens + newTokens) > GetRecommendedContextLimit();
    }

    /// <inheritdoc />
    public string TruncateToTokenLimit(string content, int maxTokens)
    {
        if (string.IsNullOrEmpty(content))
            return content ?? string.Empty;

        var currentTokens = EstimateTokens(content);
        if (currentTokens <= maxTokens)
            return content;

        // Estimate characters needed for target tokens
        var targetChars = (int)(maxTokens * CharsPerToken);

        // Binary search for optimal truncation point
        var low = 0;
        var high = Math.Min(content.Length, targetChars + 100);

        while (low < high)
        {
            var mid = (low + high + 1) / 2;
            var truncated = content[..mid];
            var tokens = EstimateTokens(truncated);

            if (tokens <= maxTokens)
                low = mid;
            else
                high = mid - 1;
        }

        // Truncate at word boundary if possible
        var result = content[..low];
        var lastSpace = result.LastIndexOf(' ');
        if (lastSpace > low * 0.8) // Only if we don't lose too much
        {
            result = result[..lastSpace];
        }

        return result + "\n... (truncated)";
    }

    /// <inheritdoc />
    public TokenUsageBreakdown GetUsageBreakdown(IEnumerable<string> contents)
    {
        var items = contents
            .Select((content, index) => new TokenUsageItem(
                $"Item {index + 1}",
                EstimateTokens(content)
            ))
            .ToList();

        return new TokenUsageBreakdown
        {
            TotalTokens = items.Sum(i => i.Tokens),
            RecommendedLimit = GetRecommendedContextLimit(),
            Items = items
        };
    }

    #region Character-Based Estimation

    private static int EstimateByCharacters(string content)
    {
        // Simple: ~3.5 characters per token (conservative for code)
        return (int)Math.Ceiling(content.Length / CharsPerToken);
    }

    #endregion

    #region Word-Based Estimation

    [GeneratedRegex(@"\b\w+\b")]
    private static partial Regex WordPattern();

    [GeneratedRegex(@"[^\w\s]")]
    private static partial Regex PunctuationPattern();

    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex WhitespaceSequencePattern();

    private static int EstimateByWords(string content)
    {
        // Count words (sequences of word characters)
        var wordCount = WordPattern().Matches(content).Count;

        // Count punctuation and special characters
        var punctuationCount = PunctuationPattern().Matches(content).Count;

        // Count newlines (often separate tokens)
        var newlineCount = content.Count(c => c == '\n');

        // Count whitespace sequences > 1 (indentation)
        var whitespaceSequences = WhitespaceSequencePattern().Matches(content).Count;

        // Weighted combination
        var estimate = (int)Math.Ceiling(
            wordCount / WordsPerToken +
            punctuationCount * PunctuationWeight +
            newlineCount * 0.5 +
            whitespaceSequences * 0.3
        );

        return Math.Max(1, estimate);
    }

    #endregion

    #region BPE-Approximate Estimation

    private static readonly string[] CommonTokens =
    [
        "public", "private", "protected", "static", "void", "class", "interface",
        "async", "await", "return", "string", "int", "bool", "var", "const",
        "function", "export", "import", "from", "=>", "->", "==", "!=", "<=", ">=",
        "&&", "||", "++", "--", "+=", "-=", "*=", "/=", "<<", ">>", "::", "..",
        "/**", "*/", "///", "//", "/*"
    ];

    private static int EstimateByBpe(string content)
    {
        var tokens = 0;
        var i = 0;

        while (i < content.Length)
        {
            var remaining = content.AsSpan(i);

            // Try to match common multi-character tokens first
            if (TryMatchCommonToken(remaining, out var matchLength))
            {
                tokens++;
                i += matchLength;
                continue;
            }

            var c = content[i];

            if (char.IsWhiteSpace(c))
            {
                tokens++;
                // Skip consecutive whitespace of same type
                while (i + 1 < content.Length && content[i + 1] == c)
                    i++;
            }
            else if (char.IsLetterOrDigit(c))
            {
                var wordStart = i;
                while (i < content.Length && char.IsLetterOrDigit(content[i]))
                    i++;
                var wordLength = i - wordStart;

                // Estimate tokens for word (longer words = more tokens)
                tokens += Math.Max(1, (wordLength + 3) / 4);
                continue;
            }
            else
            {
                // Punctuation/symbols - usually single tokens
                tokens++;
            }

            i++;
        }

        return Math.Max(1, tokens);
    }

    private static bool TryMatchCommonToken(ReadOnlySpan<char> text, out int length)
    {
        foreach (var token in CommonTokens)
        {
            if (text.StartsWith(token.AsSpan()))
            {
                length = token.Length;
                return true;
            }
        }

        length = 0;
        return false;
    }

    #endregion
}
