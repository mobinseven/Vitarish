namespace Vitarish.Intelligence.Interface.Abstractions;

public interface IStreamRewriter : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether a rewrite operation is currently in progress.
    /// </summary>
    bool Rewriting { get; }

    /// <summary>
    /// Gets the current rewritten value as it is produced.
    /// </summary>
    string Value { get; }

    /// <summary>
    /// Raised when a rewrite operation starts.
    /// </summary>
    event EventHandler? RewriteStarted;

    /// <summary>
    /// Raised when the rewritten value changes during the rewrite operation.
    /// </summary>
    event EventHandler<string>? ValueChanged;

    /// <summary>
    /// Raised when the rewrite operation completes.
    /// </summary>
    event EventHandler? RewriteCompleted;

    /// <summary>
    /// Raised when an exception occurs during rewriting.
    /// </summary>
    event EventHandler<Exception>? RewriteErrored;

    /// <summary>
    /// Starts an asynchronous rewrite operation for the specified prompt.
    /// </summary>
    /// <param name="prompt">The prompt to rewrite.</param>
    /// <param name="cancellationToken">Token to cancel the rewrite operation.</param>
    Task StartRewriteAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels the current rewrite operation.
    /// </summary>
    void CancelRewrite();
}
