using Vitarish.Intelligence.Application.Abstractions;

namespace Vitarish.Intelligence.Interface.Services;

public sealed class StreamResponder(IResponder responseHubClient) : IStreamResponder
{
    private readonly List<ResponderOutput> _outputs = [];
    private readonly SemaphoreSlim _gate = new(1, 1);

    public bool Responding { get; private set; }
    public IReadOnlyList<ResponderOutput> Outputs => _outputs;

    public event EventHandler<ResponderOutput>? OutputReceived;
    public event EventHandler<IReadOnlyList<ResponderOutput>>? OutputCompleted;

    public async Task Submit(IEnumerable<ResponderInput> inputs, ResponderOptions options, CancellationToken ct = default)
    {
        if (!await _gate.WaitAsync(0, ct))
        {
            return;
        }

        try
        {
            Responding = true;
            _outputs.Clear();

            await foreach (ResponderOutput? item in responseHubClient.StreamAsync([.. inputs], options, ct).WithCancellation(ct))
            {
                ResponderOutput toRaise = item;

                if (item.Type == ResponderOutputType.Text)
                {
                    if (_outputs.Count > 0 && _outputs[^1].Type == ResponderOutputType.Text)
                    {
                        ResponderOutput last = _outputs[^1];
                        ResponderOutput merged = last with { Text = last.Text + item.Text };
                        _outputs[^1] = merged;
                        toRaise = merged;
                    }
                    else
                    {
                        _outputs.Add(item);
                    }
                }
                else
                {
                    _outputs.Add(item);
                }

                OutputReceived?.Invoke(this, toRaise);
            }
        }
        finally
        {
            Responding = false;
            OutputCompleted?.Invoke(this, _outputs);
            _ = _gate.Release();
        }
    }
}
