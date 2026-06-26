
namespace Vitarish.Appointments.Interface.Components.ServiceProviders;

public partial class ServiceProviderForm : ServiceProvidersBase
{
    [Parameter] public ServiceProviderReadDto? ServiceProvider { get; set; }
    [Parameter] public EventCallback<Guid> OnSaved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    private ServiceProviderWriteDto writeDto = new();

    protected override void OnParametersSet() => writeDto = ServiceProvider is not null ? ServiceProvider.ToWrite() : new ServiceProviderWriteDto();

    private async Task SaveAsync()
    {
        EntityResult<ServiceProviderReadDto> result = writeDto.Id == Guid.Empty
            ? await ServiceProviderManager.AddAsync(writeDto, CancellationToken.None)
            : await ServiceProviderManager.UpdateAsync(writeDto, CancellationToken.None);

        if (result.IsSuccess && result.Value is not null)
        {
            await OnSaved.InvokeAsync(result.Value.Id);
        }
    }
}
