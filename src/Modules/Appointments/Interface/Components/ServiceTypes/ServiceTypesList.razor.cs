namespace Vitarish.Appointments.Interface.Components.ServiceTypes;

public partial class ServiceTypesList : ServiceTypesBase
{
    [Parameter] public EventCallback<Guid> OnClicked { get; set; }
    [Parameter] public EventCallback OnAdd { get; set; }

    private List<ServiceTypeReadDto> serviceTypes = [];

    protected override Task OnSearchedAsync() => ReloadRequired();

    protected override async Task ReloadRequired()
    {
        IReadOnlyList<ServiceTypeReadDto> items = await ServiceTypeManager
            .GetListAsync(new ServiceTypeDescriptorDto { Skip = 0, Take = int.MaxValue, Search = SearchString }, CancellationToken.None)
            .GetValueOrDefaultAsync() ?? [];
        serviceTypes = [.. items];
        await InvokeAsync(StateHasChanged);
    }
}
