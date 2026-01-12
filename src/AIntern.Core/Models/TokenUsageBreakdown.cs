namespace AIntern.Core.Models;

/// <summary>
/// Breakdown of token usage across multiple content items.
/// </summary>
public sealed class TokenUsageBreakdown
{
    /// <summary>
    /// Total tokens across all items.
    /// </summary>
    public int TotalTokens { get; init; }

    /// <summary>
    /// Recommended maximum token limit.
    /// </summary>
    public int RecommendedLimit { get; init; }

    /// <summary>
    /// Usage as a percentage of the limit (0-100+).
    /// </summary>
    public double UsagePercentage => RecommendedLimit > 0
        ? (double)TotalTokens / RecommendedLimit * 100
        : 0;

    /// <summary>
    /// Whether total tokens exceed the recommended limit.
    /// </summary>
    public bool IsOverLimit => TotalTokens > RecommendedLimit;

    /// <summary>
    /// Number of tokens remaining before hitting limit.
    /// </summary>
    public int RemainingTokens => Math.Max(0, RecommendedLimit - TotalTokens);

    /// <summary>
    /// Whether usage is above the warning threshold (80%).
    /// </summary>
    public bool IsWarning => UsagePercentage >= 80 && !IsOverLimit;

    /// <summary>
    /// Per-item token breakdown.
    /// </summary>
    public IReadOnlyList<TokenUsageItem> Items { get; init; } = [];
}

/// <summary>
/// Token usage for a single content item.
/// </summary>
public sealed record TokenUsageItem(string Label, int Tokens);
