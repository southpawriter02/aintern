using AIntern.Desktop.Converters;
using System.Globalization;
using Xunit;

namespace AIntern.Desktop.Tests.Converters;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ MultiplyConverterTests (v0.5.2f)                                             │
// │ Unit tests for the MultiplyConverter value converter.                        │
// └─────────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Unit tests for <see cref="MultiplyConverter"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tests cover:
/// <list type="bullet">
/// <item>Valid multiplication with various factors</item>
/// <item>Invalid factor handling (non-numeric strings)</item>
/// <item>Null value handling</item>
/// <item>ConvertBack throws NotSupportedException</item>
/// </list>
/// </para>
/// </remarks>
public class MultiplyConverterTests
{
    #region Convert Tests

    /// <summary>
    /// Verifies that Convert multiplies the input value by the factor parameter.
    /// </summary>
    /// <param name="input">The input value to multiply.</param>
    /// <param name="factor">The multiplication factor as a string.</param>
    /// <param name="expected">The expected result.</param>
    [Theory]
    [InlineData(800.0, "0.7", 560.0)]
    [InlineData(1000.0, "0.5", 500.0)]
    [InlineData(600.0, "1.0", 600.0)]
    [InlineData(100.0, "0.0", 0.0)]
    [InlineData(250.0, "2.0", 500.0)]
    public void Convert_ValidInputs_ReturnsMultipliedValue(
        double input, 
        string factor, 
        double expected)
    {
        // Arrange
        var converter = MultiplyConverter.Instance;

        // Act
        var result = converter.Convert(
            input, 
            typeof(double), 
            factor, 
            CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Verifies that Convert returns the original value when the factor is invalid.
    /// </summary>
    [Fact]
    public void Convert_InvalidFactor_ReturnsOriginalValue()
    {
        // Arrange
        var converter = MultiplyConverter.Instance;
        var inputValue = 800.0;

        // Act
        var result = converter.Convert(
            inputValue, 
            typeof(double), 
            "invalid", 
            CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(inputValue, result);
    }

    /// <summary>
    /// Verifies that Convert returns the original value when the parameter is null.
    /// </summary>
    [Fact]
    public void Convert_NullParameter_ReturnsOriginalValue()
    {
        // Arrange
        var converter = MultiplyConverter.Instance;
        var inputValue = 800.0;

        // Act
        var result = converter.Convert(
            inputValue, 
            typeof(double), 
            null, 
            CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(inputValue, result);
    }

    /// <summary>
    /// Verifies that Convert returns null when the input value is null.
    /// </summary>
    [Fact]
    public void Convert_NullValue_ReturnsNull()
    {
        // Arrange
        var converter = MultiplyConverter.Instance;

        // Act
        var result = converter.Convert(
            null, 
            typeof(double), 
            "0.7", 
            CultureInfo.InvariantCulture);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that Convert returns the original value when input is not a double.
    /// </summary>
    [Fact]
    public void Convert_NonDoubleValue_ReturnsOriginalValue()
    {
        // Arrange
        var converter = MultiplyConverter.Instance;
        var inputValue = "not a number";

        // Act
        var result = converter.Convert(
            inputValue, 
            typeof(double), 
            "0.7", 
            CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(inputValue, result);
    }

    #endregion

    #region ConvertBack Tests

    /// <summary>
    /// Verifies that ConvertBack throws NotSupportedException as it is a one-way converter.
    /// </summary>
    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        // Arrange
        var converter = MultiplyConverter.Instance;

        // Act & Assert
        Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack(
                560.0, 
                typeof(double), 
                "0.7", 
                CultureInfo.InvariantCulture));
    }

    #endregion

    #region Singleton Instance Tests

    /// <summary>
    /// Verifies that the Instance property returns the same instance.
    /// </summary>
    [Fact]
    public void Instance_ReturnsSingleton()
    {
        // Act
        var instance1 = MultiplyConverter.Instance;
        var instance2 = MultiplyConverter.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    #endregion
}
