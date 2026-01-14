using Xunit;
using AIntern.Desktop.Converters;
using Avalonia.Media;
using System.Globalization;

namespace AIntern.Desktop.Tests.Converters;

/// <summary>
/// Unit tests for <see cref="IconColorConverter"/>.
/// </summary>
public class IconColorConverterTests
{
    private readonly IconColorConverter _converter = IconColorConverter.Instance;

    #region Folder Tests

    [Fact]
    public void Convert_Directory_ReturnsFolderGold()
    {
        var values = new object?[] { "folder", true };

        var result = _converter.Convert(values, typeof(IBrush), null, CultureInfo.InvariantCulture);

        var brush = Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(Color.Parse("#E8A838"), brush.Color);
    }

    [Fact]
    public void Convert_AnyIconKeyWithIsDirectoryTrue_ReturnsFolderGold()
    {
        var values = new object?[] { "file-csharp", true };

        var result = _converter.Convert(values, typeof(IBrush), null, CultureInfo.InvariantCulture);

        var brush = Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(Color.Parse("#E8A838"), brush.Color);
    }

    #endregion

    #region Language-Specific Color Tests

    [Theory]
    [InlineData("file-csharp", "#68217A")]
    [InlineData("file-javascript", "#F7DF1E")]
    [InlineData("file-typescript", "#3178C6")]
    [InlineData("file-python", "#3776AB")]
    [InlineData("file-html", "#E34F26")]
    [InlineData("file-css", "#1572B6")]
    [InlineData("file-git", "#F05032")]
    [InlineData("file-json", "#CBB078")]
    [InlineData("file-markdown", "#083FA1")]
    [InlineData("file-rust", "#DEA584")]
    [InlineData("file-go", "#00ADD8")]
    [InlineData("file-java", "#B07219")]
    [InlineData("file-shell", "#89E051")]
    public void Convert_KnownIconKey_ReturnsLanguageColor(string iconKey, string expectedHex)
    {
        var values = new object?[] { iconKey, false };

        var result = _converter.Convert(values, typeof(IBrush), null, CultureInfo.InvariantCulture);

        var brush = Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(Color.Parse(expectedHex), brush.Color);
    }

    #endregion

    #region Default/Fallback Tests

    [Fact]
    public void Convert_UnknownIconKey_ReturnsDefaultGray()
    {
        var values = new object?[] { "file-unknown", false };

        var result = _converter.Convert(values, typeof(IBrush), null, CultureInfo.InvariantCulture);

        var brush = Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(Color.Parse("#8B8B8B"), brush.Color);
    }

    [Fact]
    public void Convert_EmptyValues_ReturnsDefaultGray()
    {
        var values = new object?[] { };

        var result = _converter.Convert(values, typeof(IBrush), null, CultureInfo.InvariantCulture);

        var brush = Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(Color.Parse("#8B8B8B"), brush.Color);
    }

    [Fact]
    public void Convert_NullIconKey_ReturnsDefaultGray()
    {
        var values = new object?[] { null, false };

        var result = _converter.Convert(values, typeof(IBrush), null, CultureInfo.InvariantCulture);

        var brush = Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(Color.Parse("#8B8B8B"), brush.Color);
    }

    [Fact]
    public void Convert_SingleValue_ReturnsDefaultGray()
    {
        var values = new object?[] { "file-csharp" };

        var result = _converter.Convert(values, typeof(IBrush), null, CultureInfo.InvariantCulture);

        var brush = Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(Color.Parse("#8B8B8B"), brush.Color);
    }

    #endregion

    #region Static Helper Tests

    [Theory]
    [InlineData("file-csharp", false, "#68217A")]
    [InlineData("file-typescript", false, "#3178C6")]
    [InlineData("file-unknown", false, "#8B8B8B")]
    [InlineData("folder", true, "#E8A838")]
    [InlineData("file-csharp", true, "#E8A838")]  // Directory overrides iconKey
    public void GetColorForIconKey_ReturnsCorrectColor(string iconKey, bool isDirectory, string expectedHex)
    {
        var color = IconColorConverter.GetColorForIconKey(iconKey, isDirectory);

        Assert.Equal(Color.Parse(expectedHex), color);
    }

    [Fact]
    public void GetAllColors_ReturnsAllMappings()
    {
        var colors = IconColorConverter.GetAllColors();

        Assert.True(colors.Count >= 17);
        Assert.Contains("file-csharp", colors.Keys);
        Assert.Contains("file-typescript", colors.Keys);
        Assert.Contains("file-javascript", colors.Keys);
        Assert.Contains("file-python", colors.Keys);
    }

    [Fact]
    public void GetFolderColor_ReturnsGold()
    {
        var color = IconColorConverter.GetFolderColor();

        Assert.Equal(Color.Parse("#E8A838"), color);
    }

    [Fact]
    public void GetDefaultColor_ReturnsGray()
    {
        var color = IconColorConverter.GetDefaultColor();

        Assert.Equal(Color.Parse("#8B8B8B"), color);
    }

    [Fact]
    public void Instance_ReturnsSameInstance()
    {
        var instance1 = IconColorConverter.Instance;
        var instance2 = IconColorConverter.Instance;

        Assert.Same(instance1, instance2);
    }

    #endregion
}
