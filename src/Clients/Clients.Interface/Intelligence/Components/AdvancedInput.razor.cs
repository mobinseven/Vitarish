using Vitarish.Clients.Interface.Files.Components;
using Vitarish.Clients.Modules.Files.Models;

namespace Vitarish.Clients.Interface.Intelligence.Components;

public partial class AdvancedInput : CommonBase
{
    private AiStreamTranscriber? _transcriber;
    private AiStreamRewriter? _rewriter;
    private StreamTransfer? _streamTransferComponent;

    private const int MinStringLengthToRewrite = 10;
    private string _value = string.Empty;
    private bool _transcribing;
    private bool _recording;
    private bool _rewriting;
    private bool _transferring;
    private bool fileAttachmentMode => _streamTransferComponent?.PendingFiles.Count > 0;
    private bool _busy => _rewriting || _recording || _transcribing || _transferring;

    [Parameter] public RenderFragment? StartContent { get; set; }
    [Parameter] public RenderFragment? EndContent { get; set; }
    [Parameter] public string? UserId { get; set; }
    [Parameter] public EventCallback<FileInformationDto> FileUploaded { get; set; }
    [Parameter] public EventCallback<string> OnSubmit { get; set; }
    [Parameter] public bool FileUploadEnabled { get; set; } = true;
    [Parameter] public bool DisplaySubmit { get; set; } = true;

    [Parameter] public string? Value { get; set; }
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }

    [Parameter] public bool Busy { get; set; }
    [Parameter] public EventCallback<bool> BusyChanged { get; set; }

    public async Task SubmitAsync()
    {
        if (fileAttachmentMode)
        {
            await (_streamTransferComponent?.StartTransferAsync() ?? throw new InvalidOperationException("streamTransferComponent was null in fileAttachmentMode"));
        }

        if (OnSubmit.HasDelegate)
        {
            await OnSubmit.InvokeAsync(_value);
        }
    }

    protected override void OnParametersSet()
    {
        if (Value is null)
        {
            return;
        }

        string incoming = Value;
        if (!string.Equals(_value, incoming, StringComparison.Ordinal))
        {
            _value = incoming;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_streamTransferComponent is not null)
        {
            bool transferState = _streamTransferComponent.Transferring;
            if (_transferring != transferState)
            {
                _transferring = transferState;
                await UpdateStatusAsync();
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task UpdateValueAsync(string? next, bool notifyParent = true)
    {
        string incoming = next ?? string.Empty;
        if (!string.Equals(_value, incoming, StringComparison.Ordinal))
        {
            _value = incoming;
            if (notifyParent && ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(_value);
            }
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task UpdateBusyAsync()
    {
        bool busy = _recording || _transcribing || _rewriting || _transferring;
        if (Busy != busy)
        {
            Busy = busy;
            if (BusyChanged.HasDelegate)
            {
                await BusyChanged.InvokeAsync(busy);
            }
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task OnRecordingChanged(bool recording)
    {
        _recording = recording;
        await UpdateStatusAsync();
    }

    private async Task OnTranscribingChanged(bool transcribing)
    {
        _transcribing = transcribing;
        await UpdateStatusAsync();
    }

    private async Task OnTransferringChangedAsync(bool transferring)
    {
        _transferring = transferring;
        await UpdateStatusAsync();
    }

    private async Task UpdateStatusAsync() => await UpdateBusyAsync();

    private async Task ToggleRecordingAsync()
    {
        if (_transcriber is null)
        {
            return;
        }

        await _transcriber.ToggleRecordingAsync();
        await UpdateStatusAsync();
    }

    private async Task RewriteAsync()
    {
        if (_rewriter is null)
        {
            return;
        }

        if (_rewriting)
        {
            _rewriter.CancelRewrite();
            _rewriting = false;
            await UpdateBusyAsync();
            return;
        }

        _rewriting = true;
        await UpdateBusyAsync();

        string prompt = _value;
        await _rewriter.StartRewriteAsync(prompt);
    }

    private async Task OnRewriteStarted()
    {
        _rewriting = true;
        await UpdateBusyAsync();
    }

    private async Task OnRewriteCompleted()
    {
        _rewriting = false;
        await UpdateBusyAsync();
    }

    private async Task OnRewriteError(Exception exception)
    {
        _rewriting = false;
        await UpdateBusyAsync();
    }

    private async Task OnChunk(string chunk) => await UpdateValueAsync(_value + chunk, notifyParent: true);

    private async Task OnTranscriptionEnded() => await UpdateStatusAsync();

    private async Task OnRewriteValueChanged(string value) => await UpdateValueAsync(value, notifyParent: true);

    private async Task HandleFileUploaded(FileInformationDto file)
    {
        if (FileUploaded.HasDelegate)
        {
            await FileUploaded.InvokeAsync(file);
        }
    }

    private void RemovePendingFile(StreamTransfer transfer, PendingFileDto item) => transfer.RemovePendingFileAsync(item);
}
