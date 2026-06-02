using Vitarish.Appointments.Application.Abstractions;
using Vitarish.Appointments.Domain.Dto;

namespace Vitarish.Main.Pages.Clinic;

public class ClinicOwnerServicePageBase : TenantPageBase
{
    [Inject] protected IServiceManager ServiceManager { get; set; } = default!;

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
        Service = await ServiceManager.GetAsync(ServiceId, CancellationToken.None).GetValueOrDefaultAsync();

        if (Service is null)
        {
            Back();
            return;
        }

        NavService.OverridePageTitle(Service.Name, Localizer["Service"]);
        await InvokeAsync(StateHasChanged);
    }
}
