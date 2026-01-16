namespace AIntern.Desktop.Tests.ViewModels;

using AIntern.Desktop.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="StatusBarViewModel"/>.
/// </summary>
public class StatusBarViewModelTests
{
    private readonly StatusBarViewModel _sut;

    public StatusBarViewModelTests()
    {
        _sut = new StatusBarViewModel(NullLogger<StatusBarViewModel>.Instance);
    }

    #region Initial State

    [Fact]
    public void InitialState_HasDefaultValues()
    {
        // Assert
        Assert.Null(_sut.WorkspaceName);
        Assert.False(_sut.HasWorkspace);
        Assert.Null(_sut.ActiveFileName);
        Assert.False(_sut.HasActiveFile);
        Assert.Equal(1, _sut.Line);
        Assert.Equal(1, _sut.Column);
        Assert.Equal(0, _sut.SelectionLength);
        Assert.Equal("UTF-8", _sut.Encoding);
        Assert.Equal("LF", _sut.LineEnding);
        Assert.False(_sut.HasUnsavedChanges);
        Assert.False(_sut.IsModelLoaded);
    }

    #endregion

    #region CursorDisplay

    [Fact]
    public void CursorDisplay_WithoutSelection_ShowsLineColumn()
    {
        // Arrange
        _sut.Line = 42;
        _sut.Column = 10;
        _sut.SelectionLength = 0;

        // Act & Assert
        Assert.Equal("Ln 42, Col 10", _sut.CursorDisplay);
    }

    [Fact]
    public void CursorDisplay_WithSelection_ShowsSelectionCount()
    {
        // Arrange
        _sut.Line = 10;
        _sut.Column = 5;
        _sut.SelectionLength = 25;

        // Act & Assert
        Assert.Equal("Ln 10, Col 5 (25 selected)", _sut.CursorDisplay);
    }

    #endregion

    #region UpdateFromEditor

    [Fact]
    public void UpdateFromEditor_WithNull_ClearsActiveFile()
    {
        // Arrange
        _sut.HasActiveFile = true;
        _sut.ActiveFileName = "test.cs";

        // Act
        _sut.UpdateFromEditor(null, null, null, 1, 1, 0, "UTF-8", "LF");

        // Assert
        Assert.False(_sut.HasActiveFile);
        Assert.Null(_sut.ActiveFileName);
    }

    [Fact]
    public void UpdateFromEditor_WithFile_SetsProperties()
    {
        // Act
        _sut.UpdateFromEditor("Test.cs", "/path/Test.cs", "csharp", 42, 10, 5, "UTF-16", "CRLF");

        // Assert
        Assert.True(_sut.HasActiveFile);
        Assert.Equal("Test.cs", _sut.ActiveFileName);
        Assert.Equal("/path/Test.cs", _sut.ActiveFilePath);
        Assert.Equal("csharp", _sut.Language);
        Assert.Equal("C#", _sut.LanguageDisplayName);
        Assert.Equal(42, _sut.Line);
        Assert.Equal(10, _sut.Column);
        Assert.Equal(5, _sut.SelectionLength);
        Assert.Equal("UTF-16", _sut.Encoding);
        Assert.Equal("CRLF", _sut.LineEnding);
    }

    #endregion

    #region UpdateWorkspace

    [Fact]
    public void UpdateWorkspace_WithName_SetsHasWorkspace()
    {
        // Act
        _sut.UpdateWorkspace("MyProject");

        // Assert
        Assert.True(_sut.HasWorkspace);
        Assert.Equal("MyProject", _sut.WorkspaceName);
    }

    [Fact]
    public void UpdateWorkspace_WithNull_ClearsWorkspace()
    {
        // Arrange
        _sut.UpdateWorkspace("Project");

        // Act
        _sut.UpdateWorkspace(null);

        // Assert
        Assert.False(_sut.HasWorkspace);
        Assert.Null(_sut.WorkspaceName);
    }

    #endregion

    #region UpdateUnsavedStatus

    [Fact]
    public void UpdateUnsavedStatus_WithCount_SetsHasUnsaved()
    {
        // Act
        _sut.UpdateUnsavedStatus(3);

        // Assert
        Assert.True(_sut.HasUnsavedChanges);
        Assert.Equal(3, _sut.UnsavedFilesCount);
    }

    [Fact]
    public void UpdateUnsavedStatus_WithZero_ClearsUnsaved()
    {
        // Arrange
        _sut.UpdateUnsavedStatus(2);

        // Act
        _sut.UpdateUnsavedStatus(0);

        // Assert
        Assert.False(_sut.HasUnsavedChanges);
        Assert.Equal(0, _sut.UnsavedFilesCount);
    }

    #endregion

    #region UpdateModelStatus

    [Fact]
    public void UpdateModelStatus_WhenLoaded_SetsProperties()
    {
        // Act
        _sut.UpdateModelStatus("llama-3.2", true, false);

        // Assert
        Assert.Equal("llama-3.2", _sut.ModelName);
        Assert.True(_sut.IsModelLoaded);
        Assert.False(_sut.IsModelLoading);
    }

    [Fact]
    public void UpdateModelStatus_WhenLoading_SetsLoadingFlag()
    {
        // Act
        _sut.UpdateModelStatus(null, false, true);

        // Assert
        Assert.False(_sut.IsModelLoaded);
        Assert.True(_sut.IsModelLoading);
    }

    #endregion

    #region Commands Raise Events

    [Fact]
    public void OpenWorkspaceCommand_RaisesEvent()
    {
        // Arrange
        var raised = false;
        _sut.OpenWorkspaceRequested += (s, e) => raised = true;

        // Act
        _sut.OpenWorkspaceCommand.Execute(null);

        // Assert
        Assert.True(raised);
    }

    [Fact]
    public void GoToLineCommand_RaisesEvent()
    {
        // Arrange
        var raised = false;
        _sut.GoToLineRequested += (s, e) => raised = true;

        // Act
        _sut.GoToLineCommand.Execute(null);

        // Assert
        Assert.True(raised);
    }

    [Fact]
    public void RevealActiveFileCommand_WhenHasActiveFile_RaisesEvent()
    {
        // Arrange
        _sut.HasActiveFile = true;
        var raised = false;
        _sut.RevealFileRequested += (s, e) => raised = true;

        // Act
        _sut.RevealActiveFileCommand.Execute(null);

        // Assert
        Assert.True(raised);
    }

    [Fact]
    public void RevealActiveFileCommand_WhenNoActiveFile_DoesNotRaiseEvent()
    {
        // Arrange
        _sut.HasActiveFile = false;
        var raised = false;
        _sut.RevealFileRequested += (s, e) => raised = true;

        // Act
        _sut.RevealActiveFileCommand.Execute(null);

        // Assert
        Assert.False(raised);
    }

    #endregion

    #region Tooltips

    [Fact]
    public void WorkspaceTooltip_WithWorkspace_ShowsName()
    {
        // Arrange
        _sut.UpdateWorkspace("TestProject");

        // Act & Assert
        Assert.Contains("TestProject", _sut.WorkspaceTooltip);
    }

    [Fact]
    public void ModelTooltip_WhenLoaded_ShowsModelName()
    {
        // Arrange
        _sut.UpdateModelStatus("gpt-4", true, false);

        // Act & Assert
        Assert.Contains("gpt-4", _sut.ModelTooltip);
    }

    [Fact]
    public void LineEndingTooltip_ForLF_ShowsUnix()
    {
        // Arrange
        _sut.LineEnding = "LF";

        // Act & Assert
        Assert.Contains("Unix", _sut.LineEndingTooltip);
    }

    [Fact]
    public void LineEndingTooltip_ForCRLF_ShowsWindows()
    {
        // Arrange
        _sut.LineEnding = "CRLF";

        // Act & Assert
        Assert.Contains("Windows", _sut.LineEndingTooltip);
    }

    #endregion
}
