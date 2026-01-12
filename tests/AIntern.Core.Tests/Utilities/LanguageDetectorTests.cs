using Xunit;
using AIntern.Core.Utilities;

namespace AIntern.Core.Tests.Utilities;

/// <summary>
/// Unit tests for LanguageDetector (v0.3.1c).
/// </summary>
public class LanguageDetectorTests
{
    #region DetectByExtension Tests

    [Theory]
    [InlineData(".cs", "csharp")]
    [InlineData(".CS", "csharp")]  // Case insensitive
    [InlineData(".Cs", "csharp")]  // Mixed case
    [InlineData(".py", "python")]
    [InlineData(".ts", "typescript")]
    [InlineData(".tsx", "typescriptreact")]
    [InlineData(".axaml", "xml")]
    [InlineData(".js", "javascript")]
    [InlineData(".jsx", "javascriptreact")]
    [InlineData(".rs", "rust")]
    [InlineData(".go", "go")]
    [InlineData(".md", "markdown")]
    [InlineData(".json", "json")]
    [InlineData(".yaml", "yaml")]
    [InlineData(".yml", "yaml")]
    public void DetectByExtension_ReturnsCorrectLanguage(string extension, string expected)
    {
        var result = LanguageDetector.DetectByExtension(extension);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DetectByExtension_UnknownExtension_ReturnsNull()
    {
        Assert.Null(LanguageDetector.DetectByExtension(".xyz"));
        Assert.Null(LanguageDetector.DetectByExtension(".unknown"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void DetectByExtension_NullOrEmpty_ReturnsNull(string? extension)
    {
        Assert.Null(LanguageDetector.DetectByExtension(extension));
    }

    #endregion

    #region DetectByFileName Tests

    [Theory]
    [InlineData("Dockerfile", "dockerfile")]
    [InlineData("dockerfile", "dockerfile")]  // Case insensitive
    [InlineData("Makefile", "makefile")]
    [InlineData("makefile", "makefile")]
    [InlineData("GNUmakefile", "makefile")]
    [InlineData("package.json", "json")]
    [InlineData("tsconfig.json", "jsonc")]
    [InlineData("jsconfig.json", "jsonc")]
    [InlineData("Cargo.toml", "toml")]
    [InlineData("go.mod", "go")]
    [InlineData("requirements.txt", "pip-requirements")]
    [InlineData("Gemfile", "ruby")]
    [InlineData("Rakefile", "ruby")]
    public void DetectByFileName_SpecialFiles_ReturnsCorrectLanguage(string fileName, string expected)
    {
        var result = LanguageDetector.DetectByFileName(fileName);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DetectByFileName_RegularFile_FallsBackToExtension()
    {
        Assert.Equal("csharp", LanguageDetector.DetectByFileName("Program.cs"));
        Assert.Equal("python", LanguageDetector.DetectByFileName("main.py"));
        Assert.Equal("typescript", LanguageDetector.DetectByFileName("index.ts"));
        Assert.Equal("javascript", LanguageDetector.DetectByFileName("app.js"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void DetectByFileName_NullOrEmpty_ReturnsNull(string? fileName)
    {
        Assert.Null(LanguageDetector.DetectByFileName(fileName));
    }

    [Fact]
    public void DetectByFileName_UnknownFile_ReturnsNull()
    {
        Assert.Null(LanguageDetector.DetectByFileName("unknownfile"));
        Assert.Null(LanguageDetector.DetectByFileName("file.unknown"));
    }

    #endregion

    #region DetectByPath Tests

    [Fact]
    public void DetectByPath_ExtractsFileNameAndDetects()
    {
        Assert.Equal("csharp", LanguageDetector.DetectByPath("/home/user/project/src/Program.cs"));
        Assert.Equal("dockerfile", LanguageDetector.DetectByPath("/app/Dockerfile"));
        Assert.Equal("python", LanguageDetector.DetectByPath("C:\\Users\\Dev\\main.py"));
        Assert.Equal("makefile", LanguageDetector.DetectByPath("/project/Makefile"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void DetectByPath_NullOrEmpty_ReturnsNull(string? path)
    {
        Assert.Null(LanguageDetector.DetectByPath(path));
    }

    #endregion

    #region GetDisplayName Tests

    [Theory]
    [InlineData("csharp", "C#")]
    [InlineData("fsharp", "F#")]
    [InlineData("typescript", "TypeScript")]
    [InlineData("javascript", "JavaScript")]
    [InlineData("javascriptreact", "JavaScript (React)")]
    [InlineData("typescriptreact", "TypeScript (React)")]
    [InlineData("cpp", "C++")]
    [InlineData("c", "C")]
    [InlineData("python", "Python")]  // Capitalized fallback
    [InlineData("rust", "Rust")]  // Capitalized fallback
    [InlineData("shellscript", "Shell Script")]
    [InlineData("dockerfile", "Dockerfile")]
    [InlineData("markdown", "Markdown")]
    public void GetDisplayName_ReturnsHumanReadableName(string language, string expected)
    {
        Assert.Equal(expected, LanguageDetector.GetDisplayName(language));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetDisplayName_NullOrEmpty_ReturnsPlainText(string? language)
    {
        Assert.Equal("Plain Text", LanguageDetector.GetDisplayName(language));
    }

    [Fact]
    public void GetDisplayName_UnknownLanguage_CapitalizesFirstLetter()
    {
        Assert.Equal("Customlang", LanguageDetector.GetDisplayName("customlang"));
    }

    #endregion

    #region Query Method Tests

    [Fact]
    public void GetAllSupportedExtensions_ContainsCommonExtensions()
    {
        var extensions = LanguageDetector.GetAllSupportedExtensions();

        Assert.Contains(".cs", extensions);
        Assert.Contains(".py", extensions);
        Assert.Contains(".js", extensions);
        Assert.Contains(".ts", extensions);
        Assert.Contains(".json", extensions);
        Assert.Contains(".md", extensions);
        Assert.Contains(".rs", extensions);
        Assert.Contains(".go", extensions);
    }

    [Fact]
    public void GetAllSupportedExtensions_HasExpectedCount()
    {
        var extensions = LanguageDetector.GetAllSupportedExtensions();
        Assert.True(extensions.Count >= 80, $"Expected at least 80 extensions, got {extensions.Count}");
    }

    [Fact]
    public void GetAllSpecialFileNames_ContainsCommonFiles()
    {
        var fileNames = LanguageDetector.GetAllSpecialFileNames();

        Assert.Contains("Dockerfile", fileNames);
        Assert.Contains("Makefile", fileNames);
        Assert.Contains("package.json", fileNames);
        Assert.Contains("Cargo.toml", fileNames);
        Assert.Contains("go.mod", fileNames);
    }

    [Fact]
    public void GetAllLanguages_ReturnsUniqueList()
    {
        var languages = LanguageDetector.GetAllLanguages();

        Assert.Contains("csharp", languages);
        Assert.Contains("python", languages);
        Assert.Contains("javascript", languages);
        Assert.Contains("dockerfile", languages);

        // Verify uniqueness
        Assert.Equal(languages.Count, languages.Distinct().Count());
    }

    [Fact]
    public void GetExtensionsForLanguage_ReturnsAllMatching()
    {
        var csharpExtensions = LanguageDetector.GetExtensionsForLanguage("csharp");
        Assert.Contains(".cs", csharpExtensions);
        Assert.Contains(".csx", csharpExtensions);

        var pythonExtensions = LanguageDetector.GetExtensionsForLanguage("python");
        Assert.Contains(".py", pythonExtensions);
        Assert.Contains(".pyi", pythonExtensions);

        var yamlExtensions = LanguageDetector.GetExtensionsForLanguage("yaml");
        Assert.Contains(".yaml", yamlExtensions);
        Assert.Contains(".yml", yamlExtensions);
    }

    [Fact]
    public void GetExtensionsForLanguage_CaseInsensitive()
    {
        var csharpLower = LanguageDetector.GetExtensionsForLanguage("csharp");
        var csharpUpper = LanguageDetector.GetExtensionsForLanguage("CSHARP");
        var csharpMixed = LanguageDetector.GetExtensionsForLanguage("CSharp");

        Assert.Equal(csharpLower, csharpUpper);
        Assert.Equal(csharpLower, csharpMixed);
    }

    #endregion

    #region Utility Method Tests

    [Fact]
    public void IsExtensionSupported_ReturnsTrueForKnownExtensions()
    {
        Assert.True(LanguageDetector.IsExtensionSupported(".cs"));
        Assert.True(LanguageDetector.IsExtensionSupported(".py"));
        Assert.True(LanguageDetector.IsExtensionSupported(".js"));
        Assert.True(LanguageDetector.IsExtensionSupported(".CS"));  // Case insensitive
    }

    [Fact]
    public void IsExtensionSupported_ReturnsFalseForUnknownExtensions()
    {
        Assert.False(LanguageDetector.IsExtensionSupported(".xyz"));
        Assert.False(LanguageDetector.IsExtensionSupported(".unknown"));
        Assert.False(LanguageDetector.IsExtensionSupported(null));
        Assert.False(LanguageDetector.IsExtensionSupported(""));
    }

    [Fact]
    public void IsLikelyTextFile_IdentifiesTextFiles()
    {
        // Known language files
        Assert.True(LanguageDetector.IsLikelyTextFile("readme.md"));
        Assert.True(LanguageDetector.IsLikelyTextFile("Program.cs"));
        Assert.True(LanguageDetector.IsLikelyTextFile("Dockerfile"));
        Assert.True(LanguageDetector.IsLikelyTextFile("config.json"));

        // Common text extensions not in language map
        Assert.True(LanguageDetector.IsLikelyTextFile("data.csv"));
        Assert.True(LanguageDetector.IsLikelyTextFile("output.txt"));
        Assert.True(LanguageDetector.IsLikelyTextFile("app.log"));
        Assert.True(LanguageDetector.IsLikelyTextFile("changes.diff"));
    }

    [Fact]
    public void IsLikelyTextFile_ReturnsFalseForBinaryFiles()
    {
        Assert.False(LanguageDetector.IsLikelyTextFile("image.png"));
        Assert.False(LanguageDetector.IsLikelyTextFile("photo.jpg"));
        Assert.False(LanguageDetector.IsLikelyTextFile("archive.zip"));
        Assert.False(LanguageDetector.IsLikelyTextFile("binary.exe"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsLikelyTextFile_ReturnsFalseForNullOrEmpty(string? fileName)
    {
        Assert.False(LanguageDetector.IsLikelyTextFile(fileName));
    }

    #endregion

    #region GetIconName Tests

    [Theory]
    [InlineData("csharp", "dotnet")]
    [InlineData("fsharp", "dotnet")]
    [InlineData("javascript", "javascript")]
    [InlineData("typescript", "typescript")]
    [InlineData("python", "python")]
    [InlineData("rust", "rust")]
    [InlineData("go", "go")]
    [InlineData("dockerfile", "docker")]
    [InlineData("markdown", "markdown")]
    [InlineData("json", "json")]
    [InlineData("sql", "database")]
    [InlineData("unknown", "file")]  // Default fallback
    [InlineData(null, "file")]  // Null fallback
    public void GetIconName_ReturnsCorrectIcon(string? language, string expected)
    {
        Assert.Equal(expected, LanguageDetector.GetIconName(language));
    }

    #endregion
}
