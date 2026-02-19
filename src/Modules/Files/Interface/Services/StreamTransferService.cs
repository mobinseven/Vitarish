using Microsoft.JSInterop;
using System.Threading.Channels;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Interface.Components;

namespace Vitarish.Files.Interface.Services;

public sealed class StreamTransferService(IFilesService _filesClient, IJSRuntime _jsRuntime) : IStreamTransfer, IAsyncDisposable
{
    private const string ModulePath = "./_content/Vitarish.Files.Interface/Components/StreamTransfer.razor.js";

    private const string Accept = "image/*,video/*";
    private readonly List<FileInformationReadDto> _uploadedFiles = [];
    private readonly string _id = $"{nameof(StreamTransfer)}-{Guid.NewGuid()}";

    private DotNetObjectReference<StreamTransferService>? _selfRef;
    private IJSObjectReference? _module;
    private List<PendingFileDto> _pendingFiles = [];
    private RenderFragment? _activatorContent;
    private readonly SemaphoreSlim _initializationGate = new(1, 1);
    private bool _initialized;

    public Func<Task>? OnStateChangedAsync { get; set; }
    public Func<FileInformationReadDto, Task>? OnFileUploadedAsync { get; set; }

    public string? UserId { get; set; }

    public bool Transferring { get; private set; }

    public RenderFragment ActContent { get => field ??= BuildInputHost(); private set; }

    public IReadOnlyList<PendingFileDto> PendingFiles => _pendingFiles;

    public IReadOnlyList<FileInformationReadDto> UploadedFiles => _uploadedFiles;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        await _initializationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return;
            }

            await EnsureModuleAsync();
            await EnsureInputElementAsync();
            _initialized = true;
        }
        finally
        {
            _ = _initializationGate.Release();
        }
    }
    public void ClearPendings()
    {
        _pendingFiles.Clear();
        TriggerStateChanged();
    }

    public async Task RemovePendingFileAsync(PendingFileDto file, CancellationToken cancellationToken = default)
    {
        if (!_pendingFiles.Remove(file))
        {
            return;
        }

        if (_module is not null)
        {
            await _module.InvokeVoidAsync("removeFile", cancellationToken, _id, file.Name);
        }

        await TriggerStateChangedAsync();
    }

    public async Task StartTransferAsync(CancellationToken cancellationToken = default)
    {
        if (Transferring || _pendingFiles.Count == 0 || _module is null)
        {
            return;
        }

        Transferring = true;
        await TriggerStateChangedAsync();

        try
        {
            foreach (PendingFileDto file in _pendingFiles.ToArray())
            {
                await using IJSStreamReference? streamRef = await _module.InvokeAsync<IJSStreamReference>("getFileStream", _id, file.Name);

                if (streamRef is null)
                {
                    throw new Exception("StartTransferAsync getFileStream");
                }

                await using Stream stream = await streamRef.OpenReadStreamAsync(file.Size, cancellationToken);

                Channel<byte[]> channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(1)
                {
                    FullMode = BoundedChannelFullMode.Wait
                });

                Task pump = Task.Run(async () =>
                {
                    try
                    {
                        const int bufferLength = 20 * 1024;
                        byte[] buffer = new byte[bufferLength];

                        while (true)
                        {
                            int read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                            if (read == 0)
                            {
                                break;
                            }

                            byte[] chunk = new byte[read];
                            Array.Copy(buffer, 0, chunk, 0, read);
                            await channel.Writer.WriteAsync(chunk, cancellationToken);

                            file.Progress += 100d * Convert.ToDouble(read) / file.Size;
                            await TriggerStateChangedAsync();
                        }

                        channel.Writer.Complete();
                    }
                    catch (Exception ex)
                    {
                        _ = channel.Writer.TryComplete(ex);
                        throw;
                    }
                }, cancellationToken);

                file.UserId = UserId;
                FileInformationReadDto info = new()
                {
                    Checksum = file.Checksum,
                    Thumbnail = file.Thumbnail,
                    AllowedOrigins = "*",
                    ContentType = file.Type,
                    Metadata = new FileMetadata
                    {
                        Size = file.Size
                    }
                };

                FileInformationReadDto? fileInfo = await _filesClient.SaveFile(channel.Reader, info, cancellationToken).ConfigureAwait(false) ?? throw new InvalidDataException("The returned result must not be null");
                await pump.ConfigureAwait(false);

                _ = _pendingFiles.Remove(file);

                _uploadedFiles.Add(fileInfo);

                if (OnFileUploadedAsync is not null)
                {
                    await OnFileUploadedAsync.Invoke(fileInfo);
                }

                await TriggerStateChangedAsync();
            }
        }
        finally
        {
            Transferring = false;
            await TriggerStateChangedAsync();
        }
    }

    public async Task OnAfterRenderAsync(bool firstRender)
    {
        await EnsureModuleAsync();

        if (_module is not null)
        {
            await EnsureInputElementAsync();
        }
    }

    public void SetActivatorContent(RenderFragment? activatorContent)
    {
        _activatorContent = activatorContent;
        ActContent = BuildInputHost();
        TriggerStateChanged();
    }

    [JSInvokable]
    public async Task OnPendingFilesChanged(List<PendingFileDto> files)
    {
        _pendingFiles = files;
        await TriggerStateChangedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null && _selfRef is not null)
            {
                await _module.InvokeVoidAsync("disposeFileInput", _id);
                await _module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
            // Expected when the JS runtime is no longer available.
        }
        finally
        {
            _selfRef?.Dispose();
            _selfRef = null;
            _module = null;
            GC.SuppressFinalize(this);
        }
    }

    private RenderFragment BuildInputHost() => builder =>
    {
        if (_activatorContent is not null)
        {
            builder.OpenElement(2, "label");
            builder.AddAttribute(3, "for", _id);
            builder.AddContent(4, _activatorContent);
            builder.CloseElement();
        }
    };
    private async Task EnsureModuleAsync()
    {
        _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
        _selfRef ??= DotNetObjectReference.Create(this);
    }

    private ValueTask EnsureInputElementAsync() => _module is null || _selfRef is null
            ? ValueTask.CompletedTask
            : _module.InvokeVoidAsync("ensureFileInput", new
            {
                inputId = _id,
                dotNetRef = _selfRef,
                accept = Accept,
                hidden = _activatorContent is not null,
                multiple = true
            });

    private void TriggerStateChanged() => _ = TriggerStateChangedAsync();

    private Task TriggerStateChangedAsync() => OnStateChangedAsync?.Invoke() ?? Task.CompletedTask;
}
