using Microsoft.JSInterop;
using Vitarish.Files.Interface.Abstractions;
using Vitarish.Files.Interface.Components;
using Vitarish.Files.Interface.Models;

namespace Vitarish.Files.Interface.Services;

public sealed class StreamTransferService(
    IFileSelectionSession fileSelectionSession,
    IFileTransferCoordinator fileTransferCoordinator,
    IFileInputInterop fileInputInterop,
    IFileInputOptionsFactory fileInputOptionsFactory) : IStreamTransfer, IAsyncDisposable
{
    private readonly List<FileInformationReadDto> _uploadedFiles = [];
    private readonly string _id = $"{nameof(StreamTransfer)}-{Guid.NewGuid()}";

    private DotNetObjectReference<StreamTransferService>? _selfRef;
    private RenderFragment? _activatorContent;
    private readonly SemaphoreSlim _initializationGate = new(1, 1);
    private bool _initialized;

    public Func<Task>? OnStateChangedAsync { get; set; }
    public Func<FileInformationReadDto, Task>? OnFileUploadedAsync { get; set; }
    public string? UserId { get; set; }
    public FileSelectionMode SelectionMode { get; set; } = FileSelectionMode.GeneralMulti;
    public bool Transferring { get; private set; }
    public RenderFragment ActContent { get => field ??= BuildInputHost(); private set; }
    public IReadOnlyList<PendingFileDto> PendingFiles => fileSelectionSession.PendingFiles;
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

            _selfRef ??= DotNetObjectReference.Create(this);
            await EnsureInputElementAsync(cancellationToken);
            _initialized = true;
        }
        finally
        {
            _ = _initializationGate.Release();
        }
    }

    public void ClearPendings()
    {
        fileSelectionSession.Clear();
        TriggerStateChanged();
    }

    public async Task RemovePendingFileAsync(PendingFileDto file, CancellationToken cancellationToken = default)
    {
        bool removed = fileSelectionSession.Remove(file, SelectionMode);
        if (!removed)
        {
            return;
        }

        await fileInputInterop.RemoveFileAsync(_id, file.Name, cancellationToken);
        await TriggerStateChangedAsync();
    }

    public async Task StartTransferAsync(CancellationToken cancellationToken = default)
    {
        if (Transferring || PendingFiles.Count == 0)
        {
            return;
        }

        Transferring = true;
        await TriggerStateChangedAsync();

        try
        {
            await fileTransferCoordinator.StartTransferAsync(
                PendingFiles,
                _id,
                UserId,
                SelectionMode,
                TriggerStateChangedAsync,
                async fileInfo =>
                {
                    _uploadedFiles.Add(fileInfo);
                    if (OnFileUploadedAsync is not null)
                    {
                        await OnFileUploadedAsync.Invoke(fileInfo);
                    }

                    await TriggerStateChangedAsync();
                },
                (pendingFile, mode) => fileSelectionSession.Remove(pendingFile, mode),
                cancellationToken);
        }
        finally
        {
            Transferring = false;
            await TriggerStateChangedAsync();
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
        fileSelectionSession.ReplaceWith(files, SelectionMode);
        await TriggerStateChangedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _selfRef?.Dispose();
            _selfRef = null;
            await fileInputInterop.DisposeInputAsync(_id);
        }
        catch (JSDisconnectedException)
        {
            // Expected when the JS runtime is no longer available.
        }
        finally
        {
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

    private Task EnsureInputElementAsync(CancellationToken cancellationToken)
    {
        if (_selfRef is null)
        {
            return Task.CompletedTask;
        }

        FileInputOptions options = fileInputOptionsFactory.Create(SelectionMode, hidden: _activatorContent is not null);
        return fileInputInterop.EnsureInitializedAsync(_id, _selfRef, options, cancellationToken);
    }

    private void TriggerStateChanged() => _ = TriggerStateChangedAsync();

    private Task TriggerStateChangedAsync() => OnStateChangedAsync?.Invoke() ?? Task.CompletedTask;
}
