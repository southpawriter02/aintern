namespace AIntern.Desktop.Tests.ViewModels;

using System;
using AIntern.Desktop.ViewModels;
using AIntern.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for <see cref="RecentWorkspaceItemViewModel"/>.
/// </summary>
public class RecentWorkspaceItemViewModelTests
{
    #region Constructor

    [Fact]
    public void Constructor_WithWorkspace_SetsProperties()
    {
        // Arrange
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            RootPath = "/Users/test/project",
            Name = "Test Project",
            LastAccessedAt = DateTime.UtcNow,
            IsPinned = true
        };

        // Act
        var sut = new RecentWorkspaceItemViewModel(workspace);

        // Assert
        Assert.Equal(workspace.Id, sut.Id);
        Assert.Equal("Test Project", sut.Name);
        Assert.Equal(workspace.RootPath, sut.RootPath);
        Assert.True(sut.IsPinned);
    }

    #endregion

    #region ShortenPath

    [Fact]
    public void ShortenPath_WithHomeDir_ReplacesWithTilde()
    {
        // Arrange
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var path = Path.Combine(home, "Documents", "project");

        // Act
        var result = RecentWorkspaceItemViewModel.ShortenPath(path);

        // Assert
        Assert.StartsWith("~", result);
        Assert.Contains("Documents", result);
    }

    [Fact]
    public void ShortenPath_WithNonHomePath_ReturnsUnchanged()
    {
        // Arrange
        var path = "/var/www/html";

        // Act
        var result = RecentWorkspaceItemViewModel.ShortenPath(path);

        // Assert
        Assert.Equal(path, result);
    }

    [Fact]
    public void ShortenPath_WithEmptyPath_ReturnsEmpty()
    {
        // Act
        var result = RecentWorkspaceItemViewModel.ShortenPath(string.Empty);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region FormatTimeAgo

    [Fact]
    public void FormatTimeAgo_JustNow_ReturnsJustNow()
    {
        // Arrange
        var time = DateTime.UtcNow.AddSeconds(-30);

        // Act
        var result = RecentWorkspaceItemViewModel.FormatTimeAgo(time);

        // Assert
        Assert.Equal("Just now", result);
    }

    [Fact]
    public void FormatTimeAgo_Minutes_ReturnsMinutesAgo()
    {
        // Arrange
        var time = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var result = RecentWorkspaceItemViewModel.FormatTimeAgo(time);

        // Assert
        Assert.Equal("5m ago", result);
    }

    [Fact]
    public void FormatTimeAgo_Hours_ReturnsHoursAgo()
    {
        // Arrange
        var time = DateTime.UtcNow.AddHours(-3);

        // Act
        var result = RecentWorkspaceItemViewModel.FormatTimeAgo(time);

        // Assert
        Assert.Equal("3h ago", result);
    }

    [Fact]
    public void FormatTimeAgo_Days_ReturnsDaysAgo()
    {
        // Arrange
        var time = DateTime.UtcNow.AddDays(-2);

        // Act
        var result = RecentWorkspaceItemViewModel.FormatTimeAgo(time);

        // Assert
        Assert.Equal("2d ago", result);
    }

    [Fact]
    public void FormatTimeAgo_Weeks_ReturnsWeeksAgo()
    {
        // Arrange
        var time = DateTime.UtcNow.AddDays(-14);

        // Act
        var result = RecentWorkspaceItemViewModel.FormatTimeAgo(time);

        // Assert
        Assert.Equal("2w ago", result);
    }

    [Fact]
    public void FormatTimeAgo_OlderThan30Days_ReturnsDateFormat()
    {
        // Arrange
        var time = DateTime.UtcNow.AddDays(-45);

        // Act
        var result = RecentWorkspaceItemViewModel.FormatTimeAgo(time);

        // Assert
        Assert.Matches(@"[A-Z][a-z]{2} \d+", result);
    }

    #endregion

    #region PinTooltip

    [Fact]
    public void PinTooltip_WhenPinned_ReturnsUnpin()
    {
        // Arrange
        var sut = new RecentWorkspaceItemViewModel { IsPinned = true };

        // Assert
        Assert.Equal("Unpin", sut.PinTooltip);
    }

    [Fact]
    public void PinTooltip_WhenNotPinned_ReturnsPinToTop()
    {
        // Arrange
        var sut = new RecentWorkspaceItemViewModel { IsPinned = false };

        // Assert
        Assert.Equal("Pin to top", sut.PinTooltip);
    }

    #endregion
}
