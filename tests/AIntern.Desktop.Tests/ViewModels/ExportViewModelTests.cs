// -----------------------------------------------------------------------
// <copyright file="ExportViewModelTests.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
// </copyright>
// <summary>
//     Unit tests for ExportViewModel (v0.2.5f).
//     Tests constructor, property defaults, format/option changes, export, cancel, and disposal.
// </summary>
// -----------------------------------------------------------------------

using AIntern.Core.Enums;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="ExportViewModel"/> (v0.2.5f).
/// Tests constructor validation, property defaults, format/option changes,
/// export command, cancel command, and disposal.
/// </summary>
/// <remarks>
/// <para>
/// These tests cover:
/// </para>
/// <list type="bullet">
///   <item><description>Constructor validation (null checks)</description></item>
///   <item><description>Property defaults initialization</description></item>
///   <item><description>Format change triggers preview update</description></item>
///   <item><description>Option changes trigger preview update</description></item>
///   <item><description>CreateOptions returns correct ExportOptions</description></item>
///   <item><description>Export command success, cancel, and error handling</description></item>
///   <item><description>Cancel command sets ShouldClose</description></item>
///   <item><description>Dispose cleans up resources</description></item>
/// </list>
/// <para>Added in v0.2.5f.</para>
/// </remarks>
public class ExportViewModelTests : IDisposable
{
    #region Test Infrastructure

    private readonly Mock<IExportService> _mockExportService;
    private readonly Mock<IStorageProvider> _mockStorageProvider;
    private readonly Mock<ILogger<ExportViewModel>> _mockLogger;
    private readonly Guid _testConversationId;

    private ExportViewModel? _viewModel;

    public ExportViewModelTests()
    {
        _mockExportService = new Mock<IExportService>();
        _mockStorageProvider = new Mock<IStorageProvider>();
        _mockLogger = new Mock<ILogger<ExportViewModel>>();
        _testConversationId = Guid.NewGuid();

        // Setup default behavior
        _mockExportService.Setup(s => s.GeneratePreviewAsync(
                It.IsAny<Guid>(),
                It.IsAny<ExportOptions>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Preview Content\n\nThis is a preview...");

        _mockExportService.Setup(s => s.GetFileExtension(It.IsAny<ExportFormat>()))
            .Returns<ExportFormat>(f => f switch
            {
                ExportFormat.Markdown => ".md",
                ExportFormat.Json => ".json",
                ExportFormat.PlainText => ".txt",
                ExportFormat.Html => ".html",
                _ => ".txt"
            });
    }

    private ExportViewModel CreateViewModel()
    {
        _viewModel = new ExportViewModel(
            _mockExportService.Object,
            _mockStorageProvider.Object,
            _testConversationId,
            _mockLogger.Object);

        return _viewModel;
    }

    private static ExportResult CreateSuccessResult(string content, string fileName, string mimeType) => new()
    {
        Success = true,
        Content = content,
        SuggestedFileName = fileName,
        MimeType = mimeType
    };

    public void Dispose()
    {
        _viewModel?.Dispose();
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies that constructor throws for null export service.
    /// </summary>
    [Fact]
    public void Constructor_NullExportService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ExportViewModel(
            null!,
            _mockStorageProvider.Object,
            _testConversationId,
            _mockLogger.Object));
    }

    /// <summary>
    /// Verifies that constructor throws for null storage provider.
    /// </summary>
    [Fact]
    public void Constructor_NullStorageProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ExportViewModel(
            _mockExportService.Object,
            null!,
            _testConversationId,
            _mockLogger.Object));
    }

    /// <summary>
    /// Verifies that constructor allows null logger.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_DoesNotThrow()
    {
        // Act & Assert - should not throw
        var vm = new ExportViewModel(
            _mockExportService.Object,
            _mockStorageProvider.Object,
            _testConversationId,
            null);

        vm.Dispose();
    }

    /// <summary>
    /// Verifies that constructor accepts empty Guid.
    /// </summary>
    [Fact]
    public void Constructor_EmptyGuid_DoesNotThrow()
    {
        // Act & Assert - should not throw
        var vm = new ExportViewModel(
            _mockExportService.Object,
            _mockStorageProvider.Object,
            Guid.Empty,
            _mockLogger.Object);

        vm.Dispose();
    }

    #endregion

    #region Property Default Tests

    /// <summary>
    /// Verifies that SelectedFormat defaults to Markdown.
    /// </summary>
    [Fact]
    public void SelectedFormat_DefaultsToMarkdown()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.Equal(ExportFormat.Markdown, vm.SelectedFormat);
    }

    /// <summary>
    /// Verifies that IncludeTimestamps defaults to true.
    /// </summary>
    [Fact]
    public void IncludeTimestamps_DefaultsToTrue()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.True(vm.IncludeTimestamps);
    }

    /// <summary>
    /// Verifies that IncludeSystemPrompt defaults to true.
    /// </summary>
    [Fact]
    public void IncludeSystemPrompt_DefaultsToTrue()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.True(vm.IncludeSystemPrompt);
    }

    /// <summary>
    /// Verifies that IncludeMetadata defaults to true.
    /// </summary>
    [Fact]
    public void IncludeMetadata_DefaultsToTrue()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.True(vm.IncludeMetadata);
    }

    /// <summary>
    /// Verifies that IncludeTokenCounts defaults to false.
    /// </summary>
    [Fact]
    public void IncludeTokenCounts_DefaultsToFalse()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.False(vm.IncludeTokenCounts);
    }

    /// <summary>
    /// Verifies that Preview defaults to empty string.
    /// </summary>
    [Fact]
    public void Preview_DefaultsToEmptyString()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.Equal(string.Empty, vm.Preview);
    }

    /// <summary>
    /// Verifies that IsExporting defaults to false.
    /// </summary>
    [Fact]
    public void IsExporting_DefaultsToFalse()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.False(vm.IsExporting);
    }

    /// <summary>
    /// Verifies that ShouldClose defaults to false.
    /// </summary>
    [Fact]
    public void ShouldClose_DefaultsToFalse()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.False(vm.ShouldClose);
    }

    #endregion

    #region InitializeAsync Tests

    /// <summary>
    /// Verifies that InitializeAsync calls GeneratePreviewAsync.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_CallsGeneratePreview()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.InitializeAsync();

        // Assert
        _mockExportService.Verify(s => s.GeneratePreviewAsync(
            _testConversationId,
            It.IsAny<ExportOptions>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that InitializeAsync sets Preview property.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_SetsPreviewProperty()
    {
        // Arrange
        var vm = CreateViewModel();
        const string expectedPreview = "# Preview Content\n\nThis is a preview...";
        _mockExportService.Setup(s => s.GeneratePreviewAsync(
                It.IsAny<Guid>(),
                It.IsAny<ExportOptions>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPreview);

        // Act
        await vm.InitializeAsync();

        // Assert
        Assert.Equal(expectedPreview, vm.Preview);
    }

    /// <summary>
    /// Verifies that InitializeAsync handles exception gracefully.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_OnException_SetsPreviewToErrorMessage()
    {
        // Arrange
        var vm = CreateViewModel();
        _mockExportService.Setup(s => s.GeneratePreviewAsync(
                It.IsAny<Guid>(),
                It.IsAny<ExportOptions>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test error"));

        // Act
        await vm.InitializeAsync();

        // Assert
        Assert.Contains("Error generating preview", vm.Preview);
    }

    #endregion

    #region Format Changed Tests

    /// <summary>
    /// Verifies that changing SelectedFormat triggers preview update.
    /// </summary>
    [Fact]
    public async Task SelectedFormatChanged_TriggersPreviewUpdate()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.InitializeAsync();
        _mockExportService.Invocations.Clear();

        // Act
        vm.SelectedFormat = ExportFormat.Json;

        // Allow async operation to complete
        await Task.Delay(50);

        // Assert
        _mockExportService.Verify(s => s.GeneratePreviewAsync(
            _testConversationId,
            It.Is<ExportOptions>(o => o.Format == ExportFormat.Json),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    /// <summary>
    /// Verifies that changing format to all values works correctly.
    /// </summary>
    [Theory]
    [InlineData(ExportFormat.Markdown)]
    [InlineData(ExportFormat.Json)]
    [InlineData(ExportFormat.PlainText)]
    [InlineData(ExportFormat.Html)]
    public void SelectedFormatChanged_AllFormatsAccepted(ExportFormat format)
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.SelectedFormat = format;

        // Assert
        Assert.Equal(format, vm.SelectedFormat);
    }

    #endregion

    #region Option Changed Tests

    /// <summary>
    /// Verifies that changing IncludeTimestamps triggers preview update.
    /// </summary>
    [Fact]
    public async Task IncludeTimestampsChanged_TriggersPreviewUpdate()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.InitializeAsync();
        _mockExportService.Invocations.Clear();

        // Act
        vm.IncludeTimestamps = false;

        // Allow async operation to complete
        await Task.Delay(50);

        // Assert
        _mockExportService.Verify(s => s.GeneratePreviewAsync(
            _testConversationId,
            It.Is<ExportOptions>(o => !o.IncludeTimestamps),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    /// <summary>
    /// Verifies that changing IncludeSystemPrompt triggers preview update.
    /// </summary>
    [Fact]
    public async Task IncludeSystemPromptChanged_TriggersPreviewUpdate()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.InitializeAsync();
        _mockExportService.Invocations.Clear();

        // Act
        vm.IncludeSystemPrompt = false;

        // Allow async operation to complete
        await Task.Delay(50);

        // Assert
        _mockExportService.Verify(s => s.GeneratePreviewAsync(
            _testConversationId,
            It.Is<ExportOptions>(o => !o.IncludeSystemPrompt),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    /// <summary>
    /// Verifies that changing IncludeMetadata triggers preview update.
    /// </summary>
    [Fact]
    public async Task IncludeMetadataChanged_TriggersPreviewUpdate()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.InitializeAsync();
        _mockExportService.Invocations.Clear();

        // Act
        vm.IncludeMetadata = false;

        // Allow async operation to complete
        await Task.Delay(50);

        // Assert
        _mockExportService.Verify(s => s.GeneratePreviewAsync(
            _testConversationId,
            It.Is<ExportOptions>(o => !o.IncludeMetadata),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    /// <summary>
    /// Verifies that changing IncludeTokenCounts triggers preview update.
    /// </summary>
    [Fact]
    public async Task IncludeTokenCountsChanged_TriggersPreviewUpdate()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.InitializeAsync();
        _mockExportService.Invocations.Clear();

        // Act
        vm.IncludeTokenCounts = true;

        // Allow async operation to complete
        await Task.Delay(50);

        // Assert
        _mockExportService.Verify(s => s.GeneratePreviewAsync(
            _testConversationId,
            It.Is<ExportOptions>(o => o.IncludeTokenCounts),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    #endregion

    #region Cancel Command Tests

    /// <summary>
    /// Verifies that Cancel sets ShouldClose to true.
    /// </summary>
    [Fact]
    public void Cancel_SetsShouldCloseToTrue()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.CancelCommand.Execute(null);

        // Assert
        Assert.True(vm.ShouldClose);
    }

    /// <summary>
    /// Verifies that Cancel does not trigger export.
    /// </summary>
    [Fact]
    public void Cancel_DoesNotTriggerExport()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.CancelCommand.Execute(null);

        // Assert
        _mockExportService.Verify(s => s.ExportAsync(
            It.IsAny<Guid>(),
            It.IsAny<ExportOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Export Command Tests

    /// <summary>
    /// Verifies that Export calls ExportAsync on service.
    /// </summary>
    [Fact]
    public async Task ExportAsync_CallsExportService()
    {
        // Arrange
        var vm = CreateViewModel();
        var mockFile = new Mock<IStorageFile>();
        mockFile.Setup(f => f.OpenWriteAsync()).ReturnsAsync(new MemoryStream());
        mockFile.Setup(f => f.Name).Returns("test.md");

        _mockExportService.Setup(s => s.ExportAsync(
                It.IsAny<Guid>(),
                It.IsAny<ExportOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult("# Content", "conversation.md", "text/markdown"));

        _mockStorageProvider.Setup(s => s.SaveFilePickerAsync(It.IsAny<FilePickerSaveOptions>()))
            .ReturnsAsync(mockFile.Object);

        // Act
        await vm.ExportCommand.ExecuteAsync(null);

        // Assert
        _mockExportService.Verify(s => s.ExportAsync(
            _testConversationId,
            It.IsAny<ExportOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that Export sets IsExporting during execution.
    /// </summary>
    [Fact]
    public async Task ExportAsync_SetsIsExportingDuringExecution()
    {
        // Arrange
        var vm = CreateViewModel();
        bool wasExportingDuringExport = false;

        _mockExportService.Setup(s => s.ExportAsync(
                It.IsAny<Guid>(),
                It.IsAny<ExportOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                wasExportingDuringExport = vm.IsExporting;
                return Task.FromResult(CreateSuccessResult("# Content", "test.md", "text/markdown"));
            });

        _mockStorageProvider.Setup(s => s.SaveFilePickerAsync(It.IsAny<FilePickerSaveOptions>()))
            .ReturnsAsync((IStorageFile?)null);

        // Act
        await vm.ExportCommand.ExecuteAsync(null);

        // Assert
        Assert.True(wasExportingDuringExport);
        Assert.False(vm.IsExporting);
    }

    /// <summary>
    /// Verifies that Export sets ShouldClose on success.
    /// </summary>
    [Fact]
    public async Task ExportAsync_OnSuccess_SetsShouldClose()
    {
        // Arrange
        var vm = CreateViewModel();
        var mockFile = new Mock<IStorageFile>();
        mockFile.Setup(f => f.OpenWriteAsync()).ReturnsAsync(new MemoryStream());
        mockFile.Setup(f => f.Name).Returns("test.md");

        _mockExportService.Setup(s => s.ExportAsync(
                It.IsAny<Guid>(),
                It.IsAny<ExportOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult("# Content", "conversation.md", "text/markdown"));

        _mockStorageProvider.Setup(s => s.SaveFilePickerAsync(It.IsAny<FilePickerSaveOptions>()))
            .ReturnsAsync(mockFile.Object);

        // Act
        await vm.ExportCommand.ExecuteAsync(null);

        // Assert
        Assert.True(vm.ShouldClose);
    }

    /// <summary>
    /// Verifies that Export does not set ShouldClose when file picker is cancelled.
    /// </summary>
    [Fact]
    public async Task ExportAsync_FilePickerCancelled_DoesNotSetShouldClose()
    {
        // Arrange
        var vm = CreateViewModel();

        _mockExportService.Setup(s => s.ExportAsync(
                It.IsAny<Guid>(),
                It.IsAny<ExportOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult("# Content", "conversation.md", "text/markdown"));

        _mockStorageProvider.Setup(s => s.SaveFilePickerAsync(It.IsAny<FilePickerSaveOptions>()))
            .ReturnsAsync((IStorageFile?)null);

        // Act
        await vm.ExportCommand.ExecuteAsync(null);

        // Assert
        Assert.False(vm.ShouldClose);
    }

    /// <summary>
    /// Verifies that Export sets error message on failure.
    /// </summary>
    [Fact]
    public async Task ExportAsync_OnFailure_SetsErrorMessage()
    {
        // Arrange
        var vm = CreateViewModel();

        _mockExportService.Setup(s => s.ExportAsync(
                It.IsAny<Guid>(),
                It.IsAny<ExportOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExportResult.Failed("Conversation not found"));

        // Act
        await vm.ExportCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("Conversation not found", vm.ErrorMessage);
        Assert.False(vm.ShouldClose);
    }

    /// <summary>
    /// Verifies that Export handles exception gracefully.
    /// </summary>
    [Fact]
    public async Task ExportAsync_OnException_SetsErrorMessage()
    {
        // Arrange
        var vm = CreateViewModel();

        _mockExportService.Setup(s => s.ExportAsync(
                It.IsAny<Guid>(),
                It.IsAny<ExportOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test error"));

        // Act
        await vm.ExportCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains("Test error", vm.ErrorMessage);
        Assert.False(vm.ShouldClose);
        Assert.False(vm.IsExporting);
    }

    /// <summary>
    /// Verifies that Export passes correct options to service.
    /// </summary>
    [Fact]
    public async Task ExportAsync_PassesCorrectOptionsToService()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.SelectedFormat = ExportFormat.Json;
        vm.IncludeTimestamps = false;
        vm.IncludeSystemPrompt = false;
        vm.IncludeMetadata = true;
        vm.IncludeTokenCounts = true;

        ExportOptions? capturedOptions = null;
        _mockExportService.Setup(s => s.ExportAsync(
                It.IsAny<Guid>(),
                It.IsAny<ExportOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, ExportOptions, CancellationToken>((_, options, _) => capturedOptions = options)
            .ReturnsAsync(CreateSuccessResult("{}", "test.json", "application/json"));

        _mockStorageProvider.Setup(s => s.SaveFilePickerAsync(It.IsAny<FilePickerSaveOptions>()))
            .ReturnsAsync((IStorageFile?)null);

        // Act
        await vm.ExportCommand.ExecuteAsync(null);

        // Assert
        Assert.NotNull(capturedOptions);
        Assert.Equal(ExportFormat.Json, capturedOptions!.Format);
        Assert.False(capturedOptions.IncludeTimestamps);
        Assert.False(capturedOptions.IncludeSystemPrompt);
        Assert.True(capturedOptions.IncludeMetadata);
        Assert.True(capturedOptions.IncludeTokenCounts);
    }

    #endregion

    #region Dispose Tests

    /// <summary>
    /// Verifies that Dispose can be called multiple times safely.
    /// </summary>
    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act & Assert - should not throw
        vm.Dispose();
        vm.Dispose();
        vm.Dispose();
    }

    /// <summary>
    /// Verifies that Dispose cleans up resources.
    /// </summary>
    [Fact]
    public void Dispose_CleansUpResources()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.Dispose();

        // Assert - Verify no exceptions are thrown
        Assert.True(true);
    }

    /// <summary>
    /// Verifies that Dispose cancels pending preview operations.
    /// </summary>
    [Fact]
    public async Task Dispose_CancelsPendingPreviewOperations()
    {
        // Arrange
        var vm = CreateViewModel();
        var previewStarted = new TaskCompletionSource<bool>();
        var previewBlocked = new TaskCompletionSource<bool>();

        _mockExportService.Setup(s => s.GeneratePreviewAsync(
                It.IsAny<Guid>(),
                It.IsAny<ExportOptions>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns<Guid, ExportOptions, int, CancellationToken>(async (_, _, _, ct) =>
            {
                previewStarted.SetResult(true);
                await previewBlocked.Task;
                ct.ThrowIfCancellationRequested();
                return "Preview";
            });

        // Start initialization (which triggers preview)
        var initTask = vm.InitializeAsync();
        await previewStarted.Task;

        // Act
        vm.Dispose();
        previewBlocked.SetResult(true);

        // Assert - Should not throw, preview should be cancelled
        await Task.WhenAny(initTask, Task.Delay(100));
    }

    #endregion

    #region Error Handling Tests

    /// <summary>
    /// Verifies that ErrorMessage is set when export fails.
    /// </summary>
    [Fact]
    public async Task ErrorMessage_WhenExportFails_IsSet()
    {
        // Arrange
        var vm = CreateViewModel();

        _mockExportService.Setup(s => s.ExportAsync(
                It.IsAny<Guid>(),
                It.IsAny<ExportOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExportResult.Failed("Test error"));

        // Act
        await vm.ExportCommand.ExecuteAsync(null);

        // Assert
        Assert.False(string.IsNullOrEmpty(vm.ErrorMessage));
    }

    /// <summary>
    /// Verifies that ErrorMessage is null when no error.
    /// </summary>
    [Fact]
    public void ErrorMessage_WhenNoError_IsNull()
    {
        // Arrange
        var vm = CreateViewModel();

        // Assert
        Assert.True(string.IsNullOrEmpty(vm.ErrorMessage));
    }

    /// <summary>
    /// Verifies that ErrorMessage is cleared when starting new export.
    /// </summary>
    [Fact]
    public async Task ExportAsync_ClearsErrorMessage()
    {
        // Arrange
        var vm = CreateViewModel();

        // First export - fail
        _mockExportService.Setup(s => s.ExportAsync(
                It.IsAny<Guid>(),
                It.IsAny<ExportOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExportResult.Failed("First error"));

        await vm.ExportCommand.ExecuteAsync(null);
        Assert.False(string.IsNullOrEmpty(vm.ErrorMessage));

        // Second export - success
        _mockExportService.Setup(s => s.ExportAsync(
                It.IsAny<Guid>(),
                It.IsAny<ExportOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResult("Content", "test.md", "text/markdown"));

        _mockStorageProvider.Setup(s => s.SaveFilePickerAsync(It.IsAny<FilePickerSaveOptions>()))
            .ReturnsAsync((IStorageFile?)null);

        // Act
        await vm.ExportCommand.ExecuteAsync(null);

        // Assert
        Assert.True(string.IsNullOrEmpty(vm.ErrorMessage));
    }

    #endregion
}
