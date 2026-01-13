using AIntern.Core.Entities;
using AIntern.Core.Models;
using Xunit;

namespace AIntern.Core.Tests.Models;

/// <summary>
/// Unit tests for the <see cref="SystemPrompt"/> domain model.
/// Verifies validation, duplication, computed properties, and entity mapping.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify:
/// </para>
/// <list type="bullet">
///   <item><description>Field validation rules (required, max lengths)</description></item>
///   <item><description>Computed properties (<see cref="SystemPrompt.CharacterCount"/>, <see cref="SystemPrompt.EstimatedTokenCount"/>)</description></item>
///   <item><description>Duplication behavior (<see cref="SystemPrompt.Duplicate"/>)</description></item>
///   <item><description>Entity round-trip mapping</description></item>
/// </list>
/// </remarks>
public class SystemPromptTests
{
    #region Validation Tests

    /// <summary>
    /// Verifies that a valid system prompt passes validation.
    /// </summary>
    [Fact]
    public void Validate_ReturnsSuccess_WhenAllFieldsValid()
    {
        // Arrange
        var prompt = new SystemPrompt
        {
            Name = "Test Prompt",
            Content = "You are a helpful assistant.",
            Description = "A test description",
            Category = "General"
        };

        // Act
        var result = prompt.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// Verifies that validation fails when name is empty.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_ReturnsError_WhenNameEmpty(string? name)
    {
        // Arrange
        var prompt = new SystemPrompt
        {
            Name = name!,
            Content = "Valid content"
        };

        // Act
        var result = prompt.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Name is required"));
    }

    /// <summary>
    /// Verifies that validation fails when name exceeds maximum length.
    /// </summary>
    [Fact]
    public void Validate_ReturnsError_WhenNameTooLong()
    {
        // Arrange
        var prompt = new SystemPrompt
        {
            Name = new string('x', SystemPrompt.NameMaxLength + 1),
            Content = "Valid content"
        };

        // Act
        var result = prompt.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains($"{SystemPrompt.NameMaxLength}"));
    }

    /// <summary>
    /// Verifies that validation fails when content is empty.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_ReturnsError_WhenContentEmpty(string? content)
    {
        // Arrange
        var prompt = new SystemPrompt
        {
            Name = "Valid Name",
            Content = content!
        };

        // Act
        var result = prompt.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Content is required"));
    }

    /// <summary>
    /// Verifies that validation fails when content exceeds maximum length.
    /// </summary>
    [Fact]
    public void Validate_ReturnsError_WhenContentTooLong()
    {
        // Arrange
        var prompt = new SystemPrompt
        {
            Name = "Valid Name",
            Content = new string('x', SystemPrompt.ContentMaxLength + 1)
        };

        // Act
        var result = prompt.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Content"));
    }

    /// <summary>
    /// Verifies that validation fails when description exceeds maximum length.
    /// </summary>
    [Fact]
    public void Validate_ReturnsError_WhenDescriptionTooLong()
    {
        // Arrange
        var prompt = new SystemPrompt
        {
            Name = "Valid Name",
            Content = "Valid content",
            Description = new string('x', SystemPrompt.DescriptionMaxLength + 1)
        };

        // Act
        var result = prompt.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Description"));
    }

    /// <summary>
    /// Verifies that validation fails when category is empty.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_ReturnsError_WhenCategoryEmpty(string? category)
    {
        // Arrange
        var prompt = new SystemPrompt
        {
            Name = "Valid Name",
            Content = "Valid content",
            Category = category!
        };

        // Act
        var result = prompt.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Category is required"));
    }

    /// <summary>
    /// Verifies validation collects all errors, not just the first one.
    /// </summary>
    [Fact]
    public void Validate_CollectsAllErrors_WhenMultipleFailures()
    {
        // Arrange
        var prompt = new SystemPrompt
        {
            Name = "",
            Content = "",
            Category = ""
        };

        // Act
        var result = prompt.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 3);
    }

    #endregion

    #region Computed Property Tests

    /// <summary>
    /// Verifies CharacterCount returns content length.
    /// </summary>
    [Fact]
    public void CharacterCount_ReturnsContentLength()
    {
        // Arrange
        var content = "Hello, World!";
        var prompt = new SystemPrompt { Content = content };

        // Act & Assert
        Assert.Equal(content.Length, prompt.CharacterCount);
    }

    /// <summary>
    /// Verifies CharacterCount returns 0 for null content.
    /// </summary>
    [Fact]
    public void CharacterCount_ReturnsZero_WhenContentNull()
    {
        // Arrange
        var prompt = new SystemPrompt { Content = null! };

        // Act & Assert
        Assert.Equal(0, prompt.CharacterCount);
    }

    /// <summary>
    /// Verifies EstimatedTokenCount is approximately CharacterCount / 4.
    /// </summary>
    [Fact]
    public void EstimatedTokenCount_ReturnsApproximateTokens()
    {
        // Arrange
        var content = new string('a', 100);
        var prompt = new SystemPrompt { Content = content };

        // Act & Assert
        Assert.Equal(25, prompt.EstimatedTokenCount);
    }

    #endregion

    #region Duplicate Tests

    /// <summary>
    /// Verifies Duplicate creates new prompt with new ID.
    /// </summary>
    [Fact]
    public void Duplicate_CreatesNewId()
    {
        // Arrange
        var original = new SystemPrompt
        {
            Id = Guid.NewGuid(),
            Name = "Original",
            Content = "Content"
        };

        // Act
        var copy = original.Duplicate();

        // Assert
        Assert.NotEqual(original.Id, copy.Id);
        Assert.NotEqual(Guid.Empty, copy.Id);
    }

    /// <summary>
    /// Verifies Duplicate uses provided name or appends " (Copy)".
    /// </summary>
    [Fact]
    public void Duplicate_SetsCorrectName()
    {
        // Arrange
        var original = new SystemPrompt { Name = "Original", Content = "Content" };

        // Act
        var copyWithDefault = original.Duplicate();
        var copyWithCustom = original.Duplicate("Custom Name");

        // Assert
        Assert.Equal("Original (Copy)", copyWithDefault.Name);
        Assert.Equal("Custom Name", copyWithCustom.Name);
    }

    /// <summary>
    /// Verifies Duplicate copies content fields exactly.
    /// </summary>
    [Fact]
    public void Duplicate_CopiesContentFields()
    {
        // Arrange
        var original = new SystemPrompt
        {
            Name = "Original",
            Content = "Test content",
            Description = "Test description",
            Category = "Test"
        };

        // Act
        var copy = original.Duplicate();

        // Assert
        Assert.Equal(original.Content, copy.Content);
        Assert.Equal(original.Description, copy.Description);
        Assert.Equal(original.Category, copy.Category);
    }

    /// <summary>
    /// Verifies Duplicate deep-copies tags list.
    /// </summary>
    [Fact]
    public void Duplicate_DeepCopiesTags()
    {
        // Arrange
        var original = new SystemPrompt
        {
            Name = "Original",
            Content = "Content",
            Tags = ["tag1", "tag2"]
        };

        // Act
        var copy = original.Duplicate();
        copy.Tags.Add("tag3");  // Modify copy

        // Assert
        Assert.Equal(2, original.Tags.Count);  // Original unchanged
        Assert.Equal(3, copy.Tags.Count);
    }

    /// <summary>
    /// Verifies Duplicate resets flags appropriately.
    /// </summary>
    [Fact]
    public void Duplicate_ResetsFlags()
    {
        // Arrange
        var original = new SystemPrompt
        {
            Name = "Original",
            Content = "Content",
            IsBuiltIn = true,
            IsDefault = true,
            UsageCount = 100
        };

        // Act
        var copy = original.Duplicate();

        // Assert
        Assert.False(copy.IsBuiltIn);
        Assert.False(copy.IsDefault);
        Assert.True(copy.IsActive);
        Assert.Equal(0, copy.UsageCount);
    }

    /// <summary>
    /// Verifies Duplicate sets new timestamps.
    /// </summary>
    [Fact]
    public void Duplicate_SetsNewTimestamps()
    {
        // Arrange
        var pastTime = DateTime.UtcNow.AddDays(-30);
        var original = new SystemPrompt
        {
            Name = "Original",
            Content = "Content",
            CreatedAt = pastTime,
            UpdatedAt = pastTime
        };

        // Act
        var copy = original.Duplicate();

        // Assert
        Assert.True(copy.CreatedAt > pastTime);
        Assert.True(copy.UpdatedAt > pastTime);
    }

    #endregion

    #region Entity Mapping Tests

    /// <summary>
    /// Verifies FromEntity correctly maps all properties.
    /// </summary>
    [Fact]
    public void FromEntity_MapsAllProperties()
    {
        // Arrange
        var entity = new SystemPromptEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Entity",
            Content = "Entity content",
            Description = "Entity description",
            Category = "Test",
            TagsJson = "[\"tag1\", \"tag2\"]",
            IsBuiltIn = true,
            IsDefault = true,
            IsActive = true,
            UsageCount = 5
        };

        // Act
        var model = SystemPrompt.FromEntity(entity);

        // Assert
        Assert.Equal(entity.Id, model.Id);
        Assert.Equal(entity.Name, model.Name);
        Assert.Equal(entity.Content, model.Content);
        Assert.Equal(entity.Description, model.Description);
        Assert.Equal(entity.Category, model.Category);
        Assert.Equal(2, model.Tags.Count);
        Assert.Contains("tag1", model.Tags);
        Assert.Contains("tag2", model.Tags);
        Assert.Equal(entity.IsBuiltIn, model.IsBuiltIn);
        Assert.Equal(entity.IsDefault, model.IsDefault);
        Assert.Equal(entity.IsActive, model.IsActive);
        Assert.Equal(entity.UsageCount, model.UsageCount);
    }

    /// <summary>
    /// Verifies FromEntity handles null TagsJson gracefully.
    /// </summary>
    [Fact]
    public void FromEntity_HandlesNullTagsJson()
    {
        // Arrange
        var entity = new SystemPromptEntity
        {
            Name = "Test",
            Content = "Content",
            TagsJson = null
        };

        // Act
        var model = SystemPrompt.FromEntity(entity);

        // Assert
        Assert.NotNull(model.Tags);
        Assert.Empty(model.Tags);
    }

    /// <summary>
    /// Verifies ToEntity correctly maps all properties.
    /// </summary>
    [Fact]
    public void ToEntity_MapsAllProperties()
    {
        // Arrange
        var model = new SystemPrompt
        {
            Id = Guid.NewGuid(),
            Name = "Test Model",
            Content = "Model content",
            Description = "Model description",
            Category = "Test",
            Tags = ["tag1", "tag2"],
            IsBuiltIn = true,
            IsDefault = true,
            IsActive = true,
            UsageCount = 10
        };

        // Act
        var entity = model.ToEntity();

        // Assert
        Assert.Equal(model.Id, entity.Id);
        Assert.Equal(model.Name, entity.Name);
        Assert.Equal(model.Content, entity.Content);
        Assert.Equal(model.Description, entity.Description);
        Assert.Equal(model.Category, entity.Category);
        Assert.NotNull(entity.TagsJson);
        Assert.Contains("tag1", entity.TagsJson);
        Assert.Contains("tag2", entity.TagsJson);
        Assert.Equal(model.IsBuiltIn, entity.IsBuiltIn);
        Assert.Equal(model.IsDefault, entity.IsDefault);
        Assert.Equal(model.IsActive, entity.IsActive);
        Assert.Equal(model.UsageCount, entity.UsageCount);
    }

    /// <summary>
    /// Verifies ToEntity sets TagsJson to null when tags are empty.
    /// </summary>
    [Fact]
    public void ToEntity_SetsNullTagsJson_WhenTagsEmpty()
    {
        // Arrange
        var model = new SystemPrompt
        {
            Name = "Test",
            Content = "Content",
            Tags = []
        };

        // Act
        var entity = model.ToEntity();

        // Assert
        Assert.Null(entity.TagsJson);
    }

    /// <summary>
    /// Verifies round-trip entity mapping preserves all data.
    /// </summary>
    [Fact]
    public void EntityMapping_RoundTrip_PreservesData()
    {
        // Arrange
        var original = new SystemPrompt
        {
            Id = Guid.NewGuid(),
            Name = "Round Trip Test",
            Content = "Test content for round trip",
            Description = "Testing entity mapping",
            Category = "Testing",
            Tags = ["round", "trip", "test"],
            IsBuiltIn = true,
            IsDefault = false,
            IsActive = true,
            UsageCount = 42
        };

        // Act
        var entity = original.ToEntity();
        var restored = SystemPrompt.FromEntity(entity);

        // Assert
        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.Name, restored.Name);
        Assert.Equal(original.Content, restored.Content);
        Assert.Equal(original.Description, restored.Description);
        Assert.Equal(original.Category, restored.Category);
        Assert.Equal(original.Tags.Count, restored.Tags.Count);
        Assert.Equal(original.IsBuiltIn, restored.IsBuiltIn);
        Assert.Equal(original.IsDefault, restored.IsDefault);
        Assert.Equal(original.IsActive, restored.IsActive);
        Assert.Equal(original.UsageCount, restored.UsageCount);
    }

    #endregion
}
