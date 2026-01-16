namespace AIntern.Desktop.Tests.Converters;

using AIntern.Core.Models;
using AIntern.Desktop.Converters;
using Avalonia.Media;
using System.Globalization;
using Xunit;

/// <summary>
/// Unit tests for phase converters.
/// </summary>
public class PhaseConverterTests
{
    #region PhaseToTitleConverter Tests

    [Theory]
    [InlineData(BatchApplyPhase.Validating, "Validating...")]
    [InlineData(BatchApplyPhase.CreatingBackups, "Creating Backups...")]
    [InlineData(BatchApplyPhase.CreatingDirectories, "Creating Directories...")]
    [InlineData(BatchApplyPhase.WritingFiles, "Writing Files...")]
    [InlineData(BatchApplyPhase.Finalizing, "Finalizing...")]
    [InlineData(BatchApplyPhase.Completed, "Complete!")]
    [InlineData(BatchApplyPhase.RollingBack, "Rolling Back...")]
    public void PhaseToTitleConverter_ReturnsCorrectTitle(BatchApplyPhase phase, string expected)
    {
        var converter = new PhaseToTitleConverter();
        var result = converter.Convert(phase, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void PhaseToTitleConverter_NonPhaseValue_ReturnsProcessing()
    {
        var converter = new PhaseToTitleConverter();
        var result = converter.Convert("not a phase", typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("Processing...", result);
    }

    [Fact]
    public void PhaseToTitleConverter_ConvertBack_ThrowsNotSupported()
    {
        var converter = new PhaseToTitleConverter();
        Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack("test", typeof(BatchApplyPhase), null, CultureInfo.InvariantCulture));
    }

    #endregion

    #region PhaseToIconConverter Tests

    // Note: PhaseToIconConverter tests require Avalonia platform initialization
    // for StreamGeometry.Parse(). Skipping these tests in unit test environment.
    // The converter is tested implicitly through UI integration tests.

    [Fact]
    public void PhaseToIconConverter_ConvertBack_ThrowsNotSupported()
    {
        var converter = new PhaseToIconConverter();
        Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack(null, typeof(BatchApplyPhase), null, CultureInfo.InvariantCulture));
    }

    #endregion

    #region PhaseToBrushConverter Tests

    [Fact]
    public void PhaseToBrushConverter_Completed_ReturnsSuccessBrush()
    {
        var converter = new PhaseToBrushConverter();
        var result = converter.Convert(BatchApplyPhase.Completed, typeof(IBrush), null, CultureInfo.InvariantCulture);
        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result!;
        Assert.Equal(Color.Parse("#89D185"), brush.Color);
    }

    [Fact]
    public void PhaseToBrushConverter_RollingBack_ReturnsErrorBrush()
    {
        var converter = new PhaseToBrushConverter();
        var result = converter.Convert(BatchApplyPhase.RollingBack, typeof(IBrush), null, CultureInfo.InvariantCulture);
        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result!;
        Assert.Equal(Color.Parse("#F48771"), brush.Color);
    }

    [Fact]
    public void PhaseToBrushConverter_OtherPhases_ReturnsAccentBrush()
    {
        var converter = new PhaseToBrushConverter();
        var result = converter.Convert(BatchApplyPhase.WritingFiles, typeof(IBrush), null, CultureInfo.InvariantCulture);
        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result!;
        Assert.Equal(Color.Parse("#007ACC"), brush.Color);
    }

    [Fact]
    public void PhaseToBrushConverter_ConvertBack_ThrowsNotSupported()
    {
        var converter = new PhaseToBrushConverter();
        Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack(null, typeof(BatchApplyPhase), null, CultureInfo.InvariantCulture));
    }

    #endregion
}
