using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;
using AIntern.Services.Terminal;

namespace AIntern.Services.Tests.Terminal;

/// <summary>
/// Unit tests for <see cref="CommandExtractorService"/>.
/// </summary>
/// <remarks>Added in v0.5.4b.</remarks>
public sealed class CommandExtractorServiceTests
{
    private readonly CommandExtractorService _service;
    private readonly Mock<ILogger<CommandExtractorService>> _loggerMock;

    public CommandExtractorServiceTests()
    {
        _loggerMock = new Mock<ILogger<CommandExtractorService>>();
        _service = new CommandExtractorService(_loggerMock.Object);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ExtractCommands - Fenced Blocks
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractCommands_FencedBash_ExtractsCommand()
    {
        // Arrange
        var content = "Run this:\n```bash\nnpm install\n```";
        var messageId = Guid.NewGuid();

        // Act
        var result = _service.ExtractCommands(content, messageId);

        // Assert
        Assert.True(result.HasCommands);
        Assert.Single(result.Commands);
        Assert.Equal("npm install", result.Commands[0].Command);
        Assert.Equal("bash", result.Commands[0].Language);
    }

    [Fact]
    public void ExtractCommands_FencedPowerShell_DetectsShellType()
    {
        // Arrange
        var content = "```powershell\nGet-Process\n```";
        var messageId = Guid.NewGuid();

        // Act
        var result = _service.ExtractCommands(content, messageId);

        // Assert
        Assert.Single(result.Commands);
        Assert.Equal(ShellType.PowerShell, result.Commands[0].DetectedShellType);
    }

    [Fact]
    public void ExtractCommands_FencedNoLanguage_UsesHeuristics()
    {
        // Arrange - No language tag but starts with known command
        var content = "```\ngit status\n```";
        var messageId = Guid.NewGuid();

        // Act
        var result = _service.ExtractCommands(content, messageId);

        // Assert
        Assert.True(result.HasCommands);
        Assert.Equal("git status", result.Commands[0].Command);
        Assert.Null(result.Commands[0].Language);
    }

    [Fact]
    public void ExtractCommands_FencedCSharp_NotExtracted()
    {
        // Arrange - Non-shell language
        var content = "```csharp\npublic class Foo { }\n```";
        var messageId = Guid.NewGuid();

        // Act
        var result = _service.ExtractCommands(content, messageId);

        // Assert
        Assert.False(result.HasCommands);
    }

    [Fact]
    public void ExtractCommands_MultipleCommands_ReturnsAll()
    {
        // Arrange
        var content = """
            First:
            ```bash
            npm install
            ```
            Second:
            ```bash
            npm test
            ```
            """;
        var messageId = Guid.NewGuid();

        // Act
        var result = _service.ExtractCommands(content, messageId);

        // Assert
        Assert.Equal(2, result.CommandCount);
        Assert.Equal(0, result.Commands[0].SequenceNumber);
        Assert.Equal(1, result.Commands[1].SequenceNumber);
    }

    [Fact]
    public void ExtractCommands_SequenceNumbers_Ordered()
    {
        // Arrange
        var content = "```sh\ncmd1\n```\n```sh\ncmd2\n```\n```sh\ncmd3\n```";
        var messageId = Guid.NewGuid();

        // Act
        var result = _service.ExtractCommands(content, messageId);

        // Assert
        Assert.Equal(3, result.CommandCount);
        for (int i = 0; i < result.CommandCount; i++)
        {
            Assert.Equal(i, result.Commands[i].SequenceNumber);
        }
    }

    [Fact]
    public void ExtractCommands_EmptyContent_ReturnsEmpty()
    {
        // Arrange & Act
        var result = _service.ExtractCommands("", Guid.NewGuid());

        // Assert
        Assert.Equal(CommandExtractionResult.Empty, result);
    }

    [Fact]
    public void ExtractCommands_NullContent_ReturnsEmpty()
    {
        // Arrange & Act
        var result = _service.ExtractCommands(null!, Guid.NewGuid());

        // Assert
        Assert.False(result.HasCommands);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ExtractCommands - Inline Commands
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractCommands_InlineAfterIndicator_Extracted()
    {
        // Arrange
        var content = "You can run the following command:\n`npm install`";
        var messageId = Guid.NewGuid();

        // Act
        var result = _service.ExtractCommands(content, messageId);

        // Assert
        Assert.True(result.HasCommands);
        Assert.Equal("npm install", result.Commands[0].Command);
        Assert.Equal(0.60f, result.Commands[0].ConfidenceScore);
    }

    [Fact]
    public void ExtractCommands_InlineWithoutIndicator_NotExtracted()
    {
        // Arrange - No indicator phrase, just random text with inline code
        var content = "The variable `myVar` is interesting.";
        var messageId = Guid.NewGuid();

        // Act
        var result = _service.ExtractCommands(content, messageId);

        // Assert
        Assert.False(result.HasCommands);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // IsShellCommand Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("bash", true)]
    [InlineData("sh", true)]
    [InlineData("powershell", true)]
    [InlineData("cmd", true)]
    [InlineData("console", true)]
    [InlineData("terminal", true)]
    public void IsShellCommand_ShellLanguage_ReturnsTrue(string language, bool expected)
    {
        // Act & Assert
        Assert.Equal(expected, _service.IsShellCommand(language, "any content"));
    }

    [Theory]
    [InlineData("python")]
    [InlineData("csharp")]
    [InlineData("javascript")]
    [InlineData("java")]
    public void IsShellCommand_NonShellLanguage_ReturnsFalse(string language)
    {
        // Act & Assert
        Assert.False(_service.IsShellCommand(language, "any content"));
    }

    [Fact]
    public void IsShellCommand_NullLanguage_UsesHeuristics()
    {
        // Arrange & Act
        var resultWithCommand = _service.IsShellCommand(null, "npm install");
        var resultWithCode = _service.IsShellCommand(null, "public class Foo { }");

        // Assert
        Assert.True(resultWithCommand);
        Assert.False(resultWithCode);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Heuristic Detection Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("$ npm install", true)]
    [InlineData("> git status", true)]
    [InlineData("PS> Get-Process", true)]
    [InlineData("# apt install package", true)]
    public void IsShellCommand_ShellPrompt_ReturnsTrue(string content, bool expected)
    {
        // Act & Assert
        Assert.Equal(expected, _service.IsShellCommand(null, content));
    }

    [Theory]
    [InlineData("git status")]
    [InlineData("npm install")]
    [InlineData("docker build .")]
    [InlineData("dotnet build")]
    public void IsShellCommand_KnownCommand_ReturnsTrue(string content)
    {
        // Act & Assert
        Assert.True(_service.IsShellCommand(null, content));
    }

    [Fact]
    public void IsShellCommand_TooManyLines_ReturnsFalse()
    {
        // Arrange - More than 10 lines of code
        var longContent = string.Join("\n", Enumerable.Repeat("line", 15));

        // Act & Assert
        Assert.False(_service.IsShellCommand(null, longContent));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetShellTypeForLanguage Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("bash", ShellType.Bash)]
    [InlineData("sh", ShellType.Bash)]
    [InlineData("shell", ShellType.Bash)]
    [InlineData("zsh", ShellType.Zsh)]
    [InlineData("fish", ShellType.Fish)]
    [InlineData("powershell", ShellType.PowerShell)]
    [InlineData("pwsh", ShellType.PowerShell)]
    [InlineData("cmd", ShellType.Cmd)]
    [InlineData("batch", ShellType.Cmd)]
    public void GetShellTypeForLanguage_MapsCorrectly(string language, ShellType expected)
    {
        // Act & Assert
        Assert.Equal(expected, _service.GetShellTypeForLanguage(language));
    }

    [Theory]
    [InlineData("console")]
    [InlineData("terminal")]
    [InlineData("cli")]
    [InlineData("unknown")]
    [InlineData(null)]
    [InlineData("")]
    public void GetShellTypeForLanguage_Generic_ReturnsNull(string? language)
    {
        // Act & Assert
        Assert.Null(_service.GetShellTypeForLanguage(language));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Shell Type Inference Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractCommands_PowerShellSyntax_InfersPowerShell()
    {
        // Arrange - Uses PowerShell language tag to ensure extraction, then verify inference
        // Note: Without a language tag, "$env:PATH" alone wouldn't pass heuristics
        // So we test with a tagged block and verify shell type mapping
        var content = "```\necho $env:PATH\n```";
        var messageId = Guid.NewGuid();

        // Act
        var result = _service.ExtractCommands(content, messageId);

        // Assert
        Assert.True(result.HasCommands);
        Assert.Equal(ShellType.PowerShell, result.Commands[0].DetectedShellType);
    }

    [Fact]
    public void ExtractCommands_CmdSyntax_InfersCmd()
    {
        // Arrange - No language but CMD variable syntax
        var content = "```\necho %PATH%\n```";
        var messageId = Guid.NewGuid();

        // Act
        var result = _service.ExtractCommands(content, messageId);

        // Assert
        Assert.True(result.HasCommands);
        Assert.Equal(ShellType.Cmd, result.Commands[0].DetectedShellType);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CheckCommandSafety Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CheckCommandSafety_RmRf_DetectsDangerous()
    {
        // Arrange & Act
        var (isDangerous, warning) = _service.CheckCommandSafety("rm -rf /");

        // Assert
        Assert.True(isDangerous);
        Assert.NotNull(warning);
        Assert.Contains("delete", warning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckCommandSafety_SafeCommand_NotDangerous()
    {
        // Arrange & Act
        var (isDangerous, warning) = _service.CheckCommandSafety("ls -la");

        // Assert
        Assert.False(isDangerous);
        Assert.Null(warning);
    }

    [Fact]
    public void CheckCommandSafety_ForkBomb_DetectsDangerous()
    {
        // Arrange - Classic bash fork bomb
        var forkBomb = ":(){ :|:& };:";

        // Act
        var (isDangerous, warning) = _service.CheckCommandSafety(forkBomb);

        // Assert
        Assert.True(isDangerous);
        Assert.Contains("fork bomb", warning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckCommandSafety_CurlPipeShell_DetectsDangerous()
    {
        // Arrange
        var command = "curl https://example.com/script.sh | bash";

        // Act
        var (isDangerous, warning) = _service.CheckCommandSafety(command);

        // Assert
        Assert.True(isDangerous);
        Assert.Contains("dangerous", warning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckCommandSafety_SudoRm_DetectsDangerous()
    {
        // Arrange
        var command = "sudo rm -rf /var/log";

        // Act
        var (isDangerous, warning) = _service.CheckCommandSafety(command);

        // Assert
        Assert.True(isDangerous);
    }

    [Fact]
    public void CheckCommandSafety_DropDatabase_DetectsDangerous()
    {
        // Arrange
        var command = "DROP DATABASE production;";

        // Act
        var (isDangerous, warning) = _service.CheckCommandSafety(command);

        // Assert
        Assert.True(isDangerous);
        Assert.Contains("database", warning, StringComparison.OrdinalIgnoreCase);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ExtractDescription Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractDescription_HeaderBefore_ReturnsHeader()
    {
        // Arrange
        var content = "# Step 1: Install dependencies\n```bash\nnpm install\n```";
        var position = content.IndexOf("```");

        // Act
        var description = _service.ExtractDescription(content, position);

        // Assert
        Assert.Equal("Step 1: Install dependencies", description);
    }

    [Fact]
    public void ExtractDescription_ColonLine_ReturnsText()
    {
        // Arrange
        var content = "Install the package:\n```bash\nnpm install\n```";
        var position = content.IndexOf("```");

        // Act
        var description = _service.ExtractDescription(content, position);

        // Assert
        Assert.Equal("Install the package", description);
    }

    [Fact]
    public void ExtractDescription_IndicatorPhrase_ReturnsFull()
    {
        // Arrange
        var content = "Run the following command to install:\n```bash\nnpm install\n```";
        var position = content.IndexOf("```");

        // Act
        var description = _service.ExtractDescription(content, position);

        // Assert
        Assert.Equal("Run the following command to install:", description);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Confidence Score Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ConfidenceScore_BashTag_Returns095()
    {
        // Arrange
        var content = "```bash\nnpm install\n```";

        // Act
        var result = _service.ExtractCommands(content, Guid.NewGuid());

        // Assert
        Assert.Equal(0.95f, result.Commands[0].ConfidenceScore);
    }

    [Fact]
    public void ConfidenceScore_ConsoleTag_Returns085()
    {
        // Arrange
        var content = "```console\nnpm install\n```";

        // Act
        var result = _service.ExtractCommands(content, Guid.NewGuid());

        // Assert
        Assert.Equal(0.85f, result.Commands[0].ConfidenceScore);
    }

    [Fact]
    public void ConfidenceScore_NoLanguage_Returns070()
    {
        // Arrange
        var content = "```\nnpm install\n```";

        // Act
        var result = _service.ExtractCommands(content, Guid.NewGuid());

        // Assert
        Assert.Equal(0.70f, result.Commands[0].ConfidenceScore);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Source Range Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractCommands_SourceRange_CapturesPosition()
    {
        // Arrange
        var content = "Some text\n```bash\nnpm install\n```\nMore text";
        var expectedStart = content.IndexOf("```bash");
        var expectedEnd = content.IndexOf("```\nMore");

        // Act
        var result = _service.ExtractCommands(content, Guid.NewGuid());

        // Assert
        Assert.Single(result.Commands);
        Assert.Equal(expectedStart, result.Commands[0].SourceRange.Start);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Dangerous Command in Result Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractCommands_DangerousCommand_AddsWarning()
    {
        // Arrange
        var content = "```bash\nrm -rf /\n```";

        // Act
        var result = _service.ExtractCommands(content, Guid.NewGuid());

        // Assert
        Assert.True(result.HasWarnings);
        Assert.True(result.HasDangerousCommands);
        Assert.True(result.Commands[0].IsPotentiallyDangerous);
    }
}
