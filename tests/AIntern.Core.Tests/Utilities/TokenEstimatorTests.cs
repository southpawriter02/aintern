using Xunit;
using AIntern.Core.Utilities;

namespace AIntern.Core.Tests.Utilities;

/// <summary>
/// Unit tests for TokenEstimator (v0.3.1a).
/// </summary>
public class TokenEstimatorTests
{
    #region Estimate(string) Tests

    [Fact]
    public void Estimate_EmptyContent_ReturnsZero()
    {
        Assert.Equal(0, TokenEstimator.Estimate(""));
        Assert.Equal(0, TokenEstimator.Estimate(null!));
    }

    [Fact]
    public void Estimate_ShortContent_ReturnsReasonableCount()
    {
        // "Hello" = 5 chars, at 3.5 chars/token = ~1.4 tokens, ceiling = 2
        var result = TokenEstimator.Estimate("Hello");
        Assert.Equal(2, result);
    }

    [Fact]
    public void Estimate_LongerContent_ScalesCorrectly()
    {
        // 35 chars at 3.5 chars/token = 10 tokens
        var content = new string('a', 35);
        var result = TokenEstimator.Estimate(content);
        Assert.Equal(10, result);
    }

    [Fact]
    public void Estimate_AlwaysRoundsUp()
    {
        // 1 char at 3.5 chars/token = 0.28 tokens, ceiling = 1
        Assert.Equal(1, TokenEstimator.Estimate("a"));

        // 4 chars at 3.5 chars/token = 1.14 tokens, ceiling = 2
        Assert.Equal(2, TokenEstimator.Estimate("test"));
    }

    #endregion

    #region Estimate(string, string) with Language Tests

    [Theory]
    [InlineData("csharp", 1.2)]
    [InlineData("c#", 1.2)]
    [InlineData("java", 1.2)]
    [InlineData("cpp", 1.25)]
    [InlineData("c++", 1.25)]
    [InlineData("typescript", 1.15)]
    [InlineData("javascript", 1.1)]
    [InlineData("rust", 1.2)]
    [InlineData("go", 1.15)]
    [InlineData("python", 1.0)]
    [InlineData("ruby", 1.0)]
    [InlineData("yaml", 0.95)]
    [InlineData("json", 1.3)]
    [InlineData("xml", 1.35)]
    [InlineData("html", 1.3)]
    [InlineData("markdown", 0.9)]
    [InlineData("md", 0.9)]
    [InlineData("text", 0.85)]
    [InlineData("txt", 0.85)]
    public void GetLanguageMultiplier_ReturnsCorrectMultiplier(string language, double expected)
    {
        var result = TokenEstimator.GetLanguageMultiplier(language);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetLanguageMultiplier_UnknownLanguage_ReturnsDefault()
    {
        Assert.Equal(1.0, TokenEstimator.GetLanguageMultiplier("unknown"));
        Assert.Equal(1.0, TokenEstimator.GetLanguageMultiplier(null));
        Assert.Equal(1.0, TokenEstimator.GetLanguageMultiplier(""));
    }

    [Fact]
    public void GetLanguageMultiplier_CaseInsensitive()
    {
        Assert.Equal(TokenEstimator.GetLanguageMultiplier("csharp"),
                     TokenEstimator.GetLanguageMultiplier("CSHARP"));
        Assert.Equal(TokenEstimator.GetLanguageMultiplier("python"),
                     TokenEstimator.GetLanguageMultiplier("Python"));
    }

    [Fact]
    public void Estimate_WithLanguage_AppliesMultiplier()
    {
        // 35 chars base = 10 tokens
        var content = new string('a', 35);

        // C# has 1.2 multiplier, so 10 * 1.2 = 12
        var csharpResult = TokenEstimator.Estimate(content, "csharp");
        Assert.Equal(12, csharpResult);

        // Python has 1.0 multiplier, so 10 * 1.0 = 10
        var pythonResult = TokenEstimator.Estimate(content, "python");
        Assert.Equal(10, pythonResult);

        // YAML has 0.95 multiplier, so 10 * 0.95 = 9.5, ceiling = 10
        var yamlResult = TokenEstimator.Estimate(content, "yaml");
        Assert.Equal(10, yamlResult);
    }

    [Fact]
    public void Estimate_WithNullLanguage_UsesDefaultMultiplier()
    {
        var content = new string('a', 35);
        var withNull = TokenEstimator.Estimate(content, null);
        var withoutLanguage = TokenEstimator.Estimate(content);
        Assert.Equal(withoutLanguage, withNull);
    }

    #endregion

    #region MaxContentLength Tests

    [Fact]
    public void MaxContentLength_ReturnsCorrectValue()
    {
        // 100 tokens at 3.5 chars/token = 350 chars
        var result = TokenEstimator.MaxContentLength(100);
        Assert.Equal(350, result);
    }

    [Fact]
    public void MaxContentLength_WithLanguage_AdjustsForMultiplier()
    {
        // 100 tokens at 3.5 chars/token, adjusted for C# (1.2 multiplier)
        // 100 * 3.5 / 1.2 = 291.67, truncated to 291
        var csharpResult = TokenEstimator.MaxContentLength(100, "csharp");
        Assert.Equal(291, csharpResult);

        // Python (1.0 multiplier) = 350
        var pythonResult = TokenEstimator.MaxContentLength(100, "python");
        Assert.Equal(350, pythonResult);
    }

    [Fact]
    public void MaxContentLength_ZeroBudget_ReturnsZero()
    {
        Assert.Equal(0, TokenEstimator.MaxContentLength(0));
    }

    #endregion

    #region ExceedsBudget Tests

    [Fact]
    public void ExceedsBudget_UnderBudget_ReturnsFalse()
    {
        // 35 chars = 10 tokens
        var content = new string('a', 35);
        Assert.False(TokenEstimator.ExceedsBudget(content, 10));
        Assert.False(TokenEstimator.ExceedsBudget(content, 15));
        Assert.False(TokenEstimator.ExceedsBudget(content, 100));
    }

    [Fact]
    public void ExceedsBudget_OverBudget_ReturnsTrue()
    {
        // 35 chars = 10 tokens
        var content = new string('a', 35);
        Assert.True(TokenEstimator.ExceedsBudget(content, 9));
        Assert.True(TokenEstimator.ExceedsBudget(content, 5));
        Assert.True(TokenEstimator.ExceedsBudget(content, 1));
    }

    [Fact]
    public void ExceedsBudget_ExactBudget_ReturnsFalse()
    {
        // 35 chars = exactly 10 tokens
        var content = new string('a', 35);
        Assert.False(TokenEstimator.ExceedsBudget(content, 10));
    }

    [Fact]
    public void ExceedsBudget_WithLanguage_ConsidersMultiplier()
    {
        // 35 chars base = 10 tokens, with C# multiplier (1.2) = 12 tokens
        var content = new string('a', 35);

        Assert.False(TokenEstimator.ExceedsBudget(content, 12, "csharp"));
        Assert.True(TokenEstimator.ExceedsBudget(content, 11, "csharp"));
    }

    [Fact]
    public void ExceedsBudget_EmptyContent_NeverExceedsBudget()
    {
        Assert.False(TokenEstimator.ExceedsBudget("", 1));
        Assert.False(TokenEstimator.ExceedsBudget("", 0));
    }

    #endregion
}
