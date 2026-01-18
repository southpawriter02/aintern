using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;
using AIntern.Services.Terminal;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AIntern.Services.Tests.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SHELL CONFIGURATION SERVICE TESTS (v0.5.3b)                             │
// │ Unit tests for shell configuration retrieval and command formatting.    │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Unit tests for <see cref="ShellConfigurationService"/>.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.3b.</para>
/// <para>
/// Tests cover:
/// <list type="bullet">
///   <item>Configuration retrieval per shell type</item>
///   <item>CD command formatting with path escaping</item>
///   <item>OSC escape sequence generation</item>
///   <item>Integration script generation</item>
/// </list>
/// </para>
/// </remarks>
public sealed class ShellConfigurationServiceTests
{
    // ─────────────────────────────────────────────────────────────────────
    // Fixtures
    // ─────────────────────────────────────────────────────────────────────

    private readonly Mock<ILogger<ShellConfigurationService>> _loggerMock;
    private readonly Mock<IShellDetectionService> _detectionMock;
    private readonly ShellConfigurationService _service;

    public ShellConfigurationServiceTests()
    {
        _loggerMock = new Mock<ILogger<ShellConfigurationService>>();
        _detectionMock = new Mock<IShellDetectionService>();

        // Set up detection mock to return expected types
        _detectionMock.Setup(x => x.DetectShellType("/bin/bash")).Returns(ShellType.Bash);
        _detectionMock.Setup(x => x.DetectShellType("/bin/zsh")).Returns(ShellType.Zsh);
        _detectionMock.Setup(x => x.DetectShellType("pwsh")).Returns(ShellType.PowerShellCore);
        _detectionMock.Setup(x => x.DetectShellType("cmd.exe")).Returns(ShellType.Cmd);

        _service = new ShellConfigurationService(_loggerMock.Object, _detectionMock.Object);
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetConfiguration(ShellType) Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> GetConfiguration returns Bash config with correct properties.<br/>
    /// </summary>
    [Fact]
    public void GetConfiguration_Bash_ReturnsBashConfig()
    {
        // Act
        var config = _service.GetConfiguration(ShellType.Bash);

        // Assert
        Assert.Equal(ShellType.Bash, config.Type);
        Assert.Equal("clear", config.ClearCommand);
        Assert.Equal("cd", config.ChangeDirectoryCommand);
        Assert.Equal("pwd", config.PrintWorkingDirectoryCommand);
        Assert.True(config.SupportsOsc7);
        Assert.True(config.SupportsOsc133);
        Assert.Equal("--login", config.LoginArguments);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetConfiguration returns Zsh config with correct properties.<br/>
    /// </summary>
    [Fact]
    public void GetConfiguration_Zsh_ReturnsZshConfig()
    {
        // Act
        var config = _service.GetConfiguration(ShellType.Zsh);

        // Assert
        Assert.Equal(ShellType.Zsh, config.Type);
        Assert.True(config.SupportsOsc7);
        Assert.Contains("~/.zshrc", config.ProfileFiles);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetConfiguration returns PowerShell config with correct syntax.<br/>
    /// </summary>
    [Fact]
    public void GetConfiguration_PowerShell_HasCorrectSyntax()
    {
        // Act
        var config = _service.GetConfiguration(ShellType.PowerShell);

        // Assert
        Assert.Equal("Clear-Host", config.ClearCommand);
        Assert.Equal("Set-Location", config.ChangeDirectoryCommand);
        Assert.Equal("`", config.LineContinuation);
        Assert.Equal("$env:", config.EnvironmentVariablePrefix);
        Assert.False(config.SupportsOsc7);
        Assert.True(config.SupportsOsc9);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetConfiguration returns Cmd config with Windows-specific commands.<br/>
    /// </summary>
    [Fact]
    public void GetConfiguration_Cmd_HasWindowsCommands()
    {
        // Act
        var config = _service.GetConfiguration(ShellType.Cmd);

        // Assert
        Assert.Equal("cls", config.ClearCommand);
        Assert.Equal("cd /d", config.ChangeDirectoryCommand);
        Assert.Equal("&", config.CommandSeparator);
        Assert.Equal("^", config.LineContinuation);
        Assert.Equal("REM", config.CommentPrefix);
        Assert.Equal("%", config.EnvironmentVariablePrefix);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetConfiguration returns Fish config with correct properties.<br/>
    /// </summary>
    [Fact]
    public void GetConfiguration_Fish_HasCorrectConfig()
    {
        // Act
        var config = _service.GetConfiguration(ShellType.Fish);

        // Assert
        Assert.Equal(ShellType.Fish, config.Type);
        Assert.True(config.SupportsOsc7);
        Assert.Equal("; and", config.CommandSeparator);
        Assert.Equal("set -x {0} {1}", config.SetEnvironmentVariableTemplate);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetConfiguration returns Nushell config with correct properties.<br/>
    /// </summary>
    [Fact]
    public void GetConfiguration_Nushell_HasCorrectConfig()
    {
        // Act
        var config = _service.GetConfiguration(ShellType.Nushell);

        // Assert
        Assert.Equal(ShellType.Nushell, config.Type);
        Assert.True(config.SupportsOsc7);
        Assert.Equal("$env.", config.EnvironmentVariablePrefix);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetConfiguration returns Unknown for undefined shell types.<br/>
    /// </summary>
    [Fact]
    public void GetConfiguration_Unknown_ReturnsUnknownConfig()
    {
        // Act
        var config = _service.GetConfiguration(ShellType.Unknown);

        // Assert
        Assert.Equal(ShellType.Unknown, config.Type);
        Assert.False(config.SupportsOsc7);
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetConfiguration(string) Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> GetConfiguration with path uses shell detection.<br/>
    /// </summary>
    [Fact]
    public void GetConfiguration_WithPath_UsesShellDetection()
    {
        // Act
        var config = _service.GetConfiguration("/bin/bash");

        // Assert
        Assert.Equal(ShellType.Bash, config.Type);
        _detectionMock.Verify(x => x.DetectShellType("/bin/bash"), Times.Once);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetConfiguration with null path returns Unknown.<br/>
    /// </summary>
    [Fact]
    public void GetConfiguration_NullPath_ReturnsUnknown()
    {
        // Act
        var config = _service.GetConfiguration((string)null!);

        // Assert
        Assert.Equal(ShellType.Unknown, config.Type);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetConfiguration with empty path returns Unknown.<br/>
    /// </summary>
    [Fact]
    public void GetConfiguration_EmptyPath_ReturnsUnknown()
    {
        // Act
        var config = _service.GetConfiguration("");

        // Assert
        Assert.Equal(ShellType.Unknown, config.Type);
    }

    // ─────────────────────────────────────────────────────────────────────
    // FormatChangeDirectoryCommand Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> FormatChangeDirectoryCommand with simple path.<br/>
    /// </summary>
    [Fact]
    public void FormatChangeDirectoryCommand_SimplePath_NoEscaping()
    {
        // Act
        var command = _service.FormatChangeDirectoryCommand(ShellType.Bash, "/home/user");

        // Assert
        Assert.Equal("cd /home/user", command);
    }

    /// <summary>
    /// <b>Unit Test:</b> FormatChangeDirectoryCommand escapes spaces in Bash.<br/>
    /// </summary>
    [Fact]
    public void FormatChangeDirectoryCommand_WithSpaces_EscapesBash()
    {
        // Act
        var command = _service.FormatChangeDirectoryCommand(ShellType.Bash, "/home/my folder");

        // Assert
        Assert.Equal("cd '/home/my folder'", command);
    }

    /// <summary>
    /// <b>Unit Test:</b> FormatChangeDirectoryCommand uses Set-Location for PowerShell.<br/>
    /// </summary>
    [Fact]
    public void FormatChangeDirectoryCommand_PowerShell_UsesSetLocation()
    {
        // Act
        var command = _service.FormatChangeDirectoryCommand(ShellType.PowerShell, "C:\\Users");

        // Assert
        Assert.StartsWith("Set-Location ", command);
    }

    /// <summary>
    /// <b>Unit Test:</b> FormatChangeDirectoryCommand uses cd /d for Cmd.<br/>
    /// </summary>
    [Fact]
    public void FormatChangeDirectoryCommand_Cmd_UsesCdD()
    {
        // Act
        var command = _service.FormatChangeDirectoryCommand(ShellType.Cmd, "C:\\Users");

        // Assert
        Assert.StartsWith("cd /d ", command);
    }

    /// <summary>
    /// <b>Unit Test:</b> FormatChangeDirectoryCommand escapes single quotes.<br/>
    /// </summary>
    [Fact]
    public void FormatChangeDirectoryCommand_WithQuote_EscapesCorrectly()
    {
        // Act
        var command = _service.FormatChangeDirectoryCommand(ShellType.Bash, "/home/it's a test");

        // Assert
        Assert.Contains("'\\''", command); // Bash escapes ' as '\''
    }

    /// <summary>
    /// <b>Unit Test:</b> FormatChangeDirectoryCommand with empty path returns empty.<br/>
    /// </summary>
    [Fact]
    public void FormatChangeDirectoryCommand_EmptyPath_ReturnsEmpty()
    {
        // Act
        var command = _service.FormatChangeDirectoryCommand(ShellType.Bash, "");

        // Assert
        Assert.Empty(command);
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetCwdReportingEscapeSequence Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> GetCwdReportingEscapeSequence returns OSC 7 for Bash.<br/>
    /// </summary>
    [Fact]
    public void GetCwdReportingEscapeSequence_Bash_ReturnsOsc7()
    {
        // Act
        var sequence = _service.GetCwdReportingEscapeSequence(ShellType.Bash, "/home/user");

        // Assert
        Assert.NotNull(sequence);
        Assert.StartsWith("\x1b]7;file://", sequence);
        Assert.EndsWith("\x07", sequence);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetCwdReportingEscapeSequence returns OSC 9 for PowerShell.<br/>
    /// </summary>
    [Fact]
    public void GetCwdReportingEscapeSequence_PowerShell_ReturnsOsc9()
    {
        // Act
        var sequence = _service.GetCwdReportingEscapeSequence(ShellType.PowerShell, "C:\\Users");

        // Assert
        Assert.NotNull(sequence);
        Assert.StartsWith("\x1b]9;9;", sequence);
        Assert.EndsWith("\x07", sequence);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetCwdReportingEscapeSequence returns null for Cmd.<br/>
    /// </summary>
    [Fact]
    public void GetCwdReportingEscapeSequence_Cmd_ReturnsNull()
    {
        // Act
        var sequence = _service.GetCwdReportingEscapeSequence(ShellType.Cmd, "C:\\Users");

        // Assert
        Assert.Null(sequence);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetCwdReportingEscapeSequence returns null for empty directory.<br/>
    /// </summary>
    [Fact]
    public void GetCwdReportingEscapeSequence_EmptyDirectory_ReturnsNull()
    {
        // Act
        var sequence = _service.GetCwdReportingEscapeSequence(ShellType.Bash, "");

        // Assert
        Assert.Null(sequence);
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetDefaultEnvironment Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> GetDefaultEnvironment returns TERM for Bash.<br/>
    /// </summary>
    [Fact]
    public void GetDefaultEnvironment_Bash_HasTermVariable()
    {
        // Act
        var env = _service.GetDefaultEnvironment(ShellType.Bash);

        // Assert
        Assert.True(env.ContainsKey("TERM"));
        Assert.Equal("xterm-256color", env["TERM"]);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetDefaultEnvironment returns copy, not reference.<br/>
    /// </summary>
    [Fact]
    public void GetDefaultEnvironment_ReturnsCopy()
    {
        // Act
        var env1 = _service.GetDefaultEnvironment(ShellType.Bash);
        var env2 = _service.GetDefaultEnvironment(ShellType.Bash);

        env1["TEST"] = "modified";

        // Assert
        Assert.False(env2.ContainsKey("TEST"));
    }

    // ─────────────────────────────────────────────────────────────────────
    // GenerateShellIntegrationScript Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> GenerateShellIntegrationScript returns script for Bash.<br/>
    /// </summary>
    [Fact]
    public void GenerateShellIntegrationScript_Bash_HasPromptCommand()
    {
        // Act
        var script = _service.GenerateShellIntegrationScript(ShellType.Bash);

        // Assert
        Assert.NotNull(script);
        Assert.Contains("PROMPT_COMMAND", script);
        Assert.Contains("__aintern_prompt_command", script);
    }

    /// <summary>
    /// <b>Unit Test:</b> GenerateShellIntegrationScript returns script for Zsh.<br/>
    /// </summary>
    [Fact]
    public void GenerateShellIntegrationScript_Zsh_HasHook()
    {
        // Act
        var script = _service.GenerateShellIntegrationScript(ShellType.Zsh);

        // Assert
        Assert.NotNull(script);
        Assert.Contains("add-zsh-hook", script);
        Assert.Contains("chpwd", script);
    }

    /// <summary>
    /// <b>Unit Test:</b> GenerateShellIntegrationScript returns script for Fish.<br/>
    /// </summary>
    [Fact]
    public void GenerateShellIntegrationScript_Fish_HasFunction()
    {
        // Act
        var script = _service.GenerateShellIntegrationScript(ShellType.Fish);

        // Assert
        Assert.NotNull(script);
        Assert.Contains("function", script);
        Assert.Contains("--on-variable PWD", script);
    }

    /// <summary>
    /// <b>Unit Test:</b> GenerateShellIntegrationScript returns script for PowerShell.<br/>
    /// </summary>
    [Fact]
    public void GenerateShellIntegrationScript_PowerShell_HasPromptFunction()
    {
        // Act
        var script = _service.GenerateShellIntegrationScript(ShellType.PowerShell);

        // Assert
        Assert.NotNull(script);
        Assert.Contains("function global:prompt", script);
    }

    /// <summary>
    /// <b>Unit Test:</b> GenerateShellIntegrationScript returns null for Cmd.<br/>
    /// </summary>
    [Fact]
    public void GenerateShellIntegrationScript_Cmd_ReturnsNull()
    {
        // Act
        var script = _service.GenerateShellIntegrationScript(ShellType.Cmd);

        // Assert
        Assert.Null(script);
    }

    // ─────────────────────────────────────────────────────────────────────
    // ShellConfiguration Model Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> ShellConfiguration.ToString returns type name.<br/>
    /// </summary>
    [Fact]
    public void ShellConfiguration_ToString_ContainsType()
    {
        // Act
        var config = _service.GetConfiguration(ShellType.Bash);

        // Assert
        Assert.Contains("Bash", config.ToString());
    }

    /// <summary>
    /// <b>Unit Test:</b> All ShellTypes have configurations.<br/>
    /// </summary>
    [Theory]
    [InlineData(ShellType.Bash)]
    [InlineData(ShellType.Zsh)]
    [InlineData(ShellType.Fish)]
    [InlineData(ShellType.PowerShell)]
    [InlineData(ShellType.PowerShellCore)]
    [InlineData(ShellType.Cmd)]
    [InlineData(ShellType.Nushell)]
    [InlineData(ShellType.Wsl)]
    [InlineData(ShellType.Tcsh)]
    [InlineData(ShellType.Ksh)]
    [InlineData(ShellType.Unknown)]
    public void GetConfiguration_AllShellTypes_HaveConfig(ShellType shellType)
    {
        // Act
        var config = _service.GetConfiguration(shellType);

        // Assert
        Assert.NotNull(config);
        Assert.Equal(shellType, config.Type);
        Assert.NotNull(config.ClearCommand);
        Assert.NotNull(config.ChangeDirectoryCommand);
    }
}
