using Vitarish.Appointments.Client;
using Vitarish.Appointments.Contracts.Dto;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Main.Pages.Clinic;

/// <summary>
/// Base class for Clinic owner pages that load a single shift by route parameter.
/// Handles null-guard, fetch, redirect-on-missing, and page title.
/// </summary>
public class ClinicOwnerShiftPageBase : TenantPageBase
{
    [Inject] protected IAppointmentsClient AppointmentsClient { get; set; } = default!;

    [Parameter] public Guid? shiftId { get; set; }

    protected ShiftReadDto? Shift;
    protected Guid ShiftId;

    protected virtual void Back() => NavigationManager.NavigateTo(AppRoutes.Clinic.OwnerShifts);

    protected override async Task ReloadRequired()
    {
        if (shiftId is null)
        {
            Back();
            return;
        }

        ShiftId = shiftId.Value;
        EntityResult<ShiftReadDto> result = await ((IShiftManager)AppointmentsClient).GetAsync(ShiftId, CancellationToken.None);
        Shift = result.IsSuccess ? result.Value : null;

        if (Shift is null)
        {
            Back();
            return;
        }

        NavService.OverridePageTitle(ShiftId.ToString(), Localizer["Shift"]);
        await InvokeAsync(StateHasChanged);
    }
}
