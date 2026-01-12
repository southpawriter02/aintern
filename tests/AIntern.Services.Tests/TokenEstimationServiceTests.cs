using Xunit;
using AIntern.Core.Models;
using AIntern.Services;

namespace AIntern.Services.Tests;

/// <summary>
/// Unit tests for TokenEstimationService (v0.3.4a).
/// </summary>
public class TokenEstimationServiceTests
{
    private readonly TokenEstimationService _service;

    public TokenEstimationServiceTests()
    {
        _service = new TokenEstimationService();
    }

    #region EstimateTokens (Default) Tests

    [Fact]
    public void EstimateTokens_EmptyContent_ReturnsZero()
    {
        // Arrange & Act
        var result = _service.EstimateTokens(string.Empty);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void EstimateTokens_NullContent_ReturnsZero()
    {
        // Arrange & Act
        var result = _service.EstimateTokens(null!);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void EstimateTokens_ShortContent_ReturnsPositive()
    {
        // Arrange
        var content = "Hello world";

        // Act
        var result = _service.EstimateTokens(content);

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void EstimateTokens_LongContent_ScalesWithLength()
    {
        // Arrange
        var shortContent = "Hello world";
        var longContent = string.Join(" ", Enumerable.Repeat("Hello world", 100));

        // Act
        var shortResult = _service.EstimateTokens(shortContent);
        var longResult = _service.EstimateTokens(longContent);

        // Assert
        Assert.True(longResult > shortResult * 50); // Should scale roughly with length
    }

    #endregion

    #region CharacterBased Tests

    [Fact]
    public void EstimateTokens_CharacterBased_SimpleText()
    {
        // Arrange
        var content = "Hello world"; // 11 chars

        // Act
        var result = _service.EstimateTokens(content, TokenEstimationMethod.CharacterBased);

        // Assert - ~11/3.5 = ~4 tokens
        Assert.True(result >= 3 && result <= 5);
    }

    [Fact]
    public void EstimateTokens_CharacterBased_Code()
    {
        // Arrange
        var content = "public void Main()"; // 18 chars

        // Act
        var result = _service.EstimateTokens(content, TokenEstimationMethod.CharacterBased);

        // Assert - ~18/3.5 = ~6 tokens
        Assert.True(result >= 5 && result <= 7);
    }

    [Fact]
    public void EstimateTokens_CharacterBased_SpecialChars()
    {
        // Arrange
        var content = "!@#$%^&*()"; // 10 chars

        // Act
        var result = _service.EstimateTokens(content, TokenEstimationMethod.CharacterBased);

        // Assert - ~10/3.5 = ~3 tokens
        Assert.True(result >= 2 && result <= 4);
    }

    #endregion

    #region WordBased Tests

    [Fact]
    public void EstimateTokens_WordBased_CountsWords()
    {
        // Arrange
        var content = "one two three four five";

        // Act
        var result = _service.EstimateTokens(content, TokenEstimationMethod.WordBased);

        // Assert - 5 words / 0.75 = ~7 + some for spaces
        Assert.True(result >= 5 && result <= 10);
    }

    [Fact]
    public void EstimateTokens_WordBased_CountsPunctuation()
    {
        // Arrange
        var content = "Hello, world! How are you?";

        // Act
        var result = _service.EstimateTokens(content, TokenEstimationMethod.WordBased);

        // Assert - should account for punctuation
        Assert.True(result > 0);
    }

    [Fact]
    public void EstimateTokens_WordBased_CountsNewlines()
    {
        // Arrange
        var content = "line1\nline2\nline3";

        // Act
        var result = _service.EstimateTokens(content, TokenEstimationMethod.WordBased);

        // Assert - includes newline tokens
        Assert.True(result > 3);
    }

    [Fact]
    public void EstimateTokens_WordBased_CountsWhitespace()
    {
        // Arrange
        var content = "    indented\n        more indented";

        // Act
        var result = _service.EstimateTokens(content, TokenEstimationMethod.WordBased);

        // Assert - includes whitespace sequences
        Assert.True(result > 2);
    }

    #endregion

    #region BpeApproximate Tests

    [Fact]
    public void EstimateTokens_BpeApproximate_MatchesCommonTokens()
    {
        // Arrange - contains common programming tokens
        var content = "public static void";

        // Act
        var result = _service.EstimateTokens(content, TokenEstimationMethod.BpeApproximate);

        // Assert - should be around 3-5 tokens
        Assert.True(result >= 3 && result <= 6);
    }

    [Fact]
    public void EstimateTokens_BpeApproximate_MatchesOperators()
    {
        // Arrange - common operators
        var content = "a => b == c && d || e";

        // Act
        var result = _service.EstimateTokens(content, TokenEstimationMethod.BpeApproximate);

        // Assert - operators should be counted
        Assert.True(result > 5);
    }

    [Fact]
    public void EstimateTokens_BpeApproximate_HandlesLongWords()
    {
        // Arrange
        var content = "supercalifragilisticexpialidocious"; // 34 chars

        // Act
        var result = _service.EstimateTokens(content, TokenEstimationMethod.BpeApproximate);

        // Assert - (34 + 3) / 4 = ~9 tokens
        Assert.True(result >= 7 && result <= 12);
    }

    [Fact]
    public void EstimateTokens_BpeApproximate_MixedContent()
    {
        // Arrange
        var content = "public async Task<string> GetDataAsync() { return await _service.FetchAsync(); }";

        // Act
        var result = _service.EstimateTokens(content, TokenEstimationMethod.BpeApproximate);

        // Assert - should be reasonable for code
        Assert.True(result >= 15 && result <= 35);
    }

    #endregion

    #region GetRecommendedContextLimit Tests

    [Fact]
    public void GetRecommendedContextLimit_ReturnsExpectedValue()
    {
        // Arrange & Act
        var result = _service.GetRecommendedContextLimit();

        // Assert
        Assert.Equal(8000, result);
    }

    #endregion

    #region WouldExceedLimit Tests

    [Fact]
    public void WouldExceedLimit_UnderLimit_ReturnsFalse()
    {
        // Arrange
        var currentTokens = 100;
        var newContent = "small addition";

        // Act
        var result = _service.WouldExceedLimit(currentTokens, newContent);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void WouldExceedLimit_AtLimit_ReturnsFalse()
    {
        // Arrange - at exactly the limit (8000)
        var currentTokens = 7990;
        var newContent = "hi"; // ~1-2 tokens

        // Act
        var result = _service.WouldExceedLimit(currentTokens, newContent);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void WouldExceedLimit_OverLimit_ReturnsTrue()
    {
        // Arrange
        var currentTokens = 7990;
        var newContent = string.Join(" ", Enumerable.Repeat("word", 100)); // Many tokens

        // Act
        var result = _service.WouldExceedLimit(currentTokens, newContent);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region TruncateToTokenLimit Tests

    [Fact]
    public void TruncateToTokenLimit_UnderLimit_ReturnsOriginal()
    {
        // Arrange
        var content = "short content";
        var maxTokens = 100;

        // Act
        var result = _service.TruncateToTokenLimit(content, maxTokens);

        // Assert
        Assert.Equal(content, result);
    }

    [Fact]
    public void TruncateToTokenLimit_OverLimit_Truncates()
    {
        // Arrange
        var content = string.Join(" ", Enumerable.Repeat("word", 500));
        var maxTokens = 10;

        // Act
        var result = _service.TruncateToTokenLimit(content, maxTokens);

        // Assert
        Assert.True(result.Length < content.Length);
        Assert.EndsWith("... (truncated)", result);
    }

    [Fact]
    public void TruncateToTokenLimit_PreservesWordBoundary()
    {
        // Arrange
        var content = string.Join(" ", Enumerable.Repeat("longword", 100));
        var maxTokens = 10;

        // Act
        var result = _service.TruncateToTokenLimit(content, maxTokens);

        // Assert - should not cut in middle of word (ends with complete word or truncation indicator)
        var mainPart = result.Replace("\n... (truncated)", "").TrimEnd();
        var lastWord = mainPart.Split(' ').Last();
        // Word should either be empty or "longword" (complete) - not a partial like "longwor" or "long"
        Assert.True(lastWord == "" || lastWord == "longword",
            $"Expected complete word but got partial: '{lastWord}'");
    }

    [Fact]
    public void TruncateToTokenLimit_EmptyContent_ReturnsEmpty()
    {
        // Arrange & Act
        var result = _service.TruncateToTokenLimit(string.Empty, 100);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void TruncateToTokenLimit_NullContent_ReturnsEmpty()
    {
        // Arrange & Act
        var result = _service.TruncateToTokenLimit(null!, 100);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region GetUsageBreakdown Tests

    [Fact]
    public void GetUsageBreakdown_EmptyCollection_ReturnsZeroTotal()
    {
        // Arrange
        var contents = Array.Empty<string>();

        // Act
        var result = _service.GetUsageBreakdown(contents);

        // Assert
        Assert.Equal(0, result.TotalTokens);
        Assert.Equal(8000, result.RecommendedLimit);
        Assert.Empty(result.Items);
    }

    [Fact]
    public void GetUsageBreakdown_SingleItem_CorrectTotal()
    {
        // Arrange
        var contents = new[] { "Hello world" };

        // Act
        var result = _service.GetUsageBreakdown(contents);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(result.Items[0].Tokens, result.TotalTokens);
        Assert.Equal("Item 1", result.Items[0].Label);
    }

    [Fact]
    public void GetUsageBreakdown_MultipleItems_CorrectTotals()
    {
        // Arrange
        var contents = new[] { "Hello world", "Goodbye world", "More content here" };

        // Act
        var result = _service.GetUsageBreakdown(contents);

        // Assert
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(result.Items.Sum(i => i.Tokens), result.TotalTokens);
        Assert.Equal("Item 1", result.Items[0].Label);
        Assert.Equal("Item 2", result.Items[1].Label);
        Assert.Equal("Item 3", result.Items[2].Label);
    }

    [Fact]
    public void GetUsageBreakdown_CalculatesPercentage()
    {
        // Arrange - create content that's ~10% of limit (800 tokens)
        var contents = new[] { string.Join(" ", Enumerable.Repeat("word", 500)) };

        // Act
        var result = _service.GetUsageBreakdown(contents);

        // Assert
        Assert.True(result.UsagePercentage > 0);
        Assert.True(result.UsagePercentage < 100);
    }

    [Fact]
    public void GetUsageBreakdown_OverLimit_SetsIsOverLimit()
    {
        // Arrange - create content that exceeds limit
        var largeContent = string.Join(" ", Enumerable.Repeat("word", 10000));
        var contents = new[] { largeContent };

        // Act
        var result = _service.GetUsageBreakdown(contents);

        // Assert
        Assert.True(result.IsOverLimit);
        Assert.Equal(0, result.RemainingTokens);
    }

    [Fact]
    public void GetUsageBreakdown_UnderLimit_SetsRemainingTokens()
    {
        // Arrange
        var contents = new[] { "small content" };

        // Act
        var result = _service.GetUsageBreakdown(contents);

        // Assert
        Assert.False(result.IsOverLimit);
        Assert.True(result.RemainingTokens > 0);
        Assert.Equal(result.RecommendedLimit - result.TotalTokens, result.RemainingTokens);
    }

    #endregion

    #region ContextLimitsConfig Tests

    [Fact]
    public void ContextLimitsConfig_HasCorrectDefaults()
    {
        // Arrange & Act
        var config = new ContextLimitsConfig();

        // Assert
        Assert.Equal(10, config.MaxFilesAttached);
        Assert.Equal(4000, config.MaxTokensPerFile);
        Assert.Equal(8000, config.MaxTotalContextTokens);
        Assert.Equal(500_000, config.MaxFileSizeBytes);
        Assert.Equal(0.8, config.WarningThreshold);
        Assert.Equal(20, config.MaxPreviewLines);
        Assert.Equal(500, config.MaxPreviewCharacters);
    }

    [Fact]
    public void ContextLimitsConfig_CanSetCustomValues()
    {
        // Arrange & Act
        var config = new ContextLimitsConfig
        {
            MaxFilesAttached = 20,
            MaxTokensPerFile = 8000,
            MaxTotalContextTokens = 16000,
            MaxFileSizeBytes = 1_000_000,
            WarningThreshold = 0.9,
            MaxPreviewLines = 50,
            MaxPreviewCharacters = 1000
        };

        // Assert
        Assert.Equal(20, config.MaxFilesAttached);
        Assert.Equal(8000, config.MaxTokensPerFile);
        Assert.Equal(16000, config.MaxTotalContextTokens);
        Assert.Equal(1_000_000, config.MaxFileSizeBytes);
        Assert.Equal(0.9, config.WarningThreshold);
        Assert.Equal(50, config.MaxPreviewLines);
        Assert.Equal(1000, config.MaxPreviewCharacters);
    }

    #endregion

    #region TokenUsageBreakdown Tests

    [Fact]
    public void TokenUsageBreakdown_IsWarning_TrueNear80Percent()
    {
        // Arrange & Act
        var breakdown = new TokenUsageBreakdown
        {
            TotalTokens = 6500, // 81.25% of 8000
            RecommendedLimit = 8000
        };

        // Assert
        Assert.True(breakdown.IsWarning);
        Assert.False(breakdown.IsOverLimit);
    }

    [Fact]
    public void TokenUsageBreakdown_IsWarning_FalseWhenOverLimit()
    {
        // Arrange & Act
        var breakdown = new TokenUsageBreakdown
        {
            TotalTokens = 9000, // Over limit
            RecommendedLimit = 8000
        };

        // Assert
        Assert.False(breakdown.IsWarning); // Not warning, it's over limit
        Assert.True(breakdown.IsOverLimit);
    }

    #endregion
}
