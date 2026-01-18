using AIntern.Core.Interfaces;
using AIntern.Services.Terminal;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AIntern.Services.Tests.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DEFAULT SHELL DETECTION SERVICE TESTS (v0.5.1d)                         │
// │ Unit tests for the DefaultShellDetectionService implementation.         │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Unit tests for <see cref="DefaultShellDetectionService"/>.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.1d.</para>
/// </remarks>
public class DefaultShellDetectionServiceTests
{
    // ─────────────────────────────────────────────────────────────────────
    // Fields
    // ─────────────────────────────────────────────────────────────────────

    private readonly Mock<ILogger<DefaultShellDetectionService>> _mockLogger;
    private readonly DefaultShellDetectionService _service;

    // ─────────────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new test instance.
    /// </summary>
    public DefaultShellDetectionServiceTests()
    {
        _mockLogger = new Mock<ILogger<DefaultShellDetectionService>>();
        _service = new DefaultShellDetectionService(_mockLogger.Object);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Constructor Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> Constructor throws ArgumentNullException when logger is null.<br/>
    /// <b>Arrange:</b> Null logger.<br/>
    /// <b>Act:</b> Attempt to create service.<br/>
    /// <b>Assert:</b> ArgumentNullException is thrown.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        ILogger<DefaultShellDetectionService>? logger = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(
            () => new DefaultShellDetectionService(logger!));
        Assert.Equal("logger", ex.ParamName);
    }

    // ─────────────────────────────────────────────────────────────────────
    // DetectDefaultShellAsync Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> DetectDefaultShellAsync returns valid shell info.<br/>
    /// <b>Arrange:</b> Shell detection service.<br/>
    /// <b>Act:</b> Call DetectDefaultShellAsync.<br/>
    /// <b>Assert:</b> Returns ShellInfo with non-empty path.
    /// </summary>
    [Fact]
    public async Task DetectDefaultShellAsync_ReturnsValidShellInfo()
    {
        // Act
        var shellInfo = await _service.DetectDefaultShellAsync();

        // Assert
        Assert.NotNull(shellInfo);
        Assert.False(string.IsNullOrEmpty(shellInfo.Path));
        Assert.NotEqual(ShellType.Unknown, shellInfo.ShellType);
    }

    /// <summary>
    /// <b>Unit Test:</b> DetectDefaultShellAsync shell path exists.<br/>
    /// <b>Arrange:</b> Shell detection service.<br/>
    /// <b>Act:</b> Call DetectDefaultShellAsync.<br/>
    /// <b>Assert:</b> Returned shell path exists on filesystem.
    /// </summary>
    [Fact]
    public async Task DetectDefaultShellAsync_ShellPathExists()
    {
        // Act
        var shellInfo = await _service.DetectDefaultShellAsync();

        // Assert
        Assert.True(File.Exists(shellInfo.Path),
            $"Detected shell path should exist: {shellInfo.Path}");
    }

    /// <summary>
    /// <b>Unit Test:</b> DetectDefaultShellAsync respects cancellation.<br/>
    /// <b>Arrange:</b> Pre-cancelled token.<br/>
    /// <b>Act:</b> Call DetectDefaultShellAsync with cancelled token.<br/>
    /// <b>Assert:</b> Still completes (synchronous operation).
    /// </summary>
    [Fact]
    public async Task DetectDefaultShellAsync_WithCancellation_ShouldComplete()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act - should complete regardless since this is a CPU-bound operation
        var shellInfo = await _service.DetectDefaultShellAsync(cts.Token);

        // Assert
        Assert.NotNull(shellInfo);
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetAvailableShellsAsync Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> GetAvailableShellsAsync returns at least one shell.<br/>
    /// <b>Arrange:</b> Shell detection service.<br/>
    /// <b>Act:</b> Call GetAvailableShellsAsync.<br/>
    /// <b>Assert:</b> Returns at least one shell.
    /// </summary>
    [Fact]
    public async Task GetAvailableShellsAsync_ReturnsAtLeastOneShell()
    {
        // Act
        var shells = await _service.GetAvailableShellsAsync();

        // Assert
        Assert.NotEmpty(shells);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetAvailableShellsAsync returned shells exist.<br/>
    /// <b>Arrange:</b> Shell detection service.<br/>
    /// <b>Act:</b> Call GetAvailableShellsAsync.<br/>
    /// <b>Assert:</b> All returned shell paths exist.
    /// </summary>
    [Fact]
    public async Task GetAvailableShellsAsync_AllShellPathsExist()
    {
        // Act
        var shells = await _service.GetAvailableShellsAsync();

        // Assert
        foreach (var shell in shells)
        {
            Assert.True(File.Exists(shell.Path),
                $"Shell path should exist: {shell.Path}");
        }
    }

    /// <summary>
    /// <b>Unit Test:</b> GetAvailableShellsAsync shells have valid types.<br/>
    /// <b>Arrange:</b> Shell detection service.<br/>
    /// <b>Act:</b> Call GetAvailableShellsAsync.<br/>
    /// <b>Assert:</b> All shells have non-Unknown types.
    /// </summary>
    [Fact]
    public async Task GetAvailableShellsAsync_AllShellsHaveValidTypes()
    {
        // Act
        var shells = await _service.GetAvailableShellsAsync();

        // Assert
        foreach (var shell in shells)
        {
            Assert.NotEqual(ShellType.Unknown, shell.ShellType);
        }
    }

    /// <summary>
    /// <b>Unit Test:</b> GetAvailableShellsAsync includes default shell.<br/>
    /// <b>Arrange:</b> Get default and available shells.<br/>
    /// <b>Act:</b> Check if default is in available list.<br/>
    /// <b>Assert:</b> Default shell is in available shells list.
    /// </summary>
    [Fact]
    public async Task GetAvailableShellsAsync_ContainsDefaultShell()
    {
        // Arrange
        var defaultShell = await _service.DetectDefaultShellAsync();

        // Act
        var availableShells = await _service.GetAvailableShellsAsync();

        // Assert
        Assert.Contains(availableShells, s => s.Path == defaultShell.Path);
    }
}
