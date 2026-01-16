using Xunit;
using AIntern.Core.Models;

namespace AIntern.Core.Tests.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ BATCH APPLY RESULT TESTS (v0.4.4a)                                       │
// │ Unit tests for BatchApplyResult model.                                   │
// └─────────────────────────────────────────────────────────────────────────┘

public sealed class BatchApplyResultTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Computed Property Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void TotalCount_SumsAllCounts()
    {
        var result = new BatchApplyResult
        {
            SuccessCount = 5,
            FailedCount = 2,
            SkippedCount = 1
        };

        Assert.Equal(8, result.TotalCount);
    }

    [Fact]
    public void SuccessRate_CalculatesCorrectly()
    {
        var result = new BatchApplyResult
        {
            SuccessCount = 3,
            FailedCount = 1,
            SkippedCount = 0
        };

        Assert.Equal(75, result.SuccessRate);
    }

    [Fact]
    public void SuccessRate_ReturnsZeroWhenNoOperations()
    {
        var result = new BatchApplyResult();
        Assert.Equal(0, result.SuccessRate);
    }

    [Fact]
    public void Duration_CalculatesCorrectly()
    {
        var start = DateTime.UtcNow;
        var end = start.AddSeconds(5);

        var result = new BatchApplyResult
        {
            StartedAt = start,
            CompletedAt = end
        };

        Assert.Equal(TimeSpan.FromSeconds(5), result.Duration);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Factory Method Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Success_CreatesCorrectResult()
    {
        var results = new List<ApplyResult>
        {
            ApplyResult.Created("file1.cs", "file1.cs"),
            ApplyResult.Created("file2.cs", "file2.cs")
        };
        var startedAt = DateTime.UtcNow.AddSeconds(-1);

        var batchResult = BatchApplyResult.Success(results, startedAt);

        Assert.True(batchResult.AllSucceeded);
        Assert.Equal(2, batchResult.SuccessCount);
        Assert.Equal(0, batchResult.FailedCount);
        Assert.Equal(startedAt, batchResult.StartedAt);
    }

    [Fact]
    public void PartialSuccess_CreatesCorrectResult()
    {
        var results = new List<ApplyResult>
        {
            ApplyResult.Created("file1.cs", "file1.cs"),
            ApplyResult.Failed("file2.cs", ApplyResultType.PermissionDenied, "Access denied")
        };
        var startedAt = DateTime.UtcNow.AddSeconds(-1);

        var batchResult = BatchApplyResult.PartialSuccess(results, startedAt);

        Assert.False(batchResult.AllSucceeded);
        Assert.Equal(1, batchResult.SuccessCount);
        Assert.Equal(1, batchResult.FailedCount);
    }

    [Fact]
    public void Cancelled_SetsFlag()
    {
        var results = new List<ApplyResult>
        {
            ApplyResult.Created("file1.cs", "file1.cs")
        };

        var batchResult = BatchApplyResult.Cancelled(results, DateTime.UtcNow);

        Assert.True(batchResult.WasCancelled);
        Assert.False(batchResult.AllSucceeded);
    }

    [Fact]
    public void RolledBack_SetsErrorMessage()
    {
        var batchResult = BatchApplyResult.RolledBack("Critical error", DateTime.UtcNow);

        Assert.True(batchResult.WasRolledBack);
        Assert.Equal("Critical error", batchResult.BatchErrorMessage);
        Assert.Equal(0, batchResult.SuccessCount);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Filter Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void FailedResults_FiltersCorrectly()
    {
        var result = new BatchApplyResult
        {
            Results = new[]
            {
                ApplyResult.Created("file1.cs", "file1.cs"),
                ApplyResult.Failed("file2.cs", ApplyResultType.Error, "Error")
            }
        };

        Assert.Single(result.FailedResults);
    }

    [Fact]
    public void SucceededResults_FiltersCorrectly()
    {
        var result = new BatchApplyResult
        {
            Results = new[]
            {
                ApplyResult.Created("file1.cs", "file1.cs"),
                ApplyResult.Created("file2.cs", "file2.cs"),
                ApplyResult.Failed("file3.cs", ApplyResultType.Error, "Error")
            }
        };

        Assert.Equal(2, result.SucceededResults.Count());
    }

    [Fact]
    public void GetResultForPath_ReturnsCorrectResult()
    {
        var result = new BatchApplyResult
        {
            Results = new[]
            {
                ApplyResult.Created("file1.cs", "file1.cs"),
                ApplyResult.Created("file2.cs", "file2.cs")
            }
        };

        var found = result.GetResultForPath("file1.cs");
        Assert.NotNull(found);
        Assert.Equal("file1.cs", found.FilePath);
    }
}
