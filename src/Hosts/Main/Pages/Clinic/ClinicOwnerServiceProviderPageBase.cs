using Vitarish.Appointments.Application.Abstractions;
using Vitarish.Appointments.Domain.Dto;

namespace Vitarish.Main.Pages.Clinic;

public class ClinicOwnerServiceProviderPageBase : TenantPageBase
{
    [Inject] protected IServiceProviderManager ServiceProviderManager { get; set; } = default!;

    [Parameter] public Guid? serviceProviderId { get; set; }

    protected ServiceProviderReadDto? Provider;
    protected Guid ServiceProviderId;

    protected virtual void Back() => NavigationManager.NavigateTo(AppRoutes.Clinic.OwnerServiceProviders);

    protected override async Task ReloadRequired()
    {
        if (serviceProviderId is null)
        {
            Back();
            return;
        }

        ServiceProviderId = serviceProviderId.Value;
        Provider = await ServiceProviderManager.GetAsync(ServiceProviderId, CancellationToken.None).GetValueOrDefaultAsync();

        if (Provider is null)
        {
            Back();
            return;
        }

        NavService.OverridePageTitle(Provider.Name, Localizer["ServiceProvider"]);
        await InvokeAsync(StateHasChanged);
    }
}
