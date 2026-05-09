namespace Vitarish.Appointments.Interface.Components.Sessions;

public partial class SessionDetail : SessionsBase
{
    [Parameter, EditorRequired] public SessionReadDto Session { get; set; } = default!;
    [Parameter] public EventCallback OnReserved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    [Inject] protected IAccessControlledReservationManager ReservationManager { get; set; } = default!;

    private bool hasReservation;
    private Guid? activeReservationId;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await CheckReservationAsync();
    }

    private async Task CheckReservationAsync()
    {
        hasReservation = false;
        activeReservationId = null;

        if (string.IsNullOrWhiteSpace(RequestContext.UserId))
        {
            return;
        }

        ReservationDescriptorDto descriptor = new()
        {
            ReservingPartyId = RequestContext.UserId,
            Skip = 0,
            Take = 10
        };

        IReadOnlyList<ReservationReadDto>? reservations = await ReservationManager
            .GetListAsync(descriptor, CancellationToken.None)
            .GetValueOrDefaultAsync();

        hasReservation = reservations?.Any(r =>
            r.Occurrence?.SessionId == Session.Id &&
            r.Status != ReservationStatus.Cancelled &&
            r.Status != ReservationStatus.Rejected &&
            r.Status != ReservationStatus.Expired &&
            r.Status != ReservationStatus.Completed) == true;

        activeReservationId = reservations?.FirstOrDefault(r =>
            r.Occurrence?.SessionId == Session.Id &&
            r.Status != ReservationStatus.Cancelled &&
            r.Status != ReservationStatus.Rejected &&
            r.Status != ReservationStatus.Expired &&
            r.Status != ReservationStatus.Completed)?.Id;

        await InvokeAsync(StateHasChanged);
    }

    private async Task ReserveAsync(DateOnly date)
    {
        EntityResult<ReservationReadDto> result = await ReservationManager.ReserveAsync(Session.Id, date, null, CancellationToken.None);
        if (result.IsFailure)
        {
            return;
        }

        await OnReserved.InvokeAsync();
        await CheckReservationAsync();
    }

    private async Task CancelReservationAsync()
    {
        if (activeReservationId is null)
        {
            return;
        }

        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmationQuestion"].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["CancelItem", Localizer["Reservation"]].Value },
            { nameof(ConfirmDialog.Color), Color.Error }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(Localizer["CancelItem", Localizer["Reservation"]], parameters);
        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        await ReservationManager.CancelReservationAsync(activeReservationId.Value, CancellationToken.None);
        await OnCancelled.InvokeAsync();
        await CheckReservationAsync();
    }
}
