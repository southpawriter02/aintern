namespace AIntern.Desktop.Tests.ViewModels;

using System.Linq;
using Xunit;
using AIntern.Desktop.ViewModels;

/// <summary>
/// Unit tests for <see cref="ChatContextBarViewModel"/>.
/// </summary>
/// <remarks>
/// <para>Added in v0.3.4d.</para>
/// </remarks>
public class ChatContextBarViewModelTests
{
    #region HasAttachedContexts Tests

    /// <summary>
    /// Verifies HasAttachedContexts is false when empty.
    /// </summary>
    [Fact]
    public void HasAttachedContexts_Empty_False()
    {
        // Arrange
        var vm = new ChatContextBarViewModel();

        // Assert
        Assert.False(vm.HasAttachedContexts);
    }

    /// <summary>
    /// Verifies HasAttachedContexts is true when contexts added.
    /// </summary>
    [Fact]
    public void HasAttachedContexts_WithContexts_True()
    {
        // Arrange
        var vm = new ChatContextBarViewModel();
        var context = FileContextViewModel.FromFile("/test.cs", "code", 100);

        // Act
        vm.AddContext(context);

        // Assert
        Assert.True(vm.HasAttachedContexts);
    }

    #endregion

    #region HasMultipleContexts Tests

    /// <summary>
    /// Verifies HasMultipleContexts is false with one context.
    /// </summary>
    [Fact]
    public void HasMultipleContexts_OneContext_False()
    {
        // Arrange
        var vm = new ChatContextBarViewModel();
        vm.AddContext(FileContextViewModel.FromFile("/test.cs", "code", 100));

        // Assert
        Assert.False(vm.HasMultipleContexts);
    }

    /// <summary>
    /// Verifies HasMultipleContexts is true with multiple contexts.
    /// </summary>
    [Fact]
    public void HasMultipleContexts_TwoContexts_True()
    {
        // Arrange
        var vm = new ChatContextBarViewModel();
        vm.AddContext(FileContextViewModel.FromFile("/test1.cs", "code", 100));
        vm.AddContext(FileContextViewModel.FromFile("/test2.cs", "code", 100));

        // Assert
        Assert.True(vm.HasMultipleContexts);
    }

    #endregion

    #region TotalContextTokens Tests

    /// <summary>
    /// Verifies TotalContextTokens is zero when empty.
    /// </summary>
    [Fact]
    public void TotalContextTokens_Empty_Zero()
    {
        // Arrange
        var vm = new ChatContextBarViewModel();

        // Assert
        Assert.Equal(0, vm.TotalContextTokens);
    }

    /// <summary>
    /// Verifies TotalContextTokens sums all contexts.
    /// </summary>
    [Fact]
    public void TotalContextTokens_SumsAllContexts()
    {
        // Arrange
        var vm = new ChatContextBarViewModel();
        vm.AddContext(FileContextViewModel.FromFile("/test1.cs", "code", 100));
        vm.AddContext(FileContextViewModel.FromFile("/test2.cs", "code", 150));
        vm.AddContext(FileContextViewModel.FromFile("/test3.cs", "code", 250));

        // Assert
        Assert.Equal(500, vm.TotalContextTokens);
    }

    #endregion

    #region Token Limit Tests

    /// <summary>
    /// Verifies IsNearTokenLimit when at 80% of limit.
    /// </summary>
    [Fact]
    public void IsNearTokenLimit_At80Percent_True()
    {
        // Arrange
        var vm = new ChatContextBarViewModel { MaxContextTokens = 1000 };
        vm.AddContext(FileContextViewModel.FromFile("/test.cs", "code", 800));

        // Assert
        Assert.True(vm.IsNearTokenLimit);
        Assert.False(vm.IsOverTokenLimit);
    }

    /// <summary>
    /// Verifies IsOverTokenLimit when over 100%.
    /// </summary>
    [Fact]
    public void IsOverTokenLimit_Over100Percent_True()
    {
        // Arrange
        var vm = new ChatContextBarViewModel { MaxContextTokens = 1000 };
        vm.AddContext(FileContextViewModel.FromFile("/test.cs", "code", 1200));

        // Assert
        Assert.True(vm.IsOverTokenLimit);
        Assert.False(vm.IsNearTokenLimit);
    }

    /// <summary>
    /// Verifies normal state when under 80%.
    /// </summary>
    [Fact]
    public void TokenLimits_Under80Percent_BothFalse()
    {
        // Arrange
        var vm = new ChatContextBarViewModel { MaxContextTokens = 1000 };
        vm.AddContext(FileContextViewModel.FromFile("/test.cs", "code", 500));

        // Assert
        Assert.False(vm.IsNearTokenLimit);
        Assert.False(vm.IsOverTokenLimit);
    }

    #endregion

    #region AddContext Tests

    /// <summary>
    /// Verifies AddContext adds to collection.
    /// </summary>
    [Fact]
    public void AddContext_AddsToCollection()
    {
        // Arrange
        var vm = new ChatContextBarViewModel();
        var context = FileContextViewModel.FromFile("/test.cs", "code", 100);

        // Act
        vm.AddContext(context);

        // Assert
        Assert.Single(vm.AttachedContexts);
        Assert.Same(context, vm.AttachedContexts[0]);
    }

    #endregion

    #region RemoveContext Tests

    /// <summary>
    /// Verifies RemoveContext removes from collection.
    /// </summary>
    [Fact]
    public void RemoveContext_RemovesFromCollection()
    {
        // Arrange
        var vm = new ChatContextBarViewModel();
        var context = FileContextViewModel.FromFile("/test.cs", "code", 100);
        vm.AddContext(context);

        // Act
        vm.RemoveContext(context);

        // Assert
        Assert.Empty(vm.AttachedContexts);
    }

    /// <summary>
    /// Verifies RemoveContext handles null safely.
    /// </summary>
    [Fact]
    public void RemoveContext_Null_NoOp()
    {
        // Arrange
        var vm = new ChatContextBarViewModel();

        // Act & Assert
        var exception = Record.Exception(() => vm.RemoveContext(null));
        Assert.Null(exception);
    }

    #endregion

    #region ClearAllContexts Tests

    /// <summary>
    /// Verifies ClearAllContexts removes all contexts.
    /// </summary>
    [Fact]
    public void ClearAllContexts_RemovesAll()
    {
        // Arrange
        var vm = new ChatContextBarViewModel();
        vm.AddContext(FileContextViewModel.FromFile("/test1.cs", "code", 100));
        vm.AddContext(FileContextViewModel.FromFile("/test2.cs", "code", 100));

        // Act
        vm.ClearAllContexts();

        // Assert
        Assert.Empty(vm.AttachedContexts);
    }

    #endregion

    #region ShowPreview Tests

    /// <summary>
    /// Verifies ShowPreview toggles IsExpanded.
    /// </summary>
    [Fact]
    public void ShowPreview_TogglesIsExpanded()
    {
        // Arrange
        var vm = new ChatContextBarViewModel();
        var context = FileContextViewModel.FromFile("/test.cs", "code", 100);
        vm.AddContext(context);
        Assert.False(context.IsExpanded);

        // Act
        vm.ShowPreview(context);

        // Assert
        Assert.True(context.IsExpanded);

        // Act again
        vm.ShowPreview(context);

        // Assert toggled back
        Assert.False(context.IsExpanded);
    }

    #endregion
}
