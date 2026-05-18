using Vitarish.Appointments.Application.Abstractions;
using Vitarish.Appointments.Domain.Dto;
using Vitarish.Main.Components;

namespace Vitarish.Main.Pages.Clinic;

/// <summary>
/// Base class for Clinic user pages that load a single shift by route parameter.
/// Handles null-guard, fetch, redirect-on-missing, and page title.
/// </summary>
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

        var id = shiftId.Value;
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
