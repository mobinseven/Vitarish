namespace Vitarish.Appointments.Interface.Components.Sessions;

public partial class SessionReminderPolicyForm : SessionReminderPoliciesBase
{
    [Parameter, EditorRequired] public Guid SessionId { get; set; }
    [Parameter] public SessionReminderPolicyReadDto? Policy { get; set; }
    [Parameter] public EventCallback<SessionReminderPolicyReadDto> OnSaved { get; set; }

    private SessionReminderPolicyWriteDto dto = new();
    private double offsetHours = 1;

    protected override void OnParametersSet()
    {
        if (Policy is not null)
        {
            dto = Policy.ToWrite();
            offsetHours = Policy.OffsetBefore.TotalHours;
        }
        else
        {
            dto = new SessionReminderPolicyWriteDto { SessionId = SessionId, IsEnabled = true };
            offsetHours = 1;
        }
    }

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
        dto.SessionId = SessionId;
        dto.OffsetBefore = TimeSpan.FromHours(offsetHours);

        EntityResult<SessionReminderPolicyReadDto> result = dto.Id == Guid.Empty
            ? await ReminderPolicyManager.AddPolicyAsync(dto, CancellationToken.None)
            : await ReminderPolicyManager.UpdatePolicyAsync(dto, CancellationToken.None);

        if (result.IsSuccess && result.Value is not null)
        {
            await OnSaved.InvokeAsync(result.Value);
        }
    }
}
