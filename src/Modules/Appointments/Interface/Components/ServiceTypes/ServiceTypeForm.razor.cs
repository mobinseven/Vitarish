namespace Vitarish.Appointments.Interface.Components.ServiceTypes;

public partial class ServiceTypeForm : ServiceTypesBase
{
    [Parameter] public ServiceTypeReadDto? ServiceType { get; set; }
    [Parameter] public EventCallback<Guid> OnSaved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    private ServiceTypeWriteDto writeDto = new();

    protected override void OnParametersSet()
    {
        writeDto = ServiceType is not null ? ServiceType.ToWrite() : new ServiceTypeWriteDto();
    }

    private async Task SaveAsync()
    {
        EntityResult<ServiceTypeReadDto> result = writeDto.Id == Guid.Empty
            ? await ServiceTypeManager.AddAsync(writeDto, CancellationToken.None)
            : await ServiceTypeManager.UpdateAsync(writeDto, CancellationToken.None);

        if (result.IsSuccess && result.Value is not null)
        {
            await OnSaved.InvokeAsync(result.Value.Id);
        }
    }
}
