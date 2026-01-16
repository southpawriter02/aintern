namespace AIntern.Desktop.Tests.Converters;

using System.Globalization;
using Xunit;
using AIntern.Core.Models;
using AIntern.Desktop.Converters;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ CODE BLOCK CONVERTERS TESTS (v0.4.1h)                                    │
// │ Unit tests for code block UI rendering converters.                       │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Tests for <see cref="EnumEqualsConverter"/>.
/// </summary>
public class EnumEqualsConverterTests
{
    [Fact]
    public void Convert_MatchingEnum_ReturnsTrue()
    {
        var converter = EnumEqualsConverter.Instance;
        var result = converter.Convert(CodeBlockStatus.Applied, typeof(bool), "Applied", CultureInfo.InvariantCulture);
        Assert.True((bool)result);
    }

    [Fact]
    public void Convert_NonMatchingEnum_ReturnsFalse()
    {
        var converter = EnumEqualsConverter.Instance;
        var result = converter.Convert(CodeBlockStatus.Applied, typeof(bool), "Rejected", CultureInfo.InvariantCulture);
        Assert.False((bool)result);
    }

    [Fact]
    public void Convert_CaseInsensitive_ReturnsTrue()
    {
        var converter = EnumEqualsConverter.Instance;
        var result = converter.Convert(CodeBlockStatus.Applied, typeof(bool), "applied", CultureInfo.InvariantCulture);
        Assert.True((bool)result);
    }

    [Fact]
    public void Convert_NullValue_ReturnsFalse()
    {
        var converter = EnumEqualsConverter.Instance;
        var result = converter.Convert(null, typeof(bool), "Applied", CultureInfo.InvariantCulture);
        Assert.False((bool)result);
    }

    [Fact]
    public void Convert_NullParameter_ReturnsFalse()
    {
        var converter = EnumEqualsConverter.Instance;
        var result = converter.Convert(CodeBlockStatus.Applied, typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.False((bool)result);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupported()
    {
        var converter = EnumEqualsConverter.Instance;
        Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack(true, typeof(CodeBlockStatus), "Applied", CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// Tests for <see cref="GreaterThanOneConverter"/>.
/// </summary>
public class GreaterThanOneConverterTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(10, true)]
    [InlineData(100, true)]
    public void Convert_IntegerValues_ReturnsExpected(int value, bool expected)
    {
        var converter = GreaterThanOneConverter.Instance;
        var result = converter.Convert(value, typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Convert_NonInteger_ReturnsFalse()
    {
        var converter = GreaterThanOneConverter.Instance;
        var result = converter.Convert("not an int", typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.False((bool)result);
    }

    [Fact]
    public void Convert_NullValue_ReturnsFalse()
    {
        var converter = GreaterThanOneConverter.Instance;
        var result = converter.Convert(null, typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.False((bool)result);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupported()
    {
        var converter = GreaterThanOneConverter.Instance;
        Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack(true, typeof(int), null, CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// Tests for <see cref="IsApplicableBlockTypeConverter"/>.
/// </summary>
public class IsApplicableBlockTypeConverterTests
{
    [Theory]
    [InlineData(CodeBlockType.CompleteFile, true)]
    [InlineData(CodeBlockType.Snippet, true)]
    [InlineData(CodeBlockType.Example, false)]
    [InlineData(CodeBlockType.Command, false)]
    [InlineData(CodeBlockType.Output, false)]
    [InlineData(CodeBlockType.Config, false)]
    public void Convert_BlockType_ReturnsExpected(CodeBlockType type, bool expected)
    {
        var converter = IsApplicableBlockTypeConverter.Instance;
        var result = converter.Convert(type, typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Convert_NonBlockType_ReturnsFalse()
    {
        var converter = IsApplicableBlockTypeConverter.Instance;
        var result = converter.Convert("not a block type", typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.False((bool)result);
    }

    [Fact]
    public void Convert_NullValue_ReturnsFalse()
    {
        var converter = IsApplicableBlockTypeConverter.Instance;
        var result = converter.Convert(null, typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.False((bool)result);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupported()
    {
        var converter = IsApplicableBlockTypeConverter.Instance;
        Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack(true, typeof(CodeBlockType), null, CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// Tests for <see cref="InverseBoolConverter"/>.
/// </summary>
public class InverseBoolConverterTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Convert_BoolValue_ReturnsInverse(bool value, bool expected)
    {
        var converter = InverseBoolConverter.Instance;
        var result = converter.Convert(value, typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ConvertBack_BoolValue_ReturnsInverse(bool value, bool expected)
    {
        var converter = InverseBoolConverter.Instance;
        var result = converter.ConvertBack(value, typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Convert_NonBool_ReturnsFalse()
    {
        var converter = InverseBoolConverter.Instance;
        var result = converter.Convert("not a bool", typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.False((bool)result);
    }
}

/// <summary>
/// Tests for <see cref="NotNullConverter"/>.
/// </summary>
public class NotNullConverterTests
{
    [Fact]
    public void Convert_NonNullValue_ReturnsTrue()
    {
        var converter = NotNullConverter.Instance;
        var result = converter.Convert("something", typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.True((bool)result);
    }

    [Fact]
    public void Convert_NullValue_ReturnsFalse()
    {
        var converter = NotNullConverter.Instance;
        var result = converter.Convert(null, typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.False((bool)result);
    }

    [Fact]
    public void Convert_EmptyString_ReturnsTrue()
    {
        // Empty string is not null
        var converter = NotNullConverter.Instance;
        var result = converter.Convert(string.Empty, typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.True((bool)result);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupported()
    {
        var converter = NotNullConverter.Instance;
        Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack(true, typeof(object), null, CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// Tests for <see cref="GreaterThanZeroConverter"/>.
/// </summary>
public class GreaterThanZeroConverterTests
{
    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(5, true)]
    public void Convert_IntegerValues_ReturnsExpected(int value, bool expected)
    {
        var converter = GreaterThanZeroConverter.Instance;
        var result = converter.Convert(value, typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Convert_NonInteger_ReturnsFalse()
    {
        var converter = GreaterThanZeroConverter.Instance;
        var result = converter.Convert("not an int", typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.False((bool)result);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupported()
    {
        var converter = GreaterThanZeroConverter.Instance;
        Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack(true, typeof(int), null, CultureInfo.InvariantCulture));
    }
}
