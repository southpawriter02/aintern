using Xunit;
using AIntern.Desktop.Converters;
using Avalonia.Media;
using System.Globalization;

namespace AIntern.Desktop.Tests.Converters;

/// <summary>
/// Unit tests for IconColorConverter (v0.3.2e).
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
    public void Convert_DirectoryWithAnyIconKey_ReturnsFolderGold()
    {
        var values = new object?[] { "anything", true };
        
        var result = _converter.Convert(values, typeof(IBrush), null, CultureInfo.InvariantCulture);
        
        var brush = Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(Color.Parse("#E8A838"), brush.Color);
    }

    #endregion

    #region Language Color Tests

    [Theory]
    [InlineData("file-csharp", "#68217A")]
    [InlineData("file-javascript", "#F7DF1E")]
    [InlineData("file-typescript", "#3178C6")]
    [InlineData("file-python", "#3776AB")]
    [InlineData("file-html", "#E34F26")]
    [InlineData("file-git", "#F05032")]
    public void Convert_KnownIconKey_ReturnsLanguageColor(string iconKey, string expectedHex)
    {
        var values = new object?[] { iconKey, false };
        
        var result = _converter.Convert(values, typeof(IBrush), null, CultureInfo.InvariantCulture);
        
        var brush = Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(Color.Parse(expectedHex), brush.Color);
    }

    #endregion

    #region Fallback Tests

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

    #endregion

    #region Static Helper Tests

    [Fact]
    public void GetColorForIconKey_ReturnsCorrectColor()
    {
        var color = IconColorConverter.GetColorForIconKey("file-csharp");
        
        Assert.Equal(Color.Parse("#68217A"), color);
    }

    [Fact]
    public void GetColorForIconKey_WithDirectory_ReturnsFolderGold()
    {
        var color = IconColorConverter.GetColorForIconKey("file-csharp", isDirectory: true);
        
        Assert.Equal(Color.Parse("#E8A838"), color);
    }

    [Fact]
    public void GetAllColors_ReturnsAtLeast13Mappings()
    {
        var colors = IconColorConverter.GetAllColors();
        
        Assert.True(colors.Count >= 13);
        Assert.Contains("file-csharp", colors.Keys);
        Assert.Contains("file-typescript", colors.Keys);
    }

    #endregion

    #region Instance Tests

    [Fact]
    public void Instance_IsSingleton()
    {
        var instance1 = IconColorConverter.Instance;
        var instance2 = IconColorConverter.Instance;
        
        Assert.Same(instance1, instance2);
    }

    #endregion
}
