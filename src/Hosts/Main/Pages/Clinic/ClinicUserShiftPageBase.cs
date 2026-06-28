using Vitarish.Appointments.Client;
using Vitarish.Appointments.Contracts.Dto;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Main.Pages.Clinic;

public class ClinicUserShiftPageBase : TenantPageBase
{
    [Inject] protected IAppointmentsClient AppointmentsClient { get; set; } = default!;

    [Parameter] public Guid? shiftId { get; set; }

    protected ShiftReadDto? Shift;

    protected virtual void Back() => NavigationManager.NavigateTo(AppRoutes.Clinic.UserShifts);

    protected override async Task ReloadRequired()
    {
        if (shiftId is null)
        {
            Back();
            return;
        }

        Guid id = shiftId.Value;
        EntityResult<ShiftReadDto> result = await ((IShiftManager)AppointmentsClient).GetAsync(id, CancellationToken.None);
        Shift = result.IsSuccess ? result.Value : null;

        if (Shift is null)
        {
            Back();
            return;
        }

        NavService.OverridePageTitle(id.ToString(), Localizer["Shift"]);
        await InvokeAsync(StateHasChanged);
    }
}
