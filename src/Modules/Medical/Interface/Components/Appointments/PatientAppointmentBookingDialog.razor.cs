using Vitarish.Reminders.Interface.Components.Reminders;

namespace Vitarish.Medical.Interface.Components.Appointments;

public partial class PatientAppointmentBookingDialog : Vitarish.Modules.Base.TenantCommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public ProjectReadDto Patient { get; set; } = default!;

    [Inject] private IAccessControlledAppointmentSessionsService SessionsService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private string? patientUserId;
    private List<AppointmentSessionReadDto> sessions = [];
    private bool createReminder;

    protected override async Task OnInitializedAsync()
    {
        patientUserId = Patient.Metadata.TryGetValue(MetadataKeys.LinkedUserId, out object? v) ? v?.ToString() : null;
        if (!string.IsNullOrWhiteSpace(patientUserId))
        {
            await LoadSessionsAsync();
        }
    }

    private async Task LoadSessionsAsync()
    {
        AppointmentSessionDescriptorDto descriptor = new()
        {
            Status = AppointmentSessionStatus.Open,
            Skip = 0,
            Take = int.MaxValue,
            Descending = false
        };

        IReadOnlyList<AppointmentSessionReadDto>? result = await SessionsService.GetListAsync(descriptor, CancellationToken.None).GetValueOrDefaultAsync();
        sessions = [.. (result ?? []).OrderBy(s => s.DateTime)];
        await InvokeAsync(StateHasChanged);
    }

    private async Task ReserveAsync(AppointmentSessionReadDto session)
    {
        if (string.IsNullOrWhiteSpace(patientUserId))
        {
            return;
        }

        await SessionsService.ReserveForUserAsync(session.Id, patientUserId, CancellationToken.None);
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
