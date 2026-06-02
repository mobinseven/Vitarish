namespace Vitarish.Appointments.Interface.Components.Services;

public partial class ServiceForm : ServicesBase
{
    [Parameter] public ServiceReadDto? Service { get; set; }
    [Parameter] public EventCallback<Guid> OnSaved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    private ServiceWriteDto writeDto = new();
    private List<ServiceTypeReadDto> serviceTypes = [];

    protected override void OnParametersSet() => writeDto = Service is not null ? Service.ToWrite() : new ServiceWriteDto();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        IReadOnlyList<ServiceTypeReadDto> items = await ServiceTypeManager
            .GetListAsync(new ServiceTypeDescriptorDto { Skip = 0, Take = int.MaxValue }, CancellationToken.None)
            .GetValueOrDefaultAsync() ?? [];
        serviceTypes = [.. items];
    }

    private async Task SaveAsync()
    {
        EntityResult<ServiceReadDto> result = writeDto.Id == Guid.Empty
            ? await ServiceManager.AddAsync(writeDto, CancellationToken.None)
            : await ServiceManager.UpdateAsync(writeDto, CancellationToken.None);

        if (result.IsSuccess && result.Value is not null)
        {
            await OnSaved.InvokeAsync(result.Value.Id);
        }

        writeDto = Service is not null ? Service.ToWrite() : new ServiceWriteDto();

    }
}
