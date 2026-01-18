// ============================================================================
// File: TerminalShortcutServiceTests.cs
// Path: tests/AIntern.Services.Tests/TerminalShortcutServiceTests.cs
// Description: Unit tests for TerminalShortcutService.
// Created: 2026-01-18
// AI Intern v0.5.5d - Keyboard Shortcuts System
// ============================================================================

namespace AIntern.Services.Tests;

using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Core.Models.Terminal;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ TerminalShortcutServiceTests (v0.5.5d)                                       │
// │ Unit tests for the TerminalShortcutService implementation.                  │
// └─────────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Unit tests for <see cref="TerminalShortcutService"/>.
/// </summary>
public class TerminalShortcutServiceTests : IDisposable
{
    #region Test Fixtures

    private readonly Mock<ILogger<TerminalShortcutService>> _mockLogger;
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly AppSettings _settings;
    private readonly TerminalShortcutService _sut;

    public TerminalShortcutServiceTests()
    {
        _mockLogger = new Mock<ILogger<TerminalShortcutService>>();
        _mockSettingsService = new Mock<ISettingsService>();
        _settings = new AppSettings();

        _mockSettingsService.Setup(s => s.CurrentSettings).Returns(_settings);
        _mockSettingsService.Setup(s => s.SaveSettingsAsync(It.IsAny<AppSettings>()))
            .Returns(Task.CompletedTask);

        _sut = new TerminalShortcutService(_mockLogger.Object, _mockSettingsService.Object);
    }

    public void Dispose()
    {
        // No cleanup needed
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TerminalShortcutService(null!, _mockSettingsService.Object));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenSettingsServiceIsNull()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TerminalShortcutService(_mockLogger.Object, null!));
    }

    [Fact]
    public void Constructor_InitializesDefaultBindings()
    {
        // Act
        var bindings = _sut.GetAllBindings();

        // Assert - Should have many default bindings
        Assert.True(bindings.Count >= 30, $"Expected at least 30 bindings, got {bindings.Count}");
    }

    #endregion

    #region GetAllBindings Tests

    [Fact]
    public void GetAllBindings_ReturnsAllRegisteredBindings()
    {
        // Act
        var bindings = _sut.GetAllBindings();

        // Assert
        Assert.NotEmpty(bindings);
        Assert.Contains(bindings, b => b.Action == TerminalShortcutAction.ToggleTerminal);
        Assert.Contains(bindings, b => b.Action == TerminalShortcutAction.Copy);
        Assert.Contains(bindings, b => b.Action == TerminalShortcutAction.OpenSearch);
    }

    #endregion

    #region GetBindingsByCategory Tests

    [Fact]
    public void GetBindingsByCategory_ReturnsOnlyMatchingCategory()
    {
        // Act
        var panelBindings = _sut.GetBindingsByCategory("Terminal Panel");
        var searchBindings = _sut.GetBindingsByCategory("Terminal Search");

        // Assert
        Assert.NotEmpty(panelBindings);
        Assert.All(panelBindings, b => Assert.Equal("Terminal Panel", b.Category));

        Assert.NotEmpty(searchBindings);
        Assert.All(searchBindings, b => Assert.Equal("Terminal Search", b.Category));
    }

    [Fact]
    public void GetBindingsByCategory_ReturnsEmptyForUnknownCategory()
    {
        // Act
        var bindings = _sut.GetBindingsByCategory("Unknown Category");

        // Assert
        Assert.Empty(bindings);
    }

    #endregion

    #region GetCategories Tests

    [Fact]
    public void GetCategories_ReturnsAllUniqueCategories()
    {
        // Act
        var categories = _sut.GetCategories();

        // Assert
        Assert.Contains("Terminal Panel", categories);
        Assert.Contains("Terminal Input", categories);
        Assert.Contains("Terminal Search", categories);
        Assert.Contains("Terminal Selection", categories);
        Assert.Contains("Terminal Scroll", categories);
        Assert.Contains("Command Blocks", categories);
    }

    [Fact]
    public void GetCategories_ReturnsDistinctValues()
    {
        // Act
        var categories = _sut.GetCategories();

        // Assert
        Assert.Equal(categories.Count, categories.Distinct().Count());
    }

    [Fact]
    public void GetCategories_ReturnsSortedList()
    {
        // Act
        var categories = _sut.GetCategories();

        // Assert
        var sorted = categories.OrderBy(c => c).ToList();
        Assert.Equal(sorted, categories);
    }

    #endregion

    #region GetBinding Tests

    [Fact]
    public void GetBinding_ReturnsBindingForAction()
    {
        // Act
        var binding = _sut.GetBinding(TerminalShortcutAction.Copy);

        // Assert
        Assert.NotNull(binding);
        Assert.Equal(TerminalShortcutAction.Copy, binding.Action);
        Assert.Equal("Copy", binding.Description);
    }

    [Fact]
    public void GetBinding_ReturnsNullForUnregisteredAction()
    {
        // Act - Use a valid action that might not be registered
        var binding = _sut.GetBinding((TerminalShortcutAction)9999);

        // Assert
        Assert.Null(binding);
    }

    #endregion

    #region TryGetAction Tests

    [Fact]
    public void TryGetAction_ReturnsTrueForRegisteredCombination()
    {
        // Act
        var result = _sut.TryGetAction("C", KeyModifierFlags.Control | KeyModifierFlags.Shift, out var action);

        // Assert
        Assert.True(result);
        Assert.Equal(TerminalShortcutAction.Copy, action);
    }

    [Fact]
    public void TryGetAction_ReturnsFalseForUnregisteredCombination()
    {
        // Act
        var result = _sut.TryGetAction("Z", KeyModifierFlags.Alt | KeyModifierFlags.Shift, out var action);

        // Assert
        Assert.False(result);
        Assert.Equal(default(TerminalShortcutAction), action);
    }

    [Fact]
    public void TryGetAction_DistinguishesBetweenModifiers()
    {
        // Ctrl+C = SendInterrupt (PTY)
        var result1 = _sut.TryGetAction("C", KeyModifierFlags.Control, out var action1);

        // Ctrl+Shift+C = Copy
        var result2 = _sut.TryGetAction("C", KeyModifierFlags.Control | KeyModifierFlags.Shift, out var action2);

        // Assert
        Assert.True(result1);
        Assert.True(result2);
        Assert.NotEqual(action1, action2);
        Assert.Equal(TerminalShortcutAction.SendInterrupt, action1);
        Assert.Equal(TerminalShortcutAction.Copy, action2);
    }

    #endregion

    #region GetBindingByKey Tests

    [Fact]
    public void GetBindingByKey_ReturnsBindingForCombination()
    {
        // Act
        var binding = _sut.GetBindingByKey("F", KeyModifierFlags.Control);

        // Assert
        Assert.NotNull(binding);
        Assert.Equal(TerminalShortcutAction.OpenSearch, binding.Action);
    }

    [Fact]
    public void GetBindingByKey_ReturnsNullForUnregisteredCombination()
    {
        // Act
        var binding = _sut.GetBindingByKey("X", KeyModifierFlags.Alt | KeyModifierFlags.Meta);

        // Assert
        Assert.Null(binding);
    }

    #endregion

    #region PassToPty Tests

    [Fact]
    public void PassToPty_TrueForShellShortcuts()
    {
        // Act
        var sigint = _sut.GetBinding(TerminalShortcutAction.SendInterrupt);
        var sigtstp = _sut.GetBinding(TerminalShortcutAction.SendSuspend);
        var eof = _sut.GetBinding(TerminalShortcutAction.SendEof);

        // Assert
        Assert.NotNull(sigint);
        Assert.True(sigint.PassToPty, "SIGINT should pass to PTY");

        Assert.NotNull(sigtstp);
        Assert.True(sigtstp.PassToPty, "SIGTSTP should pass to PTY");

        Assert.NotNull(eof);
        Assert.True(eof.PassToPty, "EOF should pass to PTY");
    }

    [Fact]
    public void PassToPty_FalseForAppShortcuts()
    {
        // Act
        var copy = _sut.GetBinding(TerminalShortcutAction.Copy);
        var openSearch = _sut.GetBinding(TerminalShortcutAction.OpenSearch);

        // Assert
        Assert.NotNull(copy);
        Assert.False(copy.PassToPty, "Copy should NOT pass to PTY");

        Assert.NotNull(openSearch);
        Assert.False(openSearch.PassToPty, "OpenSearch should NOT pass to PTY");
    }

    #endregion

    #region UpdateBinding Tests

    [Fact]
    public void UpdateBinding_ChangesKeySuccessfully()
    {
        // Arrange
        var originalBinding = _sut.GetBinding(TerminalShortcutAction.Copy);

        // Act
        var result = _sut.UpdateBinding(
            TerminalShortcutAction.Copy,
            "X",
            KeyModifierFlags.Control | KeyModifierFlags.Shift);

        // Assert
        Assert.True(result);

        var updatedBinding = _sut.GetBinding(TerminalShortcutAction.Copy);
        Assert.NotNull(updatedBinding);
        Assert.Equal("X", updatedBinding.Key);
    }

    [Fact]
    public void UpdateBinding_ReturnsFalseForConflict()
    {
        // Arrange - Try to assign Copy to Ctrl+F which is already OpenSearch
        var result = _sut.UpdateBinding(
            TerminalShortcutAction.Copy,
            "F",
            KeyModifierFlags.Control);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UpdateBinding_PersistsToSettings()
    {
        // Act
        _sut.UpdateBinding(
            TerminalShortcutAction.Copy,
            "X",
            KeyModifierFlags.Control | KeyModifierFlags.Shift);

        // Assert
        _mockSettingsService.Verify(
            s => s.SaveSettingsAsync(It.IsAny<AppSettings>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void UpdateBinding_RaisesBindingsChangedEvent()
    {
        // Arrange
        var eventRaised = false;
        _sut.BindingsChanged += (_, _) => eventRaised = true;

        // Act
        _sut.UpdateBinding(
            TerminalShortcutAction.Copy,
            "X",
            KeyModifierFlags.Control | KeyModifierFlags.Shift);

        // Assert
        Assert.True(eventRaised);
    }

    #endregion

    #region ResetBinding Tests

    [Fact]
    public void ResetBinding_RestoresDefault()
    {
        // Arrange - First modify the binding
        _sut.UpdateBinding(
            TerminalShortcutAction.Copy,
            "X",
            KeyModifierFlags.Control | KeyModifierFlags.Shift);

        // Act
        _sut.ResetBinding(TerminalShortcutAction.Copy);

        // Assert
        var binding = _sut.GetBinding(TerminalShortcutAction.Copy);
        Assert.NotNull(binding);
        Assert.Equal("C", binding.Key); // Default key
    }

    [Fact]
    public void ResetBinding_RaisesBindingsChangedEvent()
    {
        // Arrange
        _sut.UpdateBinding(
            TerminalShortcutAction.Copy,
            "X",
            KeyModifierFlags.Control | KeyModifierFlags.Shift);

        var eventCount = 0;
        _sut.BindingsChanged += (_, _) => eventCount++;

        // Act
        _sut.ResetBinding(TerminalShortcutAction.Copy);

        // Assert
        Assert.True(eventCount >= 1);
    }

    #endregion

    #region ResetAllBindings Tests

    [Fact]
    public void ResetAllBindings_RestoresAllDefaults()
    {
        // Arrange - Modify some bindings
        _sut.UpdateBinding(TerminalShortcutAction.Copy, "X", KeyModifierFlags.Control);
        _sut.UpdateBinding(TerminalShortcutAction.Paste, "Y", KeyModifierFlags.Control);

        // Act
        _sut.ResetAllBindings();

        // Assert
        var copy = _sut.GetBinding(TerminalShortcutAction.Copy);
        var paste = _sut.GetBinding(TerminalShortcutAction.Paste);

        Assert.Equal("C", copy?.Key);
        Assert.Equal("V", paste?.Key);
    }

    #endregion

    #region HasConflict Tests

    [Fact]
    public void HasConflict_ReturnsTrueForExistingBinding()
    {
        // Act - Ctrl+F is OpenSearch
        var result = _sut.HasConflict("F", KeyModifierFlags.Control);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasConflict_ReturnsFalseForFreeBinding()
    {
        // Act
        var result = _sut.HasConflict("Q", KeyModifierFlags.Alt | KeyModifierFlags.Meta);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasConflict_ExcludesSpecifiedAction()
    {
        // Act - Check if Ctrl+F conflicts, excluding OpenSearch
        var result = _sut.HasConflict("F", KeyModifierFlags.Control, TerminalShortcutAction.OpenSearch);

        // Assert
        Assert.False(result); // No conflict when excluding self
    }

    #endregion

    #region GetConflictingBinding Tests

    [Fact]
    public void GetConflictingBinding_ReturnsConflictingBinding()
    {
        // Act
        var conflict = _sut.GetConflictingBinding("F", KeyModifierFlags.Control);

        // Assert
        Assert.NotNull(conflict);
        Assert.Equal(TerminalShortcutAction.OpenSearch, conflict.Action);
    }

    [Fact]
    public void GetConflictingBinding_ReturnsNullWhenNoConflict()
    {
        // Act
        var conflict = _sut.GetConflictingBinding("Q", KeyModifierFlags.Alt | KeyModifierFlags.Meta);

        // Assert
        Assert.Null(conflict);
    }

    #endregion

    #region Custom Binding Persistence Tests

    [Fact]
    public void Constructor_LoadsCustomBindingsFromSettings()
    {
        // Arrange
        var customSettings = new AppSettings
        {
            CustomKeyBindings = new Dictionary<string, string>
            {
                ["Copy"] = "Ctrl+Shift+X"
            }
        };

        var mockSettings = new Mock<ISettingsService>();
        mockSettings.Setup(s => s.CurrentSettings).Returns(customSettings);
        mockSettings.Setup(s => s.SaveSettingsAsync(It.IsAny<AppSettings>()))
            .Returns(Task.CompletedTask);

        // Act
        var service = new TerminalShortcutService(_mockLogger.Object, mockSettings.Object);

        // Assert
        var binding = service.GetBinding(TerminalShortcutAction.Copy);
        Assert.NotNull(binding);
        Assert.Equal("X", binding.Key);
    }

    #endregion

    #region Terminal Tab Switch Bindings Tests

    [Fact]
    public void GetBinding_ReturnsAllTerminalTabBindings()
    {
        // Act & Assert
        for (int i = 1; i <= 9; i++)
        {
            var action = (TerminalShortcutAction)(5 + i - 1); // SwitchToTerminal1 = 5
            var binding = _sut.GetBinding(action);

            Assert.NotNull(binding);
            Assert.Equal($"D{i}", binding.Key);
            Assert.Equal(KeyModifierFlags.Control | KeyModifierFlags.Shift, binding.Modifiers);
        }
    }

    #endregion
}
