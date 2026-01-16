namespace AIntern.Services.Tests;

using Xunit;
using AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ INLINE DIFF SERVICE TESTS (v0.4.2c)                                      │
// │ Unit tests for the InlineDiffService implementation.                     │
// └─────────────────────────────────────────────────────────────────────────┘

public class InlineDiffServiceTests
{
    private readonly InlineDiffService _service;

    public InlineDiffServiceTests()
    {
        _service = new InlineDiffService();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ComputeInlineChanges Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ComputeInlineChanges_IdenticalLines_ReturnsEmpty()
    {
        var changes = _service.ComputeInlineChanges("same text", "same text");

        Assert.Empty(changes);
    }

    [Fact]
    public void ComputeInlineChanges_SingleWordChange_IdentifiesCorrectly()
    {
        var changes = _service.ComputeInlineChanges(
            "Hello world",
            "Hello universe");

        // Should have at least one unchanged segment (common prefix)
        Assert.Contains(changes, c =>
            c.Type == InlineChangeType.Unchanged && c.Text.StartsWith("Hello"));
        // Should have removed and added segments for the differing parts
        Assert.Contains(changes, c => c.Type == InlineChangeType.Removed);
        Assert.Contains(changes, c => c.Type == InlineChangeType.Added);
    }

    [Fact]
    public void ComputeInlineChanges_EmptyOriginal_AllAdded()
    {
        var changes = _service.ComputeInlineChanges("", "new text");

        Assert.Single(changes);
        Assert.Equal(InlineChangeType.Added, changes[0].Type);
        Assert.Equal("new text", changes[0].Text);
    }

    [Fact]
    public void ComputeInlineChanges_EmptyProposed_AllRemoved()
    {
        var changes = _service.ComputeInlineChanges("old text", "");

        Assert.Single(changes);
        Assert.Equal(InlineChangeType.Removed, changes[0].Type);
        Assert.Equal("old text", changes[0].Text);
    }

    [Fact]
    public void ComputeInlineChanges_NullInputs_HandledGracefully()
    {
        var changes = _service.ComputeInlineChanges(null!, null!);
        Assert.Empty(changes);
    }

    [Fact]
    public void ComputeInlineChanges_PreservesColumnPositions()
    {
        var changes = _service.ComputeInlineChanges(
            "abc123xyz",
            "abc456xyz");

        var removedChange = changes.First(c => c.Type == InlineChangeType.Removed);
        var addedChange = changes.First(c => c.Type == InlineChangeType.Added);

        Assert.Equal(3, removedChange.StartColumn);
        Assert.Equal("123", removedChange.Text);
        Assert.Equal(3, addedChange.StartColumn);
        Assert.Equal("456", addedChange.Text);
    }

    [Fact]
    public void ComputeInlineChanges_MultipleChanges_AllDetected()
    {
        var changes = _service.ComputeInlineChanges(
            "var x = 10; var y = 20;",
            "var x = 15; var y = 25;");

        var addedChanges = changes.Where(c => c.Type == InlineChangeType.Added).ToList();
        var removedChanges = changes.Where(c => c.Type == InlineChangeType.Removed).ToList();

        // Should detect numeric changes
        Assert.NotEmpty(removedChanges);
        Assert.NotEmpty(addedChanges);
    }

    [Fact]
    public void ComputeInlineChanges_AddedAtEnd_Detected()
    {
        var changes = _service.ComputeInlineChanges(
            "public void Method()",
            "public void Method(int x)");

        Assert.Contains(changes, c =>
            c.Type == InlineChangeType.Added && c.Text.Contains("int x"));
    }

    [Fact]
    public void ComputeInlineChanges_RemovedFromEnd_Detected()
    {
        var changes = _service.ComputeInlineChanges(
            "public void Method(int x)",
            "public void Method()");

        Assert.Contains(changes, c =>
            c.Type == InlineChangeType.Removed && c.Text.Contains("int x"));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetInlineSegments Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetInlineSegments_OriginalSide_ExcludesAdded()
    {
        var changes = _service.ComputeInlineChanges("old", "new");

        var segments = _service.GetInlineSegments("old", changes, DiffSide.Original);

        Assert.DoesNotContain(segments, s => s.Type == InlineChangeType.Added);
    }

    [Fact]
    public void GetInlineSegments_ProposedSide_ExcludesRemoved()
    {
        var changes = _service.ComputeInlineChanges("old", "new");

        var segments = _service.GetInlineSegments("new", changes, DiffSide.Proposed);

        Assert.DoesNotContain(segments, s => s.Type == InlineChangeType.Removed);
    }

    [Fact]
    public void GetInlineSegments_NoChanges_ReturnsFullContent()
    {
        var segments = _service.GetInlineSegments(
            "unchanged content",
            [],
            DiffSide.Original);

        Assert.Single(segments);
        Assert.Equal("unchanged content", segments[0].Text);
        Assert.False(segments[0].IsChanged);
    }

    [Fact]
    public void GetInlineSegments_NullChanges_ReturnsFullContent()
    {
        var segments = _service.GetInlineSegments("content", null!, DiffSide.Original);

        Assert.Single(segments);
        Assert.Equal("content", segments[0].Text);
    }

    [Fact]
    public void GetInlineSegments_MarksChangedSegmentsCorrectly()
    {
        var changes = _service.ComputeInlineChanges("Hello world", "Hello universe");

        var segments = _service.GetInlineSegments("Hello universe", changes, DiffSide.Proposed);

        // Should have at least one unchanged and one changed segment
        Assert.Contains(segments, s => !s.IsChanged);
        Assert.Contains(segments, s => s.IsChanged && s.Type == InlineChangeType.Added);
    }
}
