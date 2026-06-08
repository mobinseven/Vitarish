
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
    public bool RenderActContent { get; set; }

    [Parameter]
    public FileSelectionMode SelectionMode { get; set; } = FileSelectionMode.GeneralMulti;

    public RenderFragment ActContent { get; private set; } = static _ => { };

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
            await Service.InitializeAsync(ActivatorContent is not null);
        }
    }

    protected override void OnParametersSet()
        => ApplyParameters();

    public void ClearPendings()
        => Service.ClearPendings();

    public Task RemovePendingFileAsync(PendingFileDto file, CancellationToken cancellationToken = default)
        => Service.RemovePendingFileAsync(file, cancellationToken);

    public Task StartTransferAsync(CancellationToken cancellationToken = default)
        => Service.StartTransferAsync(cancellationToken);

    private void ApplyParameters()
    {
        Service.SelectionMode = SelectionMode;
        ActContent = BuildInputHost();
    }

    private RenderFragment BuildInputHost() => builder =>
    {
        if (ActivatorContent is null)
        {
            return;
        }

        builder.OpenElement(0, "label");
        builder.AddAttribute(1, "for", Service.InputId);
        builder.AddContent(2, ActivatorContent);
        builder.CloseElement();
    };
}
