using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;
using Xunit;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Tests for ChatMessage and ChatMessageViewModel attached context functionality.
/// </summary>
/// <remarks>
/// <para>Added in v0.3.4h.</para>
/// </remarks>
public class ChatMessageAttachmentTests
{
    #region ChatMessage Tests

    [Fact]
    public void ChatMessage_AttachedContexts_DefaultsToEmpty()
    {
        // Arrange & Act
        var message = new ChatMessage { Role = MessageRole.User, Content = "Test" };

        // Assert
        Assert.Empty(message.AttachedContexts);
        Assert.False(message.HasAttachedContexts);
    }

    [Fact]
    public void ChatMessage_HasAttachedContexts_TrueWhenContextsPresent()
    {
        // Arrange - use factory method
        var contexts = new List<FileContext>
        {
            FileContext.FromFile("/path/test.cs", "public class Test {}")
        };

        // Act
        var message = new ChatMessage
        {
            Role = MessageRole.User,
            Content = "Test",
            AttachedContexts = contexts
        };

        // Assert
        Assert.Single(message.AttachedContexts);
        Assert.True(message.HasAttachedContexts);
    }

    #endregion

    #region ChatMessageViewModel Tests

    [Fact]
    public void ChatMessageViewModel_AttachedContexts_DefaultsToEmpty()
    {
        // Arrange & Act
        var viewModel = new ChatMessageViewModel();

        // Assert
        Assert.Empty(viewModel.AttachedContexts);
        Assert.False(viewModel.HasAttachedContexts);
        Assert.Equal(0, viewModel.AttachedContextCount);
        Assert.Equal(0, viewModel.TotalAttachedTokens);
    }

    [Fact]
    public void ChatMessageViewModel_IsContextExpanded_DefaultsToFalse()
    {
        // Arrange & Act
        var viewModel = new ChatMessageViewModel();

        // Assert
        Assert.False(viewModel.IsContextExpanded);
    }

    [Fact]
    public void ChatMessageViewModel_ToggleContextExpanded_TogglesState()
    {
        // Arrange
        var viewModel = new ChatMessageViewModel();
        Assert.False(viewModel.IsContextExpanded);

        // Act
        viewModel.ToggleContextExpandedCommand.Execute(null);

        // Assert
        Assert.True(viewModel.IsContextExpanded);

        // Act again
        viewModel.ToggleContextExpandedCommand.Execute(null);

        // Assert
        Assert.False(viewModel.IsContextExpanded);
    }

    [Fact]
    public void ChatMessageViewModel_ToChatMessage_IncludesAttachedContexts()
    {
        // Arrange - use factory method
        var originalMessage = new ChatMessage
        {
            Role = MessageRole.User,
            Content = "Test message",
            AttachedContexts = new List<FileContext>
            {
                FileContext.FromFile("/path/test.cs", "code")
            }
        };

        var viewModel = new ChatMessageViewModel(originalMessage);

        // Act
        var convertedMessage = viewModel.ToChatMessage();

        // Assert
        Assert.True(convertedMessage.HasAttachedContexts);
        Assert.Single(convertedMessage.AttachedContexts);
        Assert.Equal("test.cs", convertedMessage.AttachedContexts[0].FileName);
    }

    [Fact]
    public void ChatMessageViewModel_ComputedProperties_CalculateCorrectly()
    {
        // Arrange - use factory method
        var message = new ChatMessage
        {
            Role = MessageRole.User,
            Content = "Test",
            AttachedContexts = new List<FileContext>
            {
                FileContext.FromFile("/a.cs", "a"),
                FileContext.FromFile("/b.cs", "b")
            }
        };

        // Act
        var viewModel = new ChatMessageViewModel(message);

        // Assert
        Assert.True(viewModel.HasAttachedContexts);
        Assert.Equal(2, viewModel.AttachedContextCount);
        Assert.True(viewModel.TotalAttachedTokens >= 0); // Tokens calculated by factory
    }

    #endregion
}
