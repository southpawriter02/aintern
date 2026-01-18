using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;
using AIntern.Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AIntern.Desktop.Tests.ViewModels;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SHELL SELECTOR VIEWMODEL TESTS (v0.5.3f)                                │
// │ Unit tests for shell profile selector dialog ViewModel.                 │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Unit tests for <see cref="ShellSelectorViewModel"/>.
/// </summary>
public sealed class ShellSelectorViewModelTests
{
    // ─────────────────────────────────────────────────────────────────────
    // Test Fixtures
    // ─────────────────────────────────────────────────────────────────────

    private readonly Mock<IShellProfileService> _mockProfileService;
    private readonly Mock<IShellDetectionService> _mockShellDetection;
    private readonly Mock<ILogger<ShellSelectorViewModel>> _mockLogger;
    private readonly ShellSelectorViewModel _viewModel;

    private readonly List<ShellProfile> _testProfiles;

    public ShellSelectorViewModelTests()
    {
        _mockProfileService = new Mock<IShellProfileService>();
        _mockShellDetection = new Mock<IShellDetectionService>();
        _mockLogger = new Mock<ILogger<ShellSelectorViewModel>>();

        _testProfiles = new List<ShellProfile>
        {
            new ShellProfile { Id = Guid.NewGuid(), Name = "Bash", ShellPath = "/bin/bash", IsDefault = true },
            new ShellProfile { Id = Guid.NewGuid(), Name = "Zsh", ShellPath = "/bin/zsh" },
            new ShellProfile { Id = Guid.NewGuid(), Name = "PowerShell", ShellPath = "/usr/local/bin/pwsh", IsBuiltIn = true }
        };

        _mockProfileService.Setup(s => s.GetVisibleProfilesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testProfiles);

        _viewModel = new ShellSelectorViewModel(
            _mockProfileService.Object,
            _mockShellDetection.Object,
            _mockLogger.Object);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Profile Loading Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> LoadProfiles populates collection.<br/>
    /// </summary>
    [Fact]
    public async Task LoadProfilesAsync_PopulatesCollection()
    {
        // Act
        await _viewModel.LoadProfilesAsync();

        // Assert
        Assert.Equal(3, _viewModel.Profiles.Count);
    }

    /// <summary>
    /// <b>Unit Test:</b> LoadProfiles selects default profile.<br/>
    /// </summary>
    [Fact]
    public async Task LoadProfilesAsync_SelectsDefaultProfile()
    {
        // Act
        await _viewModel.LoadProfilesAsync();

        // Assert
        Assert.NotNull(_viewModel.SelectedProfile);
        Assert.True(_viewModel.SelectedProfile.IsDefault);
        Assert.Equal("Bash", _viewModel.SelectedProfile.Name);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Selection Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> SelectProfile sets selected profile.<br/>
    /// </summary>
    [Fact]
    public async Task SelectProfile_SetsSelected()
    {
        // Arrange
        await _viewModel.LoadProfilesAsync();
        var profile = _viewModel.Profiles[1];

        // Act
        _viewModel.SelectProfileCommand.Execute(profile);

        // Assert
        Assert.Equal(profile, _viewModel.SelectedProfile);
    }

    /// <summary>
    /// <b>Unit Test:</b> ConfirmSelection sets result.<br/>
    /// </summary>
    [Fact]
    public async Task ConfirmSelection_SetsResult()
    {
        // Arrange
        await _viewModel.LoadProfilesAsync();
        var expected = _viewModel.SelectedProfile;

        // Act
        _viewModel.ConfirmSelectionCommand.Execute(null);

        // Assert
        Assert.True(_viewModel.IsConfirmed);
        Assert.Equal(expected, _viewModel.Result);
    }

    // ─────────────────────────────────────────────────────────────────────
    // New Profile Form Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> ShowNewProfile shows form.<br/>
    /// </summary>
    [Fact]
    public void ShowNewProfile_ShowsForm()
    {
        // Act
        _viewModel.ShowNewProfileCommand.Execute(null);

        // Assert
        Assert.True(_viewModel.ShowNewProfileForm);
        Assert.Equal(string.Empty, _viewModel.NewProfileName);
        Assert.Equal(string.Empty, _viewModel.NewProfilePath);
    }

    /// <summary>
    /// <b>Unit Test:</b> CancelNewProfile hides form.<br/>
    /// </summary>
    [Fact]
    public void CancelNewProfile_HidesForm()
    {
        // Arrange
        _viewModel.ShowNewProfileCommand.Execute(null);

        // Act
        _viewModel.CancelNewProfileCommand.Execute(null);

        // Assert
        Assert.False(_viewModel.ShowNewProfileForm);
    }

    /// <summary>
    /// <b>Unit Test:</b> CreateProfile validates empty name.<br/>
    /// </summary>
    [Fact]
    public async Task CreateProfileAsync_ValidatesEmptyName()
    {
        // Arrange
        _viewModel.ShowNewProfileCommand.Execute(null);
        // Leave name empty

        // Act
        await _viewModel.CreateProfileCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("Profile name is required", _viewModel.ValidationError);
    }

    /// <summary>
    /// <b>Unit Test:</b> CreateProfile validates empty path.<br/>
    /// </summary>
    [Fact]
    public async Task CreateProfileAsync_ValidatesEmptyPath()
    {
        // Arrange
        _viewModel.ShowNewProfileCommand.Execute(null);
        _viewModel.NewProfileName = "Test";
        // Leave path empty

        // Act
        await _viewModel.CreateProfileCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("Shell path is required", _viewModel.ValidationError);
    }

    /// <summary>
    /// <b>Unit Test:</b> CreateProfile validates invalid path.<br/>
    /// </summary>
    [Fact]
    public async Task CreateProfileAsync_ValidatesInvalidPath()
    {
        // Arrange
        _viewModel.ShowNewProfileCommand.Execute(null);
        _viewModel.NewProfileName = "Test";
        _viewModel.NewProfilePath = "/nonexistent/shell";
        _mockShellDetection.Setup(s => s.ValidateShellPath("/nonexistent/shell")).Returns(false);

        // Act
        await _viewModel.CreateProfileCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains("Invalid shell path", _viewModel.ValidationError);
    }

    /// <summary>
    /// <b>Unit Test:</b> CreateProfile adds to collection.<br/>
    /// </summary>
    [Fact]
    public async Task CreateProfileAsync_AddsToCollection()
    {
        // Arrange
        var newId = Guid.NewGuid();
        _viewModel.ShowNewProfileCommand.Execute(null);
        _viewModel.NewProfileName = "New Shell";
        _viewModel.NewProfilePath = "/bin/sh";
        _mockShellDetection.Setup(s => s.ValidateShellPath("/bin/sh")).Returns(true);
        _mockShellDetection.Setup(s => s.DetectShellType("/bin/sh")).Returns(ShellType.Bash);
        _mockProfileService.Setup(s => s.CreateProfileAsync(It.IsAny<ShellProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShellProfile { Id = newId, Name = "New Shell", ShellPath = "/bin/sh" });

        // Act
        await _viewModel.CreateProfileCommand.ExecuteAsync(null);

        // Assert
        Assert.False(_viewModel.ShowNewProfileForm);
        _mockProfileService.Verify(s => s.CreateProfileAsync(It.IsAny<ShellProfile>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Profile Management Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> SetAsDefault calls service.<br/>
    /// </summary>
    [Fact]
    public async Task SetAsDefaultAsync_CallsService()
    {
        // Arrange
        await _viewModel.LoadProfilesAsync();
        var profile = _viewModel.Profiles[1];

        // Act
        await _viewModel.SetAsDefaultCommand.ExecuteAsync(profile);

        // Assert
        _mockProfileService.Verify(s => s.SetDefaultProfileAsync(profile.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// <b>Unit Test:</b> DeleteProfile rejects built-in.<br/>
    /// </summary>
    [Fact]
    public async Task DeleteProfileAsync_RejectsBuiltIn()
    {
        // Arrange
        await _viewModel.LoadProfilesAsync();
        var builtin = _viewModel.Profiles.First(p => p.IsBuiltIn);

        // Act
        await _viewModel.DeleteProfileCommand.ExecuteAsync(builtin);

        // Assert - should not call delete
        _mockProfileService.Verify(s => s.DeleteProfileAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// <b>Unit Test:</b> DeleteProfile removes non-built-in.<br/>
    /// </summary>
    [Fact]
    public async Task DeleteProfileAsync_RemovesNonBuiltIn()
    {
        // Arrange
        await _viewModel.LoadProfilesAsync();
        var profile = _viewModel.Profiles.First(p => !p.IsBuiltIn && !p.IsDefault);

        // Act
        await _viewModel.DeleteProfileCommand.ExecuteAsync(profile);

        // Assert
        _mockProfileService.Verify(s => s.DeleteProfileAsync(profile.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// <b>Unit Test:</b> DuplicateProfile calls service.<br/>
    /// </summary>
    [Fact]
    public async Task DuplicateProfileAsync_CallsService()
    {
        // Arrange
        await _viewModel.LoadProfilesAsync();
        var profile = _viewModel.Profiles[0];
        _mockProfileService.Setup(s => s.DuplicateProfileAsync(profile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShellProfile { Id = Guid.NewGuid(), Name = "Bash (copy)" });

        // Act
        await _viewModel.DuplicateProfileCommand.ExecuteAsync(profile);

        // Assert
        _mockProfileService.Verify(s => s.DuplicateProfileAsync(profile.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
