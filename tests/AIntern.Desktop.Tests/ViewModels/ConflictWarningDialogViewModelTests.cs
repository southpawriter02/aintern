using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;
using Xunit;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Unit tests for v0.4.3g ConflictWarningDialogViewModel.
/// </summary>
public class ConflictWarningDialogViewModelTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Constructor Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_Default_InitializesCorrectly()
    {
        var vm = new ConflictWarningDialogViewModel();

        Assert.Equal(ConflictResolution.Cancel, vm.SelectedResolution);
        Assert.Equal(string.Empty, vm.FilePath);
    }

    [Fact]
    public void Constructor_WithConflict_SetsProperties()
    {
        var conflict = new ConflictInfo
        {
            HasConflict = true,
            Reason = ConflictReason.ContentModified,
            Message = "Test conflict",
            LastModified = DateTime.UtcNow,
            SnapshotTime = DateTime.UtcNow.AddMinutes(-10)
        };

        var vm = new ConflictWarningDialogViewModel(
            conflict,
            "/test/file.cs",
            _ => { });

        Assert.Equal("/test/file.cs", vm.FilePath);
        Assert.Equal("file.cs", vm.FileName);
        Assert.Equal("Test conflict", vm.ConflictMessage);
        Assert.Equal(ConflictReason.ContentModified, vm.ConflictReason);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ConflictExplanation Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(ConflictReason.FileCreated, "created")]
    [InlineData(ConflictReason.FileDeleted, "deleted")]
    [InlineData(ConflictReason.ContentModified, "modified")]
    [InlineData(ConflictReason.PermissionChanged, "permissions")]
    public void ConflictExplanation_ReturnsReasonSpecificText(ConflictReason reason, string expectedContains)
    {
        var vm = new ConflictWarningDialogViewModel { ConflictReason = reason };
        Assert.Contains(expectedContains, vm.ConflictExplanation, StringComparison.OrdinalIgnoreCase);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // IsDestructiveConflict Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void IsDestructiveConflict_ContentModified_ReturnsTrue()
    {
        var vm = new ConflictWarningDialogViewModel { ConflictReason = ConflictReason.ContentModified };
        Assert.True(vm.IsDestructiveConflict);
    }

    [Fact]
    public void IsDestructiveConflict_FileCreated_ReturnsTrue()
    {
        var vm = new ConflictWarningDialogViewModel { ConflictReason = ConflictReason.FileCreated };
        Assert.True(vm.IsDestructiveConflict);
    }

    [Fact]
    public void IsDestructiveConflict_FileDeleted_ReturnsFalse()
    {
        var vm = new ConflictWarningDialogViewModel { ConflictReason = ConflictReason.FileDeleted };
        Assert.False(vm.IsDestructiveConflict);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TimeSinceSnapshotText Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void TimeSinceSnapshotText_JustNow_ReturnsCorrect()
    {
        var vm = new ConflictWarningDialogViewModel { SnapshotTime = DateTime.UtcNow };
        Assert.Equal("just now", vm.TimeSinceSnapshotText);
    }

    [Fact]
    public void TimeSinceSnapshotText_Minutes_ReturnsCorrect()
    {
        var vm = new ConflictWarningDialogViewModel { SnapshotTime = DateTime.UtcNow.AddMinutes(-5) };
        Assert.Contains("minute", vm.TimeSinceSnapshotText);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Command Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void RefreshDiffCommand_SetsResolution()
    {
        ConflictResolution? result = null;
        var vm = new ConflictWarningDialogViewModel();
        vm.SetCloseAction(r => result = r);

        vm.RefreshDiffCommand.Execute(null);

        Assert.Equal(ConflictResolution.RefreshDiff, result);
        Assert.Equal(ConflictResolution.RefreshDiff, vm.SelectedResolution);
    }

    [Fact]
    public void ForceApplyCommand_SetsResolution()
    {
        ConflictResolution? result = null;
        var vm = new ConflictWarningDialogViewModel();
        vm.SetCloseAction(r => result = r);

        vm.ForceApplyCommand.Execute(null);

        Assert.Equal(ConflictResolution.ForceApply, result);
        Assert.Equal(ConflictResolution.ForceApply, vm.SelectedResolution);
    }

    [Fact]
    public void CancelCommand_SetsResolution()
    {
        ConflictResolution? result = null;
        var vm = new ConflictWarningDialogViewModel();
        vm.SetCloseAction(r => result = r);

        vm.CancelCommand.Execute(null);

        Assert.Equal(ConflictResolution.Cancel, result);
        Assert.Equal(ConflictResolution.Cancel, vm.SelectedResolution);
    }
}
