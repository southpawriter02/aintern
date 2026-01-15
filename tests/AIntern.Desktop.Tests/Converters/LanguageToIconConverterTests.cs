namespace AIntern.Desktop.Tests.Converters;

using System;
using System.Globalization;
using AIntern.Desktop.Converters;
using Xunit;

/// <summary>
/// Unit tests for <see cref="LanguageToIconConverter"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tests verify the language-to-icon mapping logic. Since StreamGeometry resources
/// are not available in unit tests (no Avalonia Application), we test the conversion
/// behavior and null handling.
/// </para>
/// <para>Added in v0.3.3e.</para>
/// </remarks>
public class LanguageToIconConverterTests
{
    private readonly LanguageToIconConverter _converter = new();

    #region Instance Tests

    /// <summary>
    /// Verifies that the singleton Instance property returns a valid converter.
    /// </summary>
    [Fact]
    public void Instance_ReturnsNonNullConverter()
    {
        // Act
        var instance = LanguageToIconConverter.Instance;

        // Assert
        Assert.NotNull(instance);
    }

    /// <summary>
    /// Verifies that Instance always returns the same object.
    /// </summary>
    [Fact]
    public void Instance_ReturnsSameInstance()
    {
        // Act
        var instance1 = LanguageToIconConverter.Instance;
        var instance2 = LanguageToIconConverter.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    #endregion

    #region Convert Method - Null Handling

    /// <summary>
    /// Verifies that null input returns a default icon (null when resources unavailable).
    /// </summary>
    [Fact]
    public void Convert_NullValue_ReturnsNull()
    {
        // Act - Resources not loaded in tests, so GetGeometry returns null
        var result = _converter.Convert(null, typeof(object), null, CultureInfo.InvariantCulture);

        // Assert - Without Avalonia resources, we get null
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that non-string input returns default icon.
    /// </summary>
    [Fact]
    public void Convert_NonStringValue_ReturnsNull()
    {
        // Act
        var result = _converter.Convert(123, typeof(object), null, CultureInfo.InvariantCulture);

        // Assert - Non-string doesn't match any language, falls through to default
        Assert.Null(result);
    }

    #endregion

    #region Convert Method - Language Mapping

    /// <summary>
    /// Verifies that known languages are processed without throwing.
    /// </summary>
    /// <param name="language">Language identifier to test.</param>
    [Theory]
    [InlineData("csharp")]
    [InlineData("CSharp")]
    [InlineData("CSHARP")]
    [InlineData("javascript")]
    [InlineData("javascriptreact")]
    [InlineData("typescript")]
    [InlineData("typescriptreact")]
    [InlineData("python")]
    [InlineData("html")]
    [InlineData("css")]
    [InlineData("scss")]
    [InlineData("less")]
    [InlineData("json")]
    [InlineData("jsonc")]
    [InlineData("markdown")]
    [InlineData("xml")]
    [InlineData("yaml")]
    [InlineData("toml")]
    [InlineData("rust")]
    [InlineData("go")]
    [InlineData("java")]
    [InlineData("kotlin")]
    [InlineData("shellscript")]
    [InlineData("bash")]
    [InlineData("powershell")]
    [InlineData("dockerfile")]
    public void Convert_KnownLanguage_DoesNotThrow(string language)
    {
        // Act - Should not throw even if resources aren't loaded
        var exception = Record.Exception(() =>
            _converter.Convert(language, typeof(object), null, CultureInfo.InvariantCulture));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies that unknown languages fall back to default icon.
    /// </summary>
    [Theory]
    [InlineData("unknown")]
    [InlineData("cobol")]
    [InlineData("fortran")]
    [InlineData("randomlanguage")]
    [InlineData("")]
    public void Convert_UnknownLanguage_ReturnsNull(string language)
    {
        // Act
        var result = _converter.Convert(language, typeof(object), null, CultureInfo.InvariantCulture);

        // Assert - Falls back to FileIcon which is null without resources
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies case-insensitivity of language matching.
    /// </summary>
    [Theory]
    [InlineData("CSharp")]
    [InlineData("JAVASCRIPT")]
    [InlineData("Python")]
    [InlineData("HTML")]
    public void Convert_MixedCaseLanguage_DoesNotThrow(string language)
    {
        // Act
        var exception = Record.Exception(() =>
            _converter.Convert(language, typeof(object), null, CultureInfo.InvariantCulture));

        // Assert
        Assert.Null(exception);
    }

    #endregion

    #region ConvertBack Method

    /// <summary>
    /// Verifies that ConvertBack throws NotSupportedException.
    /// </summary>
    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        // Act & Assert
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack(null, typeof(string), null, CultureInfo.InvariantCulture));
    }

    #endregion
}
