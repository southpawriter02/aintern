using Moq;
using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="TerminalContextViewModel"/>.
/// </summary>
/// <remarks>Added in v0.5.4g.</remarks>
public sealed class TerminalContextViewModelTests
{
    private readonly Mock<IOutputCaptureService> _captureServiceMock = new();
    private readonly Mock<ITerminalService> _terminalServiceMock = new();
    private readonly ILogger<TerminalContextViewModel> _logger = 
        NullLogger<TerminalContextViewModel>.Instance;

    // ═══════════════════════════════════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════════════════════════════════

    private TerminalContextViewModel CreateViewModel(TerminalOutputCapture? capture = null)
    {
        var vm = new TerminalContextViewModel(
            _captureServiceMock.Object,
            _terminalServiceMock.Object,
            _logger);

        if (capture != null)
        {
            vm.Capture = capture;
        }

        return vm;
    }

    private static TerminalOutputCapture CreateCapture(
        string output = "test output",
        Guid? sessionId = null,
        string sessionName = "bash - /project",
        OutputCaptureMode mode = OutputCaptureMode.FullBuffer,
        bool isTruncated = false,
        string? command = null,
        string? workingDirectory = null)
    {
        return new TerminalOutputCapture
        {
            SessionId = sessionId ?? Guid.NewGuid(),
            SessionName = sessionName,
            Output = output,
            OriginalLength = isTruncated ? output.Length + 1000 : output.Length,
            CaptureMode = mode,
            IsTruncated = isTruncated,
            Command = command,
            WorkingDirectory = workingDirectory,
            StartedAt = DateTime.UtcNow.AddSeconds(-5),
            CompletedAt = DateTime.UtcNow
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DisplayName Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void DisplayName_WithCapture_ReturnsSessionName()
    {
        var capture = CreateCapture(sessionName: "zsh - /home/user");
        var vm = CreateViewModel(capture);

        Assert.Equal("zsh - /home/user", vm.DisplayName);
    }

    [Fact]
    public void DisplayName_WithoutCapture_ReturnsDefault()
    {
        var vm = CreateViewModel();

        Assert.Equal("Terminal Output", vm.DisplayName);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Preview Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Preview_ShortOutput_ReturnsUnchanged()
    {
        var capture = CreateCapture(output: "short output");
        var vm = CreateViewModel(capture);

        Assert.Equal("short output", vm.Preview);
    }

    [Fact]
    public void Preview_LongOutput_TruncatesTo100Chars()
    {
        var longOutput = new string('x', 200);
        var capture = CreateCapture(output: longOutput);
        var vm = CreateViewModel(capture);

        Assert.Equal(100, vm.Preview.Length);
        Assert.EndsWith("...", vm.Preview);
    }

    [Fact]
    public void Preview_ReplacesNewlines()
    {
        var capture = CreateCapture(output: "line1\nline2\r\nline3");
        var vm = CreateViewModel(capture);

        Assert.DoesNotContain("\n", vm.Preview);
        Assert.DoesNotContain("\r", vm.Preview);
    }

    [Fact]
    public void Preview_WithoutCapture_ReturnsEmpty()
    {
        var vm = CreateViewModel();

        Assert.Equal(string.Empty, vm.Preview);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void EstimatedTokens_DelegatesToCapture()
    {
        var capture = CreateCapture(output: new string('x', 100)); // ~25 tokens
        var vm = CreateViewModel(capture);

        Assert.Equal(capture.EstimatedTokens, vm.EstimatedTokens);
    }

    [Fact]
    public void IsTruncated_DelegatesToCapture()
    {
        var capture = CreateCapture(isTruncated: true);
        var vm = CreateViewModel(capture);

        Assert.True(vm.IsTruncated);
    }

    [Fact]
    public void WorkingDirectory_DelegatesToCapture()
    {
        var capture = CreateCapture(workingDirectory: "/home/user/project");
        var vm = CreateViewModel(capture);

        Assert.Equal("/home/user/project", vm.WorkingDirectory);
    }

    [Fact]
    public void Command_DelegatesToCapture()
    {
        var capture = CreateCapture(command: "npm run build");
        var vm = CreateViewModel(capture);

        Assert.Equal("npm run build", vm.Command);
    }

    [Fact]
    public void HasCommand_TrueWhenCommandSet()
    {
        var capture = CreateCapture(command: "npm test");
        var vm = CreateViewModel(capture);

        Assert.True(vm.HasCommand);
    }

    [Fact]
    public void HasCommand_FalseWhenNoCommand()
    {
        var capture = CreateCapture();
        var vm = CreateViewModel(capture);

        Assert.False(vm.HasCommand);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CaptureModeText Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(OutputCaptureMode.FullBuffer, "Full Buffer")]
    [InlineData(OutputCaptureMode.LastCommand, "Last Command")]
    [InlineData(OutputCaptureMode.LastNLines, "Last Lines")]
    [InlineData(OutputCaptureMode.Selection, "Selection")]
    [InlineData(OutputCaptureMode.Manual, "Manual")]
    public void CaptureModeText_MapsCorrectly(OutputCaptureMode mode, string expected)
    {
        var capture = CreateCapture(mode: mode);
        var vm = CreateViewModel(capture);

        Assert.Equal(expected, vm.CaptureModeText);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CaptureTime Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CaptureTime_ReturnsCompletedAt()
    {
        var capture = CreateCapture();
        var vm = CreateViewModel(capture);

        Assert.Equal(capture.CompletedAt, vm.CaptureTime);
    }

    [Fact]
    public void CaptureTimeText_FormatsLocalTime()
    {
        var capture = CreateCapture();
        var vm = CreateViewModel(capture);

        var expected = capture.CompletedAt.ToLocalTime().ToString("HH:mm:ss");
        Assert.Equal(expected, vm.CaptureTimeText);
    }

    [Fact]
    public void CaptureTimeText_WithoutCapture_ReturnsEmpty()
    {
        var vm = CreateViewModel();

        Assert.Equal("", vm.CaptureTimeText);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Truncation Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void WasTruncated_TrueWhenTruncated()
    {
        var capture = CreateCapture(isTruncated: true);
        var vm = CreateViewModel(capture);

        Assert.True(vm.WasTruncated);
    }

    [Fact]
    public void TruncationText_ShowsWhenTruncated()
    {
        var capture = CreateCapture(isTruncated: true);
        var vm = CreateViewModel(capture);

        Assert.Contains("Truncated from", vm.TruncationText);
    }

    [Fact]
    public void TruncationText_EmptyWhenNotTruncated()
    {
        var capture = CreateCapture(isTruncated: false);
        var vm = CreateViewModel(capture);

        Assert.Equal("", vm.TruncationText);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Command Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ToggleExpand_TogglesState()
    {
        var vm = CreateViewModel();

        Assert.False(vm.IsExpanded);
        vm.ToggleExpandCommand.Execute(null);
        Assert.True(vm.IsExpanded);
        vm.ToggleExpandCommand.Execute(null);
        Assert.False(vm.IsExpanded);
    }

    [Fact]
    public async Task RefreshAsync_ReCaptures()
    {
        var sessionId = Guid.NewGuid();
        var originalCapture = CreateCapture(output: "original", sessionId: sessionId);
        var newCapture = CreateCapture(output: "refreshed", sessionId: sessionId);

        _captureServiceMock
            .Setup(s => s.CaptureBufferAsync(
                sessionId,
                OutputCaptureMode.FullBuffer,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(newCapture);

        var vm = CreateViewModel(originalCapture);
        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Equal("refreshed", vm.FullOutput);
    }

    [Fact]
    public async Task RefreshAsync_WithNoCapture_DoesNothing()
    {
        var vm = CreateViewModel();
        await vm.RefreshCommand.ExecuteAsync(null);

        _captureServiceMock.Verify(
            s => s.CaptureBufferAsync(
                It.IsAny<Guid>(),
                It.IsAny<OutputCaptureMode>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // OnCaptureChanged Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void OnCaptureChanged_NotifiesAllProperties()
    {
        var vm = CreateViewModel();
        var changedProperties = new List<string?>();
        vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        vm.Capture = CreateCapture();

        Assert.Contains(nameof(TerminalContextViewModel.DisplayName), changedProperties);
        Assert.Contains(nameof(TerminalContextViewModel.Preview), changedProperties);
        Assert.Contains(nameof(TerminalContextViewModel.EstimatedTokens), changedProperties);
        Assert.Contains(nameof(TerminalContextViewModel.IsTruncated), changedProperties);
        Assert.Contains(nameof(TerminalContextViewModel.ContextString), changedProperties);
    }
}

/// <summary>
/// Unit tests for <see cref="TerminalContextViewModelFactory"/>.
/// </summary>
/// <remarks>Added in v0.5.4g.</remarks>
public sealed class TerminalContextViewModelFactoryTests
{
    private readonly Mock<IOutputCaptureService> _captureServiceMock = new();
    private readonly Mock<ITerminalService> _terminalServiceMock = new();
    private readonly ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;

    private TerminalContextViewModelFactory CreateFactory()
    {
        return new TerminalContextViewModelFactory(
            _captureServiceMock.Object,
            _terminalServiceMock.Object,
            _loggerFactory);
    }

    private static TerminalOutputCapture CreateCapture(string output = "test")
    {
        return new TerminalOutputCapture
        {
            SessionId = Guid.NewGuid(),
            SessionName = "bash",
            Output = output,
            OriginalLength = output.Length,
            CaptureMode = OutputCaptureMode.FullBuffer
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Create Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Create_SetsCapture()
    {
        var factory = CreateFactory();
        var capture = CreateCapture();

        var vm = factory.Create(capture);

        Assert.Equal(capture, vm.Capture);
    }

    [Fact]
    public void Create_NullCapture_ThrowsArgumentNull()
    {
        var factory = CreateFactory();

        Assert.Throws<ArgumentNullException>(() => factory.Create(null!));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Async Factory Methods
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateFromFullBufferAsync_CapturesFullBuffer()
    {
        var sessionId = Guid.NewGuid();
        var capture = CreateCapture("full buffer");

        _captureServiceMock
            .Setup(s => s.CaptureBufferAsync(
                sessionId,
                OutputCaptureMode.FullBuffer,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(capture);

        var factory = CreateFactory();
        var vm = await factory.CreateFromFullBufferAsync(sessionId);

        Assert.Equal(capture, vm.Capture);
    }

    [Fact]
    public async Task CreateFromLastLinesAsync_CapturesLastLines()
    {
        var sessionId = Guid.NewGuid();
        var capture = CreateCapture("last lines");

        _captureServiceMock
            .Setup(s => s.CaptureBufferAsync(
                sessionId,
                OutputCaptureMode.LastNLines,
                50,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(capture);

        var factory = CreateFactory();
        var vm = await factory.CreateFromLastLinesAsync(sessionId, 50);

        Assert.Equal(capture, vm.Capture);
    }

    [Fact]
    public async Task CreateFromSelectionAsync_ReturnsNullWhenNoSelection()
    {
        var sessionId = Guid.NewGuid();

        _captureServiceMock
            .Setup(s => s.CaptureSelectionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TerminalOutputCapture?)null);

        var factory = CreateFactory();
        var vm = await factory.CreateFromSelectionAsync(sessionId);

        Assert.Null(vm);
    }

    [Fact]
    public async Task CreateFromSelectionAsync_ReturnsViewModelWhenSelected()
    {
        var sessionId = Guid.NewGuid();
        var capture = CreateCapture("selected text");

        _captureServiceMock
            .Setup(s => s.CaptureSelectionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(capture);

        var factory = CreateFactory();
        var vm = await factory.CreateFromSelectionAsync(sessionId);

        Assert.NotNull(vm);
        Assert.Equal(capture, vm!.Capture);
    }

    [Fact]
    public async Task CreateFromActiveSessionAsync_ReturnsNullWhenNoActiveSession()
    {
        _terminalServiceMock
            .SetupGet(s => s.ActiveSession)
            .Returns((TerminalSession?)null);

        var factory = CreateFactory();
        var vm = await factory.CreateFromActiveSessionAsync();

        Assert.Null(vm);
    }
}
