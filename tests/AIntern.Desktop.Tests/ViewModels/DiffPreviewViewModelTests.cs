namespace AIntern.Desktop.Tests.ViewModels;

using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for <see cref="DiffPreviewViewModel"/>.
/// </summary>
public class DiffPreviewViewModelTests
{
    private readonly Mock<IDiffService> _mockDiffService = new();
    private readonly Mock<IInlineDiffService> _mockInlineDiffService = new();

    private DiffResult CreateTestDiff(
        string filePath = "/test/file.cs",
        bool isNewFile = false,
        int addedLines = 10,
        int removedLines = 5)
    {
        return new DiffResult
        {
            OriginalFilePath = filePath,
            IsNewFile = isNewFile,
            Stats = new DiffStats
            {
                AddedLines = addedLines,
                RemovedLines = removedLines
            }
        };
    }

    private DiffPreviewViewModel CreateViewModel(DiffResult diff)
    {
        return new DiffPreviewViewModel(diff, _mockDiffService.Object, _mockInlineDiffService.Object);
    }

    [Fact]
    public void FileName_ReturnsFileNameFromPath()
    {
        // Arrange
        var diff = CreateTestDiff("/some/path/to/MyFile.cs");
        var vm = CreateViewModel(diff);

        // Assert
        Assert.Equal("MyFile.cs", vm.FileName);
    }

    [Fact]
    public void FilePath_ReturnsFullPath()
    {
        // Arrange
        var diff = CreateTestDiff("/some/path/to/MyFile.cs");
        var vm = CreateViewModel(diff);

        // Assert
        Assert.Equal("/some/path/to/MyFile.cs", vm.FilePath);
    }

    [Fact]
    public void IsNewFile_ReflectsDiffResult()
    {
        // Arrange
        var diff = CreateTestDiff(isNewFile: true);
        var vm = CreateViewModel(diff);

        // Assert
        Assert.True(vm.IsNewFile);
    }

    [Fact]
    public void AddedLines_ReflectsDiffStats()
    {
        // Arrange
        var diff = CreateTestDiff(addedLines: 25);
        var vm = CreateViewModel(diff);

        // Assert
        Assert.Equal(25, vm.AddedLines);
    }

    [Fact]
    public void RemovedLines_ReflectsDiffStats()
    {
        // Arrange
        var diff = CreateTestDiff(removedLines: 15);
        var vm = CreateViewModel(diff);

        // Assert
        Assert.Equal(15, vm.RemovedLines);
    }

    [Fact]
    public void NetChange_CalculatesCorrectly()
    {
        // Arrange
        var diff = CreateTestDiff(addedLines: 20, removedLines: 8);
        var vm = CreateViewModel(diff);

        // Assert
        Assert.Equal(12, vm.NetChange);
    }

    [Fact]
    public void DisplayName_TruncatesLongFileNames()
    {
        // Arrange
        var diff = CreateTestDiff("/path/to/VeryLongFileNameThatExceedsTwentyCharacters.cs");
        var vm = CreateViewModel(diff);

        // Assert
        Assert.Equal("VeryLongFileNameT...", vm.DisplayName);
    }

    [Fact]
    public void DisplayName_KeepsShortFileNames()
    {
        // Arrange
        var diff = CreateTestDiff("/path/Short.cs");
        var vm = CreateViewModel(diff);

        // Assert
        Assert.Equal("Short.cs", vm.DisplayName);
    }

    [Fact]
    public void FileIcon_ReturnsCSharpIcon_ForCsFiles()
    {
        // Arrange
        var diff = CreateTestDiff("/path/File.cs");
        var vm = CreateViewModel(diff);

        // Assert
        Assert.Equal("CSharpIcon", vm.FileIcon);
    }

    [Fact]
    public void FileIcon_ReturnsJsonIcon_ForJsonFiles()
    {
        // Arrange
        var diff = CreateTestDiff("/path/config.json");
        var vm = CreateViewModel(diff);

        // Assert
        Assert.Equal("JsonIcon", vm.FileIcon);
    }

    [Fact]
    public void ChangesSummary_ForNewFile_ShowsOnlyAdded()
    {
        // Arrange
        var diff = CreateTestDiff(isNewFile: true, addedLines: 50);
        var vm = CreateViewModel(diff);

        // Assert
        Assert.Equal("+50", vm.ChangesSummary);
    }

    [Fact]
    public void ChangesSummary_ForModifiedFile_ShowsBoth()
    {
        // Arrange
        var diff = CreateTestDiff(isNewFile: false, addedLines: 15, removedLines: 7);
        var vm = CreateViewModel(diff);

        // Assert
        Assert.Equal("+15 -7", vm.ChangesSummary);
    }
}
