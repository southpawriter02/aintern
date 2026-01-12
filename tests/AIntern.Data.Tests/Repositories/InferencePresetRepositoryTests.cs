using Xunit;
using AIntern.Core.Entities;
using AIntern.Data.Repositories;

namespace AIntern.Data.Tests.Repositories;

/// <summary>
/// Unit tests for InferencePresetRepository (v0.2.4).
/// </summary>
public class InferencePresetRepositoryTests : IDisposable
{
    private readonly TestDbContextFactoryWrapper _contextFactory;
    private readonly InferencePresetRepository _repository;

    public InferencePresetRepositoryTests()
    {
        _contextFactory = new TestDbContextFactoryWrapper();
        _repository = new InferencePresetRepository(_contextFactory);
    }

    public void Dispose()
    {
        _contextFactory.Dispose();
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_CreatesNewPreset()
    {
        var preset = CreateTestPresetEntity("Test Preset");

        var result = await _repository.CreateAsync(preset);

        Assert.NotNull(result);
        Assert.Equal(preset.Id, result.Id);
        Assert.Equal("Test Preset", result.Name);
    }

    [Fact]
    public async Task CreateAsync_PersistsToDatabase()
    {
        var preset = CreateTestPresetEntity("Persisted Preset");

        await _repository.CreateAsync(preset);

        var retrieved = await _repository.GetByIdAsync(preset.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Persisted Preset", retrieved.Name);
    }

    [Fact]
    public async Task CreateAsync_PersistsAllProperties()
    {
        var preset = CreateTestPresetEntity("Full Test");
        preset.Temperature = 0.7f;
        preset.TopP = 0.95f;
        preset.MaxTokens = 2048;
        preset.ContextSize = 8192;

        await _repository.CreateAsync(preset);

        var retrieved = await _repository.GetByIdAsync(preset.Id);
        Assert.Equal(0.7f, retrieved!.Temperature);
        Assert.Equal(0.95f, retrieved.TopP);
        Assert.Equal(2048, retrieved.MaxTokens);
        Assert.Equal(8192, retrieved.ContextSize);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ReturnsPreset_WhenExists()
    {
        var preset = await CreateTestPreset("Test");

        var result = await _repository.GetByIdAsync(preset.Id);

        Assert.NotNull(result);
        Assert.Equal(preset.Id, result.Id);
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
    public async Task GetAllAsync_ReturnsAllPresets()
    {
        await CreateTestPreset("Preset 1");
        await CreateTestPreset("Preset 2");
        await CreateTestPreset("Preset 3");

        var result = await _repository.GetAllAsync();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByDefaultThenBuiltInThenName()
    {
        await CreateTestPreset("Zebra");
        await CreateTestPreset("Default", isDefault: true);
        await CreateTestPreset("Built-In", isBuiltIn: true);
        await CreateTestPreset("Apple");

        var result = await _repository.GetAllAsync();

        Assert.Equal(4, result.Count);
        Assert.Equal("Default", result[0].Name); // Default first
        Assert.Equal("Built-In", result[1].Name); // Then built-in
        // Then alphabetical by name for the rest
    }

    #endregion

    #region GetDefaultAsync Tests

    [Fact]
    public async Task GetDefaultAsync_ReturnsDefaultPreset()
    {
        await CreateTestPreset("Not Default");
        var defaultPreset = await CreateTestPreset("Default", isDefault: true);
        await CreateTestPreset("Also Not Default");

        var result = await _repository.GetDefaultAsync();

        Assert.NotNull(result);
        Assert.Equal("Default", result.Name);
        Assert.True(result.IsDefault);
    }

    [Fact]
    public async Task GetDefaultAsync_ReturnsNull_WhenNoDefault()
    {
        await CreateTestPreset("Preset 1");
        await CreateTestPreset("Preset 2");

        var result = await _repository.GetDefaultAsync();

        Assert.Null(result);
    }

    #endregion

    #region GetBuiltInAsync Tests

    [Fact]
    public async Task GetBuiltInAsync_OnlyReturnsBuiltIn()
    {
        await CreateTestPreset("Built-In 1", isBuiltIn: true);
        await CreateTestPreset("Built-In 2", isBuiltIn: true);
        await CreateTestPreset("User Preset");

        var result = await _repository.GetBuiltInAsync();

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.True(p.IsBuiltIn));
    }

    [Fact]
    public async Task GetBuiltInAsync_OrdersByName()
    {
        await CreateTestPreset("Zebra", isBuiltIn: true);
        await CreateTestPreset("Apple", isBuiltIn: true);
        await CreateTestPreset("Mango", isBuiltIn: true);

        var result = await _repository.GetBuiltInAsync();

        Assert.Equal("Apple", result[0].Name);
        Assert.Equal("Mango", result[1].Name);
        Assert.Equal("Zebra", result[2].Name);
    }

    #endregion

    #region GetUserPresetsAsync Tests

    [Fact]
    public async Task GetUserPresetsAsync_ExcludesBuiltIn()
    {
        await CreateTestPreset("Built-In", isBuiltIn: true);
        await CreateTestPreset("User 1");
        await CreateTestPreset("User 2");

        var result = await _repository.GetUserPresetsAsync();

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.False(p.IsBuiltIn));
    }

    [Fact]
    public async Task GetUserPresetsAsync_OrdersByName()
    {
        await CreateTestPreset("Zebra");
        await CreateTestPreset("Apple");
        await CreateTestPreset("Mango");

        var result = await _repository.GetUserPresetsAsync();

        Assert.Equal("Apple", result[0].Name);
        Assert.Equal("Mango", result[1].Name);
        Assert.Equal("Zebra", result[2].Name);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_UpdatesProperties()
    {
        var preset = await CreateTestPreset("Original");

        preset.Name = "Updated";
        preset.Temperature = 0.9f;
        await _repository.UpdateAsync(preset);

        var retrieved = await _repository.GetByIdAsync(preset.Id);
        Assert.Equal("Updated", retrieved!.Name);
        Assert.Equal(0.9f, retrieved.Temperature);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesAllParameters()
    {
        var preset = await CreateTestPreset("Test");

        preset.Temperature = 0.5f;
        preset.TopP = 0.8f;
        preset.MaxTokens = 4096;
        preset.ContextSize = 16384;
        await _repository.UpdateAsync(preset);

        var retrieved = await _repository.GetByIdAsync(preset.Id);
        Assert.Equal(0.5f, retrieved!.Temperature);
        Assert.Equal(0.8f, retrieved.TopP);
        Assert.Equal(4096, retrieved.MaxTokens);
        Assert.Equal(16384, retrieved.ContextSize);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_RemovesUserPreset()
    {
        var preset = await CreateTestPreset("To Delete");

        await _repository.DeleteAsync(preset.Id);

        var retrieved = await _repository.GetByIdAsync(preset.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task DeleteAsync_DoesNotRemoveBuiltIn()
    {
        var preset = await CreateTestPreset("Built-In", isBuiltIn: true);

        await _repository.DeleteAsync(preset.Id);

        var retrieved = await _repository.GetByIdAsync(preset.Id);
        Assert.NotNull(retrieved); // Should still exist
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
        var preset1 = await CreateTestPreset("Preset 1", isDefault: true);
        var preset2 = await CreateTestPreset("Preset 2");

        await _repository.SetDefaultAsync(preset2.Id);

        var retrieved1 = await _repository.GetByIdAsync(preset1.Id);
        var retrieved2 = await _repository.GetByIdAsync(preset2.Id);

        Assert.False(retrieved1!.IsDefault);
        Assert.True(retrieved2!.IsDefault);
    }

    [Fact]
    public async Task SetDefaultAsync_ClearsOldDefault()
    {
        var preset1 = await CreateTestPreset("First Default", isDefault: true);
        var preset2 = await CreateTestPreset("Second");

        await _repository.SetDefaultAsync(preset2.Id);

        var defaultPreset = await _repository.GetDefaultAsync();
        Assert.Equal(preset2.Id, defaultPreset!.Id);
    }

    [Fact]
    public async Task SetDefaultAsync_OnlyOneDefaultAtATime()
    {
        await CreateTestPreset("Preset 1", isDefault: true);
        var preset2 = await CreateTestPreset("Preset 2");
        await CreateTestPreset("Preset 3");

        await _repository.SetDefaultAsync(preset2.Id);

        var all = await _repository.GetAllAsync();
        var defaults = all.Where(p => p.IsDefault).ToList();
        Assert.Single(defaults);
    }

    #endregion

    #region Helper Methods

    private InferencePresetEntity CreateTestPresetEntity(
        string name,
        bool isBuiltIn = false,
        bool isDefault = false)
    {
        return new InferencePresetEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsBuiltIn = isBuiltIn,
            IsDefault = isDefault,
            Temperature = 0.7f,
            TopP = 0.9f,
            MaxTokens = 2048,
            ContextSize = 4096
        };
    }

    private async Task<InferencePresetEntity> CreateTestPreset(
        string name,
        bool isBuiltIn = false,
        bool isDefault = false)
    {
        var preset = CreateTestPresetEntity(name, isBuiltIn, isDefault);
        await _repository.CreateAsync(preset);
        return preset;
    }

    #endregion
}
