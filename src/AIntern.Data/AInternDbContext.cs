using AIntern.Core.Entities;
using AIntern.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIntern.Data;

/// <summary>
/// Entity Framework Core DbContext for the AIntern application.
/// Provides access to conversations, messages, system prompts, and inference presets.
/// </summary>
/// <remarks>
/// <para>
/// This DbContext implements the following key features:
/// </para>
/// <list type="bullet">
///   <item><description>Four DbSet properties for entity access</description></item>
///   <item><description>Automatic timestamp management (CreatedAt/UpdatedAt)</description></item>
///   <item><description>Configuration auto-discovery via ApplyConfigurationsFromAssembly</description></item>
///   <item><description>Comprehensive logging at all appropriate levels</description></item>
///   <item><description>Dual constructors for DI and design-time scenarios</description></item>
/// </list>
/// <para>
/// The context automatically sets CreatedAt when entities are added and UpdatedAt when
/// entities are modified, ensuring consistent timestamp handling across the application.
/// </para>
/// </remarks>
/// <example>
/// Using with dependency injection:
/// <code>
/// services.AddDbContext&lt;AInternDbContext&gt;(options =>
///     options.UseSqlite(connectionString));
/// </code>
/// </example>
/// <example>
/// Using for testing with in-memory database:
/// <code>
/// var options = new DbContextOptionsBuilder&lt;AInternDbContext&gt;()
///     .UseSqlite("DataSource=:memory:")
///     .Options;
/// using var context = new AInternDbContext(options);
/// </code>
/// </example>
public class AInternDbContext : DbContext
{
    #region Fields

    /// <summary>
    /// Logger instance for diagnostic output.
    /// </summary>
    private readonly ILogger<AInternDbContext> _logger;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of <see cref="AInternDbContext"/> with the specified options.
    /// </summary>
    /// <param name="options">The options to configure the context.</param>
    /// <remarks>
    /// This constructor is used when the context is resolved via dependency injection.
    /// A <see cref="NullLogger{T}"/> is used when no logger is provided.
    /// </remarks>
    public AInternDbContext(DbContextOptions<AInternDbContext> options)
        : this(options, NullLogger<AInternDbContext>.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AInternDbContext"/> with options and logging.
    /// </summary>
    /// <param name="options">The options to configure the context.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <remarks>
    /// This constructor enables full logging support and is the primary constructor
    /// for production use with dependency injection.
    /// </remarks>
    public AInternDbContext(DbContextOptions<AInternDbContext> options, ILogger<AInternDbContext> logger)
        : base(options)
    {
        _logger = logger ?? NullLogger<AInternDbContext>.Instance;
        _logger.LogDebug("AInternDbContext instance created with options");
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AInternDbContext"/> for design-time scenarios.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This parameterless constructor is required for:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>EF Core design-time tools (migrations, scaffolding)</description></item>
    ///   <item><description>Unit testing without full DI setup</description></item>
    /// </list>
    /// <para>
    /// A warning is logged when this constructor is used in non-design-time scenarios.
    /// </para>
    /// </remarks>
    public AInternDbContext()
    {
        _logger = NullLogger<AInternDbContext>.Instance;
        _logger.LogWarning("AInternDbContext created with parameterless constructor (design-time fallback)");
    }

    #endregion

    #region DbSet Properties

    /// <summary>
    /// Gets the set of conversations in the database.
    /// </summary>
    /// <remarks>
    /// Conversations are the primary organizational unit for chat sessions.
    /// Each conversation contains an ordered collection of messages.
    /// </remarks>
    public DbSet<ConversationEntity> Conversations => Set<ConversationEntity>();

    /// <summary>
    /// Gets the set of messages in the database.
    /// </summary>
    /// <remarks>
    /// Messages belong to conversations and are ordered by SequenceNumber.
    /// Deleting a conversation cascades to delete all its messages.
    /// </remarks>
    public DbSet<MessageEntity> Messages => Set<MessageEntity>();

    /// <summary>
    /// Gets the set of system prompts in the database.
    /// </summary>
    /// <remarks>
    /// System prompts are reusable templates that can be associated with conversations.
    /// When a system prompt is deleted, associated conversations have their FK set to null.
    /// </remarks>
    public DbSet<SystemPromptEntity> SystemPrompts => Set<SystemPromptEntity>();

    /// <summary>
    /// Gets the set of inference presets in the database.
    /// </summary>
    /// <remarks>
    /// Inference presets store saved configurations for model generation parameters
    /// such as temperature, top-p, and context size.
    /// </remarks>
    public DbSet<InferencePresetEntity> InferencePresets => Set<InferencePresetEntity>();

    #endregion

    #region DbContext Overrides

    /// <summary>
    /// Configures the model using Fluent API configurations.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure entities.</param>
    /// <remarks>
    /// <para>
    /// Entity configurations are automatically discovered from the assembly containing
    /// <see cref="ConversationConfiguration"/> using <c>ApplyConfigurationsFromAssembly</c>.
    /// </para>
    /// <para>
    /// This approach ensures all configurations in the <c>Configurations</c> namespace
    /// are applied without explicit registration.
    /// </para>
    /// </remarks>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _logger.LogDebug("Applying entity configurations from assembly");

        base.OnModelCreating(modelBuilder);

        // Automatically apply all IEntityTypeConfiguration<T> implementations
        // from the assembly containing ConversationConfiguration
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ConversationConfiguration).Assembly);

        _logger.LogInformation(
            "Entity configurations applied for tables: {Tables}",
            string.Join(", ", new[]
            {
                ConversationConfiguration.TableName,
                MessageConfiguration.TableName,
                SystemPromptConfiguration.TableName,
                InferencePresetConfiguration.TableName
            }));
    }

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    /// <returns>The number of state entries written to the database.</returns>
    /// <remarks>
    /// This override automatically manages CreatedAt and UpdatedAt timestamps
    /// for all entities that have these properties.
    /// </remarks>
    public override int SaveChanges()
    {
        return SaveChanges(acceptAllChangesOnSuccess: true);
    }

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess">
    /// Indicates whether <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AcceptAllChanges"/>
    /// is called after the changes have been sent successfully to the database.
    /// </param>
    /// <returns>The number of state entries written to the database.</returns>
    /// <remarks>
    /// This override automatically manages CreatedAt and UpdatedAt timestamps
    /// for all entities that have these properties.
    /// </remarks>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>
    /// Asynchronously saves all changes made in this context to the database.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous save operation.
    /// The task result contains the number of state entries written to the database.
    /// </returns>
    /// <remarks>
    /// This override automatically manages CreatedAt and UpdatedAt timestamps
    /// for all entities that have these properties.
    /// </remarks>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);
    }

    /// <summary>
    /// Asynchronously saves all changes made in this context to the database.
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess">
    /// Indicates whether <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AcceptAllChanges"/>
    /// is called after the changes have been sent successfully to the database.
    /// </param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous save operation.
    /// The task result contains the number of state entries written to the database.
    /// </returns>
    /// <remarks>
    /// This override automatically manages CreatedAt and UpdatedAt timestamps
    /// for all entities that have these properties.
    /// </remarks>
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Updates CreatedAt and UpdatedAt timestamps for tracked entities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For added entities (EntityState.Added):
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Sets CreatedAt to current UTC time if it's default</description></item>
    ///   <item><description>Sets UpdatedAt to match CreatedAt</description></item>
    /// </list>
    /// <para>
    /// For modified entities (EntityState.Modified):
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Sets UpdatedAt to current UTC time</description></item>
    /// </list>
    /// <para>
    /// This method handles ConversationEntity, SystemPromptEntity, and InferencePresetEntity.
    /// MessageEntity uses Timestamp instead of CreatedAt/UpdatedAt pattern.
    /// </para>
    /// </remarks>
    private void UpdateTimestamps()
    {
        var now = DateTime.UtcNow;
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        var addedCount = 0;
        var modifiedCount = 0;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                addedCount++;
                SetCreatedTimestamp(entry.Entity, now);
            }

            if (entry.State == EntityState.Modified)
            {
                modifiedCount++;
            }

            SetUpdatedTimestamp(entry.Entity, now);
        }

        if (addedCount > 0 || modifiedCount > 0)
        {
            _logger.LogDebug(
                "Updated timestamps for {AddedCount} added and {ModifiedCount} modified entities",
                addedCount,
                modifiedCount);
        }
    }

    /// <summary>
    /// Sets the CreatedAt timestamp for an entity if applicable.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="timestamp">The timestamp to set.</param>
    private static void SetCreatedTimestamp(object entity, DateTime timestamp)
    {
        switch (entity)
        {
            case ConversationEntity conversation when conversation.CreatedAt == default:
                conversation.CreatedAt = timestamp;
                break;

            case SystemPromptEntity prompt when prompt.CreatedAt == default:
                prompt.CreatedAt = timestamp;
                break;

            case InferencePresetEntity preset when preset.CreatedAt == default:
                preset.CreatedAt = timestamp;
                break;

            case MessageEntity message when message.Timestamp == default:
                message.Timestamp = timestamp;
                break;
        }
    }

    /// <summary>
    /// Sets the UpdatedAt timestamp for an entity if applicable.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="timestamp">The timestamp to set.</param>
    private static void SetUpdatedTimestamp(object entity, DateTime timestamp)
    {
        switch (entity)
        {
            case ConversationEntity conversation:
                conversation.UpdatedAt = timestamp;
                break;

            case SystemPromptEntity prompt:
                prompt.UpdatedAt = timestamp;
                break;

            case InferencePresetEntity preset:
                preset.UpdatedAt = timestamp;
                break;

            // MessageEntity doesn't have UpdatedAt - it uses EditedAt which is set explicitly
        }
    }

    #endregion
}
