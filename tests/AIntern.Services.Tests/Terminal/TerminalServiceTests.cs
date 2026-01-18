using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;
using AIntern.Services.Terminal;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AIntern.Services.Tests.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TERMINAL SERVICE TESTS (v0.5.1d)                                        │
// │ Unit tests for the TerminalService implementation.                      │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Unit tests for <see cref="TerminalService"/>.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.1d.</para>
/// <para>
/// Note: These tests use mocked dependencies to test the service logic
/// without spawning actual PTY processes. Integration tests that spawn
/// real terminals would be more complex and platform-dependent.
/// </para>
/// </remarks>
public class TerminalServiceTests : IAsyncDisposable
{
    // ─────────────────────────────────────────────────────────────────────
    // Fields
    // ─────────────────────────────────────────────────────────────────────

    private readonly Mock<IShellDetectionService> _mockShellDetection;
    private readonly Mock<ILogger<TerminalService>> _mockLogger;
    private readonly TerminalService _service;

    // ─────────────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new test instance.
    /// </summary>
    public TerminalServiceTests()
    {
        _mockShellDetection = new Mock<IShellDetectionService>();
        _mockLogger = new Mock<ILogger<TerminalService>>();

        // Setup default shell detection
        _mockShellDetection
            .Setup(s => s.DetectDefaultShellAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShellInfo
            {
                Path = "/bin/bash",
                ShellType = ShellType.Bash,
                DefaultArguments = ["--login"]
            });

        _service = new TerminalService(_mockShellDetection.Object, _mockLogger.Object);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _service.DisposeAsync();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Constructor Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> Constructor throws ArgumentNullException when shellDetection is null.<br/>
    /// <b>Arrange:</b> Null shell detection service.<br/>
    /// <b>Act:</b> Attempt to create TerminalService.<br/>
    /// <b>Assert:</b> ArgumentNullException is thrown.
    /// </summary>
    [Fact]
    public void Constructor_NullShellDetection_ThrowsArgumentNullException()
    {
        // Arrange
        IShellDetectionService? shellDetection = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(
            () => new TerminalService(shellDetection!, _mockLogger.Object));
        Assert.Equal("shellDetection", ex.ParamName);
    }

    /// <summary>
    /// <b>Unit Test:</b> Constructor throws ArgumentNullException when logger is null.<br/>
    /// <b>Arrange:</b> Null logger.<br/>
    /// <b>Act:</b> Attempt to create TerminalService.<br/>
    /// <b>Assert:</b> ArgumentNullException is thrown.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        ILogger<TerminalService>? logger = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(
            () => new TerminalService(_mockShellDetection.Object, logger!));
        Assert.Equal("logger", ex.ParamName);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Sessions Property Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> Sessions returns empty list initially.<br/>
    /// <b>Arrange:</b> New TerminalService.<br/>
    /// <b>Act:</b> Access Sessions property.<br/>
    /// <b>Assert:</b> Returns empty list.
    /// </summary>
    [Fact]
    public void Sessions_InitiallyEmpty_ReturnsEmptyList()
    {
        // Act
        var sessions = _service.Sessions;

        // Assert
        Assert.Empty(sessions);
    }

    // ─────────────────────────────────────────────────────────────────────
    // ActiveSession Property Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> ActiveSession returns null initially.<br/>
    /// <b>Arrange:</b> New TerminalService.<br/>
    /// <b>Act:</b> Access ActiveSession property.<br/>
    /// <b>Assert:</b> Returns null.
    /// </summary>
    [Fact]
    public void ActiveSession_InitiallyNull_ReturnsNull()
    {
        // Act
        var activeSession = _service.ActiveSession;

        // Assert
        Assert.Null(activeSession);
    }

    // ─────────────────────────────────────────────────────────────────────
    // SetActiveSession Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> SetActiveSession returns false for non-existent session.<br/>
    /// <b>Arrange:</b> Random GUID.<br/>
    /// <b>Act:</b> Call SetActiveSession.<br/>
    /// <b>Assert:</b> Returns false.
    /// </summary>
    [Fact]
    public void SetActiveSession_NonExistentSession_ReturnsFalse()
    {
        // Arrange
        var randomId = Guid.NewGuid();

        // Act
        var result = _service.SetActiveSession(randomId);

        // Assert
        Assert.False(result);
    }

    // ─────────────────────────────────────────────────────────────────────
    // WriteInputAsync Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> WriteInputAsync returns false for non-existent session.<br/>
    /// <b>Arrange:</b> Random GUID and test input.<br/>
    /// <b>Act:</b> Call WriteInputAsync.<br/>
    /// <b>Assert:</b> Returns false.
    /// </summary>
    [Fact]
    public async Task WriteInputAsync_NonExistentSession_ReturnsFalse()
    {
        // Arrange
        var randomId = Guid.NewGuid();

        // Act
        var result = await _service.WriteInputAsync(randomId, "test");

        // Assert
        Assert.False(result);
    }

    // ─────────────────────────────────────────────────────────────────────
    // ResizeAsync Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> ResizeAsync returns false for non-existent session.<br/>
    /// <b>Arrange:</b> Random GUID.<br/>
    /// <b>Act:</b> Call ResizeAsync.<br/>
    /// <b>Assert:</b> Returns false.
    /// </summary>
    [Fact]
    public async Task ResizeAsync_NonExistentSession_ReturnsFalse()
    {
        // Arrange
        var randomId = Guid.NewGuid();

        // Act
        var result = await _service.ResizeAsync(randomId, 120, 40);

        // Assert
        Assert.False(result);
    }

    // ─────────────────────────────────────────────────────────────────────
    // SendSignalAsync Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> SendSignalAsync returns false for non-existent session.<br/>
    /// <b>Arrange:</b> Random GUID.<br/>
    /// <b>Act:</b> Call SendSignalAsync.<br/>
    /// <b>Assert:</b> Returns false.
    /// </summary>
    [Fact]
    public async Task SendSignalAsync_NonExistentSession_ReturnsFalse()
    {
        // Arrange
        var randomId = Guid.NewGuid();

        // Act
        var result = await _service.SendSignalAsync(randomId, TerminalSignal.Interrupt);

        // Assert
        Assert.False(result);
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetBuffer Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> GetBuffer returns null for non-existent session.<br/>
    /// <b>Arrange:</b> Random GUID.<br/>
    /// <b>Act:</b> Call GetBuffer.<br/>
    /// <b>Assert:</b> Returns null.
    /// </summary>
    [Fact]
    public void GetBuffer_NonExistentSession_ReturnsNull()
    {
        // Arrange
        var randomId = Guid.NewGuid();

        // Act
        var buffer = _service.GetBuffer(randomId);

        // Assert
        Assert.Null(buffer);
    }

    // ─────────────────────────────────────────────────────────────────────
    // ChangeDirectoryAsync Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> ChangeDirectoryAsync returns false for non-existent session.<br/>
    /// <b>Arrange:</b> Random GUID.<br/>
    /// <b>Act:</b> Call ChangeDirectoryAsync.<br/>
    /// <b>Assert:</b> Returns false.
    /// </summary>
    [Fact]
    public async Task ChangeDirectoryAsync_NonExistentSession_ReturnsFalse()
    {
        // Arrange
        var randomId = Guid.NewGuid();

        // Act
        var result = await _service.ChangeDirectoryAsync(randomId, "/tmp");

        // Assert
        Assert.False(result);
    }

    // ─────────────────────────────────────────────────────────────────────
    // ExecuteCommandAsync Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> ExecuteCommandAsync returns false for non-existent session.<br/>
    /// <b>Arrange:</b> Random GUID.<br/>
    /// <b>Act:</b> Call ExecuteCommandAsync.<br/>
    /// <b>Assert:</b> Returns false.
    /// </summary>
    [Fact]
    public async Task ExecuteCommandAsync_NonExistentSession_ReturnsFalse()
    {
        // Arrange
        var randomId = Guid.NewGuid();

        // Act
        var result = await _service.ExecuteCommandAsync(randomId, "ls -la");

        // Assert
        Assert.False(result);
    }

    // ─────────────────────────────────────────────────────────────────────
    // CloseSessionAsync Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> CloseSessionAsync returns false for non-existent session.<br/>
    /// <b>Arrange:</b> Random GUID.<br/>
    /// <b>Act:</b> Call CloseSessionAsync.<br/>
    /// <b>Assert:</b> Returns false.
    /// </summary>
    [Fact]
    public async Task CloseSessionAsync_NonExistentSession_ReturnsFalse()
    {
        // Arrange
        var randomId = Guid.NewGuid();

        // Act
        var result = await _service.CloseSessionAsync(randomId);

        // Assert
        Assert.False(result);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Disposal Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> Sessions throws after disposal.<br/>
    /// <b>Arrange:</b> Dispose service.<br/>
    /// <b>Act:</b> Access Sessions property.<br/>
    /// <b>Assert:</b> ObjectDisposedException is thrown.
    /// </summary>
    [Fact]
    public async Task Sessions_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        var service = new TerminalService(_mockShellDetection.Object, _mockLogger.Object);
        await service.DisposeAsync();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => service.Sessions);
    }

    /// <summary>
    /// <b>Unit Test:</b> ActiveSession throws after disposal.<br/>
    /// <b>Arrange:</b> Dispose service.<br/>
    /// <b>Act:</b> Access ActiveSession property.<br/>
    /// <b>Assert:</b> ObjectDisposedException is thrown.
    /// </summary>
    [Fact]
    public async Task ActiveSession_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        var service = new TerminalService(_mockShellDetection.Object, _mockLogger.Object);
        await service.DisposeAsync();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => service.ActiveSession);
    }

    /// <summary>
    /// <b>Unit Test:</b> SetActiveSession throws after disposal.<br/>
    /// <b>Arrange:</b> Dispose service.<br/>
    /// <b>Act:</b> Call SetActiveSession.<br/>
    /// <b>Assert:</b> ObjectDisposedException is thrown.
    /// </summary>
    [Fact]
    public async Task SetActiveSession_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        var service = new TerminalService(_mockShellDetection.Object, _mockLogger.Object);
        await service.DisposeAsync();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => service.SetActiveSession(Guid.NewGuid()));
    }

    /// <summary>
    /// <b>Unit Test:</b> WriteInputAsync throws after disposal.<br/>
    /// <b>Arrange:</b> Dispose service.<br/>
    /// <b>Act:</b> Call WriteInputAsync.<br/>
    /// <b>Assert:</b> ObjectDisposedException is thrown.
    /// </summary>
    [Fact]
    public async Task WriteInputAsync_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        var service = new TerminalService(_mockShellDetection.Object, _mockLogger.Object);
        await service.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => service.WriteInputAsync(Guid.NewGuid(), "test"));
    }

    /// <summary>
    /// <b>Unit Test:</b> ResizeAsync throws after disposal.<br/>
    /// <b>Arrange:</b> Dispose service.<br/>
    /// <b>Act:</b> Call ResizeAsync.<br/>
    /// <b>Assert:</b> ObjectDisposedException is thrown.
    /// </summary>
    [Fact]
    public async Task ResizeAsync_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        var service = new TerminalService(_mockShellDetection.Object, _mockLogger.Object);
        await service.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => service.ResizeAsync(Guid.NewGuid(), 80, 24));
    }

    /// <summary>
    /// <b>Unit Test:</b> CloseSessionAsync throws after disposal.<br/>
    /// <b>Arrange:</b> Dispose service.<br/>
    /// <b>Act:</b> Call CloseSessionAsync.<br/>
    /// <b>Assert:</b> ObjectDisposedException is thrown.
    /// </summary>
    [Fact]
    public async Task CloseSessionAsync_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        var service = new TerminalService(_mockShellDetection.Object, _mockLogger.Object);
        await service.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => service.CloseSessionAsync(Guid.NewGuid()));
    }

    /// <summary>
    /// <b>Unit Test:</b> GetBuffer throws after disposal.<br/>
    /// <b>Arrange:</b> Dispose service.<br/>
    /// <b>Act:</b> Call GetBuffer.<br/>
    /// <b>Assert:</b> ObjectDisposedException is thrown.
    /// </summary>
    [Fact]
    public async Task GetBuffer_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        var service = new TerminalService(_mockShellDetection.Object, _mockLogger.Object);
        await service.DisposeAsync();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => service.GetBuffer(Guid.NewGuid()));
    }

    /// <summary>
    /// <b>Unit Test:</b> Multiple disposals do not throw.<br/>
    /// <b>Arrange:</b> TerminalService.<br/>
    /// <b>Act:</b> Dispose twice.<br/>
    /// <b>Assert:</b> No exception is thrown.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_MultipleTimes_DoesNotThrow()
    {
        // Arrange
        var service = new TerminalService(_mockShellDetection.Object, _mockLogger.Object);

        // Act & Assert - should not throw
        await service.DisposeAsync();
        await service.DisposeAsync();
    }
}
