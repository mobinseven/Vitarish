using Vitarish.Clients.Modules.Intelligence.Models;

namespace Vitarish.Clients.Interface.Components;

public partial class AiResponderChatInput : AiResponderInput
{
    private ResponderOptions _effectiveOptions = new();
    private ResponderOptions? _incomingOptions;
    private ResponderOptions? _lastProvidedOptions;
    private EventCallback<ResponderOutput> _onOutput = EventCallback<ResponderOutput>.Empty;
    private EventCallback<IEnumerable<ResponderOutput>> _onOutputEnd = EventCallback<IEnumerable<ResponderOutput>>.Empty;

    public string? PreviousResponseId { get; private set; }

    public Task ResetConversationAsync() => SetPreviousResponseIdAsync(_lastProvidedOptions?.PreviousResponseId);

    public async Task SetPreviousResponseIdAsync(string? previousResponseId)
    {
        PreviousResponseId = previousResponseId;
        ApplyEffectiveOptions();
        await InvokeAsync(StateHasChanged);
    }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (parameters.TryGetValue(nameof(Options), out ResponderOptions? options))
        {
            _incomingOptions = options;
        }

        if (parameters.TryGetValue(nameof(OnOutput), out EventCallback<ResponderOutput> onOutput))
        {
            _onOutput = onOutput;
        }

        if (parameters.TryGetValue(nameof(OnOutputEnd), out EventCallback<IEnumerable<ResponderOutput>> onOutputEnd))
        {
            _onOutputEnd = onOutputEnd;
        }

        await base.SetParametersAsync(parameters);

        OnOutput = EventCallback.Factory.Create<ResponderOutput>(this, HandleOutputAsync);
        OnOutputEnd = EventCallback.Factory.Create<IEnumerable<ResponderOutput>>(this, HandleOutputEndAsync);
    }

    protected override void OnParametersSet()
    {
        ResponderOptions provided = _incomingOptions
            ?? _lastProvidedOptions
            ?? Options
            ?? new ResponderOptions();

        if (!ReferenceEquals(_lastProvidedOptions, provided))
        {
            _lastProvidedOptions = provided;
            PreviousResponseId = provided.PreviousResponseId;
        }

        ApplyEffectiveOptions();
        _incomingOptions = null;

        base.OnParametersSet();
    }

    protected virtual async Task HandleOutputAsync(ResponderOutput output)
    {
        UpdatePreviousResponseId(output);

        if (_onOutput.HasDelegate)
        {
            await _onOutput.InvokeAsync(output);
        }
    }

    public Task<bool> SubmitPayloadAsync(IEnumerable<ResponderInput> inputs)
        => SubmitInternalAsync(inputs);

    protected virtual async Task HandleOutputEndAsync(IEnumerable<ResponderOutput> outputs)
    {
        List<ResponderOutput> materialized = outputs as List<ResponderOutput>
            ?? [.. outputs];

        foreach (ResponderOutput output in materialized)
        {
            UpdatePreviousResponseId(output);
        }

        if (_onOutputEnd.HasDelegate)
        {
            await _onOutputEnd.InvokeAsync(materialized);
        }
    }

    private void UpdatePreviousResponseId(ResponderOutput output)
    {
        if (string.IsNullOrWhiteSpace(output.Id))
        {
            return;
        }

        PreviousResponseId = output.Id;
        ApplyEffectiveOptions();

        _ = InvokeAsync(StateHasChanged);
    }

    private void ApplyEffectiveOptions()
    {
        ResponderOptions source = _lastProvidedOptions
            ?? _effectiveOptions
            ?? new ResponderOptions();

        _effectiveOptions = BuildEffectiveOptions(source);
        Options = _effectiveOptions;
    }

    protected virtual ResponderOptions BuildEffectiveOptions(ResponderOptions options) => options with { PreviousResponseId = PreviousResponseId };
}
