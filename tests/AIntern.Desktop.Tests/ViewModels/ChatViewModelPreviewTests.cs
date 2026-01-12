using Xunit;
using NSubstitute;
using AIntern.Core.Interfaces;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Unit tests for ChatViewModel preview functionality (v0.3.4e).
/// </summary>
public class ChatViewModelPreviewTests
{
    private readonly ILlmService _llmService;
    private readonly IConversationService _conversationService;
    private readonly IInferenceSettingsService _inferenceSettingsService;
    private readonly ISystemPromptService _systemPromptService;
    private readonly ChatViewModel _viewModel;

    public ChatViewModelPreviewTests()
    {
        _llmService = Substitute.For<ILlmService>();
        _conversationService = Substitute.For<IConversationService>();
        _inferenceSettingsService = Substitute.For<IInferenceSettingsService>();
        _systemPromptService = Substitute.For<ISystemPromptService>();

        _viewModel = new ChatViewModel(
            _llmService,
            _conversationService,
            _inferenceSettingsService,
            _systemPromptService);
    }

    #region ShowPreview Tests

    [Fact]
    public void ShowPreview_SetsSelectedContextAndOpens()
    {
        // Arrange
        var context = FileContextViewModel.FromFile("/test.cs", "public class Test { }", 25);

        // Act
        _viewModel.ShowPreviewCommand.Execute(context);

        // Assert
        Assert.True(_viewModel.IsPreviewOpen);
        Assert.Equal(context, _viewModel.SelectedPreviewContext);
    }

    [Fact]
    public void ShowPreview_WithNull_DoesNothing()
    {
        // Arrange
        _viewModel.IsPreviewOpen = false;

        // Act
        _viewModel.ShowPreviewCommand.Execute(null);

        // Assert
        Assert.False(_viewModel.IsPreviewOpen);
        Assert.Null(_viewModel.SelectedPreviewContext);
    }

    [Fact]
    public void ShowPreview_ReplacesExistingSelection()
    {
        // Arrange
        var context1 = FileContextViewModel.FromFile("/test1.cs", "content1", 10);
        var context2 = FileContextViewModel.FromFile("/test2.cs", "content2", 20);

        // Act
        _viewModel.ShowPreviewCommand.Execute(context1);
        _viewModel.ShowPreviewCommand.Execute(context2);

        // Assert
        Assert.True(_viewModel.IsPreviewOpen);
        Assert.Equal(context2, _viewModel.SelectedPreviewContext);
    }

    #endregion

    #region HidePreview Tests

    [Fact]
    public void HidePreview_ClearsStateAndCloses()
    {
        // Arrange
        var context = FileContextViewModel.FromFile("/test.cs", "content", 15);
        _viewModel.ShowPreviewCommand.Execute(context);

        // Act
        _viewModel.HidePreviewCommand.Execute(null);

        // Assert
        Assert.False(_viewModel.IsPreviewOpen);
        Assert.Null(_viewModel.SelectedPreviewContext);
    }

    [Fact]
    public void HidePreview_WhenAlreadyClosed_DoesNothing()
    {
        // Arrange
        _viewModel.IsPreviewOpen = false;
        _viewModel.SelectedPreviewContext = null;

        // Act
        _viewModel.HidePreviewCommand.Execute(null);

        // Assert
        Assert.False(_viewModel.IsPreviewOpen);
        Assert.Null(_viewModel.SelectedPreviewContext);
    }

    #endregion

    #region OpenContextFile Tests

    [Fact]
    public void OpenContextFile_ClosesPreview()
    {
        // Arrange
        var context = FileContextViewModel.FromFile("/test.cs", "content", 15);
        _viewModel.ShowPreviewCommand.Execute(context);

        // Act
        _viewModel.OpenContextFileCommand.Execute(null);

        // Assert
        Assert.False(_viewModel.IsPreviewOpen);
        Assert.Null(_viewModel.SelectedPreviewContext);
    }

    #endregion

    #region RemoveSelectedContext Tests

    [Fact]
    public void RemoveSelectedContext_RemovesAndCloses()
    {
        // Arrange
        var context = FileContextViewModel.FromFile("/test.cs", "content", 15);
        _viewModel.AddContext(context);
        _viewModel.ShowPreviewCommand.Execute(context);

        // Act
        _viewModel.RemoveSelectedContextCommand.Execute(null);

        // Assert
        Assert.False(_viewModel.IsPreviewOpen);
        Assert.Null(_viewModel.SelectedPreviewContext);
        Assert.Empty(_viewModel.AttachedContexts);
    }

    [Fact]
    public void RemoveSelectedContext_WithNoSelection_DoesNothing()
    {
        // Arrange
        var context = FileContextViewModel.FromFile("/test.cs", "content", 15);
        _viewModel.AddContext(context);
        _viewModel.SelectedPreviewContext = null;

        // Act
        _viewModel.RemoveSelectedContextCommand.Execute(null);

        // Assert
        Assert.Single(_viewModel.AttachedContexts); // Still has the context
    }

    [Fact]
    public void RemoveSelectedContext_OnlyRemovesSelectedContext()
    {
        // Arrange
        var context1 = FileContextViewModel.FromFile("/test1.cs", "content1", 10);
        var context2 = FileContextViewModel.FromFile("/test2.cs", "content2", 20);
        _viewModel.AddContext(context1);
        _viewModel.AddContext(context2);
        _viewModel.ShowPreviewCommand.Execute(context1);

        // Act
        _viewModel.RemoveSelectedContextCommand.Execute(null);

        // Assert
        Assert.Single(_viewModel.AttachedContexts);
        Assert.Contains(context2, _viewModel.AttachedContexts);
        Assert.DoesNotContain(context1, _viewModel.AttachedContexts);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void PreviewWorkflow_ShowAndClose()
    {
        // Arrange
        var context = FileContextViewModel.FromFile("/test.cs", "content", 15);
        _viewModel.AddContext(context);

        // Act & Assert - Open preview
        _viewModel.ShowPreviewCommand.Execute(context);
        Assert.True(_viewModel.IsPreviewOpen);
        Assert.Equal(context, _viewModel.SelectedPreviewContext);

        // Act & Assert - Close preview
        _viewModel.HidePreviewCommand.Execute(null);
        Assert.False(_viewModel.IsPreviewOpen);
        Assert.Null(_viewModel.SelectedPreviewContext);

        // Context should still be attached
        Assert.Single(_viewModel.AttachedContexts);
    }

    [Fact]
    public void PreviewWorkflow_ShowAndRemove()
    {
        // Arrange
        var context = FileContextViewModel.FromFile("/test.cs", "content", 15);
        _viewModel.AddContext(context);

        // Act & Assert - Open preview
        _viewModel.ShowPreviewCommand.Execute(context);
        Assert.True(_viewModel.IsPreviewOpen);

        // Act & Assert - Remove via preview
        _viewModel.RemoveSelectedContextCommand.Execute(null);
        Assert.False(_viewModel.IsPreviewOpen);
        Assert.Empty(_viewModel.AttachedContexts);
    }

    #endregion
}
