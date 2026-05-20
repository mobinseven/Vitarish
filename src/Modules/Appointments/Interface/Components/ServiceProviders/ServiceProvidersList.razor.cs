namespace Vitarish.Appointments.Interface.Components.ServiceProviders;

public partial class ServiceProvidersList : ServiceProvidersBase
{
    [Parameter] public EventCallback<Guid> OnClicked { get; set; }
    [Parameter] public EventCallback OnAdd { get; set; }

    private List<ServiceProviderReadDto> providers = [];

    protected override Task OnSearchedAsync() => ReloadRequired();

    protected override async Task ReloadRequired()
    {
        IReadOnlyList<ServiceProviderReadDto> items = await ServiceProviderManager
            .GetListAsync(new ServiceProviderDescriptorDto { Skip = 0, Take = int.MaxValue, Search = SearchString }, CancellationToken.None)
            .GetValueOrDefaultAsync() ?? [];
        providers = [.. items];
        await InvokeAsync(StateHasChanged);
    }
}
