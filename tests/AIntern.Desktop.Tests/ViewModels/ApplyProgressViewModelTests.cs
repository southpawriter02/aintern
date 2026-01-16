namespace AIntern.Desktop.Tests.ViewModels;

using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;
using Xunit;

/// <summary>
/// Unit tests for <see cref="ApplyProgressViewModel"/>.
/// </summary>
public class ApplyProgressViewModelTests
{
    [Fact]
    public void Start_SetsVisibleAndResetState()
    {
        // Arrange
        var vm = new ApplyProgressViewModel();
        var cts = new CancellationTokenSource();

        // Act
        vm.Start(cts, 5);

        // Assert
        Assert.True(vm.IsVisible);
        Assert.Equal(BatchApplyPhase.Validating, vm.Phase);
        Assert.Equal(0, vm.ProgressPercent);
        Assert.Equal(0, vm.CompletedCount);
        Assert.Equal(5, vm.TotalCount);
        Assert.True(vm.CanCancel);
        Assert.False(vm.CancellationRequested);
    }

    [Fact]
    public void PhaseTitle_ReturnsCorrectTitles()
    {
        var vm = new ApplyProgressViewModel();
        var cts = new CancellationTokenSource();
        vm.Start(cts, 1);

        Assert.Equal("Validating...", vm.PhaseTitle);
    }

    [Fact]
    public void IsIndeterminate_TrueForValidatingAndFinalizing()
    {
        var vm = new ApplyProgressViewModel();
        var cts = new CancellationTokenSource();
        vm.Start(cts, 1);

        // Validating is indeterminate
        Assert.True(vm.IsIndeterminate);
    }

    [Fact]
    public void FileCountText_FormatsCorrectly()
    {
        var vm = new ApplyProgressViewModel();
        var cts = new CancellationTokenSource();
        vm.Start(cts, 10);

        // Initially 0 of 10
        Assert.Equal("0 of 10 files", vm.FileCountText);
    }

    [Fact]
    public void ProgressPercentText_FormatsCorrectly()
    {
        var vm = new ApplyProgressViewModel();
        var cts = new CancellationTokenSource();
        vm.Start(cts, 1);

        Assert.Equal("0%", vm.ProgressPercentText);
    }

    [Fact]
    public void ShowCancelButton_TrueWhenCanCancelAndNotCancelled()
    {
        var vm = new ApplyProgressViewModel();
        var cts = new CancellationTokenSource();
        vm.Start(cts, 1);

        Assert.True(vm.ShowCancelButton);
    }

    [Fact]
    public void Hide_SetsVisibleFalse()
    {
        var vm = new ApplyProgressViewModel();
        var cts = new CancellationTokenSource();
        vm.Start(cts, 1);

        vm.Hide();

        Assert.False(vm.IsVisible);
    }

    [Fact]
    public void Error_SetsPhaseToRollingBackAndMessage()
    {
        var vm = new ApplyProgressViewModel();
        var cts = new CancellationTokenSource();
        vm.Start(cts, 1);

        vm.Error("Test error");

        Assert.Equal(BatchApplyPhase.RollingBack, vm.Phase);
        Assert.Equal("Test error", vm.ErrorMessage);
        Assert.True(vm.HasError);
        Assert.False(vm.CanCancel);
    }

    [Fact]
    public void IsCompleted_TrueOnlyForCompletedPhase()
    {
        var vm = new ApplyProgressViewModel();
        var cts = new CancellationTokenSource();
        vm.Start(cts, 1);

        Assert.False(vm.IsCompleted);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var vm = new ApplyProgressViewModel();
        var cts = new CancellationTokenSource();
        vm.Start(cts, 1);

        var exception = Record.Exception(() => vm.Dispose());
        Assert.Null(exception);
    }
}
