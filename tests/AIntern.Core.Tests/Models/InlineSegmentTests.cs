namespace AIntern.Core.Tests.Models;

using Xunit;
using AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ INLINE SEGMENT TESTS (v0.4.2c)                                           │
// │ Unit tests for the InlineSegment model.                                  │
// └─────────────────────────────────────────────────────────────────────────┘

public class InlineSegmentTests
{
    [Fact]
    public void Unchanged_CreatesCorrectSegment()
    {
        var segment = InlineSegment.Unchanged("test text");

        Assert.Equal("test text", segment.Text);
        Assert.False(segment.IsChanged);
        Assert.Equal(InlineChangeType.Unchanged, segment.Type);
    }

    [Fact]
    public void Added_CreatesCorrectSegment()
    {
        var segment = InlineSegment.Added("new text");

        Assert.Equal("new text", segment.Text);
        Assert.True(segment.IsChanged);
        Assert.Equal(InlineChangeType.Added, segment.Type);
    }

    [Fact]
    public void Removed_CreatesCorrectSegment()
    {
        var segment = InlineSegment.Removed("old text");

        Assert.Equal("old text", segment.Text);
        Assert.True(segment.IsChanged);
        Assert.Equal(InlineChangeType.Removed, segment.Type);
    }

    [Fact]
    public void Length_ReturnsCorrectValue()
    {
        var segment = InlineSegment.Unchanged("12345");

        Assert.Equal(5, segment.Length);
    }

    [Fact]
    public void IsEmpty_TrueForEmptyText()
    {
        var segment = InlineSegment.Unchanged("");

        Assert.True(segment.IsEmpty);
    }

    [Fact]
    public void IsEmpty_FalseForNonEmptyText()
    {
        var segment = InlineSegment.Unchanged("x");

        Assert.False(segment.IsEmpty);
    }
}
