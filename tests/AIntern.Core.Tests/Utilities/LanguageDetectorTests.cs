using Xunit;
using AIntern.Core.Utilities;

namespace AIntern.Core.Tests.Utilities;

/// <summary>
/// Unit tests for the <see cref="LanguageDetector"/> class.
/// </summary>
public class LanguageDetectorTests
{
    #region DetectByExtension Tests

    /// <summary>
    /// Verifies that DetectByExtension returns correct language for common extensions.
    /// </summary>
    [Theory]
    [InlineData(".cs", "csharp")]
    [InlineData(".py", "python")]
    [InlineData(".ts", "typescript")]
    [InlineData(".tsx", "typescriptreact")]
    [InlineData(".js", "javascript")]
    [InlineData(".json", "json")]
    [InlineData(".rs", "rust")]
    [InlineData(".go", "go")]
    public void DetectByExtension_CommonExtensions_ReturnsCorrectLanguage(string extension, string expected)
    {
        // Act
        var result = LanguageDetector.DetectByExtension(extension);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Verifies that DetectByExtension is case-insensitive.
    /// </summary>
    [Theory]
    [InlineData(".CS", "csharp")]
    [InlineData(".Py", "python")]
    [InlineData(".JSON", "json")]
    public void DetectByExtension_CaseInsensitive(string extension, string expected)
    {
        // Act
        var result = LanguageDetector.DetectByExtension(extension);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Verifies that DetectByExtension returns null for unknown extensions.
    /// </summary>
    [Theory]
    [InlineData(".xyz")]
    [InlineData(".unknown")]
    [InlineData(".abc123")]
    public void DetectByExtension_UnknownExtension_ReturnsNull(string extension)
    {
        // Act
        var result = LanguageDetector.DetectByExtension(extension);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that DetectByExtension handles null and empty values.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DetectByExtension_NullOrEmpty_ReturnsNull(string? extension)
    {
        // Act
        var result = LanguageDetector.DetectByExtension(extension);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region DetectByFileName Tests

    /// <summary>
    /// Verifies that DetectByFileName recognizes special file names.
    /// </summary>
    [Theory]
    [InlineData("Dockerfile", "dockerfile")]
    [InlineData("Makefile", "makefile")]
    [InlineData("Gemfile", "ruby")]
    [InlineData("Rakefile", "ruby")]
    [InlineData("Jenkinsfile", "groovy")]
    public void DetectByFileName_SpecialFiles_ReturnsCorrectLanguage(string fileName, string expected)
    {
        // Act
        var result = LanguageDetector.DetectByFileName(fileName);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Verifies that DetectByFileName recognizes config files with extensions.
    /// </summary>
    [Theory]
    [InlineData("package.json", "json")]
    [InlineData("tsconfig.json", "jsonc")]
    [InlineData("Cargo.toml", "toml")]
    [InlineData("go.mod", "go")]
    [InlineData("requirements.txt", "pip-requirements")]
    public void DetectByFileName_ConfigFiles_ReturnsCorrectLanguage(string fileName, string expected)
    {
        // Act
        var result = LanguageDetector.DetectByFileName(fileName);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Verifies that DetectByFileName falls back to extension for regular files.
    /// </summary>
    [Theory]
    [InlineData("Program.cs", "csharp")]
    [InlineData("main.py", "python")]
    [InlineData("index.html", "html")]
    public void DetectByFileName_RegularFiles_FallsBackToExtension(string fileName, string expected)
    {
        // Act
        var result = LanguageDetector.DetectByFileName(fileName);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region DetectByPath Tests

    /// <summary>
    /// Verifies that DetectByPath extracts filename and detects correctly.
    /// </summary>
    [Theory]
    [InlineData("/home/user/project/src/Program.cs", "csharp")]
    [InlineData("/app/Dockerfile", "dockerfile")]
    [InlineData("C:\\Projects\\app\\package.json", "json")]
    public void DetectByPath_ExtractsFileNameAndDetects(string path, string expected)
    {
        // Act
        var result = LanguageDetector.DetectByPath(path);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Verifies that DetectByPath handles null and empty paths.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DetectByPath_NullOrEmpty_ReturnsNull(string? path)
    {
        // Act
        var result = LanguageDetector.DetectByPath(path);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetDisplayName Tests

    /// <summary>
    /// Verifies that GetDisplayName returns human-readable names.
    /// </summary>
    [Theory]
    [InlineData("csharp", "C#")]
    [InlineData("fsharp", "F#")]
    [InlineData("typescript", "TypeScript")]
    [InlineData("javascriptreact", "JavaScript (React)")]
    [InlineData("cpp", "C++")]
    [InlineData("shellscript", "Shell Script")]
    public void GetDisplayName_KnownLanguages_ReturnsHumanReadable(string language, string expected)
    {
        // Act
        var result = LanguageDetector.GetDisplayName(language);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Verifies that GetDisplayName capitalizes unknown languages.
    /// </summary>
    [Fact]
    public void GetDisplayName_UnknownLanguage_CapitalizesFirstLetter()
    {
        // Act
        var result = LanguageDetector.GetDisplayName("python");

        // Assert
        Assert.Equal("Python", result);
    }

    /// <summary>
    /// Verifies that GetDisplayName returns Plain Text for null/empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetDisplayName_NullOrEmpty_ReturnsPlainText(string? language)
    {
        // Act
        var result = LanguageDetector.GetDisplayName(language);

        // Assert
        Assert.Equal("Plain Text", result);
    }

    #endregion

    #region GetIconName Tests

    /// <summary>
    /// Verifies that GetIconName returns correct icon identifiers.
    /// </summary>
    [Theory]
    [InlineData("csharp", "dotnet")]
    [InlineData("fsharp", "dotnet")]
    [InlineData("javascript", "javascript")]
    [InlineData("typescript", "typescript")]
    [InlineData("python", "python")]
    [InlineData("dockerfile", "docker")]
    [InlineData("sql", "database")]
    public void GetIconName_ReturnsCorrectIcon(string language, string expected)
    {
        // Act
        var result = LanguageDetector.GetIconName(language);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Verifies that GetIconName returns "file" for unknown languages.
    /// </summary>
    [Theory]
    [InlineData("unknown")]
    [InlineData(null)]
    public void GetIconName_Unknown_ReturnsFile(string? language)
    {
        // Act
        var result = LanguageDetector.GetIconName(language);

        // Assert
        Assert.Equal("file", result);
    }

    #endregion

    #region Query Method Tests

    /// <summary>
    /// Verifies that GetAllSupportedExtensions returns expected count.
    /// </summary>
    [Fact]
    public void GetAllSupportedExtensions_ReturnsExpectedCount()
    {
        // Act
        var extensions = LanguageDetector.GetAllSupportedExtensions();

        // Assert
        Assert.True(extensions.Count >= 80, $"Expected 80+ extensions, got {extensions.Count}");
        Assert.Contains(".cs", extensions);
        Assert.Contains(".py", extensions);
        Assert.Contains(".js", extensions);
    }

    /// <summary>
    /// Verifies that GetAllSpecialFileNames contains expected files.
    /// </summary>
    [Fact]
    public void GetAllSpecialFileNames_ContainsExpectedFiles()
    {
        // Act
        var fileNames = LanguageDetector.GetAllSpecialFileNames();

        // Assert
        Assert.Contains("Dockerfile", fileNames);
        Assert.Contains("Makefile", fileNames);
        Assert.Contains("package.json", fileNames);
    }

    /// <summary>
    /// Verifies that GetAllLanguages returns unique languages.
    /// </summary>
    [Fact]
    public void GetAllLanguages_ReturnsUniqueList()
    {
        // Act
        var languages = LanguageDetector.GetAllLanguages();

        // Assert
        Assert.True(languages.Count > 30, $"Expected 30+ languages, got {languages.Count}");
        Assert.Contains("csharp", languages);
        Assert.Contains("python", languages);
        Assert.Equal(languages.Count, languages.Distinct().Count()); // All unique
    }

    /// <summary>
    /// Verifies that GetExtensionsForLanguage returns correct extensions.
    /// </summary>
    [Fact]
    public void GetExtensionsForLanguage_ReturnsAllMatching()
    {
        // Act
        var result = LanguageDetector.GetExtensionsForLanguage("csharp");

        // Assert
        Assert.Contains(".cs", result);
        Assert.Contains(".csx", result);
    }

    #endregion

    #region IsExtensionSupported Tests

    /// <summary>
    /// Verifies that IsExtensionSupported returns true for known extensions.
    /// </summary>
    [Theory]
    [InlineData(".cs", true)]
    [InlineData(".py", true)]
    [InlineData(".xyz", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsExtensionSupported_ReturnsCorrectResult(string? extension, bool expected)
    {
        // Act
        var result = LanguageDetector.IsExtensionSupported(extension);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region IsLikelyTextFile Tests

    /// <summary>
    /// Verifies that IsLikelyTextFile identifies text files.
    /// </summary>
    [Theory]
    [InlineData("readme.md", true)]
    [InlineData("data.csv", true)]
    [InlineData("Program.cs", true)]
    [InlineData("Dockerfile", true)]
    [InlineData("notes.txt", true)]
    public void IsLikelyTextFile_TextFiles_ReturnsTrue(string fileName, bool expected)
    {
        // Act
        var result = LanguageDetector.IsLikelyTextFile(fileName);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Verifies that IsLikelyTextFile returns false for binary files.
    /// </summary>
    [Theory]
    [InlineData("image.png")]
    [InlineData("archive.zip")]
    [InlineData("video.mp4")]
    public void IsLikelyTextFile_BinaryFiles_ReturnsFalse(string fileName)
    {
        // Act
        var result = LanguageDetector.IsLikelyTextFile(fileName);

        // Assert
        Assert.False(result);
    }

    #endregion
}
