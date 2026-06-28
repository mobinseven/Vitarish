using Vitarish.Appointments.Client;
using Vitarish.Appointments.Contracts.Dto;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Main.Pages.Clinic;

public class ClinicOwnerServiceProviderPageBase : TenantPageBase
{
    [Inject] protected IAppointmentsClient AppointmentsClient { get; set; } = default!;

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
        EntityResult<ServiceProviderReadDto> result = await ((IServiceProviderManager)AppointmentsClient).GetAsync(ServiceProviderId, CancellationToken.None);
        Provider = result.IsSuccess ? result.Value : null;

        if (Provider is null)
        {
            Back();
            return;
        }

        NavService.OverridePageTitle(Provider.Name, Localizer["ServiceProvider"]);
        await InvokeAsync(StateHasChanged);
    }
}
