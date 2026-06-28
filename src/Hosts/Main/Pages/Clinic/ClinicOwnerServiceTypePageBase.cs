using Vitarish.Appointments.Client;
using Vitarish.Appointments.Contracts.Dto;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Main.Pages.Clinic;

public class ClinicOwnerServiceTypePageBase : TenantPageBase
{
    [Inject] protected IAppointmentsClient AppointmentsClient { get; set; } = default!;

    [Parameter] public Guid? serviceTypeId { get; set; }

    protected ServiceTypeReadDto? ServiceType;
    protected Guid ServiceTypeId;

    protected virtual void Back() => NavigationManager.NavigateTo(AppRoutes.Clinic.OwnerServiceTypes);

    protected override async Task ReloadRequired()
    {
        if (serviceTypeId is null)
        {
            Back();
            return;
        }

        ServiceTypeId = serviceTypeId.Value;
        EntityResult<ServiceTypeReadDto> result = await ((IServiceTypeManager)AppointmentsClient).GetAsync(ServiceTypeId, CancellationToken.None);
        ServiceType = result.IsSuccess ? result.Value : null;

        if (ServiceType is null)
        {
            Back();
            return;
        }

        NavService.OverridePageTitle(ServiceType.Name, Localizer["ServiceType"]);
        await InvokeAsync(StateHasChanged);
    }
}
