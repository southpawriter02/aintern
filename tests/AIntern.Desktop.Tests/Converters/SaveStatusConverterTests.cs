// ============================================================================
// SaveStatusConverterTests.cs
// AIntern.Desktop.Tests - Status Bar Enhancements (v0.2.5g)
// ============================================================================
// Unit tests for SaveStatusTextConverter and SaveStatusColorConverter.
// Tests conversion logic, null handling, and singleton instance patterns.
// ============================================================================

using System.Globalization;
using AIntern.Core.Events;
using AIntern.Desktop.Converters;
using Avalonia.Media;
using Xunit;

namespace AIntern.Desktop.Tests.Converters;

/// <summary>
/// Unit tests for <see cref="SaveStatusTextConverter"/> and <see cref="SaveStatusColorConverter"/>.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify:
/// </para>
/// <list type="bullet">
///   <item><description>Correct conversion logic for all save states (saving, unsaved, saved)</description></item>
///   <item><description>Proper handling of null and non-SaveStateChangedEventArgs inputs</description></item>
///   <item><description>ConvertBack throws NotSupportedException for one-way converters</description></item>
///   <item><description>Singleton instance availability and consistency</description></item>
/// </list>
/// <para>Added in v0.2.5g.</para>
/// </remarks>
public class SaveStatusConverterTests
{
    #region SaveStatusTextConverter Tests

    /// <summary>
    /// Verifies Instance returns non-null singleton.
    /// </summary>
    [Fact]
    public void SaveStatusTextConverter_Instance_ReturnsNonNull()
    {
        // Act
        var instance = SaveStatusTextConverter.Instance;

        // Assert
        Assert.NotNull(instance);
    }

    /// <summary>
    /// Verifies Instance returns same singleton each time.
    /// </summary>
    [Fact]
    public void SaveStatusTextConverter_Instance_ReturnsSameSingleton()
    {
        // Act
        var instance1 = SaveStatusTextConverter.Instance;
        var instance2 = SaveStatusTextConverter.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    /// <summary>
    /// Verifies Convert returns "Saving..." when IsSaving is true.
    /// </summary>
    [Fact]
    public void SaveStatusTextConverter_Convert_IsSavingTrue_ReturnsSavingText()
    {
        // Arrange
        var converter = SaveStatusTextConverter.Instance;
        var saveState = new SaveStateChangedEventArgs
        {
            IsSaving = true,
            HasUnsavedChanges = false
        };

        // Act
        var result = converter.Convert(saveState, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(SaveStatusTextConverter.SavingText, result);
    }

    /// <summary>
    /// Verifies Convert returns "Saving..." even when HasUnsavedChanges is also true.
    /// IsSaving takes priority over HasUnsavedChanges.
    /// </summary>
    [Fact]
    public void SaveStatusTextConverter_Convert_IsSavingTrueWithUnsavedChanges_ReturnsSavingText()
    {
        // Arrange
        var converter = SaveStatusTextConverter.Instance;
        var saveState = new SaveStateChangedEventArgs
        {
            IsSaving = true,
            HasUnsavedChanges = true
        };

        // Act
        var result = converter.Convert(saveState, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(SaveStatusTextConverter.SavingText, result);
    }

    /// <summary>
    /// Verifies Convert returns "Unsaved" when HasUnsavedChanges is true and IsSaving is false.
    /// </summary>
    [Fact]
    public void SaveStatusTextConverter_Convert_HasUnsavedChangesTrue_ReturnsUnsavedText()
    {
        // Arrange
        var converter = SaveStatusTextConverter.Instance;
        var saveState = new SaveStateChangedEventArgs
        {
            IsSaving = false,
            HasUnsavedChanges = true
        };

        // Act
        var result = converter.Convert(saveState, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(SaveStatusTextConverter.UnsavedText, result);
    }

    /// <summary>
    /// Verifies Convert returns "Saved" when both IsSaving and HasUnsavedChanges are false.
    /// </summary>
    [Fact]
    public void SaveStatusTextConverter_Convert_AllFalse_ReturnsSavedText()
    {
        // Arrange
        var converter = SaveStatusTextConverter.Instance;
        var saveState = new SaveStateChangedEventArgs
        {
            IsSaving = false,
            HasUnsavedChanges = false
        };

        // Act
        var result = converter.Convert(saveState, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(SaveStatusTextConverter.SavedText, result);
    }

    /// <summary>
    /// Verifies Convert returns "Saved" for null input (default state).
    /// </summary>
    [Fact]
    public void SaveStatusTextConverter_Convert_NullInput_ReturnsSavedText()
    {
        // Arrange
        var converter = SaveStatusTextConverter.Instance;

        // Act
        var result = converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(SaveStatusTextConverter.SavedText, result);
    }

    /// <summary>
    /// Verifies Convert returns "Saved" for non-SaveStateChangedEventArgs input.
    /// </summary>
    [Theory]
    [InlineData("Saving")]
    [InlineData(true)]
    [InlineData(42)]
    public void SaveStatusTextConverter_Convert_InvalidInput_ReturnsSavedText(object value)
    {
        // Arrange
        var converter = SaveStatusTextConverter.Instance;

        // Act
        var result = converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(SaveStatusTextConverter.SavedText, result);
    }

    /// <summary>
    /// Verifies ConvertBack throws NotSupportedException.
    /// </summary>
    [Fact]
    public void SaveStatusTextConverter_ConvertBack_ThrowsNotSupportedException()
    {
        // Arrange
        var converter = SaveStatusTextConverter.Instance;

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack("Saved", typeof(SaveStateChangedEventArgs), null, CultureInfo.InvariantCulture));
        Assert.Contains("SaveStatusTextConverter", exception.Message);
    }

    #endregion

    #region SaveStatusColorConverter Tests

    /// <summary>
    /// Verifies Instance returns non-null singleton.
    /// </summary>
    [Fact]
    public void SaveStatusColorConverter_Instance_ReturnsNonNull()
    {
        // Act
        var instance = SaveStatusColorConverter.Instance;

        // Assert
        Assert.NotNull(instance);
    }

    /// <summary>
    /// Verifies Instance returns same singleton each time.
    /// </summary>
    [Fact]
    public void SaveStatusColorConverter_Instance_ReturnsSameSingleton()
    {
        // Act
        var instance1 = SaveStatusColorConverter.Instance;
        var instance2 = SaveStatusColorConverter.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    /// <summary>
    /// Verifies Convert returns accent color brush when IsSaving is true.
    /// </summary>
    [Fact]
    public void SaveStatusColorConverter_Convert_IsSavingTrue_ReturnsAccentBrush()
    {
        // Arrange
        var converter = SaveStatusColorConverter.Instance;
        var saveState = new SaveStateChangedEventArgs
        {
            IsSaving = true,
            HasUnsavedChanges = false
        };

        // Act
        var result = converter.Convert(saveState, typeof(IBrush), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result;
        Assert.Equal(Color.Parse("#00d9ff"), brush.Color);
    }

    /// <summary>
    /// Verifies Convert returns accent color brush even when HasUnsavedChanges is also true.
    /// IsSaving takes priority over HasUnsavedChanges.
    /// </summary>
    [Fact]
    public void SaveStatusColorConverter_Convert_IsSavingTrueWithUnsavedChanges_ReturnsAccentBrush()
    {
        // Arrange
        var converter = SaveStatusColorConverter.Instance;
        var saveState = new SaveStateChangedEventArgs
        {
            IsSaving = true,
            HasUnsavedChanges = true
        };

        // Act
        var result = converter.Convert(saveState, typeof(IBrush), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result;
        Assert.Equal(Color.Parse("#00d9ff"), brush.Color);
    }

    /// <summary>
    /// Verifies Convert returns yellow brush when HasUnsavedChanges is true and IsSaving is false.
    /// </summary>
    [Fact]
    public void SaveStatusColorConverter_Convert_HasUnsavedChangesTrue_ReturnsYellowBrush()
    {
        // Arrange
        var converter = SaveStatusColorConverter.Instance;
        var saveState = new SaveStateChangedEventArgs
        {
            IsSaving = false,
            HasUnsavedChanges = true
        };

        // Act
        var result = converter.Convert(saveState, typeof(IBrush), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result;
        Assert.Equal(Color.Parse("#ffc107"), brush.Color);
    }

    /// <summary>
    /// Verifies Convert returns green brush when both IsSaving and HasUnsavedChanges are false.
    /// </summary>
    [Fact]
    public void SaveStatusColorConverter_Convert_AllFalse_ReturnsGreenBrush()
    {
        // Arrange
        var converter = SaveStatusColorConverter.Instance;
        var saveState = new SaveStateChangedEventArgs
        {
            IsSaving = false,
            HasUnsavedChanges = false
        };

        // Act
        var result = converter.Convert(saveState, typeof(IBrush), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result;
        Assert.Equal(Color.Parse("#4caf50"), brush.Color);
    }

    /// <summary>
    /// Verifies Convert returns green brush for null input (default state).
    /// </summary>
    [Fact]
    public void SaveStatusColorConverter_Convert_NullInput_ReturnsGreenBrush()
    {
        // Arrange
        var converter = SaveStatusColorConverter.Instance;

        // Act
        var result = converter.Convert(null, typeof(IBrush), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result;
        Assert.Equal(Color.Parse("#4caf50"), brush.Color);
    }

    /// <summary>
    /// Verifies Convert returns green brush for non-SaveStateChangedEventArgs input.
    /// </summary>
    [Theory]
    [InlineData("Saving")]
    [InlineData(true)]
    [InlineData(42)]
    public void SaveStatusColorConverter_Convert_InvalidInput_ReturnsGreenBrush(object value)
    {
        // Arrange
        var converter = SaveStatusColorConverter.Instance;

        // Act
        var result = converter.Convert(value, typeof(IBrush), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result;
        Assert.Equal(Color.Parse("#4caf50"), brush.Color);
    }

    /// <summary>
    /// Verifies ConvertBack throws NotSupportedException.
    /// </summary>
    [Fact]
    public void SaveStatusColorConverter_ConvertBack_ThrowsNotSupportedException()
    {
        // Arrange
        var converter = SaveStatusColorConverter.Instance;
        var brush = new SolidColorBrush(Colors.Green);

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack(brush, typeof(SaveStateChangedEventArgs), null, CultureInfo.InvariantCulture));
        Assert.Contains("SaveStatusColorConverter", exception.Message);
    }

    #endregion

    #region BoolToAccentColorConverter Tests

    /// <summary>
    /// Verifies Instance returns non-null singleton.
    /// </summary>
    [Fact]
    public void BoolToAccentColorConverter_Instance_ReturnsNonNull()
    {
        // Act
        var instance = BoolToAccentColorConverter.Instance;

        // Assert
        Assert.NotNull(instance);
    }

    /// <summary>
    /// Verifies Instance returns same singleton each time.
    /// </summary>
    [Fact]
    public void BoolToAccentColorConverter_Instance_ReturnsSameSingleton()
    {
        // Act
        var instance1 = BoolToAccentColorConverter.Instance;
        var instance2 = BoolToAccentColorConverter.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    /// <summary>
    /// Verifies Convert returns accent color brush for true.
    /// </summary>
    [Fact]
    public void BoolToAccentColorConverter_Convert_TrueReturnsAccentBrush()
    {
        // Arrange
        var converter = BoolToAccentColorConverter.Instance;

        // Act
        var result = converter.Convert(true, typeof(IBrush), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result;
        Assert.Equal(Color.Parse("#00d9ff"), brush.Color);
    }

    /// <summary>
    /// Verifies Convert returns muted color brush for false.
    /// </summary>
    [Fact]
    public void BoolToAccentColorConverter_Convert_FalseReturnsMutedBrush()
    {
        // Arrange
        var converter = BoolToAccentColorConverter.Instance;

        // Act
        var result = converter.Convert(false, typeof(IBrush), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result;
        Assert.Equal(Color.Parse("#888888"), brush.Color);
    }

    /// <summary>
    /// Verifies Convert returns muted color brush for null.
    /// </summary>
    [Fact]
    public void BoolToAccentColorConverter_Convert_NullReturnsMutedBrush()
    {
        // Arrange
        var converter = BoolToAccentColorConverter.Instance;

        // Act
        var result = converter.Convert(null, typeof(IBrush), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result;
        Assert.Equal(Color.Parse("#888888"), brush.Color);
    }

    /// <summary>
    /// Verifies Convert returns muted color brush for non-boolean values.
    /// </summary>
    [Theory]
    [InlineData("true")]
    [InlineData(1)]
    [InlineData("Accent")]
    public void BoolToAccentColorConverter_Convert_NonBooleanReturnsMutedBrush(object value)
    {
        // Arrange
        var converter = BoolToAccentColorConverter.Instance;

        // Act
        var result = converter.Convert(value, typeof(IBrush), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<SolidColorBrush>(result);
        var brush = (SolidColorBrush)result;
        Assert.Equal(Color.Parse("#888888"), brush.Color);
    }

    /// <summary>
    /// Verifies ConvertBack throws NotSupportedException.
    /// </summary>
    [Fact]
    public void BoolToAccentColorConverter_ConvertBack_ThrowsNotSupportedException()
    {
        // Arrange
        var converter = BoolToAccentColorConverter.Instance;
        var brush = new SolidColorBrush(Color.Parse("#00d9ff"));

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack(brush, typeof(bool), null, CultureInfo.InvariantCulture));
        Assert.Contains("BoolToAccentColorConverter", exception.Message);
    }

    #endregion
}
