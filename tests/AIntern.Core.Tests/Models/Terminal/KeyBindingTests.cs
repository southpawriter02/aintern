// ============================================================================
// File: KeyBindingTests.cs
// Path: tests/AIntern.Core.Tests/Models/Terminal/KeyBindingTests.cs
// Description: Unit tests for KeyBinding model.
// Created: 2026-01-18
// AI Intern v0.5.5d - Keyboard Shortcuts System
// ============================================================================

namespace AIntern.Core.Tests.Models.Terminal;

using AIntern.Core.Models.Terminal;
using Xunit;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ KeyBindingTests (v0.5.5d)                                                    │
// │ Unit tests for the KeyBinding model.                                        │
// └─────────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Unit tests for <see cref="KeyBinding"/>.
/// </summary>
public class KeyBindingTests
{
    #region Constructor and Properties

    [Fact]
    public void KeyBinding_InitializesWithDefaultValues()
    {
        // Arrange & Act
        var binding = new KeyBinding();

        // Assert
        Assert.Equal(default(TerminalShortcutAction), binding.Action);
        Assert.Equal(string.Empty, binding.Key);
        Assert.Equal(KeyModifierFlags.None, binding.Modifiers);
        Assert.Equal(string.Empty, binding.Description);
        Assert.Equal(string.Empty, binding.Category);
        Assert.True(binding.IsCustomizable);
        Assert.False(binding.PassToPty);
    }

    [Fact]
    public void KeyBinding_InitializesWithValues()
    {
        // Arrange & Act
        var binding = new KeyBinding
        {
            Action = TerminalShortcutAction.Copy,
            Key = "C",
            Modifiers = KeyModifierFlags.Control | KeyModifierFlags.Shift,
            Description = "Copy",
            Category = "Terminal Selection",
            IsCustomizable = true,
            PassToPty = false
        };

        // Assert
        Assert.Equal(TerminalShortcutAction.Copy, binding.Action);
        Assert.Equal("C", binding.Key);
        Assert.Equal(KeyModifierFlags.Control | KeyModifierFlags.Shift, binding.Modifiers);
        Assert.Equal("Copy", binding.Description);
        Assert.Equal("Terminal Selection", binding.Category);
        Assert.True(binding.IsCustomizable);
        Assert.False(binding.PassToPty);
    }

    #endregion

    #region DisplayString Tests

    [Fact]
    public void DisplayString_FormatsSimpleKey()
    {
        // Arrange
        var binding = new KeyBinding { Key = "F3", Modifiers = KeyModifierFlags.None };

        // Act
        var display = binding.DisplayString;

        // Assert
        Assert.Equal("F3", display);
    }

    [Fact]
    public void DisplayString_FormatsCtrlModifier()
    {
        // Arrange
        var binding = new KeyBinding { Key = "C", Modifiers = KeyModifierFlags.Control };

        // Act
        var display = binding.DisplayString;

        // Assert - Platform-dependent formatting
        // macOS uses ⌘, Windows/Linux use Ctrl
        if (OperatingSystem.IsMacOS())
        {
            Assert.Contains("⌘", display);
        }
        else
        {
            Assert.Contains("Ctrl", display);
        }
        Assert.Contains("C", display);
    }

    [Fact]
    public void DisplayString_FormatsMultipleModifiers()
    {
        // Arrange
        var binding = new KeyBinding
        {
            Key = "C",
            Modifiers = KeyModifierFlags.Control | KeyModifierFlags.Shift
        };

        // Act
        var display = binding.DisplayString;

        // Assert - Should contain both modifiers
        Assert.Contains("+", display);
    }

    [Fact]
    public void DisplayString_FormatsSpecialKeys()
    {
        // Arrange
        var binding = new KeyBinding { Key = "OemTilde", Modifiers = KeyModifierFlags.Control };

        // Act
        var display = binding.DisplayString;

        // Assert - OemTilde should be formatted as `
        Assert.Contains("`", display);
    }

    [Fact]
    public void DisplayString_FormatsArrowKeys()
    {
        // Arrange
        var binding = new KeyBinding { Key = "Up", Modifiers = KeyModifierFlags.Shift };

        // Act
        var display = binding.DisplayString;

        // Assert
        Assert.Contains("↑", display);
    }

    [Fact]
    public void DisplayString_FormatsDigitKeys()
    {
        // Arrange
        var binding = new KeyBinding
        {
            Key = "D1",
            Modifiers = KeyModifierFlags.Control | KeyModifierFlags.Shift
        };

        // Act
        var display = binding.DisplayString;

        // Assert
        Assert.Contains("1", display);
    }

    #endregion

    #region SerializedString Tests

    [Fact]
    public void SerializedString_UsesTextModifiers()
    {
        // Arrange
        var binding = new KeyBinding
        {
            Key = "C",
            Modifiers = KeyModifierFlags.Control | KeyModifierFlags.Shift
        };

        // Act
        var serialized = binding.SerializedString;

        // Assert - Should always use text modifiers for serialization
        Assert.Contains("Ctrl", serialized);
        Assert.Contains("Shift", serialized);
        Assert.Contains("C", serialized);
    }

    #endregion

    #region Parse Tests

    [Fact]
    public void Parse_HandlesSingleKey()
    {
        // Act
        var (key, modifiers) = KeyBinding.Parse("F3");

        // Assert
        Assert.Equal("F3", key);
        Assert.Equal(KeyModifierFlags.None, modifiers);
    }

    [Fact]
    public void Parse_HandlesCtrlModifier()
    {
        // Act
        var (key, modifiers) = KeyBinding.Parse("Ctrl+C");

        // Assert
        Assert.Equal("C", key);
        Assert.Equal(KeyModifierFlags.Control, modifiers);
    }

    [Fact]
    public void Parse_HandlesMultipleModifiers()
    {
        // Act
        var (key, modifiers) = KeyBinding.Parse("Ctrl+Shift+C");

        // Assert
        Assert.Equal("C", key);
        Assert.Equal(KeyModifierFlags.Control | KeyModifierFlags.Shift, modifiers);
    }

    [Fact]
    public void Parse_HandlesCaseInsensitiveModifiers()
    {
        // Act
        var (key, modifiers) = KeyBinding.Parse("ctrl+SHIFT+c");

        // Assert
        Assert.Equal("C", key);
        Assert.Equal(KeyModifierFlags.Control | KeyModifierFlags.Shift, modifiers);
    }

    [Fact]
    public void Parse_HandlesSpecialCharacters()
    {
        // Act - backtick
        var (key1, _) = KeyBinding.Parse("Ctrl+`");

        // Assert
        Assert.Equal("OemTilde", key1);
    }

    [Fact]
    public void Parse_HandlesArrowSymbols()
    {
        // Act
        var (key, _) = KeyBinding.Parse("Shift+↑");

        // Assert
        Assert.Equal("Up", key);
    }

    [Fact]
    public void Parse_ThrowsOnEmptyString()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => KeyBinding.Parse(""));
        Assert.Throws<ArgumentException>(() => KeyBinding.Parse("   "));
    }

    #endregion

    #region WithKey Tests

    [Fact]
    public void WithKey_CreatesNewInstance()
    {
        // Arrange
        var original = new KeyBinding
        {
            Action = TerminalShortcutAction.Copy,
            Key = "C",
            Modifiers = KeyModifierFlags.Control | KeyModifierFlags.Shift,
            Description = "Copy",
            Category = "Terminal Selection",
            IsCustomizable = true,
            PassToPty = false
        };

        // Act
        var modified = original.WithKey("V", KeyModifierFlags.Control);

        // Assert - Different key
        Assert.Equal("V", modified.Key);
        Assert.Equal(KeyModifierFlags.Control, modified.Modifiers);

        // Assert - Same metadata
        Assert.Equal(original.Action, modified.Action);
        Assert.Equal(original.Description, modified.Description);
        Assert.Equal(original.Category, modified.Category);
        Assert.Equal(original.IsCustomizable, modified.IsCustomizable);
        Assert.Equal(original.PassToPty, modified.PassToPty);

        // Assert - Original unchanged
        Assert.Equal("C", original.Key);
        Assert.Equal(KeyModifierFlags.Control | KeyModifierFlags.Shift, original.Modifiers);
    }

    #endregion

    #region Matches Tests

    [Fact]
    public void Matches_ReturnsTrueForExactMatch()
    {
        // Arrange
        var binding = new KeyBinding
        {
            Key = "C",
            Modifiers = KeyModifierFlags.Control | KeyModifierFlags.Shift
        };

        // Act & Assert
        Assert.True(binding.Matches("C", KeyModifierFlags.Control | KeyModifierFlags.Shift));
    }

    [Fact]
    public void Matches_ReturnsTrueForCaseInsensitiveKey()
    {
        // Arrange
        var binding = new KeyBinding { Key = "C", Modifiers = KeyModifierFlags.Control };

        // Act & Assert
        Assert.True(binding.Matches("c", KeyModifierFlags.Control));
    }

    [Fact]
    public void Matches_ReturnsFalseForDifferentKey()
    {
        // Arrange
        var binding = new KeyBinding { Key = "C", Modifiers = KeyModifierFlags.Control };

        // Act & Assert
        Assert.False(binding.Matches("V", KeyModifierFlags.Control));
    }

    [Fact]
    public void Matches_ReturnsFalseForDifferentModifiers()
    {
        // Arrange
        var binding = new KeyBinding { Key = "C", Modifiers = KeyModifierFlags.Control };

        // Act & Assert
        Assert.False(binding.Matches("C", KeyModifierFlags.Control | KeyModifierFlags.Shift));
    }

    #endregion

    #region HasModifiers Tests

    [Fact]
    public void HasModifiers_ReturnsFalseForNone()
    {
        // Arrange
        var binding = new KeyBinding { Key = "F3", Modifiers = KeyModifierFlags.None };

        // Assert
        Assert.False(binding.HasModifiers);
    }

    [Fact]
    public void HasModifiers_ReturnsTrueForAnyModifier()
    {
        // Arrange
        var binding = new KeyBinding { Key = "F3", Modifiers = KeyModifierFlags.Shift };

        // Assert
        Assert.True(binding.HasModifiers);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_ReturnsTrueForSameValues()
    {
        // Arrange
        var binding1 = new KeyBinding
        {
            Action = TerminalShortcutAction.Copy,
            Key = "C",
            Modifiers = KeyModifierFlags.Control | KeyModifierFlags.Shift
        };
        var binding2 = new KeyBinding
        {
            Action = TerminalShortcutAction.Copy,
            Key = "C",
            Modifiers = KeyModifierFlags.Control | KeyModifierFlags.Shift
        };

        // Assert
        Assert.Equal(binding1, binding2);
        Assert.Equal(binding1.GetHashCode(), binding2.GetHashCode());
    }

    [Fact]
    public void Equals_ReturnsFalseForDifferentAction()
    {
        // Arrange
        var binding1 = new KeyBinding { Action = TerminalShortcutAction.Copy, Key = "C" };
        var binding2 = new KeyBinding { Action = TerminalShortcutAction.Paste, Key = "C" };

        // Assert
        Assert.NotEqual(binding1, binding2);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_IncludesDescriptionAndShortcut()
    {
        // Arrange
        var binding = new KeyBinding
        {
            Key = "C",
            Modifiers = KeyModifierFlags.Control | KeyModifierFlags.Shift,
            Description = "Copy"
        };

        // Act
        var str = binding.ToString();

        // Assert
        Assert.Contains("Copy", str);
        Assert.Contains("→", str);
    }

    #endregion
}
