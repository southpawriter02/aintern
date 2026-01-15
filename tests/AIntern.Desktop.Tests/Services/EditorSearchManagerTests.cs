namespace AIntern.Desktop.Tests.Services;

using System;
using Xunit;
using AIntern.Desktop.Services;

/// <summary>
/// Unit tests for <see cref="EditorSearchManager"/>.
/// </summary>
/// <remarks>
/// <para>
/// Since EditorSearchManager is a static class that requires AvaloniaEdit
/// TextEditor instances (which need Avalonia runtime), many tests verify
/// behavior via exception handling and state methods.
/// </para>
/// <para>Added in v0.3.3f.</para>
/// </remarks>
public class EditorSearchManagerTests
{
    #region Null Argument Tests

    /// <summary>
    /// Verifies that OpenFind throws ArgumentNullException for null editor.
    /// </summary>
    [Fact]
    public void OpenFind_NullEditor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => EditorSearchManager.OpenFind(null!));
    }

    /// <summary>
    /// Verifies that OpenReplace throws ArgumentNullException for null editor.
    /// </summary>
    [Fact]
    public void OpenReplace_NullEditor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => EditorSearchManager.OpenReplace(null!));
    }

    /// <summary>
    /// Verifies that FindNext throws ArgumentNullException for null editor.
    /// </summary>
    [Fact]
    public void FindNext_NullEditor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => EditorSearchManager.FindNext(null!));
    }

    /// <summary>
    /// Verifies that FindPrevious throws ArgumentNullException for null editor.
    /// </summary>
    [Fact]
    public void FindPrevious_NullEditor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => EditorSearchManager.FindPrevious(null!));
    }

    /// <summary>
    /// Verifies that Close throws ArgumentNullException for null editor.
    /// </summary>
    [Fact]
    public void Close_NullEditor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => EditorSearchManager.Close(null!));
    }

    /// <summary>
    /// Verifies that IsInstalled throws ArgumentNullException for null editor.
    /// </summary>
    [Fact]
    public void IsInstalled_NullEditor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => EditorSearchManager.IsInstalled(null!));
    }

    /// <summary>
    /// Verifies that IsOpen throws ArgumentNullException for null editor.
    /// </summary>
    [Fact]
    public void IsOpen_NullEditor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => EditorSearchManager.IsOpen(null!));
    }

    /// <summary>
    /// Verifies that GetSearchPattern throws ArgumentNullException for null editor.
    /// </summary>
    [Fact]
    public void GetSearchPattern_NullEditor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => EditorSearchManager.GetSearchPattern(null!));
    }

    /// <summary>
    /// Verifies that SetSearchPattern throws ArgumentNullException for null editor.
    /// </summary>
    [Fact]
    public void SetSearchPattern_NullEditor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => EditorSearchManager.SetSearchPattern(null!, "test"));
    }

    /// <summary>
    /// Verifies that IsReplaceMode throws ArgumentNullException for null editor.
    /// </summary>
    [Fact]
    public void IsReplaceMode_NullEditor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => EditorSearchManager.IsReplaceMode(null!));
    }

    /// <summary>
    /// Verifies that Uninstall throws ArgumentNullException for null editor.
    /// </summary>
    [Fact]
    public void Uninstall_NullEditor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => EditorSearchManager.Uninstall(null!));
    }

    #endregion

    #region SetLogger Tests

    /// <summary>
    /// Verifies that SetLogger accepts null logger without throwing.
    /// </summary>
    [Fact]
    public void SetLogger_NullLogger_DoesNotThrow()
    {
        // Act
        var exception = Record.Exception(() => EditorSearchManager.SetLogger(null));

        // Assert
        Assert.Null(exception);
    }

    #endregion

    #region GetInstalledPanelCount Tests

    /// <summary>
    /// Verifies that GetInstalledPanelCount returns a non-negative value.
    /// </summary>
    [Fact]
    public void GetInstalledPanelCount_ReturnsNonNegative()
    {
        // Act
        var count = EditorSearchManager.GetInstalledPanelCount();

        // Assert
        Assert.True(count >= 0);
    }

    #endregion

    #region ClearAll Tests

    /// <summary>
    /// Verifies that ClearAll does not throw when called.
    /// </summary>
    [Fact]
    public void ClearAll_DoesNotThrow()
    {
        // Act
        var exception = Record.Exception(() => EditorSearchManager.ClearAll());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies that ClearAll sets count to zero.
    /// </summary>
    [Fact]
    public void ClearAll_SetsCountToZero()
    {
        // Act
        EditorSearchManager.ClearAll();
        var count = EditorSearchManager.GetInstalledPanelCount();

        // Assert
        Assert.Equal(0, count);
    }

    #endregion
}
