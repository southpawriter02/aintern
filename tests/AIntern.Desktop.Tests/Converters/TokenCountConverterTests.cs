namespace AIntern.Desktop.Tests.Converters;

using System.Globalization;
using Xunit;
using AIntern.Desktop.Converters;

/// <summary>
/// Unit tests for <see cref="TokenCountConverter"/>.
/// </summary>
/// <remarks>
/// <para>Added in v0.3.4d.</para>
/// </remarks>
public class TokenCountConverterTests
{
    private readonly TokenCountConverter _converter = new();
    private readonly CultureInfo _usCulture = CultureInfo.InvariantCulture;

    #region Convert Tests

    /// <summary>
    /// Verifies Convert formats integer with no separators for small numbers.
    /// </summary>
    [Fact]
    public void Convert_SmallInteger_NoSeparators()
    {
        // Arrange
        var value = 500;

        // Act
        var result = _converter.Convert(value, typeof(string), null, _usCulture);

        // Assert
        Assert.Equal("500", result);
    }

    /// <summary>
    /// Verifies Convert formats large integers with thousands separators.
    /// </summary>
    [Fact]
    public void Convert_LargeInteger_WithSeparators()
    {
        // Arrange
        var value = 1650;

        // Act
        var result = _converter.Convert(value, typeof(string), null, CultureInfo.GetCultureInfo("en-US"));

        // Assert
        Assert.Equal("1,650", result);
    }

    /// <summary>
    /// Verifies Convert handles long type.
    /// </summary>
    [Fact]
    public void Convert_Long_WithSeparators()
    {
        // Arrange
        var value = 1234567L;

        // Act
        var result = _converter.Convert(value, typeof(string), null, CultureInfo.GetCultureInfo("en-US"));

        // Assert
        Assert.Equal("1,234,567", result);
    }

    /// <summary>
    /// Verifies Convert handles null.
    /// </summary>
    [Fact]
    public void Convert_Null_ReturnsZero()
    {
        // Act
        var result = _converter.Convert(null, typeof(string), null, _usCulture);

        // Assert
        Assert.Equal("0", result);
    }

    /// <summary>
    /// Verifies Convert handles zero.
    /// </summary>
    [Fact]
    public void Convert_Zero_ReturnsZero()
    {
        // Act
        var result = _converter.Convert(0, typeof(string), null, _usCulture);

        // Assert
        Assert.Equal("0", result);
    }

    #endregion

    #region ConvertBack Tests

    /// <summary>
    /// Verifies ConvertBack parses formatted string.
    /// </summary>
    [Fact]
    public void ConvertBack_FormattedString_ParsesCorrectly()
    {
        // Arrange
        var value = "1,650";

        // Act
        var result = _converter.ConvertBack(value, typeof(int), null, _usCulture);

        // Assert
        Assert.Equal(1650, result);
    }

    /// <summary>
    /// Verifies ConvertBack handles invalid string.
    /// </summary>
    [Fact]
    public void ConvertBack_InvalidString_ReturnsZero()
    {
        // Arrange
        var value = "not a number";

        // Act
        var result = _converter.ConvertBack(value, typeof(int), null, _usCulture);

        // Assert
        Assert.Equal(0, result);
    }

    #endregion
}
