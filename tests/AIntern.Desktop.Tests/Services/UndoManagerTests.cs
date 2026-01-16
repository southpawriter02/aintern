using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Services;
using Moq;
using Xunit;

namespace AIntern.Desktop.Tests.Services;

/// <summary>
/// Unit tests for v0.4.3d UndoManager.
/// </summary>
public class UndoManagerTests : IDisposable
{
    private readonly Mock<IFileChangeService> _fileChangeServiceMock = new();
    private readonly UndoManager _manager;

    public UndoManagerTests()
    {
        _manager = new UndoManager(
            _fileChangeServiceMock.Object,
            options: UndoOptions.Default);
    }

    public void Dispose()
    {
        _manager.Dispose();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_NullFileChangeService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new UndoManager(null!));
    }

    [Fact]
    public void UndoWindow_ReturnsConfiguredValue()
    {
        Assert.Equal(TimeSpan.FromMinutes(30), _manager.UndoWindow);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Property Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void PendingUndoCount_InitiallyZero()
    {
        Assert.Equal(0, _manager.PendingUndoCount);
    }

    [Fact]
    public void HasPendingUndos_InitiallyFalse()
    {
        Assert.False(_manager.HasPendingUndos);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Query Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CanUndo_NoChanges_ReturnsFalse()
    {
        Assert.False(_manager.CanUndo("/path/to/file.cs"));
    }

    [Fact]
    public void CanUndoById_InvalidId_ReturnsFalse()
    {
        Assert.False(_manager.CanUndoById(Guid.NewGuid()));
    }

    [Fact]
    public void GetTimeRemaining_NoChanges_ReturnsZero()
    {
        Assert.Equal(TimeSpan.Zero, _manager.GetTimeRemaining("/path/to/file.cs"));
    }

    [Fact]
    public void GetUndoState_NoChanges_ReturnsNull()
    {
        Assert.Null(_manager.GetUndoState("/path/to/file.cs"));
    }

    [Fact]
    public void GetAllPendingUndos_NoChanges_ReturnsEmpty()
    {
        Assert.Empty(_manager.GetAllPendingUndos());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // RegisterChange Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void RegisterChange_AddsUndoState()
    {
        var record = new FileChangeRecord
        {
            FilePath = "/path/to/file.cs",
            RelativePath = "file.cs",
            ChangeType = FileChangeType.Modified
        };

        _manager.RegisterChange(record);

        Assert.Equal(1, _manager.PendingUndoCount);
        Assert.True(_manager.HasPendingUndos);
    }

    [Fact]
    public void RegisterChange_RaisesUndoAvailableEvent()
    {
        var eventRaised = false;
        _manager.UndoAvailable += (_, _) => eventRaised = true;

        _manager.RegisterChange(new FileChangeRecord
        {
            FilePath = "/test.cs",
            ChangeType = FileChangeType.Created
        });

        Assert.True(eventRaised);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Timer Management Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void PauseCountdown_InvalidId_ReturnsFalse()
    {
        Assert.False(_manager.PauseCountdown(Guid.NewGuid()));
    }

    [Fact]
    public void ResumeCountdown_InvalidId_ReturnsFalse()
    {
        Assert.False(_manager.ResumeCountdown(Guid.NewGuid()));
    }

    [Fact]
    public void ExtendTime_InvalidId_ReturnsFalse()
    {
        Assert.False(_manager.ExtendTime(Guid.NewGuid(), TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void Dismiss_InvalidId_ReturnsFalse()
    {
        Assert.False(_manager.Dismiss(Guid.NewGuid()));
    }

    [Fact]
    public void Dismiss_ValidId_RemovesUndo()
    {
        var record = new FileChangeRecord
        {
            FilePath = "/test.cs",
            ChangeType = FileChangeType.Modified
        };
        _manager.RegisterChange(record);
        Assert.Equal(1, _manager.PendingUndoCount);

        var success = _manager.Dismiss(record.Id);

        Assert.True(success);
        Assert.Equal(0, _manager.PendingUndoCount);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // UndoState Model Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void UndoState_TimeRemaining_CalculatesCorrectly()
    {
        var state = new UndoState
        {
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow + TimeSpan.FromMinutes(30)
        };

        Assert.True(state.TimeRemaining.TotalMinutes > 29);
        Assert.False(state.IsExpired);
    }

    [Fact]
    public void UndoState_IsExpired_ReturnsTrueWhenPast()
    {
        var state = new UndoState
        {
            CreatedAt = DateTime.UtcNow - TimeSpan.FromHours(1),
            ExpiresAt = DateTime.UtcNow - TimeSpan.FromMinutes(30)
        };

        Assert.True(state.IsExpired);
        Assert.Equal(TimeSpan.Zero, state.TimeRemaining);
    }

    [Fact]
    public void UndoState_FormattedTimeRemaining_FormatsCorrectly()
    {
        var state = new UndoState
        {
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow + TimeSpan.FromMinutes(28) + TimeSpan.FromSeconds(45)
        };

        Assert.Contains(":", state.FormattedTimeRemaining);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // UndoOptions Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void UndoOptions_Default_Has30MinuteWindow()
    {
        Assert.Equal(TimeSpan.FromMinutes(30), UndoOptions.Default.UndoWindow);
    }

    [Fact]
    public void UndoOptions_Quick_Has5MinuteWindow()
    {
        Assert.Equal(TimeSpan.FromMinutes(5), UndoOptions.Quick.UndoWindow);
    }

    [Fact]
    public void UndoOptions_Extended_Has1HourWindow()
    {
        Assert.Equal(TimeSpan.FromHours(1), UndoOptions.Extended.UndoWindow);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Dispose Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var manager = new UndoManager(_fileChangeServiceMock.Object);
        manager.Dispose();
        manager.Dispose(); // Should not throw
    }
}
