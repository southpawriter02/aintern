using Xunit;
using AIntern.Core.Entities;
using AIntern.Core.Models;

namespace AIntern.Core.Tests.Entities;

/// <summary>
/// Unit tests for FileContextEntity (v0.3.1b).
/// </summary>
public class FileContextEntityTests
{
    [Fact]
    public void FromFileContext_MapsAllProperties()
    {
        var fileContext = FileContext.FromFile("/test/MyClass.cs", "public class Test { }");
        var conversationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        var entity = FileContextEntity.FromFileContext(fileContext, conversationId, messageId);

        Assert.Equal(fileContext.Id, entity.Id);
        Assert.Equal(conversationId, entity.ConversationId);
        Assert.Equal(messageId, entity.MessageId);
        Assert.Equal(fileContext.FilePath, entity.FilePath);
        Assert.Equal(fileContext.FileName, entity.FileName);
        Assert.Equal(fileContext.Language, entity.Language);
        Assert.Equal(fileContext.ContentHash, entity.ContentHash);
        Assert.Equal(fileContext.LineCount, entity.LineCount);
        Assert.Equal(fileContext.EstimatedTokens, entity.EstimatedTokens);
        Assert.Equal(fileContext.StartLine, entity.StartLine);
        Assert.Equal(fileContext.EndLine, entity.EndLine);
        Assert.Equal(fileContext.AttachedAt, entity.AttachedAt);
    }

    [Fact]
    public void FromFileContext_MapsPartialContent()
    {
        var fileContext = FileContext.FromSelection("/test/MyClass.cs", "code", 10, 20);
        var conversationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        var entity = FileContextEntity.FromFileContext(fileContext, conversationId, messageId);

        Assert.Equal(10, entity.StartLine);
        Assert.Equal(20, entity.EndLine);
    }

    [Fact]
    public void ToFileContextStub_CreatesCorrectStub()
    {
        var entity = new FileContextEntity
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            FilePath = "/test/MyClass.cs",
            FileName = "MyClass.cs",
            Language = "csharp",
            ContentHash = "abc123",
            LineCount = 100,
            EstimatedTokens = 500,
            StartLine = 10,
            EndLine = 20,
            AttachedAt = DateTime.UtcNow
        };

        var stub = entity.ToFileContextStub();

        Assert.Equal(entity.Id, stub.Id);
        Assert.Equal(entity.FilePath, stub.FilePath);
        Assert.Equal(entity.FileName, stub.FileName);
        Assert.Equal(entity.Language, stub.Language);
        Assert.Equal(entity.ContentHash, stub.ContentHash);
        Assert.Equal(entity.LineCount, stub.LineCount);
        Assert.Equal(entity.EstimatedTokens, stub.EstimatedTokens);
        Assert.Equal(entity.StartLine, stub.StartLine);
        Assert.Equal(entity.EndLine, stub.EndLine);
        Assert.Equal(entity.AttachedAt, stub.AttachedAt);
    }

    [Fact]
    public void FileContextStub_IsPartialContent_TrueWhenStartLineSet()
    {
        var stub = new FileContextStub
        {
            FilePath = "/test/file.cs",
            FileName = "file.cs",
            StartLine = 10,
            EndLine = null
        };

        Assert.True(stub.IsPartialContent);
    }

    [Fact]
    public void FileContextStub_IsPartialContent_TrueWhenEndLineSet()
    {
        var stub = new FileContextStub
        {
            FilePath = "/test/file.cs",
            FileName = "file.cs",
            StartLine = null,
            EndLine = 20
        };

        Assert.True(stub.IsPartialContent);
    }

    [Fact]
    public void FileContextStub_IsPartialContent_FalseWhenNoLinesSet()
    {
        var stub = new FileContextStub
        {
            FilePath = "/test/file.cs",
            FileName = "file.cs",
            StartLine = null,
            EndLine = null
        };

        Assert.False(stub.IsPartialContent);
    }

    [Fact]
    public void FileContextStub_DisplayLabel_ShowsFileName_WhenFullFile()
    {
        var stub = new FileContextStub
        {
            FilePath = "/test/file.cs",
            FileName = "file.cs",
            StartLine = null,
            EndLine = null
        };

        Assert.Equal("file.cs", stub.DisplayLabel);
    }

    [Fact]
    public void FileContextStub_DisplayLabel_ShowsLineRange_WhenPartialContent()
    {
        var stub = new FileContextStub
        {
            FilePath = "/test/file.cs",
            FileName = "file.cs",
            StartLine = 10,
            EndLine = 20
        };

        Assert.Equal("file.cs (lines 10-20)", stub.DisplayLabel);
    }

    [Fact]
    public void Entity_NavigationProperties_AreNullByDefault()
    {
        var entity = new FileContextEntity
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            FilePath = "/test/file.cs",
            FileName = "file.cs",
            ContentHash = "abc",
            AttachedAt = DateTime.UtcNow
        };

        Assert.Null(entity.Conversation);
        Assert.Null(entity.Message);
    }
}
