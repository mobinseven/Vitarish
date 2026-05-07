using Vitarish.Communications.Interface.Components.Reminders;

namespace Vitarish.Medical.Interface.Components.Appointments;

public partial class PatientAppointmentBookingDialog : Vitarish.Modules.Base.TenantCommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public ProjectReadDto Patient { get; set; } = default!;

    [Inject] private IAccessControlledSessionManager SessionsService { get; set; } = default!;
    [Inject] private IAccessControlledReservationManager ReservationManager { get; set; } = default!;
    [Inject] private ISessionOccurrenceResolver SessionOccurrenceResolver { get; set; } = default!;
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
        sessions = [.. (result ?? [])
            .Where(s => s.Status == SessionStatus.Active && s.StartAt is not null && s.EndAt > DateTimeOffset.UtcNow)
            .OrderBy(s => s.StartAt)
            .ThenBy(s => s.Title)];
        await InvokeAsync(StateHasChanged);
    }

    private async Task ReserveAsync(SessionReadDto session)
    {
        if (string.IsNullOrWhiteSpace(patientUserId))
        {
            return;
        }

        TimeRangeQuery range = new()
        {
            Start = DateTimeOffset.UtcNow.AddDays(-1),
            End = DateTimeOffset.UtcNow.AddYears(1)
        };

        SessionOccurrenceReadDto? occurrence = (await SessionOccurrenceResolver.ResolveOccurrencesAsync(session.Id, range, CancellationToken.None))
            .OrderBy(o => o.OccurrenceStart)
            .FirstOrDefault(o => !o.IsCancelled && o.OccurrenceEnd > DateTimeOffset.UtcNow);

        if (occurrence is null)
        {
            _ = Snackbar.Add(Localizer["OccurrenceUnavailable"].Value, Severity.Warning);
            return;
        }

        EntityResult<ReservationReadDto> result = await ReservationManager.ReserveAsync(occurrence.Id, patientUserId, CancellationToken.None);
        if (result.IsFailure)
        {
            _ = Snackbar.Add(result.Errors.FirstOrDefault().Message, Severity.Error);
            return;
        }

        _ = Snackbar.Add(Localizer["Saved"], Severity.Success);

        if (createReminder)
        {
            DialogParameters parameters = new()
            {
                { nameof(ReminderCreatorDialog.SourceId), occurrence.Id }
            };
            IDialogReference reminderDialog = await DialogService.ShowAsync<ReminderCreatorDialog>(null, parameters);
            _ = await reminderDialog.Result;
        }

        MudDialog.Close(DialogResult.Ok(true));
    }

    private void Cancel() => MudDialog.Cancel();
}
