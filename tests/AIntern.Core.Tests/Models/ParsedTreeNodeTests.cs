using Xunit;
using AIntern.Core.Models;

namespace AIntern.Core.Tests.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ PARSED TREE NODE TESTS (v0.4.4b)                                         │
// │ Unit tests for ParsedTreeNode model.                                     │
// └─────────────────────────────────────────────────────────────────────────┘

public sealed class ParsedTreeNodeTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        var node = new ParsedTreeNode();

        Assert.Equal(string.Empty, node.Name);
        Assert.Equal(string.Empty, node.FullPath);
        Assert.False(node.IsDirectory);
        Assert.Equal(0, node.Depth);
        Assert.Empty(node.Children);
        Assert.Null(node.Parent);
    }

    [Fact]
    public void Extension_ReturnsExtension_ForFile()
    {
        var node = new ParsedTreeNode { Name = "User.cs" };
        Assert.Equal(".cs", node.Extension);
    }

    [Fact]
    public void Extension_ReturnsEmpty_ForDirectory()
    {
        var node = new ParsedTreeNode { Name = "Models", IsDirectory = true };
        Assert.Empty(node.Extension);
    }

    [Fact]
    public void HasChildren_ReturnsTrue_WhenChildrenExist()
    {
        var parent = new ParsedTreeNode();
        parent.Children.Add(new ParsedTreeNode { Name = "child" });

        Assert.True(parent.HasChildren);
        Assert.Equal(1, parent.ChildCount);
    }

    [Fact]
    public void IsRoot_ReturnsTrue_WhenNoParent()
    {
        var node = new ParsedTreeNode();
        Assert.True(node.IsRoot);
    }

    [Fact]
    public void IsRoot_ReturnsFalse_WhenHasParent()
    {
        var parent = new ParsedTreeNode();
        var child = new ParsedTreeNode { Parent = parent };
        Assert.False(child.IsRoot);
    }

    [Fact]
    public void GetAllFilePaths_ReturnsOwnPath_WhenFile()
    {
        var node = new ParsedTreeNode
        {
            FullPath = "src/User.cs",
            IsDirectory = false
        };

        var paths = node.GetAllFilePaths().ToList();

        Assert.Single(paths);
        Assert.Equal("src/User.cs", paths[0]);
    }

    [Fact]
    public void GetAllFilePaths_ReturnsChildPaths_WhenDirectory()
    {
        var dir = new ParsedTreeNode
        {
            Name = "src",
            FullPath = "src",
            IsDirectory = true
        };

        dir.Children.Add(new ParsedTreeNode
        {
            Name = "a.cs",
            FullPath = "src/a.cs",
            IsDirectory = false
        });
        dir.Children.Add(new ParsedTreeNode
        {
            Name = "b.cs",
            FullPath = "src/b.cs",
            IsDirectory = false
        });

        var paths = dir.GetAllFilePaths().ToList();

        Assert.Equal(2, paths.Count);
        Assert.Contains("src/a.cs", paths);
        Assert.Contains("src/b.cs", paths);
    }

    [Fact]
    public void GetAllDirectoryPaths_ReturnsOwnPath_WhenDirectory()
    {
        var dir = new ParsedTreeNode
        {
            Name = "src",
            FullPath = "src",
            IsDirectory = true
        };

        var paths = dir.GetAllDirectoryPaths().ToList();

        Assert.Single(paths);
        Assert.Equal("src", paths[0]);
    }

    [Fact]
    public void GetAllDescendants_ReturnsAllChildren()
    {
        var root = new ParsedTreeNode { Name = "root" };
        var child1 = new ParsedTreeNode { Name = "child1" };
        var child2 = new ParsedTreeNode { Name = "child2" };
        var grandchild = new ParsedTreeNode { Name = "grandchild" };

        root.Children.Add(child1);
        root.Children.Add(child2);
        child1.Children.Add(grandchild);

        var descendants = root.GetAllDescendants().ToList();

        Assert.Equal(3, descendants.Count);
    }

    [Fact]
    public void FindByPath_FindsMatchingNode()
    {
        var root = new ParsedTreeNode
        {
            Name = "root",
            FullPath = "root",
            IsDirectory = true
        };

        var child = new ParsedTreeNode
        {
            Name = "child.cs",
            FullPath = "root/child.cs"
        };
        root.Children.Add(child);

        var found = root.FindByPath("root/child.cs");

        Assert.NotNull(found);
        Assert.Equal("child.cs", found.Name);
    }

    [Fact]
    public void FindByPath_ReturnsNull_WhenNotFound()
    {
        var root = new ParsedTreeNode { FullPath = "root" };

        var found = root.FindByPath("nonexistent/path.cs");

        Assert.Null(found);
    }
}

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TREE PARSE RESULT TESTS                                                  │
// └─────────────────────────────────────────────────────────────────────────┘

public sealed class TreeParseResultTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        var result = new TreeParseResult();

        Assert.Empty(result.Paths);
        Assert.Empty(result.AllPaths);
        Assert.Empty(result.Directories);
        Assert.Equal(string.Empty, result.RawTreeText);
        Assert.False(result.Success);
        Assert.Equal(0, result.FileCount);
    }

    [Fact]
    public void Success_ReturnsTrue_WhenPathsExist()
    {
        var result = new TreeParseResult
        {
            Paths = new[] { "src/file.cs" }
        };

        Assert.True(result.Success);
        Assert.Equal(1, result.FileCount);
    }

    [Fact]
    public void Empty_CreatesEmptyResult()
    {
        var result = TreeParseResult.Empty("raw text");

        Assert.Empty(result.Paths);
        Assert.False(result.Success);
        Assert.Equal("raw text", result.RawTreeText);
        Assert.Equal(TreeFormat.Unknown, result.Format);
    }

    [Fact]
    public void Failed_CreatesFailedResult()
    {
        var result = TreeParseResult.Failed("Some error", "raw text");

        Assert.Empty(result.Paths);
        Assert.False(result.Success);
        Assert.Equal("Some error", result.ErrorMessage);
        Assert.Equal("raw text", result.RawTreeText);
    }
}

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ FILE TREE PARSER OPTIONS TESTS                                           │
// └─────────────────────────────────────────────────────────────────────────┘

public sealed class FileTreeParserOptionsTests
{
    [Fact]
    public void Default_HasExpectedValues()
    {
        var options = FileTreeParserOptions.Default;

        Assert.Equal(2, options.MinimumFilesForProposal);
        Assert.Equal(20, options.MaxTreeDepth);
        Assert.Equal(100, options.MaxFilesInProposal);
        Assert.True(options.EnableSimpleListing);
        Assert.True(options.TrimComments);
        Assert.True(options.PreserveRawTreeText);
        Assert.True(options.RequireStructureIndicator);
    }

    [Fact]
    public void Lenient_HasLenientValues()
    {
        var options = FileTreeParserOptions.Lenient;

        Assert.Equal(1, options.MinimumFilesForProposal);
        Assert.False(options.RequireStructureIndicator);
        Assert.Equal(500, options.MaxFilesInProposal);
    }

    [Fact]
    public void Strict_HasStrictValues()
    {
        var options = FileTreeParserOptions.Strict;

        Assert.Equal(3, options.MinimumFilesForProposal);
        Assert.True(options.RequireStructureIndicator);
        Assert.Equal(50, options.MaxFilesInProposal);
    }

    [Fact]
    public void StructureIndicators_ContainsExpectedTerms()
    {
        var options = FileTreeParserOptions.Default;

        Assert.Contains("project structure", options.StructureIndicators);
        Assert.Contains("file structure", options.StructureIndicators);
        Assert.Contains("create these files", options.StructureIndicators);
    }
}
