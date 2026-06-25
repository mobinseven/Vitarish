namespace Vitarish.Files.Interface.Components;

public partial class SingleFileCaptureTransfer
{
    private StreamTransfer? BaseTransfer { get; set; }

    [Parameter] public EventCallback<FileInformationDto> OnFileUploaded { get; set; }
    [Parameter] public EventCallback<FileInformationDto> OnStateChange { get; set; }
    [Parameter] public RenderFragment? ActivatorContent { get; set; }
    [Parameter] public string? UserId { get; set; }

    public Task StartTransferAsync(CancellationToken cancellationToken = default)
        => BaseTransfer?.StartTransferAsync(cancellationToken) ?? Task.CompletedTask;
}
