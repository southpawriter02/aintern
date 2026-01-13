namespace AIntern.Desktop;

using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

/// <summary>
/// Avalonia implementation of <see cref="IDispatcher"/>.
/// </summary>
/// <remarks>
/// <para>
/// Uses <c>Avalonia.Threading.Dispatcher.UIThread</c> for all UI thread operations.
/// All operations are logged at Debug level for troubleshooting cross-thread issues.
/// </para>
/// <para>
/// This implementation ensures that all UI-bound operations are executed on the
/// correct thread, preventing cross-thread access exceptions in Avalonia.
/// </para>
/// </remarks>
public sealed class AvaloniaDispatcher : IDispatcher
{
    #region Fields

    private readonly ILogger<AvaloniaDispatcher> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaDispatcher"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    public AvaloniaDispatcher(ILogger<AvaloniaDispatcher> logger)
    {
        _logger = logger;
        _logger.LogDebug("[INIT] AvaloniaDispatcher created");
    }

    #endregion

    #region IDispatcher Implementation

    /// <inheritdoc />
    public async Task InvokeAsync(Action action)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] InvokeAsync(Action)");

        await Dispatcher.UIThread.InvokeAsync(action);

        _logger.LogDebug("[EXIT] InvokeAsync(Action) completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
    }

    /// <inheritdoc />
    public async Task<T> InvokeAsync<T>(Func<T> func)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] InvokeAsync<{TypeName}>", typeof(T).Name);

        var result = await Dispatcher.UIThread.InvokeAsync(func);

        _logger.LogDebug("[EXIT] InvokeAsync<{TypeName}> completed in {ElapsedMs}ms", typeof(T).Name, sw.ElapsedMilliseconds);
        return result;
    }

    /// <inheritdoc />
    public async Task InvokeAsync(Func<Task> action)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] InvokeAsync(Func<Task>)");

        await Dispatcher.UIThread.InvokeAsync(action);

        _logger.LogDebug("[EXIT] InvokeAsync(Func<Task>) completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
    }

    #endregion
}
