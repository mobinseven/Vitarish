
namespace Vitarish.Files.Interface.Components;

public partial class StreamTransfer : ComponentBase
{
    [Inject]
    public IStreamTransfer Service { get; set; } = default!;

    [Parameter]
    public EventCallback<FileInformationReadDto> OnFileUploaded { get; set; }

    [Parameter]
    public EventCallback<FileInformationReadDto> OnStateChange { get; set; }

    [Parameter]
    public RenderFragment? ActivatorContent { get; set; } = default!;

    [Parameter]
    public string? UserId { get; set; }

    [Parameter]
    private void ApplyParameters()
    {
        Service.UserId = UserId;
        Service.EnableImageCapture = EnableImageCapture;
        Service.CaptureDevice = CaptureDevice;
        // Enforce mutual exclusivity: if capture is enabled, disable multiple selection
        Service.AllowMultipleSelection = EnableImageCapture ? false : AllowMultipleSelection;
        Service.SetActivatorContent(ActivatorContent);
    }

    [Parameter]
    public CaptureDeviceMode CaptureDevice { get; set; } = CaptureDeviceMode.Environment;

    [Parameter]
    public bool AllowMultipleSelection { get; set; } = true;

    public RenderFragment ActContent => Service.ActContent;

    public IReadOnlyList<PendingFileDto> PendingFiles => Service.PendingFiles;

    public IReadOnlyList<FileInformationReadDto> UploadedFiles => Service.UploadedFiles;

    public bool Transferring => Service.Transferring;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Service.OnStateChangedAsync = () => InvokeAsync(async () =>
        {
            StateHasChanged();
            await OnStateChange.InvokeAsync();
        });
        Service.OnFileUploadedAsync = file => InvokeAsync(async () =>
        {
            StateHasChanged();
            await OnFileUploaded.InvokeAsync(file);
            await OnStateChange.InvokeAsync();
        });
        ApplyParameters();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await Service.InitializeAsync();
        }
    }

    public void ClearPendings()
        => Service.ClearPendings();

    public Task RemovePendingFileAsync(PendingFileDto file, CancellationToken cancellationToken = default)
        => Service.RemovePendingFileAsync(file, cancellationToken);

    public Task StartTransferAsync(CancellationToken cancellationToken = default)
        => Service.StartTransferAsync(cancellationToken);

    private void ApplyParameters()
    {
        Service.UserId = UserId;
        Service.EnableImageCapture = EnableImageCapture;
        Service.CaptureDevice = CaptureDevice;
        Service.AllowMultipleSelection = AllowMultipleSelection;
        Service.SetActivatorContent(ActivatorContent);
    }
}
