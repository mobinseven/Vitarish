using Vitarish.Appointments.Application.Abstractions;
using Vitarish.Appointments.Domain.Dto;

namespace Vitarish.Main.Pages.Clinic;

public class ClinicUserShiftPageBase : TenantPageBase
{
    [Inject] protected IAccessControlledShiftManager ShiftManager { get; set; } = default!;

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
        Shift = await ShiftManager.GetAsync(id, CancellationToken.None).GetValueOrDefaultAsync();

        if (Shift is null)
        {
            Back();
            return;
        }

        NavService.OverridePageTitle(id.ToString(), Localizer["Shift"]);
        await InvokeAsync(StateHasChanged);
    }
}
