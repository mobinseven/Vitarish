namespace Vitarish.Appointments.Interface.Components.Sessions;

public partial class SessionDetail : SessionsBase
{
    [Parameter, EditorRequired] public AppointmentSessionReadDto Session { get; set; } = default!;
    [Parameter] public EventCallback OnReserved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    [Inject] protected IAccessControlledAppointmentsService AppointmentsService { get; set; } = default!;

    private AppointmentSessionReadDto current = default!;
    private bool hasReservation;

    protected override void OnParametersSet()
    {
        current = Session;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await CheckReservationAsync();
    }

    private async Task CheckReservationAsync()
    {
        AppointmentDescriptorDto descriptor = new()
        {
            SessionId = current.Id,
            Skip = 0,
            Take = 1
        };
        IReadOnlyList<AppointmentReadDto>? appointments = await AppointmentsService.GetListAsync(descriptor, CancellationToken.None).GetValueOrDefaultAsync();
        hasReservation = appointments?.Any(a => a.Status != AppointmentStatus.Cancelled) == true;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ReserveAsync()
    {
        await SessionsService.ReserveAsync(current.Id, CancellationToken.None);
        await OnReserved.InvokeAsync();
        await CheckReservationAsync();
    }

    private async Task CancelReservationAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmationQuestion"].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["CancelReservation"].Value },
            { nameof(ConfirmDialog.Color), Color.Error }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(Localizer["CancelReservation"], parameters);
        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        await SessionsService.CancelReservationAsync(current.Id, CancellationToken.None);
        await OnCancelled.InvokeAsync();
        await CheckReservationAsync();
    }
}
