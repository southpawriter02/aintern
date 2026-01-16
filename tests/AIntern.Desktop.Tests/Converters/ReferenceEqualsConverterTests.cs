namespace AIntern.Desktop.Tests.Converters;

using AIntern.Desktop.Converters;
using System.Globalization;
using Xunit;

/// <summary>
/// Unit tests for <see cref="ReferenceEqualsConverter"/>.
/// </summary>
public class ReferenceEqualsConverterTests
{
    private readonly ReferenceEqualsConverter _converter = new();

    [Fact]
    public void Convert_SameReference_ReturnsTrue()
    {
        // Arrange
        var obj = new object();

        // Act
        var result = _converter.Convert(obj, typeof(bool), obj, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(true, result);
    }

    [Fact]
    public void Convert_DifferentReferences_ReturnsFalse()
    {
        // Arrange
        var obj1 = new object();
        var obj2 = new object();

        // Act
        var result = _converter.Convert(obj1, typeof(bool), obj2, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(false, result);
    }

    [Fact]
    public void Convert_BothNull_ReturnsTrue()
    {
        // Act
        var result = _converter.Convert(null, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(true, result);
    }

    [Fact]
    public void Convert_ValueNull_ParameterNotNull_ReturnsFalse()
    {
        // Arrange
        var param = new object();

        // Act
        var result = _converter.Convert(null, typeof(bool), param, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(false, result);
    }

    [Fact]
    public void Convert_ValueNotNull_ParameterNull_ReturnsFalse()
    {
        // Arrange
        var value = new object();

        // Act
        var result = _converter.Convert(value, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(false, result);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack(true, typeof(object), null, CultureInfo.InvariantCulture));
    }
}
