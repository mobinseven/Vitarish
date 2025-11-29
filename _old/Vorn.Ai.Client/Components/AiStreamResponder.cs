namespace Vorn.Ai.Client.Components;

using Microsoft.AspNetCore.Components;
using Vorn.Ai.Client.Abstractions;
using Vorn.Ai.Client.Models;

public partial class AiStreamResponder : ComponentBase, IDisposable
{
    [Inject] private IStreamResponder Responder { get; set; } = default!;

    [Parameter] public ResponderOptions Options { get; set; } = new();

    [Parameter] public EventCallback<ResponderOutput> OnOutput { get; set; }
    [Parameter] public EventCallback<IEnumerable<ResponderOutput>> OnOutputEnd { get; set; }

    public bool Responding => Responder.Responding;
    public IReadOnlyList<ResponderOutput> Outputs => Responder.Outputs;

    private CancellationTokenSource? _cts;

    protected override void OnInitialized()
    {
        Responder.OutputReceived += OnServiceOutput;
        Responder.OutputCompleted += OnServiceCompleted;
    }

    public async Task Submit(IEnumerable<ResponderInput> inputs)
    {
        Cancel();
        _cts = new CancellationTokenSource();
        await Responder.Submit(inputs, Options, _cts.Token);
    }

    public void Cancel()
    {
        if (_cts is not null)
        {
            try
            { _cts.Cancel(); }
            catch { /* no-op */ }
            _cts.Dispose();
            _cts = null;
        }
    }

    private async void OnServiceOutput(object? sender, ResponderOutput output) =>
        await InvokeAsync(async () =>
        {
            await OnOutput.InvokeAsync(output);
            StateHasChanged();
        });

    private async void OnServiceCompleted(object? sender, IReadOnlyList<ResponderOutput> all) => await InvokeAsync(async () =>
                                                                                                      {
                                                                                                          await OnOutputEnd.InvokeAsync(all);
                                                                                                          StateHasChanged();
                                                                                                      });

    public void Dispose()
    {
        Responder.OutputReceived -= OnServiceOutput;
        Responder.OutputCompleted -= OnServiceCompleted;
        Cancel();
        GC.SuppressFinalize(this);
    }

}
