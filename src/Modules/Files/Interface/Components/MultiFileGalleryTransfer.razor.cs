namespace Vitarish.Files.Interface.Components;

public partial class MultiFileGalleryTransfer
{
    private StreamTransfer? BaseTransfer { get; set; }

    [Parameter] public EventCallback<FileInformationReadDto> OnFileUploaded { get; set; }
    [Parameter] public EventCallback<FileInformationReadDto> OnStateChange { get; set; }
    [Parameter] public RenderFragment? ActivatorContent { get; set; }
    [Parameter] public string? UserId { get; set; }

    public Task StartTransferAsync(CancellationToken cancellationToken = default)
        => BaseTransfer?.StartTransferAsync(cancellationToken) ?? Task.CompletedTask;
}
