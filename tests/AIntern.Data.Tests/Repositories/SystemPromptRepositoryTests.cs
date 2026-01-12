using Xunit;
using AIntern.Core.Entities;
using AIntern.Data.Repositories;

namespace AIntern.Data.Tests.Repositories;

/// <summary>
/// Unit tests for SystemPromptRepository (v0.2.3).
/// </summary>
public class SystemPromptRepositoryTests : IDisposable
{
    private readonly TestDbContextFactoryWrapper _contextFactory;
    private readonly SystemPromptRepository _repository;

    public SystemPromptRepositoryTests()
    {
        _contextFactory = new TestDbContextFactoryWrapper();
        _repository = new SystemPromptRepository(_contextFactory);
    }

    public void Dispose()
    {
        _contextFactory.Dispose();
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_CreatesNewPrompt()
    {
        var prompt = CreateTestPromptEntity("Test Prompt");

        var result = await _repository.CreateAsync(prompt);

        Assert.NotNull(result);
        Assert.Equal(prompt.Id, result.Id);
        Assert.Equal("Test Prompt", result.Name);
    }

    [Fact]
    public async Task CreateAsync_PersistsToDatabase()
    {
        var prompt = CreateTestPromptEntity("Persisted Prompt");

        await _repository.CreateAsync(prompt);

        var retrieved = await _repository.GetByIdAsync(prompt.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Persisted Prompt", retrieved.Name);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ReturnsPrompt_WhenExists()
    {
        var prompt = await CreateTestPrompt("Test");

        var result = await _repository.GetByIdAsync(prompt.Id);

        Assert.NotNull(result);
        Assert.Equal(prompt.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllPrompts()
    {
        await CreateTestPrompt("Prompt 1");
        await CreateTestPrompt("Prompt 2");
        await CreateTestPrompt("Prompt 3");

        var result = await _repository.GetAllAsync();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByDefaultThenName()
    {
        await CreateTestPrompt("Zebra");
        var defaultPrompt = await CreateTestPrompt("Apple", isDefault: true);
        await CreateTestPrompt("Banana");

        var result = await _repository.GetAllAsync();

        Assert.Equal(3, result.Count);
        Assert.Equal("Apple", result[0].Name); // Default first
        Assert.Equal("Banana", result[1].Name); // Then by name
        Assert.Equal("Zebra", result[2].Name);
    }

    #endregion

    #region GetDefaultAsync Tests

    [Fact]
    public async Task GetDefaultAsync_ReturnsDefaultPrompt()
    {
        await CreateTestPrompt("Not Default");
        var defaultPrompt = await CreateTestPrompt("Default", isDefault: true);
        await CreateTestPrompt("Also Not Default");

        var result = await _repository.GetDefaultAsync();

        Assert.NotNull(result);
        Assert.Equal("Default", result.Name);
        Assert.True(result.IsDefault);
    }

    [Fact]
    public async Task GetDefaultAsync_ReturnsNull_WhenNoDefault()
    {
        await CreateTestPrompt("Prompt 1");
        await CreateTestPrompt("Prompt 2");

        var result = await _repository.GetDefaultAsync();

        Assert.Null(result);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_UpdatesProperties()
    {
        var prompt = await CreateTestPrompt("Original");

        prompt.Name = "Updated";
        prompt.Content = "New content";
        await _repository.UpdateAsync(prompt);

        var retrieved = await _repository.GetByIdAsync(prompt.Id);
        Assert.Equal("Updated", retrieved!.Name);
        Assert.Equal("New content", retrieved.Content);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTimestamp()
    {
        var prompt = await CreateTestPrompt("Original");
        var originalTime = prompt.UpdatedAt;

        await Task.Delay(10); // Small delay to ensure time difference
        await _repository.UpdateAsync(prompt);

        var retrieved = await _repository.GetByIdAsync(prompt.Id);
        Assert.True(retrieved!.UpdatedAt >= originalTime);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_RemovesPrompt()
    {
        var prompt = await CreateTestPrompt("To Delete");

        await _repository.DeleteAsync(prompt.Id);

        var retrieved = await _repository.GetByIdAsync(prompt.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsForBuiltIn()
    {
        var prompt = await CreateTestPrompt("Built-In", isBuiltIn: true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _repository.DeleteAsync(prompt.Id));
    }

    [Fact]
    public async Task DeleteAsync_DoesNothingForNonExistent()
    {
        // Should not throw
        await _repository.DeleteAsync(Guid.NewGuid());
    }

    #endregion

    #region SetDefaultAsync Tests

    [Fact]
    public async Task SetDefaultAsync_SetsNewDefault()
    {
        var prompt1 = await CreateTestPrompt("Prompt 1", isDefault: true);
        var prompt2 = await CreateTestPrompt("Prompt 2");

        await _repository.SetDefaultAsync(prompt2.Id);

        var retrieved1 = await _repository.GetByIdAsync(prompt1.Id);
        var retrieved2 = await _repository.GetByIdAsync(prompt2.Id);

        Assert.False(retrieved1!.IsDefault);
        Assert.True(retrieved2!.IsDefault);
    }

    [Fact]
    public async Task SetDefaultAsync_ClearsOldDefault()
    {
        var prompt1 = await CreateTestPrompt("First Default", isDefault: true);
        var prompt2 = await CreateTestPrompt("Second");

        await _repository.SetDefaultAsync(prompt2.Id);

        var defaultPrompt = await _repository.GetDefaultAsync();
        Assert.Equal(prompt2.Id, defaultPrompt!.Id);
    }

    #endregion

    #region IncrementUsageCountAsync Tests

    [Fact]
    public async Task IncrementUsageCountAsync_IncrementsCount()
    {
        var prompt = await CreateTestPrompt("Test");
        var originalCount = prompt.UsageCount;

        await _repository.IncrementUsageCountAsync(prompt.Id);

        var retrieved = await _repository.GetByIdAsync(prompt.Id);
        Assert.Equal(originalCount + 1, retrieved!.UsageCount);
    }

    [Fact]
    public async Task IncrementUsageCountAsync_MultipleIncrements()
    {
        var prompt = await CreateTestPrompt("Test");

        await _repository.IncrementUsageCountAsync(prompt.Id);
        await _repository.IncrementUsageCountAsync(prompt.Id);
        await _repository.IncrementUsageCountAsync(prompt.Id);

        var retrieved = await _repository.GetByIdAsync(prompt.Id);
        Assert.Equal(3, retrieved!.UsageCount);
    }

    #endregion

    #region GetByNameAsync Tests

    [Fact]
    public async Task GetByNameAsync_FindsPrompt()
    {
        await CreateTestPrompt("Unique Name");

        var result = await _repository.GetByNameAsync("Unique Name");

        Assert.NotNull(result);
        Assert.Equal("Unique Name", result.Name);
    }

    [Fact]
    public async Task GetByNameAsync_ReturnsNull_WhenNotFound()
    {
        await CreateTestPrompt("Test");

        var result = await _repository.GetByNameAsync("Nonexistent");

        Assert.Null(result);
    }

    #endregion

    #region GetUserPromptsAsync Tests

    [Fact]
    public async Task GetUserPromptsAsync_ExcludesBuiltIn()
    {
        await CreateTestPrompt("Built-In", isBuiltIn: true);
        await CreateTestPrompt("User 1");
        await CreateTestPrompt("User 2");

        var result = await _repository.GetUserPromptsAsync();

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.False(p.IsBuiltIn));
    }

    [Fact]
    public async Task GetUserPromptsAsync_OrdersByName()
    {
        await CreateTestPrompt("Zebra");
        await CreateTestPrompt("Apple");
        await CreateTestPrompt("Mango");

        var result = await _repository.GetUserPromptsAsync();

        Assert.Equal("Apple", result[0].Name);
        Assert.Equal("Mango", result[1].Name);
        Assert.Equal("Zebra", result[2].Name);
    }

    #endregion

    #region GetBuiltInPromptsAsync Tests

    [Fact]
    public async Task GetBuiltInPromptsAsync_OnlyReturnsBuiltIn()
    {
        await CreateTestPrompt("Built-In 1", isBuiltIn: true);
        await CreateTestPrompt("Built-In 2", isBuiltIn: true);
        await CreateTestPrompt("User Prompt");

        var result = await _repository.GetBuiltInPromptsAsync();

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.True(p.IsBuiltIn));
    }

    #endregion

    #region GetByCategoryAsync Tests

    [Fact]
    public async Task GetByCategoryAsync_FiltersByCategory()
    {
        await CreateTestPrompt("Coding 1", category: "Coding");
        await CreateTestPrompt("Coding 2", category: "Coding");
        await CreateTestPrompt("Writing", category: "Writing");

        var result = await _repository.GetByCategoryAsync("Coding");

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.Equal("Coding", p.Category));
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_FindsByName()
    {
        await CreateTestPrompt("Python Coding");
        await CreateTestPrompt("JavaScript Help");
        await CreateTestPrompt("Python Testing");

        var result = await _repository.SearchAsync("Python");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task SearchAsync_FindsByDescription()
    {
        var prompt = CreateTestPromptEntity("Test");
        prompt.Description = "Helps with coding tasks";
        await _repository.CreateAsync(prompt);

        await CreateTestPrompt("Other");

        var result = await _repository.SearchAsync("coding");

        Assert.Single(result);
        Assert.Equal("Test", result[0].Name);
    }

    [Fact]
    public async Task SearchAsync_FindsByContent()
    {
        var prompt = CreateTestPromptEntity("Test");
        prompt.Content = "You are a helpful assistant for debugging.";
        await _repository.CreateAsync(prompt);

        await CreateTestPrompt("Other");

        var result = await _repository.SearchAsync("debugging");

        Assert.Single(result);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_ForEmptyQuery()
    {
        await CreateTestPrompt("Test");

        var result = await _repository.SearchAsync("");

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchAsync_IsCaseInsensitive()
    {
        await CreateTestPrompt("Python Helper");

        var result = await _repository.SearchAsync("PYTHON");

        Assert.Single(result);
    }

    #endregion

    #region NameExistsAsync Tests

    [Fact]
    public async Task NameExistsAsync_ReturnsTrueWhenExists()
    {
        await CreateTestPrompt("Existing Name");

        var result = await _repository.NameExistsAsync("Existing Name");

        Assert.True(result);
    }

    [Fact]
    public async Task NameExistsAsync_ReturnsFalseWhenNotExists()
    {
        await CreateTestPrompt("Test");

        var result = await _repository.NameExistsAsync("Nonexistent");

        Assert.False(result);
    }

    [Fact]
    public async Task NameExistsAsync_ExcludesSpecifiedId()
    {
        var prompt = await CreateTestPrompt("Unique Name");

        var result = await _repository.NameExistsAsync("Unique Name", prompt.Id);

        Assert.False(result); // Should not find itself
    }

    #endregion

    #region Helper Methods

    private SystemPromptEntity CreateTestPromptEntity(
        string name,
        bool isBuiltIn = false,
        bool isDefault = false,
        string category = "Custom")
    {
        return new SystemPromptEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Content = $"Test content for {name}",
            Description = $"Description for {name}",
            Category = category,
            IsBuiltIn = isBuiltIn,
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UsageCount = 0
        };
    }

    private async Task<SystemPromptEntity> CreateTestPrompt(
        string name,
        bool isBuiltIn = false,
        bool isDefault = false,
        string category = "Custom")
    {
        var prompt = CreateTestPromptEntity(name, isBuiltIn, isDefault, category);
        await _repository.CreateAsync(prompt);
        return prompt;
    }

    #endregion
}
