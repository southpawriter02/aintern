using System.Text.Json;
using AIntern.Core.Entities;
using AIntern.Core.Enums;
using AIntern.Core.Models;
using AIntern.Data.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AIntern.Services.Tests;

/// <summary>
/// Unit tests for <see cref="ExportService"/> (v0.2.5c).
/// Tests export to all formats, options handling, and utility methods.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify:
/// </para>
/// <list type="bullet">
///   <item><description>Constructor validation for null dependencies</description></item>
///   <item><description>ExportAsync exports to all formats correctly</description></item>
///   <item><description>Export options affect output (timestamps, metadata, etc.)</description></item>
///   <item><description>GeneratePreviewAsync truncates content properly</description></item>
///   <item><description>GetFileExtension and GetMimeType return correct values</description></item>
///   <item><description>SanitizeFileName handles special characters</description></item>
///   <item><description>Missing conversation returns error result</description></item>
/// </list>
/// <para>Added in v0.2.5c.</para>
/// </remarks>
public class ExportServiceTests
{
    #region Test Infrastructure

    /// <summary>
    /// Mock repository for loading conversations.
    /// </summary>
    private readonly Mock<IConversationRepository> _mockRepository;

    /// <summary>
    /// Mock logger for the export service.
    /// </summary>
    private readonly Mock<ILogger<ExportService>> _mockLogger;

    /// <summary>
    /// Test conversation ID used across tests.
    /// </summary>
    private readonly Guid _testConversationId = Guid.NewGuid();

    public ExportServiceTests()
    {
        _mockRepository = new Mock<IConversationRepository>();
        _mockLogger = new Mock<ILogger<ExportService>>();
    }

    /// <summary>
    /// Creates an ExportService with mock dependencies.
    /// </summary>
    private ExportService CreateService()
    {
        return new ExportService(_mockRepository.Object, _mockLogger.Object);
    }

    /// <summary>
    /// Creates a test conversation with messages.
    /// </summary>
    private ConversationEntity CreateTestConversation(
        string title = "Test Conversation",
        bool includeSystemPrompt = true,
        int messageCount = 2)
    {
        var conversation = new ConversationEntity
        {
            Id = _testConversationId,
            Title = title,
            CreatedAt = new DateTime(2026, 1, 14, 10, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 1, 14, 11, 0, 0, DateTimeKind.Utc),
            Messages = new List<MessageEntity>()
        };

        if (includeSystemPrompt)
        {
            conversation.SystemPrompt = new SystemPromptEntity
            {
                Id = Guid.NewGuid(),
                Name = "Test Prompt",
                Content = "You are a helpful assistant.",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        for (var i = 0; i < messageCount; i++)
        {
            var role = i % 2 == 0 ? MessageRole.User : MessageRole.Assistant;
            var content = i % 2 == 0 ? "Hello, can you help me?" : "Of course! How can I assist you?";

            conversation.Messages.Add(new MessageEntity
            {
                Id = Guid.NewGuid(),
                ConversationId = _testConversationId,
                Role = role,
                Content = content,
                SequenceNumber = i + 1,
                Timestamp = new DateTime(2026, 1, 14, 10, i, 0, DateTimeKind.Utc),
                TokenCount = 10 + i,
                IsComplete = true
            });
        }

        return conversation;
    }

    /// <summary>
    /// Sets up the mock repository to return the specified conversation.
    /// </summary>
    private void SetupRepository(ConversationEntity? conversation)
    {
        _mockRepository
            .Setup(r => r.GetByIdWithMessagesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies constructor throws when repository is null.
    /// </summary>
    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ExportService(null!, _mockLogger.Object));
    }

    /// <summary>
    /// Verifies constructor throws when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ExportService(_mockRepository.Object, null!));
    }

    /// <summary>
    /// Verifies constructor succeeds with valid dependencies.
    /// </summary>
    [Fact]
    public void Constructor_ValidDependencies_Succeeds()
    {
        // Act
        var service = CreateService();

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region ExportAsync - Not Found Tests

    /// <summary>
    /// Verifies ExportAsync returns error when conversation not found.
    /// </summary>
    [Fact]
    public async Task ExportAsync_ConversationNotFound_ReturnsError()
    {
        // Arrange
        var service = CreateService();
        SetupRepository(null);

        // Act
        var result = await service.ExportAsync(_testConversationId, ExportOptions.Default);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.Content);
    }

    #endregion

    #region ExportAsync - Markdown Tests

    /// <summary>
    /// Verifies Markdown export includes the conversation title.
    /// </summary>
    [Fact]
    public async Task ExportAsync_Markdown_IncludesTitle()
    {
        // Arrange
        var service = CreateService();
        var conversation = CreateTestConversation(title: "My Test Chat");
        SetupRepository(conversation);

        // Act
        var result = await service.ExportAsync(_testConversationId,
            new ExportOptions { Format = ExportFormat.Markdown });

        // Assert
        Assert.True(result.Success);
        Assert.StartsWith("# My Test Chat", result.Content);
        Assert.EndsWith(".md", result.SuggestedFileName);
        Assert.Equal("text/markdown", result.MimeType);
    }

    /// <summary>
    /// Verifies Markdown export includes timestamps when enabled.
    /// </summary>
    [Fact]
    public async Task ExportAsync_Markdown_IncludesTimestamps()
    {
        // Arrange
        var service = CreateService();
        SetupRepository(CreateTestConversation());

        // Act
        var result = await service.ExportAsync(_testConversationId,
            new ExportOptions { Format = ExportFormat.Markdown, IncludeTimestamps = true });

        // Assert
        Assert.True(result.Success);
        Assert.Contains("(10:00):", result.Content);
    }

    /// <summary>
    /// Verifies Markdown export excludes timestamps when disabled.
    /// </summary>
    [Fact]
    public async Task ExportAsync_Markdown_ExcludesTimestamps_WhenDisabled()
    {
        // Arrange
        var service = CreateService();
        SetupRepository(CreateTestConversation());

        // Act
        var result = await service.ExportAsync(_testConversationId,
            new ExportOptions { Format = ExportFormat.Markdown, IncludeTimestamps = false });

        // Assert
        Assert.True(result.Success);
        Assert.DoesNotContain("(10:00):", result.Content);
    }

    /// <summary>
    /// Verifies Markdown export includes system prompt when enabled.
    /// </summary>
    [Fact]
    public async Task ExportAsync_Markdown_IncludesSystemPrompt()
    {
        // Arrange
        var service = CreateService();
        SetupRepository(CreateTestConversation(includeSystemPrompt: true));

        // Act
        var result = await service.ExportAsync(_testConversationId,
            new ExportOptions { Format = ExportFormat.Markdown, IncludeSystemPrompt = true });

        // Assert
        Assert.True(result.Success);
        Assert.Contains("## System Prompt", result.Content);
        Assert.Contains("You are a helpful assistant.", result.Content);
    }

    /// <summary>
    /// Verifies Markdown export excludes system prompt when disabled.
    /// </summary>
    [Fact]
    public async Task ExportAsync_Markdown_ExcludesSystemPrompt_WhenDisabled()
    {
        // Arrange
        var service = CreateService();
        SetupRepository(CreateTestConversation(includeSystemPrompt: true));

        // Act
        var result = await service.ExportAsync(_testConversationId,
            new ExportOptions { Format = ExportFormat.Markdown, IncludeSystemPrompt = false });

        // Assert
        Assert.True(result.Success);
        Assert.DoesNotContain("## System Prompt", result.Content);
    }

    /// <summary>
    /// Verifies Markdown export includes metadata when enabled.
    /// </summary>
    [Fact]
    public async Task ExportAsync_Markdown_IncludesMetadata()
    {
        // Arrange
        var service = CreateService();
        SetupRepository(CreateTestConversation());

        // Act
        var result = await service.ExportAsync(_testConversationId,
            new ExportOptions { Format = ExportFormat.Markdown, IncludeMetadata = true });

        // Assert
        Assert.True(result.Success);
        Assert.Contains("**Created:**", result.Content);
        Assert.Contains("**Last Updated:**", result.Content);
    }

    /// <summary>
    /// Verifies Markdown export excludes metadata when disabled.
    /// </summary>
    [Fact]
    public async Task ExportAsync_Markdown_ExcludesMetadata_WhenDisabled()
    {
        // Arrange
        var service = CreateService();
        SetupRepository(CreateTestConversation());

        // Act
        var result = await service.ExportAsync(_testConversationId,
            new ExportOptions { Format = ExportFormat.Markdown, IncludeMetadata = false });

        // Assert
        Assert.True(result.Success);
        Assert.DoesNotContain("**Created:**", result.Content);
    }

    /// <summary>
    /// Verifies Markdown export includes token counts when enabled.
    /// </summary>
    [Fact]
    public async Task ExportAsync_Markdown_IncludesTokenCounts()
    {
        // Arrange
        var service = CreateService();
        SetupRepository(CreateTestConversation());

        // Act
        var result = await service.ExportAsync(_testConversationId,
            new ExportOptions { Format = ExportFormat.Markdown, IncludeTokenCounts = true });

        // Assert
        Assert.True(result.Success);
        Assert.Contains("*Tokens:", result.Content);
    }

    #endregion

    #region ExportAsync - JSON Tests

    /// <summary>
    /// Verifies JSON export produces valid JSON.
    /// </summary>
    [Fact]
    public async Task ExportAsync_Json_IsValidJson()
    {
        // Arrange
        var service = CreateService();
        SetupRepository(CreateTestConversation());

        // Act
        var result = await service.ExportAsync(_testConversationId,
            new ExportOptions { Format = ExportFormat.Json });

        // Assert
        Assert.True(result.Success);
        Assert.EndsWith(".json", result.SuggestedFileName);
        Assert.Equal("application/json", result.MimeType);

        // Verify valid JSON
        var doc = JsonDocument.Parse(result.Content);
        Assert.NotNull(doc.RootElement.GetProperty("title").GetString());
        Assert.True(doc.RootElement.GetProperty("messages").GetArrayLength() > 0);
    }

    /// <summary>
    /// Verifies JSON export includes correct structure.
    /// </summary>
    [Fact]
    public async Task ExportAsync_Json_HasCorrectStructure()
    {
        // Arrange
        var service = CreateService();
        SetupRepository(CreateTestConversation(title: "JSON Test"));

        // Act
        var result = await service.ExportAsync(_testConversationId,
            new ExportOptions { Format = ExportFormat.Json, IncludeSystemPrompt = true });

        // Assert
        var doc = JsonDocument.Parse(result.Content);
        Assert.Equal("JSON Test", doc.RootElement.GetProperty("title").GetString());
        Assert.Equal("You are a helpful assistant.", doc.RootElement.GetProperty("systemPrompt").GetString());
        Assert.True(doc.RootElement.TryGetProperty("createdAt", out _));
        Assert.True(doc.RootElement.TryGetProperty("updatedAt", out _));
        Assert.True(doc.RootElement.TryGetProperty("exportedAt", out _));
        Assert.Equal("AIntern", doc.RootElement.GetProperty("exportedBy").GetString());
    }

    #endregion

    #region ExportAsync - PlainText Tests

    /// <summary>
    /// Verifies PlainText export includes title with underline.
    /// </summary>
    [Fact]
    public async Task ExportAsync_PlainText_IncludesTitleWithUnderline()
    {
        // Arrange
        var service = CreateService();
        SetupRepository(CreateTestConversation(title: "Plain Test"));

        // Act
        var result = await service.ExportAsync(_testConversationId,
            new ExportOptions { Format = ExportFormat.PlainText });

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Plain Test\n==========", result.Content);
        Assert.EndsWith(".txt", result.SuggestedFileName);
        Assert.Equal("text/plain", result.MimeType);
    }

    /// <summary>
    /// Verifies PlainText export uses [HH:mm] timestamp format.
    /// </summary>
    [Fact]
    public async Task ExportAsync_PlainText_UsesCorrectTimestampFormat()
    {
        // Arrange
        var service = CreateService();
        SetupRepository(CreateTestConversation());

        // Act
        var result = await service.ExportAsync(_testConversationId,
            new ExportOptions { Format = ExportFormat.PlainText, IncludeTimestamps = true });

        // Assert
        Assert.True(result.Success);
        Assert.Contains("[10:00] USER:", result.Content);
    }

    #endregion

    #region ExportAsync - HTML Tests

    /// <summary>
    /// Verifies HTML export produces valid HTML structure.
    /// </summary>
    [Fact]
    public async Task ExportAsync_Html_HasValidStructure()
    {
        // Arrange
        var service = CreateService();
        SetupRepository(CreateTestConversation(title: "HTML Test"));

        // Act
        var result = await service.ExportAsync(_testConversationId,
            new ExportOptions { Format = ExportFormat.Html });

        // Assert
        Assert.True(result.Success);
        Assert.StartsWith("<!DOCTYPE html>", result.Content);
        Assert.Contains("<html lang=\"en\">", result.Content);
        Assert.Contains("<title>HTML Test</title>", result.Content);
        Assert.Contains("</html>", result.Content);
        Assert.EndsWith(".html", result.SuggestedFileName);
        Assert.Equal("text/html", result.MimeType);
    }

    /// <summary>
    /// Verifies HTML export includes embedded CSS.
    /// </summary>
    [Fact]
    public async Task ExportAsync_Html_HasEmbeddedCss()
    {
        // Arrange
        var service = CreateService();
        SetupRepository(CreateTestConversation());

        // Act
        var result = await service.ExportAsync(_testConversationId,
            new ExportOptions { Format = ExportFormat.Html });

        // Assert
        Assert.True(result.Success);
        Assert.Contains("<style>", result.Content);
        Assert.Contains("background: #1a1a2e", result.Content); // Dark theme
        Assert.Contains(".message.user", result.Content);
        Assert.Contains(".message.assistant", result.Content);
    }

    /// <summary>
    /// Verifies HTML export escapes special characters.
    /// </summary>
    [Fact]
    public async Task ExportAsync_Html_EscapesSpecialCharacters()
    {
        // Arrange
        var service = CreateService();
        var conversation = CreateTestConversation(title: "Test <script>alert('xss')</script>");
        SetupRepository(conversation);

        // Act
        var result = await service.ExportAsync(_testConversationId,
            new ExportOptions { Format = ExportFormat.Html });

        // Assert
        Assert.True(result.Success);
        Assert.DoesNotContain("<script>", result.Content);
        Assert.Contains("&lt;script&gt;", result.Content);
    }

    #endregion

    #region GeneratePreviewAsync Tests

    /// <summary>
    /// Verifies GeneratePreviewAsync returns full content if under max length.
    /// </summary>
    [Fact]
    public async Task GeneratePreviewAsync_ShortContent_ReturnsFullContent()
    {
        // Arrange
        var service = CreateService();
        SetupRepository(CreateTestConversation());

        // Act
        var preview = await service.GeneratePreviewAsync(
            _testConversationId,
            ExportOptions.Minimal,
            maxLength: 10000);

        // Assert
        Assert.DoesNotContain("(truncated)", preview);
    }

    /// <summary>
    /// Verifies GeneratePreviewAsync truncates long content.
    /// </summary>
    [Fact]
    public async Task GeneratePreviewAsync_LongContent_Truncates()
    {
        // Arrange
        var service = CreateService();
        SetupRepository(CreateTestConversation(messageCount: 10));

        // Act
        var preview = await service.GeneratePreviewAsync(
            _testConversationId,
            ExportOptions.Default,
            maxLength: 100);

        // Assert
        Assert.Contains("(truncated)", preview);
        Assert.True(preview.Length < 150); // Allow for "(truncated)" suffix
    }

    /// <summary>
    /// Verifies GeneratePreviewAsync returns error message on failure.
    /// </summary>
    [Fact]
    public async Task GeneratePreviewAsync_NotFound_ReturnsErrorMessage()
    {
        // Arrange
        var service = CreateService();
        SetupRepository(null);

        // Act
        var preview = await service.GeneratePreviewAsync(
            _testConversationId,
            ExportOptions.Default);

        // Assert
        Assert.StartsWith("Error:", preview);
    }

    #endregion

    #region GetFileExtension Tests

    /// <summary>
    /// Verifies GetFileExtension returns correct extension for each format.
    /// </summary>
    [Theory]
    [InlineData(ExportFormat.Markdown, ".md")]
    [InlineData(ExportFormat.Json, ".json")]
    [InlineData(ExportFormat.PlainText, ".txt")]
    [InlineData(ExportFormat.Html, ".html")]
    public void GetFileExtension_ReturnsCorrectExtension(ExportFormat format, string expected)
    {
        // Arrange
        var service = CreateService();

        // Act
        var extension = service.GetFileExtension(format);

        // Assert
        Assert.Equal(expected, extension);
    }

    #endregion

    #region GetMimeType Tests

    /// <summary>
    /// Verifies GetMimeType returns correct MIME type for each format.
    /// </summary>
    [Theory]
    [InlineData(ExportFormat.Markdown, "text/markdown")]
    [InlineData(ExportFormat.Json, "application/json")]
    [InlineData(ExportFormat.PlainText, "text/plain")]
    [InlineData(ExportFormat.Html, "text/html")]
    public void GetMimeType_ReturnsCorrectMimeType(ExportFormat format, string expected)
    {
        // Arrange
        var service = CreateService();

        // Act
        var mimeType = service.GetMimeType(format);

        // Assert
        Assert.Equal(expected, mimeType);
    }

    #endregion

    #region SanitizeFileName Tests

    /// <summary>
    /// Verifies SanitizeFileName removes special characters.
    /// </summary>
    [Theory]
    [InlineData("Hello: World!", "hello-world")]
    [InlineData("Test @#$% Name", "test-name")]
    [InlineData("My/Path\\File", "my-path-file")]
    [InlineData("Question?Answer!", "question-answer")]
    public void SanitizeFileName_RemovesSpecialCharacters(string input, string expected)
    {
        // Act
        var result = ExportService.SanitizeFileName(input);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Verifies SanitizeFileName handles empty input.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SanitizeFileName_EmptyInput_ReturnsDefault(string? input)
    {
        // Act
        var result = ExportService.SanitizeFileName(input!);

        // Assert
        Assert.Equal("conversation", result);
    }

    /// <summary>
    /// Verifies SanitizeFileName converts to lowercase.
    /// </summary>
    [Fact]
    public void SanitizeFileName_ConvertsToLowercase()
    {
        // Act
        var result = ExportService.SanitizeFileName("UPPERCASE Title");

        // Assert
        Assert.Equal("uppercase-title", result);
    }

    /// <summary>
    /// Verifies SanitizeFileName collapses multiple hyphens.
    /// </summary>
    [Fact]
    public void SanitizeFileName_CollapsesMultipleHyphens()
    {
        // Act
        var result = ExportService.SanitizeFileName("Test---Multiple---Hyphens");

        // Assert
        Assert.Equal("test-multiple-hyphens", result);
    }

    /// <summary>
    /// Verifies SanitizeFileName preserves valid characters.
    /// </summary>
    [Fact]
    public void SanitizeFileName_PreservesValidCharacters()
    {
        // Act
        var result = ExportService.SanitizeFileName("valid-file_name123");

        // Assert
        Assert.Equal("valid-file_name123", result);
    }

    #endregion

    #region Cancellation Tests

    /// <summary>
    /// Verifies ExportAsync respects cancellation token.
    /// </summary>
    [Fact]
    public async Task ExportAsync_Cancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var service = CreateService();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockRepository
            .Setup(r => r.GetByIdWithMessagesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await service.ExportAsync(_testConversationId, ExportOptions.Default, cts.Token));
    }

    #endregion
}
