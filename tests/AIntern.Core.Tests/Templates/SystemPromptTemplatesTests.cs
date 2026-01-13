using AIntern.Core.Templates;
using Xunit;

namespace AIntern.Core.Tests.Templates;

/// <summary>
/// Unit tests for the <see cref="SystemPromptTemplates"/> static class.
/// Verifies that all built-in templates are correctly defined.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify:
/// </para>
/// <list type="bullet">
///   <item><description>All 8 templates are present</description></item>
///   <item><description>Each template has required fields</description></item>
///   <item><description>Well-known GUIDs are unique</description></item>
///   <item><description>Only one template is marked as default</description></item>
/// </list>
/// </remarks>
public class SystemPromptTemplatesTests
{
    #region Template Count Tests

    /// <summary>
    /// Verifies that GetAllTemplates returns exactly 8 templates.
    /// </summary>
    [Fact]
    public void GetAllTemplates_ReturnsExpectedCount()
    {
        // Act
        var templates = SystemPromptTemplates.GetAllTemplates();

        // Assert
        Assert.Equal(8, templates.Count);
    }

    /// <summary>
    /// Verifies that all expected template names are present.
    /// </summary>
    [Fact]
    public void GetAllTemplates_ContainsAllExpectedNames()
    {
        // Arrange
        var expectedNames = new[]
        {
            "Default Assistant",
            "The Senior Intern",
            "Code Expert",
            "Technical Writer",
            "Rubber Duck",
            "Socratic Tutor",
            "Code Reviewer",
            "Debugger"
        };

        // Act
        var templates = SystemPromptTemplates.GetAllTemplates();
        var actualNames = templates.Select(t => t.Name).ToList();

        // Assert
        foreach (var name in expectedNames)
        {
            Assert.Contains(name, actualNames);
        }
    }

    #endregion

    #region Required Fields Tests

    /// <summary>
    /// Verifies that all templates have required fields populated.
    /// </summary>
    [Fact]
    public void GetAllTemplates_AllHaveRequiredFields()
    {
        // Act
        var templates = SystemPromptTemplates.GetAllTemplates();

        // Assert
        foreach (var template in templates)
        {
            Assert.NotEqual(Guid.Empty, template.Id);
            Assert.False(string.IsNullOrWhiteSpace(template.Name));
            Assert.False(string.IsNullOrWhiteSpace(template.Content));
            Assert.False(string.IsNullOrWhiteSpace(template.Description));
            Assert.False(string.IsNullOrWhiteSpace(template.Category));
            Assert.True(template.IsBuiltIn);
            Assert.True(template.IsActive);
        }
    }

    /// <summary>
    /// Verifies that all templates have tags defined.
    /// </summary>
    [Fact]
    public void GetAllTemplates_AllHaveTags()
    {
        // Act
        var templates = SystemPromptTemplates.GetAllTemplates();

        // Assert
        foreach (var template in templates)
        {
            Assert.False(string.IsNullOrWhiteSpace(template.TagsJson));
        }
    }

    #endregion

    #region GUID Uniqueness Tests

    /// <summary>
    /// Verifies that all template GUIDs are unique.
    /// </summary>
    [Fact]
    public void GetAllTemplates_AllHaveUniqueIds()
    {
        // Act
        var templates = SystemPromptTemplates.GetAllTemplates();
        var ids = templates.Select(t => t.Id).ToList();

        // Assert
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    /// <summary>
    /// Verifies that well-known GUIDs match template IDs.
    /// </summary>
    [Fact]
    public void WellKnownGuids_MatchTemplateIds()
    {
        // Act
        var templates = SystemPromptTemplates.GetAllTemplates();

        // Assert
        Assert.Contains(templates, t => t.Id == SystemPromptTemplates.DefaultAssistantId);
        Assert.Contains(templates, t => t.Id == SystemPromptTemplates.SeniorInternId);
        Assert.Contains(templates, t => t.Id == SystemPromptTemplates.CodeExpertId);
        Assert.Contains(templates, t => t.Id == SystemPromptTemplates.TechnicalWriterId);
        Assert.Contains(templates, t => t.Id == SystemPromptTemplates.RubberDuckId);
        Assert.Contains(templates, t => t.Id == SystemPromptTemplates.SocraticTutorId);
        Assert.Contains(templates, t => t.Id == SystemPromptTemplates.CodeReviewerId);
        Assert.Contains(templates, t => t.Id == SystemPromptTemplates.DebuggerId);
    }

    #endregion

    #region Default Template Tests

    /// <summary>
    /// Verifies that exactly one template is marked as default.
    /// </summary>
    [Fact]
    public void GetAllTemplates_HasExactlyOneDefault()
    {
        // Act
        var templates = SystemPromptTemplates.GetAllTemplates();
        var defaults = templates.Where(t => t.IsDefault).ToList();

        // Assert
        Assert.Single(defaults);
    }

    /// <summary>
    /// Verifies that Default Assistant is marked as default.
    /// </summary>
    [Fact]
    public void DefaultAssistant_IsMarkedAsDefault()
    {
        // Act
        var templates = SystemPromptTemplates.GetAllTemplates();
        var defaultTemplate = templates.Single(t => t.IsDefault);

        // Assert
        Assert.Equal("Default Assistant", defaultTemplate.Name);
        Assert.Equal(SystemPromptTemplates.DefaultAssistantId, defaultTemplate.Id);
    }

    #endregion

    #region Category Tests

    /// <summary>
    /// Verifies that all templates have valid categories.
    /// </summary>
    [Fact]
    public void GetAllTemplates_HaveValidCategories()
    {
        // Arrange
        var validCategories = new[] { "General", "Creative", "Code", "Technical" };

        // Act
        var templates = SystemPromptTemplates.GetAllTemplates();

        // Assert
        foreach (var template in templates)
        {
            Assert.Contains(template.Category, validCategories);
        }
    }

    #endregion

    #region Individual Factory Method Tests

    /// <summary>
    /// Verifies CreateDefaultAssistant returns correct template.
    /// </summary>
    [Fact]
    public void CreateDefaultAssistant_ReturnsCorrectTemplate()
    {
        // Act
        var template = SystemPromptTemplates.CreateDefaultAssistant();

        // Assert
        Assert.Equal(SystemPromptTemplates.DefaultAssistantId, template.Id);
        Assert.Equal("Default Assistant", template.Name);
        Assert.Equal("General", template.Category);
        Assert.True(template.IsDefault);
        Assert.True(template.IsBuiltIn);
    }

    /// <summary>
    /// Verifies CreateSeniorIntern returns correct template.
    /// </summary>
    [Fact]
    public void CreateSeniorIntern_ReturnsCorrectTemplate()
    {
        // Act
        var template = SystemPromptTemplates.CreateSeniorIntern();

        // Assert
        Assert.Equal(SystemPromptTemplates.SeniorInternId, template.Id);
        Assert.Equal("The Senior Intern", template.Name);
        Assert.Equal("Creative", template.Category);
        Assert.False(template.IsDefault);
        Assert.True(template.IsBuiltIn);
    }

    /// <summary>
    /// Verifies CreateCodeReviewer returns correct template.
    /// </summary>
    [Fact]
    public void CreateCodeReviewer_ReturnsCorrectTemplate()
    {
        // Act
        var template = SystemPromptTemplates.CreateCodeReviewer();

        // Assert
        Assert.Equal(SystemPromptTemplates.CodeReviewerId, template.Id);
        Assert.Equal("Code Reviewer", template.Name);
        Assert.Equal("Code", template.Category);
        Assert.False(template.IsDefault);
        Assert.True(template.IsBuiltIn);
    }

    /// <summary>
    /// Verifies CreateDebugger returns correct template.
    /// </summary>
    [Fact]
    public void CreateDebugger_ReturnsCorrectTemplate()
    {
        // Act
        var template = SystemPromptTemplates.CreateDebugger();

        // Assert
        Assert.Equal(SystemPromptTemplates.DebuggerId, template.Id);
        Assert.Equal("Debugger", template.Name);
        Assert.Equal("Code", template.Category);
        Assert.False(template.IsDefault);
        Assert.True(template.IsBuiltIn);
    }

    #endregion
}
