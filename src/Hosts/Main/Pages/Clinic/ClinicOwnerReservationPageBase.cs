using Vitarish.Appointments.Application.Abstractions;
using Vitarish.Appointments.Domain.Dto;

namespace Vitarish.Main.Pages.Clinic;

/// <summary>
/// Base class for Clinic owner pages that load a single reservation by route parameter.
/// Handles null-guard, fetch, redirect-on-missing, and page title.
/// </summary>
public class ClinicOwnerReservationPageBase : TenantPageBase
{
    [Inject] protected IAccessControlledReservationManager ReservationManager { get; set; } = default!;

    [Parameter] public Guid? reservationId { get; set; }

    protected ReservationReadDto? Reservation;

    protected virtual void Back() => NavigationManager.NavigateTo(AppRoutes.Clinic.OwnerReservations);

    protected override async Task ReloadRequired()
    {
        if (reservationId is null)
        {
            Back();
            return;
        }

        Guid id = reservationId.Value;
        Reservation = await ReservationManager.GetAsync(id, CancellationToken.None).GetValueOrDefaultAsync();

        if (Reservation is null)
        {
            Back();
            return;
        }

        NavService.OverridePageTitle(id.ToString(), Reservation.Date.ToString("f") ?? id.ToString());
        await InvokeAsync(StateHasChanged);
    }
}
