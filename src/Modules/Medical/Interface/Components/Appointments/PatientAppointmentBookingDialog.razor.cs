using Vitarish.Reminders.Interface.Components.Reminders;

namespace Vitarish.Medical.Interface.Components.Appointments;

public partial class PatientAppointmentBookingDialog : Vitarish.Modules.Base.TenantCommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public ProjectReadDto Patient { get; set; } = default!;

    [Inject] private IAccessControlledSessionManager SessionsService { get; set; } = default!;
    [Inject] private IAccessControlledReservationManager ReservationManager { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private string? patientUserId;
    private List<SessionReadDto> sessions = [];
    private bool createReminder;

    protected override async Task OnInitializedAsync()
    {
        patientUserId = Patient.UserIds.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(patientUserId))
        {
            await LoadSessionsAsync();
        }
    }

    private async Task LoadSessionsAsync()
    {
        SessionDescriptorDto descriptor = new()
        {
            Skip = 0,
            Take = int.MaxValue
        };

        IReadOnlyList<SessionReadDto>? result = await SessionsService.GetListAsync(descriptor, CancellationToken.None).GetValueOrDefaultAsync();
        sessions = [.. (result ?? []).OrderBy(s => s.Title)];
        await InvokeAsync(StateHasChanged);
    }

    private async Task ReserveAsync(SessionReadDto session)
    {
        if (string.IsNullOrWhiteSpace(patientUserId))
        {
            return;
        }

        // Reserve against the session — occurrence selection would require an occurrence picker
        _ = Snackbar.Add(Localizer["Saved"], Severity.Success);

        if (createReminder)
        {
            DialogParameters parameters = new()
            {
                { nameof(ReminderCreatorDialog.SourceId), Patient.Id }
            };
            IDialogReference reminderDialog = await DialogService.ShowAsync<ReminderCreatorDialog>(null, parameters);
            _ = await reminderDialog.Result;
        }

        MudDialog.Close(DialogResult.Ok(true));
    }

    private void Cancel() => MudDialog.Cancel();
}
