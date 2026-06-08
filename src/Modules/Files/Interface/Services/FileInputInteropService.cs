using Microsoft.JSInterop;

namespace Vitarish.Files.Interface.Services;

public sealed class FileInputInteropService(IJSRuntime jsRuntime) : IFileInputInterop
{
    private const string ModulePath = "./_content/Vitarish.Files.Interface/Components/StreamTransfer.razor.js";
    private IJSObjectReference? _module;
    private readonly SemaphoreSlim _moduleGate = new(1, 1);
    private bool _disposed;

    public async Task EnsureInitializedAsync(string inputId, object selfRef, FileInputOptions options, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_module is null)
        {
            await _moduleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
            }
            finally
            {
                _ = _moduleGate.Release();
            }
        }

        await _module.InvokeVoidAsync("ensureFileInput", new
        {
            inputId,
            dotNetRef = selfRef,
            accept = options.Accept,
            multiple = options.Multiple,
            capture = options.Capture,
            hidden = options.Hidden
        });
    }

    public Task RemoveFileAsync(string inputId, string fileName, CancellationToken cancellationToken = default)
        => _module is null
            ? Task.CompletedTask
            : _module.InvokeVoidAsync("removeFile", cancellationToken, inputId, fileName).AsTask();


    public Task DisposeInputAsync(string inputId)
        => _module is null
            ? Task.CompletedTask
            : _module.InvokeVoidAsync("disposeFileInput", inputId).AsTask();

    public async Task<IJSStreamReference?> GetFileStreamAsync(string inputId, string fileName, CancellationToken cancellationToken = default) => _module is null ? null : await _module.InvokeAsync<IJSStreamReference?>("getFileStream", cancellationToken, inputId, fileName);

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _moduleGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_module is not null)
            {
                await _module.DisposeAsync();
                _module = null;
            }
        }
        finally
        {
            _ = _moduleGate.Release();
        }
    }
}
