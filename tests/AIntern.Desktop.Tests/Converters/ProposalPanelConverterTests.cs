namespace AIntern.Desktop.Tests.Converters;

using AIntern.Core.Models;
using AIntern.Desktop.Converters;
using System.Globalization;
using Xunit;

/// <summary>
/// Unit tests for <see cref="SelectionStateToCheckStateConverter"/>.
/// </summary>
public class SelectionStateToCheckStateConverterTests
{
    private readonly SelectionStateToCheckStateConverter _converter = new();

    #region Convert Tests

    [Fact]
    public void Convert_None_ReturnsFalse()
    {
        var result = _converter.Convert(SelectionState.None, typeof(bool?), null, CultureInfo.InvariantCulture);
        Assert.Equal(false, result);
    }

    [Fact]
    public void Convert_Some_ReturnsNull()
    {
        var result = _converter.Convert(SelectionState.Some, typeof(bool?), null, CultureInfo.InvariantCulture);
        Assert.Null(result);
    }

    [Fact]
    public void Convert_All_ReturnsTrue()
    {
        var result = _converter.Convert(SelectionState.All, typeof(bool?), null, CultureInfo.InvariantCulture);
        Assert.Equal(true, result);
    }

    [Fact]
    public void Convert_InvalidValue_ReturnsFalse()
    {
        var result = _converter.Convert("invalid", typeof(bool?), null, CultureInfo.InvariantCulture);
        Assert.Equal(false, result);
    }

    [Fact]
    public void Convert_NullValue_ReturnsFalse()
    {
        var result = _converter.Convert(null, typeof(bool?), null, CultureInfo.InvariantCulture);
        Assert.Equal(false, result);
    }

    #endregion

    #region ConvertBack Tests

    [Fact]
    public void ConvertBack_True_ReturnsAll()
    {
        var result = _converter.ConvertBack(true, typeof(SelectionState), null, CultureInfo.InvariantCulture);
        Assert.Equal(SelectionState.All, result);
    }

    [Fact]
    public void ConvertBack_False_ReturnsNone()
    {
        var result = _converter.ConvertBack(false, typeof(SelectionState), null, CultureInfo.InvariantCulture);
        Assert.Equal(SelectionState.None, result);
    }

    [Fact]
    public void ConvertBack_Null_ReturnsSome()
    {
        var result = _converter.ConvertBack(null, typeof(SelectionState), null, CultureInfo.InvariantCulture);
        Assert.Equal(SelectionState.Some, result);
    }

    #endregion
}

/// <summary>
/// Unit tests for <see cref="BoolToStringConverter"/>.
/// </summary>
public class BoolToStringConverterTests
{
    private readonly BoolToStringConverter _converter = new();

    [Fact]
    public void Convert_TrueValue_ReturnsTrueString()
    {
        var result = _converter.Convert(true, typeof(string), "Yes|No", CultureInfo.InvariantCulture);
        Assert.Equal("Yes", result);
    }

    [Fact]
    public void Convert_FalseValue_ReturnsFalseString()
    {
        var result = _converter.Convert(false, typeof(string), "Yes|No", CultureInfo.InvariantCulture);
        Assert.Equal("No", result);
    }

    [Fact]
    public void Convert_NoParameter_ReturnsBoolString()
    {
        var result = _converter.Convert(true, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("True", result);
    }

    [Fact]
    public void Convert_InvalidParameter_ReturnsBoolString()
    {
        var result = _converter.Convert(true, typeof(string), "InvalidFormat", CultureInfo.InvariantCulture);
        Assert.Equal("True", result);
    }

    [Fact]
    public void Convert_NonBool_ReturnsValueString()
    {
        var result = _converter.Convert("test", typeof(string), "Yes|No", CultureInfo.InvariantCulture);
        Assert.Equal("test", result);
    }
}
