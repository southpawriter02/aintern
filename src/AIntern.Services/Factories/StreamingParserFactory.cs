namespace AIntern.Services.Factories;

using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;

/// <summary>
/// Factory for creating streaming parser instances (v0.4.1f).
/// </summary>
public interface IStreamingParserFactory
{
    /// <summary>
    /// Create a new streaming parser instance.
    /// </summary>
    IStreamingCodeBlockParser Create();

    /// <summary>
    /// Create a new streaming parser initialized for a specific message.
    /// </summary>
    IStreamingCodeBlockParser Create(Guid messageId);
}

/// <summary>
/// Default implementation of the streaming parser factory (v0.4.1f).
/// </summary>
public sealed class StreamingParserFactory : IStreamingParserFactory
{
    private readonly ILanguageDetectionService _languageService;
    private readonly IBlockClassificationService _classificationService;
    private readonly ILoggerFactory? _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamingParserFactory"/> class.
    /// </summary>
    public StreamingParserFactory(
        ILanguageDetectionService languageService,
        IBlockClassificationService classificationService,
        ILoggerFactory? loggerFactory = null)
    {
        _languageService = languageService ?? throw new ArgumentNullException(nameof(languageService));
        _classificationService = classificationService ?? throw new ArgumentNullException(nameof(classificationService));
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc/>
    public IStreamingCodeBlockParser Create()
    {
        var logger = _loggerFactory?.CreateLogger<StreamingCodeBlockParser>();
        return new StreamingCodeBlockParser(
            _languageService,
            _classificationService,
            logger);
    }

    /// <inheritdoc/>
    public IStreamingCodeBlockParser Create(Guid messageId)
    {
        var parser = Create();
        parser.Reset(messageId);
        return parser;
    }
}
