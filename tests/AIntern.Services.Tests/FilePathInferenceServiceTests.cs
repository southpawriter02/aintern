namespace AIntern.Services.Tests;

using Moq;
using Microsoft.Extensions.Logging;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for FilePathInferenceService (v0.4.1e).
/// </summary>
public class FilePathInferenceServiceTests
{
    private readonly Mock<ILanguageDetectionService> _mockLanguageService;
    private readonly Mock<ILogger<FilePathInferenceService>> _mockLogger;
    private readonly FilePathInferenceService _service;

    public FilePathInferenceServiceTests()
    {
        _mockLanguageService = new Mock<ILanguageDetectionService>();
        _mockLogger = new Mock<ILogger<FilePathInferenceService>>();
        SetupDefaultLanguageMappings();
        _service = new FilePathInferenceService(
            _mockLanguageService.Object,
            _mockLogger.Object);
    }

    private void SetupDefaultLanguageMappings()
    {
        _mockLanguageService.Setup(s => s.GetFileExtension("csharp")).Returns(".cs");
        _mockLanguageService.Setup(s => s.GetFileExtension("typescript")).Returns(".ts");
        _mockLanguageService.Setup(s => s.GetFileExtension("python")).Returns(".py");
        _mockLanguageService.Setup(s => s.GetFileExtension("go")).Returns(".go");
        _mockLanguageService.Setup(s => s.GetFileExtension("rust")).Returns(".rs");
        _mockLanguageService.Setup(s => s.GetFileExtension("java")).Returns(".java");
        _mockLanguageService.Setup(s => s.GetLanguageForExtension(".cs")).Returns("csharp");
        _mockLanguageService.Setup(s => s.GetLanguageForExtension(".ts")).Returns("typescript");
        _mockLanguageService.Setup(s => s.GetLanguageForExtension(".py")).Returns("python");
        _mockLanguageService.Setup(s => s.GetLanguageForExtension(".go")).Returns("go");
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ STRATEGY 1: EXPLICIT PATH                                                │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void InferTargetFilePath_WithExplicitPath_ReturnsExplicitPath()
    {
        var block = new CodeBlock
        {
            Content = "public class Test { }",
            Language = "csharp",
            TargetFilePath = "src/Models/Test.cs"
        };

        var result = _service.InferTargetFilePath(block, Array.Empty<FileContext>());

        Assert.True(result.IsSuccess);
        Assert.Equal("src/Models/Test.cs", result.Path);
        Assert.Equal(1.0f, result.Confidence);
        Assert.Equal(InferenceStrategy.ExplicitPath, result.Strategy);
    }

    [Fact]
    public void InferTargetFilePath_NormalizesBackslashes()
    {
        var block = new CodeBlock
        {
            Content = "public class Test { }",
            Language = "csharp",
            TargetFilePath = "src\\Models\\Test.cs"
        };

        var result = _service.InferTargetFilePath(block, Array.Empty<FileContext>());

        Assert.Equal("src/Models/Test.cs", result.Path);
    }

    [Fact]
    public void InferTargetFilePath_TrimsLeadingSlash()
    {
        var block = new CodeBlock
        {
            Content = "public class Test { }",
            Language = "csharp",
            TargetFilePath = "/src/Models/Test.cs"
        };

        var result = _service.InferTargetFilePath(block, Array.Empty<FileContext>());

        Assert.Equal("src/Models/Test.cs", result.Path);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ STRATEGY 2: SINGLE CONTEXT                                               │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void InferTargetFilePath_WithSingleContext_ReturnsContextPath()
    {
        var block = new CodeBlock
        {
            Content = "public class Test { }",
            Language = "csharp"
        };
        var context = new[]
        {
            new FileContext { FilePath = "src/Services/UserService.cs", Content = "" }
        };

        var result = _service.InferTargetFilePath(block, context);

        Assert.True(result.IsSuccess);
        Assert.Equal("src/Services/UserService.cs", result.Path);
        Assert.Equal(0.95f, result.Confidence);
        Assert.Equal(InferenceStrategy.SingleContext, result.Strategy);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ STRATEGY 3: LANGUAGE MATCH                                               │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void InferTargetFilePath_MatchesByLanguage()
    {
        var block = new CodeBlock
        {
            Content = "public class Test { }",
            Language = "csharp"
        };
        var context = new[]
        {
            new FileContext { FilePath = "app.ts", Language = "typescript", Content = "" },
            new FileContext { FilePath = "src/Model.cs", Language = "csharp", Content = "" }
        };

        var result = _service.InferTargetFilePath(block, context);

        Assert.True(result.IsSuccess);
        Assert.Equal("src/Model.cs", result.Path);
        Assert.Equal(InferenceStrategy.LanguageMatch, result.Strategy);
    }

    [Fact]
    public void InferTargetFilePath_MatchesByExtension_WhenNoExplicitLanguage()
    {
        var block = new CodeBlock
        {
            Content = "class Test {}",
            Language = "typescript"
        };
        var context = new[]
        {
            new FileContext { FilePath = "src/utils.ts", Content = "" },
            new FileContext { FilePath = "README.md", Content = "" }
        };

        var result = _service.InferTargetFilePath(block, context);

        Assert.True(result.IsSuccess);
        Assert.Equal("src/utils.ts", result.Path);
    }

    [Fact]
    public void InferTargetFilePath_ReturnsAmbiguous_WhenMultipleLanguageMatches()
    {
        var block = new CodeBlock
        {
            Content = "public class Test { }",
            Language = "csharp"
        };
        var context = new[]
        {
            new FileContext { FilePath = "src/A.cs", Language = "csharp", Content = "" },
            new FileContext { FilePath = "src/B.cs", Language = "csharp", Content = "" }
        };

        var result = _service.InferTargetFilePath(block, context);

        Assert.True(result.IsAmbiguous);
        Assert.Equal(2, result.AlternativePaths.Count);
        Assert.Equal(InferenceStrategy.Ambiguous, result.Strategy);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ STRATEGY 4: TYPE NAME MATCH                                              │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void InferTargetFilePath_MatchesByTypeName_ExactMatch()
    {
        var block = new CodeBlock
        {
            Content = "public class UserService { }",
            Language = "csharp"
        };
        // Need multiple files with extensions that don't map to block's language
        var context = new[]
        {
            new FileContext { FilePath = "src/Services/UserService.txt", Content = "" },
            new FileContext { FilePath = "src/Services/OrderService.txt", Content = "" }
        };

        var result = _service.InferTargetFilePath(block, context);

        Assert.True(result.IsSuccess);
        Assert.Equal("src/Services/UserService.txt", result.Path);
        Assert.Equal(0.90f, result.Confidence);
        Assert.Equal(InferenceStrategy.TypeNameMatch, result.Strategy);
    }

    [Fact]
    public void InferTargetFilePath_MatchesByTypeName_PartialMatch()
    {
        var block = new CodeBlock
        {
            Content = "public interface IUserService { }",
            Language = "csharp"
        };
        // Need multiple files with extensions that don't map to block's language
        var context = new[]
        {
            new FileContext { FilePath = "src/Services/UserService.txt", Content = "" },
            new FileContext { FilePath = "src/Services/OrderService.txt", Content = "" }
        };

        // IUserService contains "UserService"
        var result = _service.InferTargetFilePath(block, context);

        Assert.True(result.IsSuccess);
        Assert.Equal("src/Services/UserService.txt", result.Path);
        Assert.Equal(0.75f, result.Confidence);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ STRATEGY 5: CONTENT SIMILARITY                                           │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void InferTargetFilePath_MatchesByNamespace()
    {
        var block = new CodeBlock
        {
            Content = "namespace MyApp.Services;\npublic class NewHelper { }",
            Language = "csharp"
        };
        // Need multiple files, no language match, and no type name match
        var context = new[]
        {
            new FileContext
            {
                FilePath = "src/Services/UserService.txt",
                Content = "namespace MyApp.Services;\npublic class UserService { }"
                // No Language set, so no language match
            },
            new FileContext
            {
                FilePath = "src/Other/Other.txt",
                Content = "class Other: pass"
            }
        };

        var result = _service.InferTargetFilePath(block, context);

        Assert.True(result.IsSuccess);
        Assert.Equal("src/Services/UserService.txt", result.Path);
        Assert.Equal(InferenceStrategy.ContentSimilarity, result.Strategy);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ STRATEGY 6: GENERATE NEW PATH                                            │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void InferTargetFilePath_GeneratesNewPath_WhenNoContextMatch()
    {
        var block = new CodeBlock
        {
            Content = "public class NewFeature { }",
            Language = "csharp"
        };

        var result = _service.InferTargetFilePath(block, Array.Empty<FileContext>());

        Assert.True(result.IsSuccess);
        Assert.True(result.IsNewFile);
        Assert.Equal("src/NewFeature.cs", result.Path);
        Assert.Equal(0.50f, result.Confidence);
        Assert.Equal(InferenceStrategy.GeneratedNew, result.Strategy);
    }

    [Fact]
    public void InferTargetFilePath_GeneratesPathWithNamespace()
    {
        var block = new CodeBlock
        {
            Content = "namespace AIntern.Core.Models;\npublic class User { }",
            Language = "csharp"
        };

        var result = _service.InferTargetFilePath(block, Array.Empty<FileContext>());

        Assert.True(result.IsSuccess);
        Assert.True(result.IsNewFile);
        Assert.Equal("AIntern/Core/Models/User.cs", result.Path);
    }

    [Fact]
    public void InferTargetFilePath_DisablesNewFileSuggestions_WhenConfigured()
    {
        _service.AllowNewFileSuggestions = false;

        var block = new CodeBlock
        {
            Content = "public class NewFeature { }",
            Language = "csharp"
        };

        var result = _service.InferTargetFilePath(block, Array.Empty<FileContext>());

        Assert.False(result.IsSuccess);
        Assert.False(result.IsNewFile);
        Assert.Equal(InferenceStrategy.None, result.Strategy);
    }

    [Fact]
    public void InferTargetFilePath_UsesCustomBaseDirectory()
    {
        _service.NewFileBaseDirectory = "lib";

        var block = new CodeBlock
        {
            Content = "public class Widget { }",
            Language = "csharp"
        };

        var result = _service.InferTargetFilePath(block, Array.Empty<FileContext>());

        Assert.True(result.IsSuccess);
        Assert.Equal("lib/Widget.cs", result.Path);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ INFER FILE NAME FROM CONTENT                                             │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Theory]
    [InlineData("public class UserService { }", "csharp", "UserService")]
    [InlineData("internal sealed class DataProcessor { }", "csharp", "DataProcessor")]
    [InlineData("public interface IRepository { }", "csharp", "IRepository")]
    [InlineData("public record Person(string Name);", "csharp", "Person")]
    [InlineData("export class ApiClient { }", "typescript", "ApiClient")]
    [InlineData("export interface IConfig { }", "typescript", "IConfig")]
    [InlineData("class DataLoader:\n    pass", "python", "DataLoader")]
    [InlineData("type UserStore struct { }", "go", "UserStore")]
    [InlineData("pub struct Config { }", "rust", "Config")]
    public void InferFileNameFromContent_ExtractsTypeName(
        string content, string language, string expected)
    {
        var result = _service.InferFileNameFromContent(content, language);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void InferFileNameFromContent_ReturnsNull_ForUnknownLanguage()
    {
        var result = _service.InferFileNameFromContent("some code", "unknown");
        Assert.Null(result);
    }

    [Fact]
    public void InferFileNameFromContent_ReturnsNull_ForEmptyContent()
    {
        var result = _service.InferFileNameFromContent("", "csharp");
        Assert.Null(result);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ EXTRACT ALL TYPE NAMES                                                   │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void ExtractAllTypeNames_ReturnsMultipleTypes()
    {
        var content = @"
            public class User { }
            public class Order { }
            public interface IRepository { }
        ";

        var results = _service.ExtractAllTypeNames(content, "csharp");

        Assert.Equal(3, results.Count);
        Assert.Contains("User", results);
        Assert.Contains("Order", results);
        Assert.Contains("IRepository", results);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ GET EXTENSION FOR LANGUAGE                                               │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void GetExtensionForLanguage_ReturnsExtension()
    {
        var result = _service.GetExtensionForLanguage("csharp");
        Assert.Equal(".cs", result);
    }

    [Fact]
    public void GetExtensionForLanguage_ReturnsNull_ForNullLanguage()
    {
        var result = _service.GetExtensionForLanguage(null);
        Assert.Null(result);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ EVENTS                                                                   │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void InferTargetFilePath_RaisesPathInferredEvent()
    {
        PathInferredEventArgs? eventArgs = null;
        _service.PathInferred += (s, e) => eventArgs = e;

        var block = new CodeBlock
        {
            Content = "public class Test { }",
            Language = "csharp",
            TargetFilePath = "test.cs"
        };

        _service.InferTargetFilePath(block, Array.Empty<FileContext>());

        Assert.NotNull(eventArgs);
        Assert.Equal(block, eventArgs.CodeBlock);
        Assert.NotNull(eventArgs.Result);
        Assert.True(eventArgs.Duration >= TimeSpan.Zero);
    }

    [Fact]
    public void InferTargetFilePath_RaisesAmbiguousEvent_WhenMultipleMatches()
    {
        AmbiguousPathEventArgs? eventArgs = null;
        _service.InferenceAmbiguous += (s, e) => eventArgs = e;

        var block = new CodeBlock
        {
            Content = "public class Test { }",
            Language = "csharp"
        };
        var context = new[]
        {
            new FileContext { FilePath = "A.cs", Language = "csharp", Content = "" },
            new FileContext { FilePath = "B.cs", Language = "csharp", Content = "" }
        };

        _service.InferTargetFilePath(block, context);

        Assert.NotNull(eventArgs);
        Assert.Equal(2, eventArgs.PossiblePaths.Count);
    }

    [Fact]
    public void InferTargetFilePath_UsesSelectedPath_FromAmbiguousEventHandler()
    {
        _service.InferenceAmbiguous += (s, e) =>
        {
            e.SelectedPath = "B.cs";
        };

        var block = new CodeBlock
        {
            Content = "public class Test { }",
            Language = "csharp"
        };
        var context = new[]
        {
            new FileContext { FilePath = "A.cs", Language = "csharp", Content = "" },
            new FileContext { FilePath = "B.cs", Language = "csharp", Content = "" }
        };

        var result = _service.InferTargetFilePath(block, context);

        Assert.True(result.IsSuccess);
        Assert.Equal("B.cs", result.Path);
        Assert.False(result.IsAmbiguous);
    }

    [Fact]
    public void InferTargetFilePath_RaisesFailedEvent_WhenNoMatch()
    {
        _service.AllowNewFileSuggestions = false;

        PathInferenceFailedEventArgs? eventArgs = null;
        _service.InferenceFailed += (s, e) => eventArgs = e;

        var block = new CodeBlock
        {
            Content = "some random code",
            Language = null
        };

        _service.InferTargetFilePath(block, Array.Empty<FileContext>());

        Assert.NotNull(eventArgs);
        Assert.NotNull(eventArgs.Reason);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ BATCH PROCESSING                                                         │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void InferTargetFilePaths_ProcessesMultipleBlocks()
    {
        var blocks = new[]
        {
            new CodeBlock { Content = "public class A { }", Language = "csharp" },
            new CodeBlock { Content = "public class B { }", Language = "csharp" }
        };

        var results = _service.InferTargetFilePaths(blocks, Array.Empty<FileContext>());

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.IsSuccess));
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ CONSTRUCTOR                                                              │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void Constructor_NullLanguageService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new FilePathInferenceService(null!, null));
    }
}
