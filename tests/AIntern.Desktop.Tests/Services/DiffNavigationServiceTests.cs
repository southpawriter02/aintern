using Xunit;
using AIntern.Core.Models;
using AIntern.Desktop.Services;

namespace AIntern.Desktop.Tests.Services;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF NAVIGATION SERVICE TESTS (v0.4.2g)                                  │
// │ Tests for hunk navigation and state management.                          │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Unit tests for <see cref="DiffNavigationService"/>.
/// </summary>
public class DiffNavigationServiceTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Initial State Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_InitializesWithEmptyState()
    {
        // Arrange & Act
        var sut = new DiffNavigationService();

        // Assert
        Assert.Equal(-1, sut.CurrentIndex);
        Assert.Equal(0, sut.TotalHunks);
        Assert.False(sut.HasHunks);
        Assert.Null(sut.CurrentHunk);
        Assert.False(sut.CanMoveNext);
        Assert.False(sut.CanMovePrevious);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SetHunks Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void SetHunks_WithEmptyCollection_SetsIndexToNegativeOne()
    {
        // Arrange
        var sut = new DiffNavigationService();

        // Act
        sut.SetHunks([]);

        // Assert
        Assert.Equal(-1, sut.CurrentIndex);
        Assert.Equal(0, sut.TotalHunks);
        Assert.False(sut.HasHunks);
    }

    [Fact]
    public void SetHunks_WithHunks_SetsIndexToZero()
    {
        // Arrange
        var sut = new DiffNavigationService();
        var hunks = CreateTestHunks(3);

        // Act
        sut.SetHunks(hunks);

        // Assert
        Assert.Equal(0, sut.CurrentIndex);
        Assert.Equal(3, sut.TotalHunks);
        Assert.True(sut.HasHunks);
        Assert.NotNull(sut.CurrentHunk);
    }

    [Fact]
    public void SetHunks_RaisesCurrentHunkChangedEvent()
    {
        // Arrange
        var sut = new DiffNavigationService();
        var hunks = CreateTestHunks(2);
        HunkChangedEventArgs? capturedArgs = null;
        sut.CurrentHunkChanged += (_, args) => capturedArgs = args;

        // Act
        sut.SetHunks(hunks);

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.Equal(-1, capturedArgs.PreviousIndex);
        Assert.Equal(0, capturedArgs.NewIndex);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MoveNext Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void MoveNext_FromFirstHunk_ReturnsTrue()
    {
        // Arrange
        var sut = new DiffNavigationService();
        sut.SetHunks(CreateTestHunks(3));

        // Act
        var result = sut.MoveNext();

        // Assert
        Assert.True(result);
        Assert.Equal(1, sut.CurrentIndex);
    }

    [Fact]
    public void MoveNext_AtLastHunk_ReturnsFalse()
    {
        // Arrange
        var sut = new DiffNavigationService();
        sut.SetHunks(CreateTestHunks(2));
        sut.MoveNext(); // Move to last

        // Act
        var result = sut.MoveNext();

        // Assert
        Assert.False(result);
        Assert.Equal(1, sut.CurrentIndex);
    }

    [Fact]
    public void MoveNext_EmptyHunks_ReturnsFalse()
    {
        // Arrange
        var sut = new DiffNavigationService();

        // Act
        var result = sut.MoveNext();

        // Assert
        Assert.False(result);
        Assert.Equal(-1, sut.CurrentIndex);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MovePrevious Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void MovePrevious_FromSecondHunk_ReturnsTrue()
    {
        // Arrange
        var sut = new DiffNavigationService();
        sut.SetHunks(CreateTestHunks(3));
        sut.MoveNext(); // At index 1

        // Act
        var result = sut.MovePrevious();

        // Assert
        Assert.True(result);
        Assert.Equal(0, sut.CurrentIndex);
    }

    [Fact]
    public void MovePrevious_AtFirstHunk_ReturnsFalse()
    {
        // Arrange
        var sut = new DiffNavigationService();
        sut.SetHunks(CreateTestHunks(3));

        // Act
        var result = sut.MovePrevious();

        // Assert
        Assert.False(result);
        Assert.Equal(0, sut.CurrentIndex);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MoveTo Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public void MoveTo_ValidIndex_ReturnsTrue(int index)
    {
        // Arrange
        var sut = new DiffNavigationService();
        sut.SetHunks(CreateTestHunks(5));

        // Act
        var result = sut.MoveTo(index);

        // Assert
        Assert.True(result);
        Assert.Equal(index, sut.CurrentIndex);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    [InlineData(100)]
    public void MoveTo_InvalidIndex_ReturnsFalse(int index)
    {
        // Arrange
        var sut = new DiffNavigationService();
        sut.SetHunks(CreateTestHunks(5));

        // Act
        var result = sut.MoveTo(index);

        // Assert
        Assert.False(result);
        Assert.Equal(0, sut.CurrentIndex); // Unchanged
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Clear Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Clear_ResetsState()
    {
        // Arrange
        var sut = new DiffNavigationService();
        sut.SetHunks(CreateTestHunks(3));
        sut.MoveNext();

        // Act
        sut.Clear();

        // Assert
        Assert.Equal(-1, sut.CurrentIndex);
        Assert.Equal(0, sut.TotalHunks);
        Assert.False(sut.HasHunks);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CanMove Properties Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CanMoveNext_SingleHunk_ReturnsFalse()
    {
        // Arrange
        var sut = new DiffNavigationService();
        sut.SetHunks(CreateTestHunks(1));

        // Assert
        Assert.False(sut.CanMoveNext);
    }

    [Fact]
    public void CanMovePrevious_AtStart_ReturnsFalse()
    {
        // Arrange
        var sut = new DiffNavigationService();
        sut.SetHunks(CreateTestHunks(3));

        // Assert
        Assert.False(sut.CanMovePrevious);
    }

    [Fact]
    public void CanMoveNext_AtStart_ReturnsTrue()
    {
        // Arrange
        var sut = new DiffNavigationService();
        sut.SetHunks(CreateTestHunks(3));

        // Assert
        Assert.True(sut.CanMoveNext);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════════════════════════════════

    private static List<DiffHunk> CreateTestHunks(int count)
    {
        var hunks = new List<DiffHunk>();
        for (int i = 0; i < count; i++)
        {
            hunks.Add(new DiffHunk
            {
                Id = Guid.NewGuid(),
                Index = i,
                OriginalStartLine = i * 10 + 1,
                ProposedStartLine = i * 10 + 1,
                OriginalLineCount = 5,
                ProposedLineCount = 5,
                Lines = [DiffLine.Unchanged(i * 10 + 1, i * 10 + 1, $"Line {i}")]
            });
        }
        return hunks;
    }
}
