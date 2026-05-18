using Vitarish.Appointments.Application.Abstractions;
using Vitarish.Appointments.Domain.Dto;
using Vitarish.Main.Components;

namespace Vitarish.Main.Pages.Clinic;

/// <summary>
/// Base class for Clinic owner pages that load a single shift by route parameter.
/// Handles null-guard, fetch, redirect-on-missing, and page title.
/// </summary>
public class ClinicOwnerShiftPageBase : TenantPageBase
{
    [Inject] protected IAccessControlledShiftManager ShiftManager { get; set; } = default!;
    [Inject] protected IAccessControlledReservationManager ReservationManager { get; set; } = default!;

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
        Shift = await ShiftManager.GetAsync(ShiftId, CancellationToken.None).GetValueOrDefaultAsync();

        if (Shift is null)
        {
            Back();
            return;
        }

        NavService.OverridePageTitle(ShiftId.ToString(), Localizer["Shift"]);
        await InvokeAsync(StateHasChanged);
    }
}
