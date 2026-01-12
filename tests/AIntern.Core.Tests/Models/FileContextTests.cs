using Xunit;
using AIntern.Core.Models;

namespace AIntern.Core.Tests.Models;

/// <summary>
/// Unit tests for FileContext model (v0.3.1a).
/// </summary>
public class FileContextTests
{
    [Fact]
    public void FromFile_CreatesCorrectFileContext()
    {
        var content = "public class Test { }";
        var result = FileContext.FromFile("/test/MyClass.cs", content);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("/test/MyClass.cs", result.FilePath);
        Assert.Equal("MyClass.cs", result.FileName);
        Assert.Equal(content, result.Content);
        Assert.Equal("csharp", result.Language);
        Assert.Equal(1, result.LineCount);  // Single line
        Assert.True(result.EstimatedTokens > 0);
        Assert.NotEmpty(result.ContentHash);
        Assert.False(result.IsPartialContent);
        Assert.Null(result.StartLine);
        Assert.Null(result.EndLine);
    }

    [Fact]
    public void FromFile_CalculatesLineCountCorrectly()
    {
        var content = "line1\nline2\nline3";
        var result = FileContext.FromFile("/test/file.txt", content);

        Assert.Equal(3, result.LineCount);
    }

    [Fact]
    public void FromFile_HandlesEmptyContent()
    {
        var result = FileContext.FromFile("/test/empty.txt", "");

        Assert.Equal(0, result.LineCount);
        Assert.Equal(0, result.EstimatedTokens);
    }

    [Fact]
    public void FromSelection_CreatesCorrectFileContext()
    {
        var content = "selected code";
        var result = FileContext.FromSelection("/test/MyClass.cs", content, 10, 15);

        Assert.Equal("/test/MyClass.cs", result.FilePath);
        Assert.Equal("MyClass.cs", result.FileName);
        Assert.Equal(content, result.Content);
        Assert.Equal(10, result.StartLine);
        Assert.Equal(15, result.EndLine);
        Assert.True(result.IsPartialContent);
        Assert.Equal(6, result.LineCount);  // 15 - 10 + 1
    }

    [Fact]
    public void IsPartialContent_TrueWhenStartLineSet()
    {
        var context = new FileContext
        {
            FilePath = "/test/file.cs",
            Content = "code",
            StartLine = 5
        };

        Assert.True(context.IsPartialContent);
    }

    [Fact]
    public void IsPartialContent_TrueWhenEndLineSet()
    {
        var context = new FileContext
        {
            FilePath = "/test/file.cs",
            Content = "code",
            EndLine = 10
        };

        Assert.True(context.IsPartialContent);
    }

    [Fact]
    public void IsPartialContent_FalseWhenNoLinesSet()
    {
        var context = new FileContext
        {
            FilePath = "/test/file.cs",
            Content = "code"
        };

        Assert.False(context.IsPartialContent);
    }

    [Fact]
    public void DisplayLabel_ShowsFileName_WhenFullFile()
    {
        var context = FileContext.FromFile("/test/MyClass.cs", "content");

        Assert.Equal("MyClass.cs", context.DisplayLabel);
    }

    [Fact]
    public void DisplayLabel_ShowsLineRange_WhenPartialContent()
    {
        var context = FileContext.FromSelection("/test/MyClass.cs", "content", 10, 20);

        Assert.Equal("MyClass.cs (lines 10-20)", context.DisplayLabel);
    }

    [Fact]
    public void FormatForLlmContext_IncludesHeader()
    {
        var content = "public class Test { }";
        var context = FileContext.FromFile("/test/MyClass.cs", content);

        var formatted = context.FormatForLlmContext();

        Assert.Contains("// File: MyClass.cs", formatted);
        Assert.Contains("[csharp]", formatted);
        Assert.Contains(content, formatted);
    }

    [Fact]
    public void FormatForLlmContext_IncludesLineRange_WhenPartial()
    {
        var context = FileContext.FromSelection("/test/MyClass.cs", "code", 5, 10);

        var formatted = context.FormatForLlmContext();

        Assert.Contains("(lines 5-10)", formatted);
    }

    [Fact]
    public void ContentSizeBytes_CalculatesCorrectly()
    {
        var content = "Hello, World!";  // 13 ASCII chars = 13 bytes
        var context = FileContext.FromFile("/test/file.txt", content);

        Assert.Equal(13, context.ContentSizeBytes);
    }

    [Fact]
    public void ContentSizeBytes_HandlesUnicode()
    {
        var content = "Hello, 世界!";  // "世界" = 6 bytes in UTF-8
        var context = FileContext.FromFile("/test/file.txt", content);

        Assert.True(context.ContentSizeBytes > content.Length);
    }

    [Fact]
    public void ContentHash_IsDeterministic()
    {
        var content = "same content";
        var context1 = FileContext.FromFile("/test/file1.txt", content);
        var context2 = FileContext.FromFile("/test/file2.txt", content);

        Assert.Equal(context1.ContentHash, context2.ContentHash);
    }

    [Fact]
    public void ContentHash_DiffersForDifferentContent()
    {
        var context1 = FileContext.FromFile("/test/file.txt", "content 1");
        var context2 = FileContext.FromFile("/test/file.txt", "content 2");

        Assert.NotEqual(context1.ContentHash, context2.ContentHash);
    }

    [Fact]
    public void FileName_ExtractedFromPath()
    {
        var context = new FileContext
        {
            FilePath = "/home/user/project/src/deep/nested/MyFile.cs",
            Content = "code"
        };

        Assert.Equal("MyFile.cs", context.FileName);
    }
}
