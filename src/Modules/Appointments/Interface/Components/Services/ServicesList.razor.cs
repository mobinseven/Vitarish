namespace Vitarish.Appointments.Interface.Components.Services;

public partial class ServicesList : ServicesBase
{
    [Parameter] public EventCallback<Guid> OnClicked { get; set; }
    [Parameter] public EventCallback OnAdd { get; set; }

    private List<ServiceReadDto> services = [];

    protected override Task OnSearchedAsync() => ReloadRequired();

    protected override async Task ReloadRequired()
    {
        IReadOnlyList<ServiceReadDto> items = await ServiceManager
            .GetListAsync(new ServiceDescriptorDto { Skip = 0, Take = int.MaxValue, Search = SearchString }, CancellationToken.None)
            .GetValueOrDefaultAsync() ?? [];
        services = [.. items];
        await InvokeAsync(StateHasChanged);
    }
}
