using Microsoft.AspNetCore.Components.Rendering;

namespace Vitarish.Intelligence.Interface.Components;

public partial class AiResponderInput : AdvancedInput
{
    private const string LegacySubmitParameterName = "OnSubmit";

    private AiStreamResponder? _responder;
    private readonly List<ResponderInput> _pendingInputs = [];
    private EventCallback<string?> _externalValueChanged = EventCallback<string?>.Empty;
    private EventCallback<IReadOnlyList<ResponderInput>> _responderSubmit
        = EventCallback<IReadOnlyList<ResponderInput>>.Empty;
    private string _currentValue = string.Empty;
    private bool _inputBusy;
    private bool _responderBusy;
    private bool _lastBusyNotification;

    [Parameter]
    public ResponderOptions Options { get; set; } = new();

    [Parameter]
    public EventCallback<ResponderOutput> OnOutput { get; set; }
        = EventCallback<ResponderOutput>.Empty;

    [Parameter]
    public EventCallback<IEnumerable<ResponderOutput>> OnOutputEnd { get; set; }
        = EventCallback<IEnumerable<ResponderOutput>>.Empty;

    [Parameter]
    public EventCallback<IReadOnlyList<ResponderInput>> Submit { get; set; }
        = EventCallback<IReadOnlyList<ResponderInput>>.Empty;

    [Obsolete("Use the Submit parameter instead.")]
    public new EventCallback<IReadOnlyList<ResponderInput>> OnSubmit
    {
        get => Submit;
        set => Submit = value;
    }

    [Parameter]
    public new EventCallback<string?> OnError { get; set; }
        = EventCallback<string?>.Empty;

    [Parameter]
    public EventCallback<bool> OnBusyChanged { get; set; }
        = EventCallback<bool>.Empty;

    [Parameter]
    public RenderFragment? LeadingContent { get; set; }

    [Parameter]
    public RenderFragment? TrailingContent { get; set; }

    [Parameter]
    public string? InitializationPrompt { get; set; }

    private bool IsBusy => _inputBusy || _responderBusy;

    private bool CanSubmit => !_inputBusy
        && !_responderBusy
        && (_pendingInputs.Count > 0 || !string.IsNullOrWhiteSpace(_currentValue));

    protected override void OnParametersSet()
    {
        Options ??= new ResponderOptions();

        string incoming = Value ?? string.Empty;
        if (!string.Equals(_currentValue, incoming, StringComparison.Ordinal))
        {
            _currentValue = incoming;
        }

        _externalValueChanged = base.ValueChanged;
        base.ValueChanged = EventCallback.Factory.Create<string?>(this, OnValueChangedAsync);

        base.BusyChanged = EventCallback.Factory.Create<bool>(this, HandleInputBusyChangedAsync);
        base.FileUploaded = EventCallback.Factory.Create<FileInformationReadDto>(this, HandleFileUploadedAsync);
        base.OnSubmit = EventCallback.Factory.Create<string>(this, SubmitCurrentAsync);

        _responderSubmit = Submit;

        base.StartContent = LeadingContent;
        base.EndContent = TrailingContent;

        base.OnParametersSet();
    }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        Dictionary<string, object?> forwarded = new(StringComparer.OrdinalIgnoreCase);
        EventCallback<IReadOnlyList<ResponderInput>> submitCallback = Submit;

        foreach (ParameterValue parameter in parameters)
        {
            if (parameter.Cascading)
            {
                continue;
            }
            if (string.Equals(parameter.Name, LegacySubmitParameterName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(parameter.Name, nameof(Submit), StringComparison.OrdinalIgnoreCase))
            {
                submitCallback = (EventCallback<IReadOnlyList<ResponderInput>>)parameter.Value!;
                continue;
            }
            forwarded[parameter.Name] = parameter.Value;
        }

        Submit = submitCallback;
        _responderSubmit = submitCallback;

        await base.SetParametersAsync(ParameterView.FromDictionary(forwarded));
    }

    public void SetInitializationPrompt(string? prompt) => InitializationPrompt = prompt;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<AiStreamResponder>(0);
        builder.AddAttribute(1, nameof(Options), Options);
        builder.AddAttribute(2, nameof(OnOutput), EventCallback.Factory.Create<ResponderOutput>(this, HandleOutputAsync));
        builder.AddAttribute(3, nameof(OnOutputEnd), EventCallback.Factory.Create<IEnumerable<ResponderOutput>>(this, HandleOutputEndAsync));
        builder.AddComponentReferenceCapture(4, inst => _responder = (AiStreamResponder)inst);
        builder.CloseComponent();

        base.BuildRenderTree(builder);
    }

    private async Task SubmitCurrentAsync(string value)
    {
        await OnValueChangedAsync(value);
        await SubmitResponderPayloadAsync();
    }

    private async Task SubmitResponderPayloadAsync()
    {
        bool hasText = !string.IsNullOrWhiteSpace(_currentValue);
        bool hasAttachments = _pendingInputs.Count > 0;

        if (!hasText && !hasAttachments)
        {
            return;
        }

        List<ResponderInput> payloadInputs = [];

        if (hasText)
        {
            payloadInputs.Add(ResponderInput.TextInput(_currentValue));
        }

        if (hasAttachments)
        {
            payloadInputs.AddRange(_pendingInputs);
        }

        bool submitted = await SubmitInternalAsync(payloadInputs);

        if (submitted && hasAttachments)
        {
            _pendingInputs.Clear();
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task OnValueChangedAsync(string? value)
    {
        string incoming = value ?? string.Empty;
        if (!string.Equals(_currentValue, incoming, StringComparison.Ordinal))
        {
            _currentValue = incoming;
        }

        if (_externalValueChanged.HasDelegate)
        {
            await _externalValueChanged.InvokeAsync(value);
        }
    }

    private async Task HandleInputBusyChangedAsync(bool busy)
    {
        if (_inputBusy != busy)
        {
            _inputBusy = busy;
            await UpdateBusyAsync();
        }
    }

    private Task HandleOutputAsync(ResponderOutput output) => OnOutput.InvokeAsync(output);

    private Task HandleOutputEndAsync(IEnumerable<ResponderOutput> outputs) => OnOutputEnd.InvokeAsync(outputs);

    private async Task HandleFileUploadedAsync(FileInformationReadDto file)
    {
        if (string.IsNullOrWhiteSpace(file.Url))
        {
            return;
        }

        ResponderInput attachment;

        string contentType = file.ContentType ?? string.Empty;

        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            attachment = ResponderInput.ImageInput(
                file.Url!,
                ResponderInputImageDetail.Auto);

            _ = _pendingInputs.RemoveAll(input => input.Type == ResponderInputType.Image
                && string.Equals(input.ImageUrl, file.Url, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            string fileId = file.Id.ToString();
            string fileData = file.Url!;
            string filename = ResolveFileName(file);

            attachment = ResponderInput.FileInput(fileId, fileData, filename);

            _ = _pendingInputs.RemoveAll(input => input.Type == ResponderInputType.File
                && string.Equals(input.FileId, fileId, StringComparison.OrdinalIgnoreCase));
        }

        _pendingInputs.Add(attachment);

        await InvokeAsync(StateHasChanged);
    }

    protected async Task<bool> SubmitInternalAsync(IEnumerable<ResponderInput> inputs)
    {
        if (_responder is null)
        {
            return false;
        }

        List<ResponderInput> payload = [];

        if (!string.IsNullOrWhiteSpace(InitializationPrompt))
        {
            payload.Add(ResponderInput.TextInput(InitializationPrompt));
        }

        payload.AddRange(inputs);

        if (payload.Count == 0)
        {
            return false;
        }

        if (_responder.Responding)
        {
            return false;
        }

        _responderBusy = true;
        await UpdateBusyAsync();

        bool submitted = false;

        try
        {
            if (OnError.HasDelegate)
            {
                await OnError.InvokeAsync(null);
            }

            if (_responderSubmit.HasDelegate)
            {
                await _responderSubmit.InvokeAsync(payload);
            }

            await _responder.Submit(payload);
            submitted = true;
        }
        catch (Exception ex)
        {
            if (OnError.HasDelegate)
            {
                await OnError.InvokeAsync(ex.Message);
            }
        }
        finally
        {
            _responderBusy = false;
            await UpdateBusyAsync();
        }

        return submitted;
    }

    private static string ResolveFileName(FileInformationReadDto file)
    {
        if (!string.IsNullOrWhiteSpace(file.Tag))
        {
            return file.Tag!;
        }

        if (!string.IsNullOrWhiteSpace(file.Url))
        {
            string url = file.Url!;

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri? absolute))
            {
                string path = absolute.LocalPath;
                if (!string.IsNullOrWhiteSpace(path))
                {
                    string candidate = Path.GetFileName(path);
                    if (!string.IsNullOrWhiteSpace(candidate))
                    {
                        return candidate;
                    }
                }
            }

            string fallback = Path.GetFileName(url);
            if (!string.IsNullOrWhiteSpace(fallback))
            {
                return fallback;
            }
        }

        return file.Id.ToString();
    }

    private async Task UpdateBusyAsync()
    {
        bool busy = IsBusy;
        if (_lastBusyNotification != busy)
        {
            _lastBusyNotification = busy;
            if (OnBusyChanged.HasDelegate)
            {
                await OnBusyChanged.InvokeAsync(busy);
            }
        }

        await InvokeAsync(StateHasChanged);
    }
}
