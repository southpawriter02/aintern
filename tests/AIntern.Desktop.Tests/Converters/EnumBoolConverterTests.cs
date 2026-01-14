// -----------------------------------------------------------------------
// <copyright file="EnumBoolConverterTests.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
// </copyright>
// <summary>
//     Unit tests for EnumBoolConverter.
//     Added in v0.2.5f.
// </summary>
// -----------------------------------------------------------------------

using System.Globalization;
using AIntern.Core.Enums;
using AIntern.Desktop.Converters;
using Avalonia.Data;
using Xunit;

namespace AIntern.Desktop.Tests.Converters;

/// <summary>
/// Unit tests for <see cref="EnumBoolConverter"/>.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify:
/// </para>
/// <list type="bullet">
///   <item><description>Singleton instance availability</description></item>
///   <item><description>Convert returns true when enum matches parameter</description></item>
///   <item><description>Convert returns false when enum doesn't match parameter</description></item>
///   <item><description>Convert handles null value and null parameter</description></item>
///   <item><description>ConvertBack parses enum when checked</description></item>
///   <item><description>ConvertBack returns DoNothing when unchecked</description></item>
///   <item><description>ConvertBack handles null parameter</description></item>
/// </list>
/// <para>Added in v0.2.5f.</para>
/// </remarks>
public class EnumBoolConverterTests
{
    #region Instance Tests

    /// <summary>
    /// Verifies Instance returns non-null singleton.
    /// </summary>
    [Fact]
    public void Instance_ReturnsNonNull()
    {
        // Act
        var instance = EnumBoolConverter.Instance;

        // Assert
        Assert.NotNull(instance);
    }

    /// <summary>
    /// Verifies Instance returns same singleton each time.
    /// </summary>
    [Fact]
    public void Instance_ReturnsSameSingleton()
    {
        // Act
        var instance1 = EnumBoolConverter.Instance;
        var instance2 = EnumBoolConverter.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    #endregion

    #region Convert Tests

    /// <summary>
    /// Verifies Convert returns true when enum value matches parameter.
    /// </summary>
    [Fact]
    public void Convert_EnumMatchesParameter_ReturnsTrue()
    {
        // Arrange
        var converter = EnumBoolConverter.Instance;
        var value = ExportFormat.Markdown;

        // Act
        var result = converter.Convert(value, typeof(bool), "Markdown", CultureInfo.InvariantCulture);

        // Assert
        Assert.True((bool)result!);
    }

    /// <summary>
    /// Verifies Convert returns false when enum value doesn't match parameter.
    /// </summary>
    [Fact]
    public void Convert_EnumDoesNotMatchParameter_ReturnsFalse()
    {
        // Arrange
        var converter = EnumBoolConverter.Instance;
        var value = ExportFormat.Json;

        // Act
        var result = converter.Convert(value, typeof(bool), "Markdown", CultureInfo.InvariantCulture);

        // Assert
        Assert.False((bool)result!);
    }

    /// <summary>
    /// Verifies Convert is case-insensitive when comparing parameter.
    /// </summary>
    [Theory]
    [InlineData("markdown")]
    [InlineData("MARKDOWN")]
    [InlineData("Markdown")]
    public void Convert_CaseInsensitiveMatch_ReturnsTrue(string parameter)
    {
        // Arrange
        var converter = EnumBoolConverter.Instance;
        var value = ExportFormat.Markdown;

        // Act
        var result = converter.Convert(value, typeof(bool), parameter, CultureInfo.InvariantCulture);

        // Assert
        Assert.True((bool)result!);
    }

    /// <summary>
    /// Verifies Convert returns false for null value.
    /// </summary>
    [Fact]
    public void Convert_NullValue_ReturnsFalse()
    {
        // Arrange
        var converter = EnumBoolConverter.Instance;

        // Act
        var result = converter.Convert(null, typeof(bool), "Markdown", CultureInfo.InvariantCulture);

        // Assert
        Assert.False((bool)result!);
    }

    /// <summary>
    /// Verifies Convert returns false for null parameter.
    /// </summary>
    [Fact]
    public void Convert_NullParameter_ReturnsFalse()
    {
        // Arrange
        var converter = EnumBoolConverter.Instance;
        var value = ExportFormat.Markdown;

        // Act
        var result = converter.Convert(value, typeof(bool), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.False((bool)result!);
    }

    /// <summary>
    /// Verifies Convert works with all ExportFormat values.
    /// </summary>
    [Theory]
    [InlineData(ExportFormat.Markdown, "Markdown")]
    [InlineData(ExportFormat.Json, "Json")]
    [InlineData(ExportFormat.PlainText, "PlainText")]
    [InlineData(ExportFormat.Html, "Html")]
    public void Convert_AllExportFormats_ReturnsTrue(ExportFormat format, string parameter)
    {
        // Arrange
        var converter = EnumBoolConverter.Instance;

        // Act
        var result = converter.Convert(format, typeof(bool), parameter, CultureInfo.InvariantCulture);

        // Assert
        Assert.True((bool)result!);
    }

    #endregion

    #region ConvertBack Tests

    /// <summary>
    /// Verifies ConvertBack parses enum when IsChecked is true.
    /// </summary>
    [Fact]
    public void ConvertBack_IsCheckedTrue_ParsesEnum()
    {
        // Arrange
        var converter = EnumBoolConverter.Instance;

        // Act
        var result = converter.ConvertBack(true, typeof(ExportFormat), "Json", CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(ExportFormat.Json, result);
    }

    /// <summary>
    /// Verifies ConvertBack returns DoNothing when IsChecked is false.
    /// </summary>
    [Fact]
    public void ConvertBack_IsCheckedFalse_ReturnsDoNothing()
    {
        // Arrange
        var converter = EnumBoolConverter.Instance;

        // Act
        var result = converter.ConvertBack(false, typeof(ExportFormat), "Json", CultureInfo.InvariantCulture);

        // Assert
        Assert.Same(BindingOperations.DoNothing, result);
    }

    /// <summary>
    /// Verifies ConvertBack returns DoNothing for null value.
    /// </summary>
    [Fact]
    public void ConvertBack_NullValue_ReturnsDoNothing()
    {
        // Arrange
        var converter = EnumBoolConverter.Instance;

        // Act
        var result = converter.ConvertBack(null, typeof(ExportFormat), "Json", CultureInfo.InvariantCulture);

        // Assert
        Assert.Same(BindingOperations.DoNothing, result);
    }

    /// <summary>
    /// Verifies ConvertBack returns DoNothing for null parameter.
    /// </summary>
    [Fact]
    public void ConvertBack_NullParameter_ReturnsDoNothing()
    {
        // Arrange
        var converter = EnumBoolConverter.Instance;

        // Act
        var result = converter.ConvertBack(true, typeof(ExportFormat), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Same(BindingOperations.DoNothing, result);
    }

    /// <summary>
    /// Verifies ConvertBack returns DoNothing for non-boolean value.
    /// </summary>
    [Theory]
    [InlineData("true")]
    [InlineData(1)]
    [InlineData("Json")]
    public void ConvertBack_NonBooleanValue_ReturnsDoNothing(object value)
    {
        // Arrange
        var converter = EnumBoolConverter.Instance;

        // Act
        var result = converter.ConvertBack(value, typeof(ExportFormat), "Json", CultureInfo.InvariantCulture);

        // Assert
        Assert.Same(BindingOperations.DoNothing, result);
    }

    /// <summary>
    /// Verifies ConvertBack works with all ExportFormat values.
    /// </summary>
    [Theory]
    [InlineData("Markdown", ExportFormat.Markdown)]
    [InlineData("Json", ExportFormat.Json)]
    [InlineData("PlainText", ExportFormat.PlainText)]
    [InlineData("Html", ExportFormat.Html)]
    public void ConvertBack_AllExportFormats_ParsesCorrectly(string parameter, ExportFormat expected)
    {
        // Arrange
        var converter = EnumBoolConverter.Instance;

        // Act
        var result = converter.ConvertBack(true, typeof(ExportFormat), parameter, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion
}
