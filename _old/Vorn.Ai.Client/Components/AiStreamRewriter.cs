
using Microsoft.AspNetCore.Components;
using Vorn.Ai.Client.Abstractions;

namespace Vorn.Ai.Client.Components;

public partial class AiStreamRewriter : ComponentBase, IDisposable
{
    [Inject] private IStreamRewriter StreamRewriter { get; set; } = default!;

    [Parameter] public EventCallback OnRewriteStarted { get; set; }
    [Parameter] public EventCallback OnRewriteCompleted { get; set; }
    [Parameter] public EventCallback<Exception> OnRewriteError { get; set; }
    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    [Parameter]
    public string Value { get; set; } = string.Empty;

    private string CurrentValue => Value ?? string.Empty;

    public bool Rewriting => StreamRewriter.Rewriting;

    protected override void OnInitialized()
    {
        StreamRewriter.RewriteStarted += OnServiceRewriteStarted;
        StreamRewriter.ValueChanged += OnServiceValueChanged;
        StreamRewriter.RewriteCompleted += OnServiceRewriteCompleted;
        StreamRewriter.RewriteErrored += OnServiceRewriteErrored;
    }

    protected override void OnParametersSet()
    {
        Value ??= string.Empty;
    }

    public async Task StartRewriteAsync(string? prompt = null)
    {
        if(Rewriting)
        {
            CancelRewrite();
        }

        string effectivePrompt = prompt ?? CurrentValue;
        await StreamRewriter.StartRewriteAsync(effectivePrompt);
    }

    public void CancelRewrite()
    {
        StreamRewriter.CancelRewrite();
    }

    private async void OnServiceRewriteStarted(object? sender, EventArgs args)
    {
        await InvokeAsync(OnRewriteStarted.InvokeAsync);
    }

    private async void OnServiceValueChanged(object? sender, string newValue)
    {
        await InvokeAsync(async () =>
        {
            if(!string.Equals(CurrentValue, newValue, StringComparison.Ordinal))
            {
                Value = newValue;
                await ValueChanged.InvokeAsync(newValue);
            }
        });
    }

    private async void OnServiceRewriteCompleted(object? sender, EventArgs args)
    {
        await InvokeAsync(OnRewriteCompleted.InvokeAsync);
    }

    private async void OnServiceRewriteErrored(object? sender, Exception exception)
    {
        await InvokeAsync(async () =>
        {
            await OnRewriteError.InvokeAsync(exception);
        });
    }

    public void Dispose()
    {
        StreamRewriter.RewriteStarted -= OnServiceRewriteStarted;
        StreamRewriter.ValueChanged -= OnServiceValueChanged;
        StreamRewriter.RewriteCompleted -= OnServiceRewriteCompleted;
        StreamRewriter.RewriteErrored -= OnServiceRewriteErrored;
        StreamRewriter.CancelRewrite();
        GC.SuppressFinalize(this);
    }

    private void SetValueFromUi(string? nextValue)
    {
        string sanitized = nextValue ?? string.Empty;

        if(string.Equals(CurrentValue, sanitized, StringComparison.Ordinal))
        {
            return;
        }

        Value = sanitized;
        _ = ValueChanged.InvokeAsync(sanitized);
    }
}
