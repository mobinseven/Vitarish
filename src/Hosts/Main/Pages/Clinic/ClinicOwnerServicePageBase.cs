using Vitarish.Appointments.Client;
using Vitarish.Appointments.Contracts.Dto;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Main.Pages.Clinic;

public class ClinicOwnerServicePageBase : TenantPageBase
{
    [Inject] protected IAppointmentsClient AppointmentsClient { get; set; } = default!;

    [Parameter] public Guid? serviceId { get; set; }

    protected ServiceReadDto? Service;
    protected Guid ServiceId;

    protected virtual void Back() => NavigationManager.NavigateTo(AppRoutes.Clinic.OwnerServices);

    protected override async Task ReloadRequired()
    {
        if (serviceId is null)
        {
            Back();
            return;
        }

        ServiceId = serviceId.Value;
        EntityResult<ServiceReadDto> result = await ((IServiceManager)AppointmentsClient).GetAsync(ServiceId, CancellationToken.None);
        Service = result.IsSuccess ? result.Value : null;

        if (Service is null)
        {
            Back();
            return;
        }

        NavService.OverridePageTitle(Service.Name, Localizer["Service"]);
        await InvokeAsync(StateHasChanged);
    }
}
