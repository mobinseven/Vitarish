namespace Vitarish.Appointments.Interface.Components.Shifts;

public partial class ShiftReminderPolicyForm : ShiftReminderPoliciesBase
{
    [Parameter, EditorRequired] public Guid ShiftId { get; set; }
    [Parameter] public ShiftReminderPolicyReadDto? Policy { get; set; }
    [Parameter] public EventCallback<ShiftReminderPolicyReadDto> OnSaved { get; set; }

    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private ShiftReminderPolicyWriteDto dto = new();

    protected override void OnInitialized() => dto = Policy is not null ? Policy.ToWrite() : new ShiftReminderPolicyWriteDto { ShiftId = ShiftId, IsEnabled = true };

    private void ToggleStatus(ReservationStatus status, bool include)
    {
        if (include && !dto.QualifyingStatuses.Contains(status))
        {
            dto.QualifyingStatuses.Add(status);
        }
        else if (!include)
        {
            dto.QualifyingStatuses.Remove(status);
        }
    }

    private async Task SaveAsync()
    {
        dto.ShiftId = ShiftId;

        EntityResult<ShiftReminderPolicyReadDto> result = dto.Id == Guid.Empty
            ? await ReminderPolicyManager.AddAsync(dto, CancellationToken.None)
            : await ReminderPolicyManager.UpdateAsync(dto, CancellationToken.None);

        if (result.IsSuccess && result.Value is not null)
        {
            _ = Snackbar.Add(Localizer["Saved"], Severity.Success);
            await OnSaved.InvokeAsync(result.Value);
        }
        else if (result.IsFailure)
        {
            string message = result.Errors.Count > 0
                ? string.Join(" ", result.Errors.Select(e => Localizer[e.Message].Value))
                : Localizer["SaveFailed"].Value;
            _ = Snackbar.Add(message, Severity.Error);
        }
    }
}
