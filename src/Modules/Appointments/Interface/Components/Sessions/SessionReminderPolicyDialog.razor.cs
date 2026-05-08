namespace Vitarish.Appointments.Interface.Components.Sessions;

public partial class SessionReminderPolicyDialog : SessionReminderPoliciesBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public Guid SessionId { get; set; }
    [Parameter] public SessionReminderPolicyReadDto? Policy { get; set; }

    private void OnSavedInternal(SessionReminderPolicyReadDto saved) => MudDialog.Close(DialogResult.Ok(saved));

    private void Close() => MudDialog.Cancel();
}
