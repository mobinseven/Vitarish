using Vitarish.Appointments.Client;
using Vitarish.Appointments.Contracts.Dto;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Main.Pages.Clinic;

public class ClinicUserReservationPageBase : TenantPageBase
{
    [Inject] protected IAppointmentsClient AppointmentsClient { get; set; } = default!;

    [Parameter] public Guid? reservationId { get; set; }

    protected ReservationReadDto? Reservation;

    protected virtual void Back() => NavigationManager.NavigateTo(AppRoutes.Clinic.UserReservations);

    protected override async Task ReloadRequired()
    {
        if (reservationId is null)
        {
            Back();
            return;
        }

        Guid id = reservationId.Value;
        EntityResult<ReservationReadDto> result = await ((IReservationManager)AppointmentsClient).GetAsync(id, CancellationToken.None);
        Reservation = result.IsSuccess ? result.Value : null;

        if (Reservation is null)
        {
            Back();
            return;
        }

        NavService.OverridePageTitle(id.ToString(), Reservation.Date.ToString("f") ?? id.ToString());
        await InvokeAsync(StateHasChanged);
    }
}
