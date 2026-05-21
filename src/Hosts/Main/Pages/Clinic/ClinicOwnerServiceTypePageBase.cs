using Vitarish.Appointments.Application.Abstractions;
using Vitarish.Appointments.Domain.Dto;

namespace Vitarish.Main.Pages.Clinic;

public class ClinicOwnerServiceTypePageBase : TenantPageBase
{
    [Inject] protected IServiceTypeManager ServiceTypeManager { get; set; } = default!;

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
        ServiceType = await ServiceTypeManager.GetAsync(ServiceTypeId, CancellationToken.None).GetValueOrDefaultAsync();

        if (ServiceType is null)
        {
            Back();
            return;
        }

        NavService.OverridePageTitle(ServiceType.Name, Localizer["ServiceType"]);
        await InvokeAsync(StateHasChanged);
    }
}
