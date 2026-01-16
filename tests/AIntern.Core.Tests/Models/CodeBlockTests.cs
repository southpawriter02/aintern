namespace AIntern.Core.Tests.Models;

using AIntern.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for code block models (v0.4.1a).
/// </summary>
public class CodeBlockTests
{
    #region TextRange Tests

    [Fact]
    public void TextRange_Length_ReturnsCorrectValue()
    {
        var range = new TextRange(10, 25);
        Assert.Equal(15, range.Length);
    }

    [Fact]
    public void TextRange_IsEmpty_TrueWhenStartEqualsEnd()
    {
        var range = new TextRange(5, 5);
        Assert.True(range.IsEmpty);
    }

    [Fact]
    public void TextRange_Contains_ReturnsTrueForPositionInRange()
    {
        var range = new TextRange(10, 20);
        Assert.True(range.Contains(15));
        Assert.False(range.Contains(20)); // End is exclusive
        Assert.False(range.Contains(5));
    }

    [Fact]
    public void TextRange_Overlaps_DetectsOverlappingRanges()
    {
        var range1 = new TextRange(10, 20);
        var range2 = new TextRange(15, 25);
        var range3 = new TextRange(25, 30);

        Assert.True(range1.Overlaps(range2));
        Assert.False(range1.Overlaps(range3));
    }

    [Fact]
    public void TextRange_FromLength_CreatesCorrectRange()
    {
        var range = TextRange.FromLength(10, 5);
        Assert.Equal(10, range.Start);
        Assert.Equal(15, range.End);
    }

    #endregion

    #region LineRange Tests

    [Fact]
    public void LineRange_LineCount_ReturnsInclusiveCount()
    {
        var range = new LineRange(10, 15);
        Assert.Equal(6, range.LineCount);
    }

    [Fact]
    public void LineRange_SingleLine_CreatesSingleLineRange()
    {
        var range = LineRange.SingleLine(42);
        Assert.Equal(42, range.StartLine);
        Assert.Equal(42, range.EndLine);
        Assert.Equal(1, range.LineCount);
    }

    [Fact]
    public void LineRange_IsValid_ChecksPositiveAndOrdered()
    {
        Assert.True(new LineRange(1, 10).IsValid);
        Assert.False(new LineRange(0, 10).IsValid);
        Assert.False(new LineRange(10, 5).IsValid);
    }

    [Fact]
    public void LineRange_ToString_FormatsCorrectly()
    {
        Assert.Equal("Line 5", new LineRange(5, 5).ToString());
        Assert.Equal("Lines 5-10", new LineRange(5, 10).ToString());
    }

    #endregion

    #region CodeBlock Tests

    [Fact]
    public void CodeBlock_IsApplicable_TrueForCompleteFileWithPath()
    {
        var block = new CodeBlock
        {
            BlockType = CodeBlockType.CompleteFile,
            TargetFilePath = "src/Test.cs"
        };
        Assert.True(block.IsApplicable);
    }

    [Fact]
    public void CodeBlock_IsApplicable_FalseForExampleBlock()
    {
        var block = new CodeBlock
        {
            BlockType = CodeBlockType.Example,
            TargetFilePath = "src/Test.cs"
        };
        Assert.False(block.IsApplicable);
    }

    [Fact]
    public void CodeBlock_IsApplicable_FalseWithoutPath()
    {
        var block = new CodeBlock
        {
            BlockType = CodeBlockType.CompleteFile,
            TargetFilePath = null
        };
        Assert.False(block.IsApplicable);
    }

    [Fact]
    public void CodeBlock_LineCount_CountsNewlines()
    {
        var block = new CodeBlock { Content = "line1\nline2\nline3" };
        Assert.Equal(3, block.LineCount);
    }

    [Fact]
    public void CodeBlock_With_CreatesModifiedCopy()
    {
        var original = new CodeBlock
        {
            Content = "original",
            Status = CodeBlockStatus.Pending
        };

        var modified = original.With(content: "modified", status: CodeBlockStatus.Applied);

        Assert.Equal("original", original.Content);
        Assert.Equal(CodeBlockStatus.Pending, original.Status);
        Assert.Equal("modified", modified.Content);
        Assert.Equal(CodeBlockStatus.Applied, modified.Status);
        Assert.Equal(original.Id, modified.Id); // ID preserved
    }

    [Fact]
    public void CodeBlock_DefaultValues_AreCorrect()
    {
        var block = new CodeBlock();
        
        Assert.NotEqual(Guid.Empty, block.Id);
        Assert.Equal(CodeBlockType.Snippet, block.BlockType);
        Assert.Equal(CodeBlockStatus.Pending, block.Status);
        Assert.Equal(1.0f, block.ConfidenceScore);
    }

    #endregion
}
