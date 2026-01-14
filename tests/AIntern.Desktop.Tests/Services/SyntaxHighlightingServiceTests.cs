using AIntern.Desktop.Services;
using TextMateSharp.Grammars;
using Xunit;

namespace AIntern.Desktop.Tests.Services;

/// <summary>
/// Unit tests for <see cref="SyntaxHighlightingService"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tests cover:
/// </para>
/// <list type="bullet">
///   <item><description>Constructor configuration</description></item>
///   <item><description>Language and scope mapping</description></item>
///   <item><description>Theme management</description></item>
///   <item><description>Dispose behavior</description></item>
/// </list>
/// <para>
/// Note: Tests that require actual TextEditor instances are excluded
/// as they require Avalonia platform initialization.
/// </para>
/// <para>Added in v0.3.3c.</para>
/// </remarks>
public class SyntaxHighlightingServiceTests : IDisposable
{
    private SyntaxHighlightingService? _service;

    public void Dispose()
    {
        _service?.Dispose();
    }

    #region Constructor Tests

    /// <summary>
    /// Verifies constructor defaults to dark theme.
    /// </summary>
    [Fact]
    public void Constructor_DefaultsToDarkTheme()
    {
        // Act
        _service = new SyntaxHighlightingService();

        // Assert
        Assert.Equal(ThemeName.DarkPlus, _service.CurrentTheme);
    }

    /// <summary>
    /// Verifies constructor respects useDarkTheme parameter.
    /// </summary>
    [Fact]
    public void Constructor_RespectsLightThemeParameter()
    {
        // Act
        _service = new SyntaxHighlightingService(useDarkTheme: false);

        // Assert
        Assert.Equal(ThemeName.LightPlus, _service.CurrentTheme);
    }

    /// <summary>
    /// Verifies constructor initializes registry options.
    /// </summary>
    [Fact]
    public void Constructor_InitializesRegistryOptions()
    {
        // Act
        _service = new SyntaxHighlightingService();

        // Assert
        Assert.NotNull(_service.RegistryOptions);
    }

    #endregion

    #region SupportedLanguages Tests

    /// <summary>
    /// Verifies SupportedLanguages contains expected count.
    /// </summary>
    [Fact]
    public void SupportedLanguages_ContainsAtLeast46Languages()
    {
        // Arrange
        _service = new SyntaxHighlightingService();

        // Assert
        Assert.True(_service.SupportedLanguages.Count >= 46,
            $"Expected at least 46 languages, got {_service.SupportedLanguages.Count}");
    }

    /// <summary>
    /// Verifies SupportedLanguages contains essential languages.
    /// </summary>
    [Theory]
    [InlineData("csharp")]
    [InlineData("javascript")]
    [InlineData("typescript")]
    [InlineData("python")]
    [InlineData("java")]
    [InlineData("html")]
    [InlineData("css")]
    [InlineData("json")]
    [InlineData("markdown")]
    public void SupportedLanguages_ContainsEssentialLanguages(string language)
    {
        // Arrange
        _service = new SyntaxHighlightingService();

        // Assert
        Assert.Contains(language, _service.SupportedLanguages);
    }

    #endregion

    #region IsLanguageSupported Tests

    /// <summary>
    /// Verifies IsLanguageSupported returns true for supported language.
    /// </summary>
    [Fact]
    public void IsLanguageSupported_ReturnsTrue_ForSupportedLanguage()
    {
        // Arrange
        _service = new SyntaxHighlightingService();

        // Act & Assert
        Assert.True(_service.IsLanguageSupported("csharp"));
        Assert.True(_service.IsLanguageSupported("javascript"));
        Assert.True(_service.IsLanguageSupported("python"));
    }

    /// <summary>
    /// Verifies IsLanguageSupported returns false for unsupported language.
    /// </summary>
    [Fact]
    public void IsLanguageSupported_ReturnsFalse_ForUnsupportedLanguage()
    {
        // Arrange
        _service = new SyntaxHighlightingService();

        // Act & Assert
        Assert.False(_service.IsLanguageSupported("nonexistent"));
        Assert.False(_service.IsLanguageSupported("fake_lang"));
    }

    /// <summary>
    /// Verifies IsLanguageSupported is case-insensitive.
    /// </summary>
    [Fact]
    public void IsLanguageSupported_IsCaseInsensitive()
    {
        // Arrange
        _service = new SyntaxHighlightingService();

        // Act & Assert
        Assert.True(_service.IsLanguageSupported("CSharp"));
        Assert.True(_service.IsLanguageSupported("PYTHON"));
        Assert.True(_service.IsLanguageSupported("JavaScript"));
    }

    #endregion

    #region GetScopeForLanguage Tests

    /// <summary>
    /// Verifies GetScopeForLanguage returns correct scope.
    /// </summary>
    [Theory]
    [InlineData("csharp", "source.cs")]
    [InlineData("javascript", "source.js")]
    [InlineData("typescript", "source.ts")]
    [InlineData("python", "source.python")]
    [InlineData("html", "text.html.basic")]
    [InlineData("json", "source.json")]
    [InlineData("markdown", "text.html.markdown")]
    public void GetScopeForLanguage_ReturnsCorrectScope(string language, string expectedScope)
    {
        // Arrange
        _service = new SyntaxHighlightingService();

        // Act
        var scope = _service.GetScopeForLanguage(language);

        // Assert
        Assert.Equal(expectedScope, scope);
    }

    /// <summary>
    /// Verifies GetScopeForLanguage returns null for unknown language.
    /// </summary>
    [Fact]
    public void GetScopeForLanguage_ReturnsNull_ForUnknownLanguage()
    {
        // Arrange
        _service = new SyntaxHighlightingService();

        // Act
        var scope = _service.GetScopeForLanguage("unknown_language");

        // Assert
        Assert.Null(scope);
    }

    #endregion

    #region AvailableThemes Tests

    /// <summary>
    /// Verifies AvailableThemes contains 7 themes.
    /// </summary>
    [Fact]
    public void AvailableThemes_ContainsSevenThemes()
    {
        // Assert
        Assert.Equal(7, SyntaxHighlightingService.AvailableThemes.Count);
    }

    /// <summary>
    /// Verifies AvailableThemes contains expected themes.
    /// </summary>
    [Fact]
    public void AvailableThemes_ContainsExpectedThemes()
    {
        // Assert
        Assert.Contains(ThemeName.DarkPlus, SyntaxHighlightingService.AvailableThemes);
        Assert.Contains(ThemeName.LightPlus, SyntaxHighlightingService.AvailableThemes);
        Assert.Contains(ThemeName.Monokai, SyntaxHighlightingService.AvailableThemes);
        Assert.Contains(ThemeName.SolarizedDark, SyntaxHighlightingService.AvailableThemes);
        Assert.Contains(ThemeName.SolarizedLight, SyntaxHighlightingService.AvailableThemes);
    }

    #endregion

    #region RegisteredEditorCount Tests

    /// <summary>
    /// Verifies RegisteredEditorCount starts at zero.
    /// </summary>
    [Fact]
    public void RegisteredEditorCount_StartsAtZero()
    {
        // Arrange
        _service = new SyntaxHighlightingService();

        // Assert
        Assert.Equal(0, _service.RegisteredEditorCount);
    }

    #endregion

    #region Dispose Tests

    /// <summary>
    /// Verifies Dispose clears installations.
    /// </summary>
    [Fact]
    public void Dispose_ClearsInstallations()
    {
        // Arrange
        _service = new SyntaxHighlightingService();

        // Act
        _service.Dispose();

        // Assert - no exceptions and count is 0
        Assert.Equal(0, _service.RegisteredEditorCount);
    }

    /// <summary>
    /// Verifies Dispose is safe to call multiple times.
    /// </summary>
    [Fact]
    public void Dispose_SafeToCallMultipleTimes()
    {
        // Arrange
        _service = new SyntaxHighlightingService();

        // Act & Assert - no exceptions
        _service.Dispose();
        _service.Dispose();
        _service.Dispose();
    }

    #endregion

    #region Language Category Tests

    /// <summary>
    /// Verifies .NET languages are supported.
    /// </summary>
    [Theory]
    [InlineData("csharp")]
    [InlineData("fsharp")]
    [InlineData("vb")]
    public void Supports_DotNetLanguages(string language)
    {
        // Arrange
        _service = new SyntaxHighlightingService();

        // Assert
        Assert.True(_service.IsLanguageSupported(language));
    }

    /// <summary>
    /// Verifies web languages are supported.
    /// </summary>
    [Theory]
    [InlineData("javascript")]
    [InlineData("typescript")]
    [InlineData("html")]
    [InlineData("css")]
    [InlineData("vue")]
    public void Supports_WebLanguages(string language)
    {
        // Arrange
        _service = new SyntaxHighlightingService();

        // Assert
        Assert.True(_service.IsLanguageSupported(language));
    }

    /// <summary>
    /// Verifies systems languages are supported.
    /// </summary>
    [Theory]
    [InlineData("c")]
    [InlineData("cpp")]
    [InlineData("rust")]
    [InlineData("go")]
    [InlineData("swift")]
    public void Supports_SystemsLanguages(string language)
    {
        // Arrange
        _service = new SyntaxHighlightingService();

        // Assert
        Assert.True(_service.IsLanguageSupported(language));
    }

    /// <summary>
    /// Verifies data/config languages are supported.
    /// </summary>
    [Theory]
    [InlineData("json")]
    [InlineData("xml")]
    [InlineData("yaml")]
    [InlineData("toml")]
    public void Supports_DataConfigLanguages(string language)
    {
        // Arrange
        _service = new SyntaxHighlightingService();

        // Assert
        Assert.True(_service.IsLanguageSupported(language));
    }

    #endregion
}
