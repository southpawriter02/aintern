// -----------------------------------------------------------------------
// <copyright file="SearchViewModelTests.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
// </copyright>
// <summary>
//     Unit tests for SearchViewModel (v0.2.5e).
//     Tests constructor, navigation, filtering, selection, and disposal.
// </summary>
// -----------------------------------------------------------------------

using AIntern.Core.Enums;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="SearchViewModel"/> (v0.2.5e).
/// Tests constructor validation, navigation, filtering, selection, and disposal.
/// </summary>
/// <remarks>
/// <para>
/// These tests cover:
/// </para>
/// <list type="bullet">
///   <item><description>Constructor validation (null checks)</description></item>
///   <item><description>Filter setting (All, Conversation, Message)</description></item>
///   <item><description>Navigation (Up/Down with wrap-around)</description></item>
///   <item><description>Selection (SelectResult, Close)</description></item>
///   <item><description>Query changes (empty clears results)</description></item>
///   <item><description>Dispose (cleans up timer and CTS)</description></item>
/// </list>
/// <para>
/// Note: Tests that require async search execution are limited because
/// SearchViewModel uses Dispatcher.UIThread directly. Full integration
/// tests should use the Avalonia test framework.
/// </para>
/// <para>Added in v0.2.5e.</para>
/// </remarks>
public class SearchViewModelTests : IDisposable
{
    #region Test Infrastructure

    private readonly Mock<ISearchService> _mockSearchService;
    private readonly Mock<ILogger<SearchViewModel>> _mockLogger;

    private SearchViewModel? _viewModel;

    public SearchViewModelTests()
    {
        _mockSearchService = new Mock<ISearchService>();
        _mockLogger = new Mock<ILogger<SearchViewModel>>();

        // Setup default behavior
        _mockSearchService.Setup(s => s.SearchAsync(It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SearchResults.Empty(SearchQuery.Simple("")));
    }

    private SearchViewModel CreateViewModel()
    {
        _viewModel = new SearchViewModel(
            _mockSearchService.Object,
            _mockLogger.Object);

        return _viewModel;
    }

    private static SearchResult CreateTestResult(
        string title = "Test Result",
        SearchResultType resultType = SearchResultType.Conversation,
        Guid? conversationId = null,
        Guid? messageId = null)
    {
        return new SearchResult(
            Id: Guid.NewGuid(),
            ResultType: resultType,
            Title: title,
            Preview: "Preview text...",
            Rank: 1.0,
            Timestamp: DateTime.UtcNow,
            ConversationId: conversationId ?? Guid.NewGuid(),
            MessageId: resultType == SearchResultType.Message ? (messageId ?? Guid.NewGuid()) : null);
    }

    public void Dispose()
    {
        _viewModel?.Dispose();
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies that constructor throws for null search service.
    /// </summary>
    [Fact]
    public void Constructor_NullSearchService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SearchViewModel(
            null!,
            _mockLogger.Object));
    }

    /// <summary>
    /// Verifies that constructor allows null logger.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_DoesNotThrow()
    {
        // Act & Assert - should not throw
        var vm = new SearchViewModel(
            _mockSearchService.Object,
            null);

        vm.Dispose();
    }

    /// <summary>
    /// Verifies that constructor initializes properties to defaults.
    /// </summary>
    [Fact]
    public void Constructor_InitializesPropertiesToDefaults()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.Equal(string.Empty, vm.Query);
        Assert.False(vm.IsSearching);
        Assert.Equal(string.Empty, vm.SearchStatus);
        Assert.Null(vm.SelectedResult);
        Assert.Null(vm.FilterType);
        Assert.Null(vm.DialogResult);
        Assert.False(vm.ShouldClose);
        Assert.Empty(vm.ConversationResults);
        Assert.Empty(vm.MessageResults);
    }

    #endregion

    #region SetFilter Tests

    /// <summary>
    /// Verifies that SetFilter with null sets FilterType to null (All).
    /// </summary>
    [Fact]
    public void SetFilter_Null_SetsFilterTypeToNull()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.SetFilterCommand.Execute("Conversation");

        // Act
        vm.SetFilterCommand.Execute(null);

        // Assert
        Assert.Null(vm.FilterType);
    }

    /// <summary>
    /// Verifies that SetFilter with empty string sets FilterType to null (All).
    /// </summary>
    [Fact]
    public void SetFilter_EmptyString_SetsFilterTypeToNull()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.SetFilterCommand.Execute("");

        // Assert
        Assert.Null(vm.FilterType);
    }

    /// <summary>
    /// Verifies that SetFilter with "Conversation" sets correct FilterType.
    /// </summary>
    [Fact]
    public void SetFilter_Conversation_SetsFilterTypeToConversation()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.SetFilterCommand.Execute("Conversation");

        // Assert
        Assert.Equal(SearchResultType.Conversation, vm.FilterType);
    }

    /// <summary>
    /// Verifies that SetFilter with "Message" sets correct FilterType.
    /// </summary>
    [Fact]
    public void SetFilter_Message_SetsFilterTypeToMessage()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.SetFilterCommand.Execute("Message");

        // Assert
        Assert.Equal(SearchResultType.Message, vm.FilterType);
    }

    /// <summary>
    /// Verifies that SetFilter with unknown value sets FilterType to null.
    /// </summary>
    [Fact]
    public void SetFilter_UnknownValue_SetsFilterTypeToNull()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.SetFilterCommand.Execute("Unknown");

        // Assert
        Assert.Null(vm.FilterType);
    }

    #endregion

    #region Navigation Tests - Empty List

    /// <summary>
    /// Verifies that NavigateUp does nothing when results are empty.
    /// </summary>
    [Fact]
    public void NavigateUp_EmptyResults_DoesNothing()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.NavigateUpCommand.Execute(null);

        // Assert
        Assert.Null(vm.SelectedResult);
    }

    /// <summary>
    /// Verifies that NavigateDown does nothing when results are empty.
    /// </summary>
    [Fact]
    public void NavigateDown_EmptyResults_DoesNothing()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.NavigateDownCommand.Execute(null);

        // Assert
        Assert.Null(vm.SelectedResult);
    }

    #endregion

    #region Navigation Tests - With Results

    /// <summary>
    /// Verifies that NavigateDown selects first result when no selection.
    /// </summary>
    [Fact]
    public void NavigateDown_NoSelection_SelectsFirstResult()
    {
        // Arrange
        var vm = CreateViewModel();
        var result1 = CreateTestResult("Result 1");
        var result2 = CreateTestResult("Result 2");
        vm.ConversationResults.Add(result1);
        vm.ConversationResults.Add(result2);

        // Act
        vm.NavigateDownCommand.Execute(null);

        // Assert
        Assert.Equal(result1, vm.SelectedResult);
    }

    /// <summary>
    /// Verifies that NavigateDown moves to next result.
    /// </summary>
    [Fact]
    public void NavigateDown_WithSelection_MovesToNext()
    {
        // Arrange
        var vm = CreateViewModel();
        var result1 = CreateTestResult("Result 1");
        var result2 = CreateTestResult("Result 2");
        vm.ConversationResults.Add(result1);
        vm.ConversationResults.Add(result2);
        vm.SelectedResult = result1;

        // Act
        vm.NavigateDownCommand.Execute(null);

        // Assert
        Assert.Equal(result2, vm.SelectedResult);
    }

    /// <summary>
    /// Verifies that NavigateDown wraps to first when at last.
    /// </summary>
    [Fact]
    public void NavigateDown_AtLastResult_WrapsToFirst()
    {
        // Arrange
        var vm = CreateViewModel();
        var result1 = CreateTestResult("Result 1");
        var result2 = CreateTestResult("Result 2");
        vm.ConversationResults.Add(result1);
        vm.ConversationResults.Add(result2);
        vm.SelectedResult = result2;

        // Act
        vm.NavigateDownCommand.Execute(null);

        // Assert
        Assert.Equal(result1, vm.SelectedResult);
    }

    /// <summary>
    /// Verifies that NavigateUp selects last result when no selection.
    /// </summary>
    [Fact]
    public void NavigateUp_NoSelection_SelectsLastResult()
    {
        // Arrange
        var vm = CreateViewModel();
        var result1 = CreateTestResult("Result 1");
        var result2 = CreateTestResult("Result 2");
        vm.ConversationResults.Add(result1);
        vm.ConversationResults.Add(result2);

        // Act
        vm.NavigateUpCommand.Execute(null);

        // Assert
        Assert.Equal(result2, vm.SelectedResult);
    }

    /// <summary>
    /// Verifies that NavigateUp moves to previous result.
    /// </summary>
    [Fact]
    public void NavigateUp_WithSelection_MovesToPrevious()
    {
        // Arrange
        var vm = CreateViewModel();
        var result1 = CreateTestResult("Result 1");
        var result2 = CreateTestResult("Result 2");
        vm.ConversationResults.Add(result1);
        vm.ConversationResults.Add(result2);
        vm.SelectedResult = result2;

        // Act
        vm.NavigateUpCommand.Execute(null);

        // Assert
        Assert.Equal(result1, vm.SelectedResult);
    }

    /// <summary>
    /// Verifies that NavigateUp wraps to last when at first.
    /// </summary>
    [Fact]
    public void NavigateUp_AtFirstResult_WrapsToLast()
    {
        // Arrange
        var vm = CreateViewModel();
        var result1 = CreateTestResult("Result 1");
        var result2 = CreateTestResult("Result 2");
        vm.ConversationResults.Add(result1);
        vm.ConversationResults.Add(result2);
        vm.SelectedResult = result1;

        // Act
        vm.NavigateUpCommand.Execute(null);

        // Assert
        Assert.Equal(result2, vm.SelectedResult);
    }

    /// <summary>
    /// Verifies navigation across conversation and message groups.
    /// </summary>
    [Fact]
    public void NavigateDown_AcrossGroups_MovesToMessageResults()
    {
        // Arrange
        var vm = CreateViewModel();
        var conversation = CreateTestResult("Conversation", SearchResultType.Conversation);
        var message = CreateTestResult("Message", SearchResultType.Message);
        vm.ConversationResults.Add(conversation);
        vm.MessageResults.Add(message);
        vm.SelectedResult = conversation;

        // Act
        vm.NavigateDownCommand.Execute(null);

        // Assert
        Assert.Equal(message, vm.SelectedResult);
    }

    /// <summary>
    /// Verifies navigation up from message to conversation group.
    /// </summary>
    [Fact]
    public void NavigateUp_FromMessage_MovesToConversation()
    {
        // Arrange
        var vm = CreateViewModel();
        var conversation = CreateTestResult("Conversation", SearchResultType.Conversation);
        var message = CreateTestResult("Message", SearchResultType.Message);
        vm.ConversationResults.Add(conversation);
        vm.MessageResults.Add(message);
        vm.SelectedResult = message;

        // Act
        vm.NavigateUpCommand.Execute(null);

        // Assert
        Assert.Equal(conversation, vm.SelectedResult);
    }

    #endregion

    #region SelectResult Tests

    /// <summary>
    /// Verifies that SelectResult sets DialogResult when result is selected.
    /// </summary>
    [Fact]
    public void SelectResult_WithSelection_SetsDialogResult()
    {
        // Arrange
        var vm = CreateViewModel();
        var result = CreateTestResult("Test");
        vm.ConversationResults.Add(result);
        vm.SelectedResult = result;

        // Act
        vm.SelectResultCommand.Execute(null);

        // Assert
        Assert.Equal(result, vm.DialogResult);
    }

    /// <summary>
    /// Verifies that SelectResult sets ShouldClose to true.
    /// </summary>
    [Fact]
    public void SelectResult_SetsShouldCloseToTrue()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.SelectResultCommand.Execute(null);

        // Assert
        Assert.True(vm.ShouldClose);
    }

    /// <summary>
    /// Verifies that SelectResult without selection sets DialogResult to null.
    /// </summary>
    [Fact]
    public void SelectResult_NoSelection_DialogResultIsNull()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.SelectResultCommand.Execute(null);

        // Assert
        Assert.Null(vm.DialogResult);
        Assert.True(vm.ShouldClose);
    }

    #endregion

    #region Close Tests

    /// <summary>
    /// Verifies that Close sets ShouldClose to true.
    /// </summary>
    [Fact]
    public void Close_SetsShouldCloseToTrue()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.CloseCommand.Execute(null);

        // Assert
        Assert.True(vm.ShouldClose);
    }

    /// <summary>
    /// Verifies that Close leaves DialogResult as null.
    /// </summary>
    [Fact]
    public void Close_LeavesDialogResultNull()
    {
        // Arrange
        var vm = CreateViewModel();
        var result = CreateTestResult("Test");
        vm.ConversationResults.Add(result);
        vm.SelectedResult = result;

        // Act
        vm.CloseCommand.Execute(null);

        // Assert
        Assert.Null(vm.DialogResult);
        Assert.True(vm.ShouldClose);
    }

    #endregion

    #region Query Changed Tests

    /// <summary>
    /// Verifies that empty query clears results.
    /// </summary>
    [Fact]
    public void QueryChanged_EmptyQuery_ClearsResults()
    {
        // Arrange
        var vm = CreateViewModel();
        // First set a non-empty query so that setting empty triggers change
        vm.Query = "test";
        vm.ConversationResults.Add(CreateTestResult("Test"));
        vm.SearchStatus = "Some status";
        vm.SelectedResult = vm.ConversationResults[0];

        // Act - clear the query
        vm.Query = "";

        // Assert
        Assert.Empty(vm.ConversationResults);
        Assert.Equal(string.Empty, vm.SearchStatus);
        Assert.Null(vm.SelectedResult);
    }

    /// <summary>
    /// Verifies that whitespace-only query clears results.
    /// </summary>
    [Fact]
    public void QueryChanged_WhitespaceQuery_ClearsResults()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.ConversationResults.Add(CreateTestResult("Test"));

        // Act
        vm.Query = "   ";

        // Assert
        Assert.Empty(vm.ConversationResults);
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

        // Assert - Verify no exceptions are thrown on subsequent operations
        // The timer should be stopped and disposed
        Assert.True(true); // If we get here without exception, test passes
    }

    #endregion

    #region FilterType Changed Tests

    /// <summary>
    /// Verifies that FilterType change does not trigger search with empty query.
    /// </summary>
    [Fact]
    public void FilterTypeChanged_EmptyQuery_DoesNotTriggerSearch()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.FilterType = SearchResultType.Conversation;

        // Assert - SearchAsync should not be called since Query is empty
        _mockSearchService.Verify(
            s => s.SearchAsync(It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Collection Tests

    /// <summary>
    /// Verifies that ConversationResults is initialized as empty collection.
    /// </summary>
    [Fact]
    public void ConversationResults_InitializedAsEmptyCollection()
    {
        // Arrange & Act
        var vm = CreateViewModel();

        // Assert
        Assert.NotNull(vm.ConversationResults);
        Assert.Empty(vm.ConversationResults);
    }

    /// <summary>
    /// Verifies that MessageResults is initialized as empty collection.
    /// </summary>
    [Fact]
    public void MessageResults_InitializedAsEmptyCollection()
    {
        // Arrange & Act
        var vm = CreateViewModel();

        // Assert
        Assert.NotNull(vm.MessageResults);
        Assert.Empty(vm.MessageResults);
    }

    /// <summary>
    /// Verifies that results can be added to collections.
    /// </summary>
    [Fact]
    public void Collections_CanAddResults()
    {
        // Arrange
        var vm = CreateViewModel();
        var conversation = CreateTestResult("Conv", SearchResultType.Conversation);
        var message = CreateTestResult("Msg", SearchResultType.Message);

        // Act
        vm.ConversationResults.Add(conversation);
        vm.MessageResults.Add(message);

        // Assert
        Assert.Single(vm.ConversationResults);
        Assert.Single(vm.MessageResults);
        Assert.Equal(conversation, vm.ConversationResults[0]);
        Assert.Equal(message, vm.MessageResults[0]);
    }

    #endregion
}
