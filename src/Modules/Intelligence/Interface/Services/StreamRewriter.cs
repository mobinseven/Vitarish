using Vitarish.Intelligence.Client;
using Vitarish.Intelligence.Contracts;

namespace Vitarish.Intelligence.Interface.Services;

public sealed class StreamRewriter(IIntelligenceClient intelligenceClient) : IStreamRewriter
{
    private readonly SemaphoreSlim _gate = new(1, 1); private CancellationTokenSource? _cts;
    public bool Rewriting { get; private set; } public string Value { get; private set; } = string.Empty;
    public event EventHandler? RewriteStarted; public event EventHandler<string>? ValueChanged; public event EventHandler? RewriteCompleted; public event EventHandler<Exception>? RewriteErrored;
    public async Task StartRewriteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt); bool gateTaken = false;
        try
        {
            await _gate.WaitAsync(cancellationToken); gateTaken = true; _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken); CancellationToken token = _cts.Token;
            Rewriting = true; Value = string.Empty; RewriteStarted?.Invoke(this, EventArgs.Empty); StringBuilder builder = new(Math.Max(16, prompt.Length));
            await foreach (IntelligenceStreamChunk<RewriteProgress> chunk in intelligenceClient.StreamRewriteAsync(new RewriteRequest(prompt), token).WithCancellation(token))
            {
                if (chunk.Kind == StreamChunkKind.Failed || chunk.Error is not null) { RewriteErrored?.Invoke(this, new InvalidOperationException(chunk.Error?.Message ?? "Rewrite failed.")); break; }
                string text = chunk.Delta?.Text ?? string.Empty; if (text.Length == 0) continue;
                _ = builder.Append(text); string nextValue = builder.ToString(); if (!string.Equals(Value, nextValue, StringComparison.Ordinal)) { Value = nextValue; ValueChanged?.Invoke(this, Value); }
            }
            RewriteCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { RewriteErrored?.Invoke(this, ex); throw; }
        finally { Rewriting = false; if (gateTaken) { _cts?.Dispose(); _cts = null; _ = _gate.Release(); } }
    }
    public void CancelRewrite() { try { _cts?.Cancel(); } catch (ObjectDisposedException) { } }
    public ValueTask DisposeAsync() { CancelRewrite(); _cts?.Dispose(); _cts = null; return ValueTask.CompletedTask; }
}
