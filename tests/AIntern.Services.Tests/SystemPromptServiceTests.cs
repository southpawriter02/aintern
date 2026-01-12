using Xunit;
using NSubstitute;
using AIntern.Core.Entities;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Services;

namespace AIntern.Services.Tests;

/// <summary>
/// Unit tests for SystemPromptService (v0.2.3).
/// </summary>
public class SystemPromptServiceTests
{
    private readonly ISystemPromptRepository _repository;
    private readonly IConversationRepository _conversationRepository;
    private readonly ISettingsService _settingsService;
    private readonly SystemPromptService _service;
    private readonly AppSettings _appSettings;

    public SystemPromptServiceTests()
    {
        _repository = Substitute.For<ISystemPromptRepository>();
        _conversationRepository = Substitute.For<IConversationRepository>();
        _settingsService = Substitute.For<ISettingsService>();

        _appSettings = new AppSettings();
        _settingsService.CurrentSettings.Returns(_appSettings);

        _service = new SystemPromptService(_repository, _conversationRepository, _settingsService);
    }

    #region InitializeAsync Tests

    [Fact]
    public async Task InitializeAsync_SeedsBuiltInPrompts()
    {
        await _service.InitializeAsync();

        await _repository.Received(1).SeedBuiltInPromptsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeAsync_RestoresSavedPrompt()
    {
        var savedPromptId = Guid.NewGuid();
        _appSettings.CurrentSystemPromptId = savedPromptId;

        var savedPrompt = CreateTestEntity("Saved Prompt", savedPromptId);
        _repository.GetByIdAsync(savedPromptId, Arg.Any<CancellationToken>())
            .Returns(savedPrompt);

        await _service.InitializeAsync();

        Assert.NotNull(_service.CurrentPrompt);
        Assert.Equal(savedPromptId, _service.CurrentPrompt.Id);
    }

    [Fact]
    public async Task InitializeAsync_FallsBackToDefault_WhenSavedNotFound()
    {
        _appSettings.CurrentSystemPromptId = Guid.NewGuid();
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((SystemPromptEntity?)null);

        var defaultPrompt = CreateTestEntity("Default", isDefault: true);
        _repository.GetDefaultAsync(Arg.Any<CancellationToken>())
            .Returns(defaultPrompt);

        await _service.InitializeAsync();

        Assert.NotNull(_service.CurrentPrompt);
        Assert.Equal("Default", _service.CurrentPrompt.Name);
    }

    #endregion

    #region Query Method Tests

    [Fact]
    public async Task GetUserPromptsAsync_ReturnsUserPrompts()
    {
        var entities = new List<SystemPromptEntity>
        {
            CreateTestEntity("User 1"),
            CreateTestEntity("User 2")
        };
        _repository.GetUserPromptsAsync(Arg.Any<CancellationToken>())
            .Returns(entities);

        var result = await _service.GetUserPromptsAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetTemplatesAsync_ReturnsBuiltInPrompts()
    {
        var entities = new List<SystemPromptEntity>
        {
            CreateTestEntity("Template 1", isBuiltIn: true),
            CreateTestEntity("Template 2", isBuiltIn: true)
        };
        _repository.GetBuiltInPromptsAsync(Arg.Any<CancellationToken>())
            .Returns(entities);

        var result = await _service.GetTemplatesAsync();

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.True(p.IsBuiltIn));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsPrompt()
    {
        var id = Guid.NewGuid();
        var entity = CreateTestEntity("Test", id);
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(entity);

        var result = await _service.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((SystemPromptEntity?)null);

        var result = await _service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDefaultPromptAsync_ReturnsDefault()
    {
        var entity = CreateTestEntity("Default", isDefault: true);
        _repository.GetDefaultAsync(Arg.Any<CancellationToken>())
            .Returns(entity);

        var result = await _service.GetDefaultPromptAsync();

        Assert.NotNull(result);
        Assert.True(result.IsDefault);
    }

    [Fact]
    public async Task SearchPromptsAsync_ReturnsSearchResults()
    {
        var entities = new List<SystemPromptEntity>
        {
            CreateTestEntity("Python Helper"),
            CreateTestEntity("Python Coder")
        };
        _repository.SearchAsync("Python", Arg.Any<CancellationToken>())
            .Returns(entities);

        var result = await _service.SearchPromptsAsync("Python");

        Assert.Equal(2, result.Count);
    }

    #endregion

    #region CreatePromptAsync Tests

    [Fact]
    public async Task CreatePromptAsync_CreatesNewPrompt()
    {
        _repository.NameExistsAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _repository.CreateAsync(Arg.Any<SystemPromptEntity>(), Arg.Any<CancellationToken>())
            .Returns(x => x.Arg<SystemPromptEntity>());

        var result = await _service.CreatePromptAsync("New Prompt", "Content");

        Assert.Equal("New Prompt", result.Name);
        Assert.Equal("Content", result.Content);
    }

    [Fact]
    public async Task CreatePromptAsync_ThrowsForEmptyName()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreatePromptAsync("", "Content"));
    }

    [Fact]
    public async Task CreatePromptAsync_ThrowsForEmptyContent()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreatePromptAsync("Name", ""));
    }

    [Fact]
    public async Task CreatePromptAsync_ThrowsForDuplicateName()
    {
        _repository.NameExistsAsync("Existing", Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreatePromptAsync("Existing", "Content"));
    }

    [Fact]
    public async Task CreatePromptAsync_RaisesPromptListChangedEvent()
    {
        _repository.NameExistsAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _repository.CreateAsync(Arg.Any<SystemPromptEntity>(), Arg.Any<CancellationToken>())
            .Returns(x => x.Arg<SystemPromptEntity>());

        PromptListChangedEventArgs? eventArgs = null;
        _service.PromptListChanged += (_, args) => eventArgs = args;

        await _service.CreatePromptAsync("New", "Content");

        Assert.NotNull(eventArgs);
        Assert.Equal(PromptListChangeType.PromptCreated, eventArgs.ChangeType);
    }

    #endregion

    #region UpdatePromptAsync Tests

    [Fact]
    public async Task UpdatePromptAsync_UpdatesPrompt()
    {
        var entity = CreateTestEntity("Original");
        _repository.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(entity);
        _repository.NameExistsAsync(Arg.Any<string>(), entity.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        await _service.UpdatePromptAsync(entity.Id, name: "Updated");

        await _repository.Received(1).UpdateAsync(Arg.Is<SystemPromptEntity>(e => e.Name == "Updated"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdatePromptAsync_ThrowsForBuiltIn()
    {
        var entity = CreateTestEntity("Built-In", isBuiltIn: true);
        _repository.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(entity);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdatePromptAsync(entity.Id, name: "Updated"));
    }

    [Fact]
    public async Task UpdatePromptAsync_ThrowsForNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((SystemPromptEntity?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdatePromptAsync(Guid.NewGuid(), name: "Updated"));
    }

    #endregion

    #region DeletePromptAsync Tests

    [Fact]
    public async Task DeletePromptAsync_DeletesPrompt()
    {
        var entity = CreateTestEntity("To Delete");
        _repository.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(entity);

        await _service.DeletePromptAsync(entity.Id);

        await _repository.Received(1).DeleteAsync(entity.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeletePromptAsync_ThrowsForBuiltIn()
    {
        var entity = CreateTestEntity("Built-In", isBuiltIn: true);
        _repository.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(entity);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeletePromptAsync(entity.Id));
    }

    [Fact]
    public async Task DeletePromptAsync_RaisesPromptListChangedEvent()
    {
        var entity = CreateTestEntity("To Delete");
        _repository.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(entity);

        PromptListChangedEventArgs? eventArgs = null;
        _service.PromptListChanged += (_, args) => eventArgs = args;

        await _service.DeletePromptAsync(entity.Id);

        Assert.NotNull(eventArgs);
        Assert.Equal(PromptListChangeType.PromptDeleted, eventArgs.ChangeType);
    }

    #endregion

    #region SetAsDefaultAsync Tests

    [Fact]
    public async Task SetAsDefaultAsync_SetsDefault()
    {
        var entity = CreateTestEntity("New Default");
        _repository.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(entity);

        await _service.SetAsDefaultAsync(entity.Id);

        await _repository.Received(1).SetDefaultAsync(entity.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAsDefaultAsync_ThrowsForNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((SystemPromptEntity?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SetAsDefaultAsync(Guid.NewGuid()));
    }

    #endregion

    #region SetCurrentPromptAsync Tests

    [Fact]
    public async Task SetCurrentPromptAsync_SetsCurrentPrompt()
    {
        var entity = CreateTestEntity("Current");
        _repository.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(entity);

        await _service.SetCurrentPromptAsync(entity.Id);

        Assert.NotNull(_service.CurrentPrompt);
        Assert.Equal(entity.Id, _service.CurrentPrompt.Id);
    }

    [Fact]
    public async Task SetCurrentPromptAsync_IncrementsUsageCount()
    {
        var entity = CreateTestEntity("Current");
        _repository.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(entity);

        await _service.SetCurrentPromptAsync(entity.Id);

        await _repository.Received(1).IncrementUsageCountAsync(entity.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetCurrentPromptAsync_PersistsToSettings()
    {
        var entity = CreateTestEntity("Current");
        _repository.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(entity);

        await _service.SetCurrentPromptAsync(entity.Id);

        await _settingsService.Received(1).SaveSettingsAsync(Arg.Is<AppSettings>(s => s.CurrentSystemPromptId == entity.Id));
    }

    [Fact]
    public async Task SetCurrentPromptAsync_ClearsCurrentPrompt_WhenIdIsNull()
    {
        // First set a current prompt
        var entity = CreateTestEntity("Current");
        _repository.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(entity);
        await _service.SetCurrentPromptAsync(entity.Id);

        // Then clear it
        await _service.SetCurrentPromptAsync(null);

        Assert.Null(_service.CurrentPrompt);
    }

    [Fact]
    public async Task SetCurrentPromptAsync_RaisesCurrentPromptChangedEvent()
    {
        var entity = CreateTestEntity("Current");
        _repository.GetByIdAsync(entity.Id, Arg.Any<CancellationToken>())
            .Returns(entity);

        CurrentPromptChangedEventArgs? eventArgs = null;
        _service.CurrentPromptChanged += (_, args) => eventArgs = args;

        await _service.SetCurrentPromptAsync(entity.Id);

        Assert.NotNull(eventArgs);
        Assert.NotNull(eventArgs.NewPrompt);
        Assert.Equal(entity.Id, eventArgs.NewPrompt.Id);
    }

    #endregion

    #region FormatPromptForContext Tests

    [Fact]
    public void FormatPromptForContext_TrimsContent()
    {
        var prompt = new SystemPrompt
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Content = "   Content with whitespace   ",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = _service.FormatPromptForContext(prompt);

        Assert.Equal("Content with whitespace", result);
    }

    #endregion

    #region DuplicatePromptAsync Tests

    [Fact]
    public async Task DuplicatePromptAsync_CreatesNewPromptWithCopy()
    {
        var original = CreateTestEntity("Original");
        original.Content = "Original content";
        original.Description = "Original description";

        _repository.GetByIdAsync(original.Id, Arg.Any<CancellationToken>())
            .Returns(original);
        _repository.NameExistsAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _repository.CreateAsync(Arg.Any<SystemPromptEntity>(), Arg.Any<CancellationToken>())
            .Returns(x => x.Arg<SystemPromptEntity>());

        var result = await _service.DuplicatePromptAsync(original.Id);

        Assert.Contains("Copy", result.Name);
        Assert.Equal("Original content", result.Content);
    }

    [Fact]
    public async Task DuplicatePromptAsync_UsesProvidedName()
    {
        var original = CreateTestEntity("Original");
        _repository.GetByIdAsync(original.Id, Arg.Any<CancellationToken>())
            .Returns(original);
        _repository.NameExistsAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _repository.CreateAsync(Arg.Any<SystemPromptEntity>(), Arg.Any<CancellationToken>())
            .Returns(x => x.Arg<SystemPromptEntity>());

        var result = await _service.DuplicatePromptAsync(original.Id, "Custom Name");

        Assert.Equal("Custom Name", result.Name);
    }

    #endregion

    #region Helper Methods

    private static SystemPromptEntity CreateTestEntity(
        string name,
        Guid? id = null,
        bool isBuiltIn = false,
        bool isDefault = false)
    {
        return new SystemPromptEntity
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Content = $"Content for {name}",
            Description = $"Description for {name}",
            Category = "Custom",
            IsBuiltIn = isBuiltIn,
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UsageCount = 0
        };
    }

    #endregion
}
