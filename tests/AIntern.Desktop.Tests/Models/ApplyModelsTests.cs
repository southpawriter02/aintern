using AIntern.Core.Models;
using Xunit;

namespace AIntern.Desktop.Tests.Models;

/// <summary>
/// Unit tests for v0.4.3a Apply Changes Core Models.
/// </summary>
public class ApplyModelsTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // ApplyOptions Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ApplyOptions_Default_HasExpectedValues()
    {
        var options = ApplyOptions.Default;

        Assert.True(options.CreateBackup);
        Assert.False(options.AllowConflictOverwrite);
        Assert.True(options.CheckForConflicts);
        Assert.True(options.RefreshEditorAfterApply);
        Assert.Equal(TimeSpan.FromMinutes(30), options.UndoWindow);
        Assert.True(options.ShowConfirmationDialog);
        Assert.True(options.ValidateEncoding);
        Assert.True(options.PreserveLineEndings);
    }

    [Fact]
    public void ApplyOptions_Silent_DisablesBackupAndDialogs()
    {
        var options = ApplyOptions.Silent;

        Assert.False(options.CreateBackup);
        Assert.False(options.ShowConfirmationDialog);
        Assert.False(options.ShowSuccessToast);
        Assert.False(options.CheckForConflicts);
    }

    [Fact]
    public void ApplyOptions_Batch_CreatesBackupsButSkipsDialogs()
    {
        var options = ApplyOptions.Batch;

        Assert.True(options.CreateBackup);
        Assert.False(options.ShowConfirmationDialog);
        Assert.False(options.ShowSuccessToast);
        Assert.False(options.RefreshEditorAfterApply);
    }

    [Fact]
    public void ApplyOptions_WithoutBackup_DisablesBackup()
    {
        var options = ApplyOptions.Default.WithoutBackup();

        Assert.False(options.CreateBackup);
        Assert.True(options.ShowConfirmationDialog); // Other settings preserved
    }

    [Fact]
    public void ApplyOptions_WithUndoWindow_SetsCustomWindow()
    {
        var customWindow = TimeSpan.FromHours(2);
        var options = ApplyOptions.Default.WithUndoWindow(customWindow);

        Assert.Equal(customWindow, options.UndoWindow);
    }

    [Fact]
    public void ApplyOptions_WithConflictOverwrite_EnablesOverwrite()
    {
        var options = ApplyOptions.Default.WithConflictOverwrite();

        Assert.True(options.AllowConflictOverwrite);
    }

    [Fact]
    public void ApplyOptions_WithoutDialogs_DisablesDialogs()
    {
        var options = ApplyOptions.Default.WithoutDialogs();

        Assert.False(options.ShowConfirmationDialog);
        Assert.False(options.ShowSuccessToast);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ApplyResultType Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(ApplyResultType.Success, true)]
    [InlineData(ApplyResultType.Created, true)]
    [InlineData(ApplyResultType.Modified, true)]
    [InlineData(ApplyResultType.Conflict, false)]
    [InlineData(ApplyResultType.FileNotFound, false)]
    [InlineData(ApplyResultType.Error, false)]
    public void ApplyResultType_IsSuccess_ReturnsCorrectValue(ApplyResultType type, bool expected)
    {
        Assert.Equal(expected, type.IsSuccess());
    }

    [Theory]
    [InlineData(ApplyResultType.FileLocked, true)]
    [InlineData(ApplyResultType.Conflict, true)]
    [InlineData(ApplyResultType.PermissionDenied, true)]
    [InlineData(ApplyResultType.FileNotFound, false)]
    [InlineData(ApplyResultType.ValidationFailed, false)]
    public void ApplyResultType_IsRetryable_ReturnsCorrectValue(ApplyResultType type, bool expected)
    {
        Assert.Equal(expected, type.IsRetryable());
    }

    [Fact]
    public void ApplyResultType_GetDescription_ReturnsNonEmpty()
    {
        foreach (ApplyResultType type in Enum.GetValues<ApplyResultType>())
        {
            var description = type.GetDescription();
            Assert.False(string.IsNullOrEmpty(description));
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ApplyResult Factory Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ApplyResult_Modified_CreatesSuccessResult()
    {
        var result = ApplyResult.Modified(
            "/path/to/file.cs",
            "file.cs",
            backupPath: "/backup/file.cs.bak");

        Assert.True(result.Success);
        Assert.Equal(ApplyResultType.Modified, result.ResultType);
        Assert.True(result.CanUndo);
        Assert.Equal("/backup/file.cs.bak", result.BackupPath);
    }

    [Fact]
    public void ApplyResult_Created_CreatesSuccessWithCanUndo()
    {
        var result = ApplyResult.Created(
            "/path/to/new.cs",
            "new.cs",
            linesAdded: 50);

        Assert.True(result.Success);
        Assert.Equal(ApplyResultType.Created, result.ResultType);
        Assert.True(result.CanUndo);
        Assert.Equal(50, result.LinesAdded);
    }

    [Fact]
    public void ApplyResult_Failed_CreatesFailureResult()
    {
        var result = ApplyResult.Failed(
            "/path/to/file.cs",
            ApplyResultType.PermissionDenied,
            "Access denied");

        Assert.False(result.Success);
        Assert.Equal(ApplyResultType.PermissionDenied, result.ResultType);
        Assert.False(result.CanUndo);
        Assert.Equal("Access denied", result.ErrorMessage);
    }

    [Fact]
    public void ApplyResult_Conflict_CreatesConflictResult()
    {
        var result = ApplyResult.Conflict(
            "/path/to/file.cs",
            "file.cs",
            "abc123",
            "def456",
            "File was modified externally");

        Assert.False(result.Success);
        Assert.Equal(ApplyResultType.Conflict, result.ResultType);
        Assert.Equal("abc123", result.ExpectedContentHash);
        Assert.Equal("def456", result.ActualContentHash);
    }

    [Fact]
    public void ApplyResult_Cancelled_CreatesCancelledResult()
    {
        var result = ApplyResult.Cancelled("/path/to/file.cs");

        Assert.False(result.Success);
        Assert.Equal(ApplyResultType.Cancelled, result.ResultType);
    }

    [Fact]
    public void ApplyResult_GetSummary_ReturnsDescriptiveText()
    {
        var result = ApplyResult.Modified("/path/to/file.cs", "file.cs");
        var summary = result.GetSummary();

        Assert.Contains("file.cs", summary);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FileChangeRecord Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void FileChangeRecord_GetUndoTimeRemaining_ReturnsPositiveWhenFresh()
    {
        var record = new FileChangeRecord
        {
            FilePath = "/path/to/file.cs",
            ChangeType = FileChangeType.Modified,
            ChangedAt = DateTime.UtcNow
        };

        var remaining = record.GetUndoTimeRemaining(TimeSpan.FromMinutes(30));
        Assert.True(remaining > TimeSpan.Zero);
    }

    [Fact]
    public void FileChangeRecord_GetUndoTimeRemaining_ReturnsZeroWhenExpired()
    {
        var record = new FileChangeRecord
        {
            FilePath = "/path/to/file.cs",
            ChangeType = FileChangeType.Modified,
            ChangedAt = DateTime.UtcNow - TimeSpan.FromHours(1)
        };

        var remaining = record.GetUndoTimeRemaining(TimeSpan.FromMinutes(30));
        Assert.Equal(TimeSpan.Zero, remaining);
    }

    [Fact]
    public void FileChangeRecord_IsUndoExpired_ReturnsTrueWhenUndone()
    {
        var record = new FileChangeRecord
        {
            FilePath = "/path/to/file.cs",
            ChangeType = FileChangeType.Modified,
            IsUndone = true
        };

        Assert.True(record.IsUndoExpired(TimeSpan.FromMinutes(30)));
    }

    [Fact]
    public void FileChangeRecord_CanUndo_ReturnsFalseWithoutBackup()
    {
        var record = new FileChangeRecord
        {
            FilePath = "/path/to/file.cs",
            ChangeType = FileChangeType.Modified,
            BackupPath = null
        };

        Assert.False(record.CanUndo(TimeSpan.FromMinutes(30)));
    }

    [Fact]
    public void FileChangeRecord_CanUndo_ReturnsTrueForCreatedWithoutBackup()
    {
        var record = new FileChangeRecord
        {
            FilePath = "/path/to/file.cs",
            ChangeType = FileChangeType.Created,
            BackupPath = null
        };

        Assert.True(record.CanUndo(TimeSpan.FromMinutes(30)));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // LineEndingStyle Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("line1\nline2\n", LineEndingStyle.LF)]
    [InlineData("line1\r\nline2\r\n", LineEndingStyle.CRLF)]
    [InlineData("line1\rline2\r", LineEndingStyle.CR)]
    [InlineData("line1\nline2\r\n", LineEndingStyle.Mixed)]
    [InlineData("singleline", LineEndingStyle.Unknown)]
    [InlineData("", LineEndingStyle.Unknown)]
    public void LineEndingStyle_DetectLineEndings_DetectsCorrectly(string content, LineEndingStyle expected)
    {
        var detected = LineEndingStyleExtensions.DetectLineEndings(content);
        Assert.Equal(expected, detected);
    }

    [Theory]
    [InlineData(LineEndingStyle.LF, "\n")]
    [InlineData(LineEndingStyle.CRLF, "\r\n")]
    [InlineData(LineEndingStyle.CR, "\r")]
    public void LineEndingStyle_ToLineEnding_ReturnsCorrectString(LineEndingStyle style, string expected)
    {
        Assert.Equal(expected, style.ToLineEnding());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ConflictCheckResult Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ConflictCheckResult_NoConflict_CreatesNoConflict()
    {
        var result = ConflictCheckResult.NoConflict();

        Assert.False(result.HasConflict);
    }

    [Fact]
    public void ConflictCheckResult_Detected_CreatesConflict()
    {
        var result = ConflictCheckResult.Detected("abc", "def", "File changed");

        Assert.True(result.HasConflict);
        Assert.Equal("abc", result.ExpectedHash);
        Assert.Equal("def", result.ActualHash);
        Assert.False(result.CanOverwrite);
    }
}
