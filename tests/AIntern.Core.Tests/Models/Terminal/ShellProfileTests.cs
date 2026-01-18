using System.Text.Json;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;
using Xunit;

namespace AIntern.Core.Tests.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SHELL PROFILE TESTS (v0.5.3c)                                           │
// │ Unit tests for shell profile models and enumerations.                   │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Unit tests for <see cref="ShellProfile"/> and related models.
/// </summary>
public sealed class ShellProfileTests
{
    // ─────────────────────────────────────────────────────────────────────
    // ShellProfile Default Values
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> ShellProfile has correct default values.<br/>
    /// </summary>
    [Fact]
    public void ShellProfile_DefaultValues_AreCorrect()
    {
        // Act
        var profile = new ShellProfile();

        // Assert
        Assert.NotEqual(Guid.Empty, profile.Id);
        Assert.Equal("Default", profile.Name);
        Assert.Null(profile.IconPath);
        Assert.Equal(string.Empty, profile.ShellPath);
        Assert.Equal(ShellType.Unknown, profile.ShellType);
        Assert.Equal(ProfileCloseOnExit.OnCleanExit, profile.CloseOnExit);
        Assert.Equal(TerminalBellStyle.Audible, profile.BellStyle);
        Assert.False(profile.IsDefault);
        Assert.False(profile.IsHidden);
        Assert.False(profile.IsBuiltIn);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Clone Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> Clone creates new ID.<br/>
    /// </summary>
    [Fact]
    public void ShellProfile_Clone_CreatesNewId()
    {
        // Arrange
        var original = new ShellProfile { Name = "Original" };

        // Act
        var clone = original.Clone();

        // Assert
        Assert.NotEqual(original.Id, clone.Id);
    }

    /// <summary>
    /// <b>Unit Test:</b> Clone copies shell settings.<br/>
    /// </summary>
    [Fact]
    public void ShellProfile_Clone_CopiesSettings()
    {
        // Arrange
        var original = new ShellProfile
        {
            ShellPath = "/bin/bash",
            ShellType = ShellType.Bash,
            Arguments = "-i",
            StartingDirectory = "/home/user",
            StartupCommand = "clear",
            FontFamily = "JetBrains Mono",
            FontSize = 12,
            ThemeName = "One Dark",
            CursorStyle = TerminalCursorStyle.Bar,
            CursorBlink = false,
            ScrollbackLines = 5000,
            CloseOnExit = ProfileCloseOnExit.Never,
            BellStyle = TerminalBellStyle.Visual
        };

        // Act
        var clone = original.Clone();

        // Assert
        Assert.Equal(original.ShellPath, clone.ShellPath);
        Assert.Equal(original.ShellType, clone.ShellType);
        Assert.Equal(original.Arguments, clone.Arguments);
        Assert.Equal(original.StartingDirectory, clone.StartingDirectory);
        Assert.Equal(original.FontFamily, clone.FontFamily);
        Assert.Equal(original.FontSize, clone.FontSize);
        Assert.Equal(original.CursorStyle, clone.CursorStyle);
        Assert.Equal(original.BellStyle, clone.BellStyle);
    }

    /// <summary>
    /// <b>Unit Test:</b> Clone deep copies Environment dictionary.<br/>
    /// </summary>
    [Fact]
    public void ShellProfile_Clone_DeepCopiesEnvironment()
    {
        // Arrange
        var original = new ShellProfile();
        original.Environment["KEY"] = "value";

        // Act
        var clone = original.Clone();
        clone.Environment["KEY"] = "modified";

        // Assert
        Assert.Equal("value", original.Environment["KEY"]);
        Assert.Equal("modified", clone.Environment["KEY"]);
    }

    /// <summary>
    /// <b>Unit Test:</b> Clone sets IsDefault to false.<br/>
    /// </summary>
    [Fact]
    public void ShellProfile_Clone_SetsNotDefault()
    {
        // Arrange
        var original = new ShellProfile { IsDefault = true };

        // Act
        var clone = original.Clone();

        // Assert
        Assert.False(clone.IsDefault);
    }

    /// <summary>
    /// <b>Unit Test:</b> Clone sets IsBuiltIn to false.<br/>
    /// </summary>
    [Fact]
    public void ShellProfile_Clone_SetsNotBuiltIn()
    {
        // Arrange
        var original = new ShellProfile { IsBuiltIn = true };

        // Act
        var clone = original.Clone();

        // Assert
        Assert.False(clone.IsBuiltIn);
    }

    /// <summary>
    /// <b>Unit Test:</b> Clone appends "(Copy)" to name.<br/>
    /// </summary>
    [Fact]
    public void ShellProfile_Clone_IncrementsName()
    {
        // Arrange
        var original = new ShellProfile { Name = "PowerShell" };

        // Act
        var clone = original.Clone();

        // Assert
        Assert.Equal("PowerShell (Copy)", clone.Name);
    }

    /// <summary>
    /// <b>Unit Test:</b> Clone increments SortOrder.<br/>
    /// </summary>
    [Fact]
    public void ShellProfile_Clone_IncrementsSortOrder()
    {
        // Arrange
        var original = new ShellProfile { SortOrder = 5 };

        // Act
        var clone = original.Clone();

        // Assert
        Assert.Equal(6, clone.SortOrder);
    }

    // ─────────────────────────────────────────────────────────────────────
    // IsValid Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> IsValid requires ShellPath.<br/>
    /// </summary>
    [Fact]
    public void ShellProfile_IsValid_RequiresShellPath()
    {
        // Arrange
        var profile = new ShellProfile { Name = "Test", ShellPath = "" };

        // Assert
        Assert.False(profile.IsValid);
    }

    /// <summary>
    /// <b>Unit Test:</b> IsValid requires Name.<br/>
    /// </summary>
    [Fact]
    public void ShellProfile_IsValid_RequiresName()
    {
        // Arrange
        var profile = new ShellProfile { Name = "", ShellPath = "/bin/bash" };

        // Assert
        Assert.False(profile.IsValid);
    }

    /// <summary>
    /// <b>Unit Test:</b> IsValid returns true when both set.<br/>
    /// </summary>
    [Fact]
    public void ShellProfile_IsValid_WhenBothSet_ReturnsTrue()
    {
        // Arrange
        var profile = new ShellProfile { Name = "Bash", ShellPath = "/bin/bash" };

        // Assert
        Assert.True(profile.IsValid);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Serialization Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> ShellProfile can round-trip through JSON.<br/>
    /// </summary>
    [Fact]
    public void ShellProfile_Serialize_RoundTrip()
    {
        // Arrange
        var original = new ShellProfile
        {
            Name = "Test Profile",
            ShellPath = "/bin/zsh",
            ShellType = ShellType.Zsh,
            FontSize = 16,
            CursorStyle = TerminalCursorStyle.Bar
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<ShellProfile>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Id, deserialized.Id);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.ShellPath, deserialized.ShellPath);
        Assert.Equal(original.FontSize, deserialized.FontSize);
        Assert.Equal(original.CursorStyle, deserialized.CursorStyle);
    }

    /// <summary>
    /// <b>Unit Test:</b> JSON preserves null overrides.<br/>
    /// </summary>
    [Fact]
    public void ShellProfile_Serialize_PreservesNulls()
    {
        // Arrange
        var original = new ShellProfile
        {
            Name = "Test",
            ShellPath = "/bin/bash",
            FontFamily = null,
            FontSize = null,
            CursorStyle = null
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<ShellProfile>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Null(deserialized.FontFamily);
        Assert.Null(deserialized.FontSize);
        Assert.Null(deserialized.CursorStyle);
    }

    // ─────────────────────────────────────────────────────────────────────
    // ShellProfileDefaults Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> ShellProfileDefaults has correct values.<br/>
    /// </summary>
    [Fact]
    public void ShellProfileDefaults_HasCorrectValues()
    {
        // Act
        var defaults = new ShellProfileDefaults();

        // Assert
        Assert.Equal("Cascadia Mono, Consolas, monospace", defaults.FontFamily);
        Assert.Equal(14, defaults.FontSize);
        Assert.Equal("Dark", defaults.ThemeName);
        Assert.Equal(TerminalCursorStyle.Block, defaults.CursorStyle);
        Assert.True(defaults.CursorBlink);
        Assert.Equal(10000, defaults.ScrollbackLines);
        Assert.Equal(TerminalBellStyle.Audible, defaults.BellStyle);
        Assert.Equal(ProfileCloseOnExit.OnCleanExit, defaults.CloseOnExit);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Enumeration Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> TerminalCursorStyle has all values.<br/>
    /// </summary>
    [Fact]
    public void TerminalCursorStyle_AllValuesExist()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(TerminalCursorStyle), TerminalCursorStyle.Block));
        Assert.True(Enum.IsDefined(typeof(TerminalCursorStyle), TerminalCursorStyle.Underline));
        Assert.True(Enum.IsDefined(typeof(TerminalCursorStyle), TerminalCursorStyle.Bar));
        Assert.Equal(3, Enum.GetValues<TerminalCursorStyle>().Length);
    }

    /// <summary>
    /// <b>Unit Test:</b> ProfileCloseOnExit has all values.<br/>
    /// </summary>
    [Fact]
    public void ProfileCloseOnExit_AllValuesExist()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(ProfileCloseOnExit), ProfileCloseOnExit.Always));
        Assert.True(Enum.IsDefined(typeof(ProfileCloseOnExit), ProfileCloseOnExit.OnCleanExit));
        Assert.True(Enum.IsDefined(typeof(ProfileCloseOnExit), ProfileCloseOnExit.Never));
        Assert.Equal(3, Enum.GetValues<ProfileCloseOnExit>().Length);
    }

    /// <summary>
    /// <b>Unit Test:</b> TerminalBellStyle has all values.<br/>
    /// </summary>
    [Fact]
    public void TerminalBellStyle_AllValuesExist()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(TerminalBellStyle), TerminalBellStyle.Audible));
        Assert.True(Enum.IsDefined(typeof(TerminalBellStyle), TerminalBellStyle.Visual));
        Assert.True(Enum.IsDefined(typeof(TerminalBellStyle), TerminalBellStyle.Both));
        Assert.True(Enum.IsDefined(typeof(TerminalBellStyle), TerminalBellStyle.None));
        Assert.Equal(4, Enum.GetValues<TerminalBellStyle>().Length);
    }

    /// <summary>
    /// <b>Unit Test:</b> DirectorySyncMode has all values.<br/>
    /// </summary>
    [Fact]
    public void DirectorySyncMode_AllValuesExist()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(DirectorySyncMode), DirectorySyncMode.ActiveTerminalOnly));
        Assert.True(Enum.IsDefined(typeof(DirectorySyncMode), DirectorySyncMode.AllLinkedTerminals));
        Assert.True(Enum.IsDefined(typeof(DirectorySyncMode), DirectorySyncMode.Manual));
        Assert.Equal(3, Enum.GetValues<DirectorySyncMode>().Length);
    }

    // ─────────────────────────────────────────────────────────────────────
    // ToString Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> ToString returns name for regular profile.<br/>
    /// </summary>
    [Fact]
    public void ShellProfile_ToString_ReturnsName()
    {
        // Arrange
        var profile = new ShellProfile { Name = "PowerShell" };

        // Assert
        Assert.Equal("PowerShell", profile.ToString());
    }

    /// <summary>
    /// <b>Unit Test:</b> ToString includes "(Default)" for default profile.<br/>
    /// </summary>
    [Fact]
    public void ShellProfile_ToString_IncludesDefault()
    {
        // Arrange
        var profile = new ShellProfile { Name = "PowerShell", IsDefault = true };

        // Assert
        Assert.Equal("PowerShell (Default)", profile.ToString());
    }
}
