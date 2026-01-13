namespace AIntern.Desktop;

/// <summary>
/// Abstraction for UI thread dispatching.
/// </summary>
/// <remarks>
/// <para>
/// This interface enables unit testing of ViewModels without requiring the
/// Avalonia runtime. In production, <see cref="AvaloniaDispatcher"/> provides
/// the implementation using <c>Avalonia.Threading.Dispatcher.UIThread</c>.
/// </para>
/// <para>
/// For unit tests, create a mock or synchronous implementation that executes
/// actions immediately on the calling thread.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Production usage (via DI)
/// public ConversationListViewModel(IDispatcher dispatcher)
/// {
///     _dispatcher = dispatcher;
/// }
/// 
/// // Test usage
/// var mockDispatcher = new Mock&lt;IDispatcher&gt;();
/// mockDispatcher.Setup(d => d.InvokeAsync(It.IsAny&lt;Action&gt;()))
///     .Callback&lt;Action&gt;(a => a())
///     .Returns(Task.CompletedTask);
/// </code>
/// </example>
public interface IDispatcher
{
    /// <summary>
    /// Invokes an action on the UI thread.
    /// </summary>
    /// <param name="action">The action to invoke.</param>
    /// <returns>A task that completes when the action has executed.</returns>
    Task InvokeAsync(Action action);

    /// <summary>
    /// Invokes a function on the UI thread and returns the result.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="func">The function to invoke.</param>
    /// <returns>A task containing the result of the function.</returns>
    Task<T> InvokeAsync<T>(Func<T> func);

    /// <summary>
    /// Invokes an async action on the UI thread.
    /// </summary>
    /// <param name="action">The async action to invoke.</param>
    /// <returns>A task that completes when the async action has executed.</returns>
    Task InvokeAsync(Func<Task> action);
}
