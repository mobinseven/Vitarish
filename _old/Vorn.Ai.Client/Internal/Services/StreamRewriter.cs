namespace Vorn.Ai.Client.Internal.Services;

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vorn.Ai.Client.Abstractions;
using Vorn.Ai.Client.Models;

public sealed class StreamRewriter(IResponseHubClient responseHubClient) : IStreamRewriter
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _cts;
    private string _value = string.Empty;

    public bool Rewriting { get; private set; }

    public string Value => _value;

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
            _value = string.Empty;
            RewriteStarted?.Invoke(this, EventArgs.Empty);

            StringBuilder builder = new(Math.Max(16, prompt.Length));

            await foreach (ResponderOutput? response in responseHubClient.StreamRewriteAsync(prompt, token).WithCancellation(token).ConfigureAwait(false))
            {
                if (response is null)
                {
                    continue;
                }

                if (response.Type != ResponderOutputType.Text)
                {
                    continue;
                }

                string chunk = response.Text ?? string.Empty;

                if (chunk.Length == 0)
                {
                    continue;
                }

                builder.Append(chunk);
                string nextValue = builder.ToString();

                if (!string.Equals(_value, nextValue, StringComparison.Ordinal))
                {
                    _value = nextValue;
                    ValueChanged?.Invoke(this, _value);
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
                _gate.Release();
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
