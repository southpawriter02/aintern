using AIntern.Desktop.Services;
using Xunit;

namespace AIntern.Desktop.Tests.Services;

public class EditorSearchManagerTests
{
    // Note: Most EditorSearchManager methods require a TextEditor with UI context
    // which cannot be easily tested in unit tests. These tests verify null handling.

    [Fact]
    public void OpenFind_NullEditor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => EditorSearchManager.OpenFind(null!));
    }

    [Fact]
    public void OpenReplace_NullEditor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => EditorSearchManager.OpenReplace(null!));
    }

    [Fact]
    public void FindNext_NullEditor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => EditorSearchManager.FindNext(null!));
    }

    [Fact]
    public void FindPrevious_NullEditor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => EditorSearchManager.FindPrevious(null!));
    }

    [Fact]
    public void Close_NullEditor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => EditorSearchManager.Close(null!));
    }

    [Fact]
    public void IsOpen_NullEditor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => EditorSearchManager.IsOpen(null!));
    }

    [Fact]
    public void GetSearchPattern_NullEditor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => EditorSearchManager.GetSearchPattern(null!));
    }

    [Fact]
    public void SetSearchPattern_NullEditor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => EditorSearchManager.SetSearchPattern(null!, "test"));
    }

    [Fact]
    public void Uninstall_NullEditor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => EditorSearchManager.Uninstall(null!));
    }
}
