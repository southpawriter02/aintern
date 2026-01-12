using System.Text.Json;
using Xunit;
using AIntern.Core.Models;
using AIntern.Services;

namespace AIntern.Services.Tests;

/// <summary>
/// Unit tests for ContextFormatter (v0.3.4b).
/// </summary>
public class ContextFormatterTests
{
    private readonly ContextFormatter _formatter;

    public ContextFormatterTests()
    {
        _formatter = new ContextFormatter();
    }

    #region Helper Methods

    private static FileContext CreateFullFileContext(
        string filePath = "/src/Services/UserService.cs",
        string content = "public class UserService { }",
        string? language = "csharp")
    {
        return new FileContext
        {
            FilePath = filePath,
            Content = content,
            Language = language,
            LineCount = content.Split('\n').Length,
            EstimatedTokens = content.Length / 4
        };
    }

    private static FileContext CreateSelectionContext(
        string filePath = "/src/Services/UserService.cs",
        string content = "public async Task<User> GetByIdAsync(int id) { }",
        int startLine = 10,
        int endLine = 15,
        string? language = "csharp")
    {
        return new FileContext
        {
            FilePath = filePath,
            Content = content,
            Language = language,
            LineCount = endLine - startLine + 1,
            EstimatedTokens = content.Length / 4,
            StartLine = startLine,
            EndLine = endLine
        };
    }

    #endregion

    #region FormatForPrompt Tests

    [Fact]
    public void FormatForPrompt_EmptyCollection_ReturnsEmptyString()
    {
        // Arrange
        var contexts = Array.Empty<FileContext>();

        // Act
        var result = _formatter.FormatForPrompt(contexts);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void FormatForPrompt_SingleContext_ContainsHeaderAndFooter()
    {
        // Arrange
        var context = CreateFullFileContext();
        var contexts = new[] { context };

        // Act
        var result = _formatter.FormatForPrompt(contexts);

        // Assert
        Assert.Contains("I'm providing you with the following code context:", result);
        Assert.Contains("Please consider this context when responding", result);
        Assert.Contains("UserService.cs", result);
    }

    [Fact]
    public void FormatForPrompt_MultipleContexts_ContainsAllFiles()
    {
        // Arrange
        var context1 = CreateFullFileContext("/src/UserService.cs", "class A { }");
        var context2 = CreateFullFileContext("/src/Repository.cs", "class B { }");
        var contexts = new[] { context1, context2 };

        // Act
        var result = _formatter.FormatForPrompt(contexts);

        // Assert
        Assert.Contains("UserService.cs", result);
        Assert.Contains("Repository.cs", result);
        Assert.Contains("class A { }", result);
        Assert.Contains("class B { }", result);
    }

    #endregion

    #region FormatSingleContext Tests

    [Fact]
    public void FormatSingleContext_FullFile_ContainsFileNameAndContent()
    {
        // Arrange
        var context = CreateFullFileContext();

        // Act
        var result = _formatter.FormatSingleContext(context);

        // Assert
        Assert.Contains("### File: `UserService.cs`", result);
        Assert.Contains("public class UserService { }", result);
        Assert.Contains("```csharp", result);
    }

    [Fact]
    public void FormatSingleContext_Selection_ContainsLineRange()
    {
        // Arrange
        var context = CreateSelectionContext(startLine: 10, endLine: 25);

        // Act
        var result = _formatter.FormatSingleContext(context);

        // Assert
        Assert.Contains("**Lines 10-25**", result);
    }

    [Fact]
    public void FormatSingleContext_WithLanguage_IncludesLanguageHint()
    {
        // Arrange
        var context = CreateFullFileContext(language: "typescript");

        // Act
        var result = _formatter.FormatSingleContext(context);

        // Assert
        Assert.Contains("(typescript)", result);
        Assert.Contains("```typescript", result);
    }

    #endregion

    #region FormatForDisplay Tests

    [Fact]
    public void FormatForDisplay_EmptyCollection_ReturnsEmptyString()
    {
        // Arrange
        var contexts = Array.Empty<FileContext>();

        // Act
        var result = _formatter.FormatForDisplay(contexts);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void FormatForDisplay_Collapsed_ShowsTruncatedPreview()
    {
        // Arrange
        var longContent = string.Join("\n", Enumerable.Range(1, 20).Select(i => $"line {i}"));
        var context = CreateFullFileContext(content: longContent);
        var contexts = new[] { context };

        // Act
        var result = _formatter.FormatForDisplay(contexts, expanded: false);

        // Assert
        Assert.Contains("**UserService.cs**", result);
        Assert.Contains("// ... (10 more lines)", result);
    }

    [Fact]
    public void FormatForDisplay_Expanded_ShowsFullContent()
    {
        // Arrange
        var longContent = string.Join("\n", Enumerable.Range(1, 20).Select(i => $"line {i}"));
        var context = CreateFullFileContext(content: longContent);
        var contexts = new[] { context };

        // Act
        var result = _formatter.FormatForDisplay(contexts, expanded: true);

        // Assert
        Assert.Contains("**UserService.cs**", result);
        Assert.Contains("line 20", result);
        Assert.DoesNotContain("more lines", result);
    }

    [Fact]
    public void FormatForDisplay_Selection_ShowsLineRange()
    {
        // Arrange
        var context = CreateSelectionContext(startLine: 15, endLine: 22);
        var contexts = new[] { context };

        // Act
        var result = _formatter.FormatForDisplay(contexts);

        // Assert
        Assert.Contains("_Lines 15-22_", result);
    }

    [Fact]
    public void FormatForDisplay_ShortContent_NoTruncation()
    {
        // Arrange
        var shortContent = "line 1\nline 2\nline 3";
        var context = CreateFullFileContext(content: shortContent);
        var contexts = new[] { context };

        // Act
        var result = _formatter.FormatForDisplay(contexts, expanded: false);

        // Assert
        Assert.DoesNotContain("more lines", result);
        Assert.Contains("line 3", result);
    }

    #endregion

    #region FormatCodeBlock Tests

    [Fact]
    public void FormatCodeBlock_WithLanguage_IncludesLanguageHint()
    {
        // Arrange
        var content = "console.log('hello');";

        // Act
        var result = _formatter.FormatCodeBlock(content, "javascript");

        // Assert
        Assert.StartsWith("```javascript", result);
        Assert.Contains("console.log('hello');", result);
        Assert.EndsWith("```\n", result);
    }

    [Fact]
    public void FormatCodeBlock_WithoutLanguage_NoLanguageHint()
    {
        // Arrange
        var content = "some text";

        // Act
        var result = _formatter.FormatCodeBlock(content, null);

        // Assert
        Assert.StartsWith("```\n", result);
        Assert.Contains("some text", result);
    }

    [Fact]
    public void FormatCodeBlock_EmptyContent_ReturnsEmptyBlock()
    {
        // Arrange & Act
        var result = _formatter.FormatCodeBlock(string.Empty, "csharp");

        // Assert
        Assert.Contains("```csharp", result);
        Assert.Contains("```", result);
    }

    #endregion

    #region FormatContextHeader Tests

    [Fact]
    public void FormatContextHeader_BasicFile_ContainsFileName()
    {
        // Arrange
        var context = CreateFullFileContext();

        // Act
        var result = _formatter.FormatContextHeader(context);

        // Assert
        Assert.Contains("### File: `UserService.cs`", result);
    }

    [Fact]
    public void FormatContextHeader_WithLineRange_ContainsLines()
    {
        // Arrange
        var context = CreateSelectionContext(startLine: 5, endLine: 10);

        // Act
        var result = _formatter.FormatContextHeader(context);

        // Assert
        Assert.Contains("**Lines 5-10**", result);
    }

    [Fact]
    public void FormatContextHeader_WithPath_ContainsShortenedPath()
    {
        // Arrange
        var context = CreateFullFileContext("/Users/dev/project/src/Services/UserService.cs");

        // Act
        var result = _formatter.FormatContextHeader(context);

        // Assert
        Assert.Contains("_Path: src/Services/UserService.cs_", result);
    }

    [Fact]
    public void FormatContextHeader_WithLanguage_ContainsLanguage()
    {
        // Arrange
        var context = CreateFullFileContext(language: "python");

        // Act
        var result = _formatter.FormatContextHeader(context);

        // Assert
        Assert.Contains("(python)", result);
    }

    #endregion

    #region FormatForStorage Tests

    [Fact]
    public void FormatForStorage_EmptyCollection_ReturnsEmptyArray()
    {
        // Arrange
        var contexts = Array.Empty<FileContext>();

        // Act
        var result = _formatter.FormatForStorage(contexts);

        // Assert
        Assert.Equal("[]", result);
    }

    [Fact]
    public void FormatForStorage_SingleContext_ReturnsValidJson()
    {
        // Arrange
        var context = CreateFullFileContext();
        var contexts = new[] { context };

        // Act
        var result = _formatter.FormatForStorage(contexts);

        // Assert
        Assert.StartsWith("[", result);
        Assert.EndsWith("]", result);

        // Should be valid JSON
        var doc = JsonDocument.Parse(result);
        Assert.Single(doc.RootElement.EnumerateArray());
    }

    [Fact]
    public void FormatForStorage_DoesNotContainContent()
    {
        // Arrange
        var context = CreateFullFileContext(content: "SECRET_CONTENT_SHOULD_NOT_APPEAR");
        var contexts = new[] { context };

        // Act
        var result = _formatter.FormatForStorage(contexts);

        // Assert
        Assert.DoesNotContain("SECRET_CONTENT_SHOULD_NOT_APPEAR", result);
    }

    [Fact]
    public void FormatForStorage_ContainsMetadata()
    {
        // Arrange
        var context = CreateFullFileContext();
        var contexts = new[] { context };

        // Act
        var result = _formatter.FormatForStorage(contexts);

        // Assert
        Assert.Contains("FilePath", result);
        Assert.Contains("FileName", result);
        Assert.Contains("Language", result);
        Assert.Contains("ContentHash", result);
        Assert.Contains("ContentLength", result);
    }

    [Fact]
    public void FormatForStorage_MultipleContexts_ReturnsAllItems()
    {
        // Arrange
        var context1 = CreateFullFileContext("/src/A.cs", "class A");
        var context2 = CreateFullFileContext("/src/B.cs", "class B");
        var contexts = new[] { context1, context2 };

        // Act
        var result = _formatter.FormatForStorage(contexts);

        // Assert
        var doc = JsonDocument.Parse(result);
        Assert.Equal(2, doc.RootElement.GetArrayLength());
    }

    #endregion

    #region GetDisplayPath Tests

    [Fact]
    public void GetDisplayPath_LongPath_TakesLastThreeSegments()
    {
        // Arrange
        var fullPath = "/Users/dev/project/src/Services/UserService.cs";

        // Act
        var result = ContextFormatter.GetDisplayPath(fullPath);

        // Assert
        Assert.Equal("src/Services/UserService.cs", result);
    }

    [Fact]
    public void GetDisplayPath_ShortPath_ReturnsFullPath()
    {
        // Arrange
        var fullPath = "src/UserService.cs";

        // Act
        var result = ContextFormatter.GetDisplayPath(fullPath);

        // Assert
        Assert.Equal("src/UserService.cs", result);
    }

    [Fact]
    public void GetDisplayPath_WindowsPath_NormalizesToForwardSlash()
    {
        // Arrange
        var fullPath = @"C:\Users\dev\project\src\Services\UserService.cs";

        // Act
        var result = ContextFormatter.GetDisplayPath(fullPath);

        // Assert
        Assert.Equal("src/Services/UserService.cs", result);
        Assert.DoesNotContain("\\", result);
    }

    #endregion

    #region ComputeHash Tests

    [Fact]
    public void ComputeHash_SameContent_ReturnsSameHash()
    {
        // Arrange
        var content = "Hello, World!";

        // Act
        var hash1 = ContextFormatter.ComputeHash(content);
        var hash2 = ContextFormatter.ComputeHash(content);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_DifferentContent_ReturnsDifferentHash()
    {
        // Arrange
        var content1 = "Hello, World!";
        var content2 = "Goodbye, World!";

        // Act
        var hash1 = ContextFormatter.ComputeHash(content1);
        var hash2 = ContextFormatter.ComputeHash(content2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_Returns16Characters()
    {
        // Arrange
        var content = "Test content";

        // Act
        var hash = ContextFormatter.ComputeHash(content);

        // Assert
        Assert.Equal(16, hash.Length);
    }

    #endregion
}
