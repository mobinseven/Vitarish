using System.Text;
using Vitarish.Clients.Interface.Intelligence.Abstractions;
using Vitarish.Clients.Modules.Intelligence.Abstractions;

namespace Vitarish.Clients.Interface.Intelligence.Services;

public sealed class StreamRewriter(IResponderClient responseHubClient) : IStreamRewriter
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _cts;

    public bool Rewriting { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public event EventHandler? RewriteStarted;
    public event EventHandler<string>? ValueChanged;
    public event EventHandler? RewriteCompleted;
    public event EventHandler<Exception>? RewriteErrored;

    public async Task StartRewriteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        bool gateTaken = false;

        try
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            gateTaken = true;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            CancellationToken token = _cts.Token;

            Rewriting = true;
            Value = string.Empty;
            RewriteStarted?.Invoke(this, EventArgs.Empty);

            StringBuilder builder = new(Math.Max(16, prompt.Length));

            await foreach (ResponderOutput? response in responseHubClient.StreamRewriteAsync(prompt, token).WithCancellation(token).ConfigureAwait(false))
            {
                if (response is null || response.Type != ResponderOutputType.Text)
                {
                    continue;
                }

                string chunk = response.Text ?? string.Empty;

                if (chunk.Length == 0)
                {
                    continue;
                }

                _ = builder.Append(chunk);
                string nextValue = builder.ToString();

                if (!string.Equals(Value, nextValue, StringComparison.Ordinal))
                {
                    Value = nextValue;
                    ValueChanged?.Invoke(this, Value);
                }
            }

            RewriteCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            // cancellation: ignore
        }
        catch (Exception ex)
        {
            RewriteErrored?.Invoke(this, ex);
            throw;
        }
        finally
        {
            Rewriting = false;

            if (gateTaken)
            {
                _cts?.Dispose();
                _cts = null;
                _ = _gate.Release();
            }
        }
    }

    public void CancelRewrite()
    {
        try
        {
            _cts?.Cancel();
        }
        catch
        {
            // ignored
        }
    }

    public ValueTask DisposeAsync()
    {
        CancelRewrite();
        _cts?.Dispose();
        _cts = null;
        return ValueTask.CompletedTask;
    }
}
