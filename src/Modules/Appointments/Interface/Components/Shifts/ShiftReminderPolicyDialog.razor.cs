namespace Vitarish.Appointments.Interface.Components.Shifts;

public partial class ShiftReminderPolicyDialog : ShiftReminderPoliciesBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public Guid ShiftId { get; set; }
    [Parameter] public ShiftReminderPolicyReadDto? Policy { get; set; }

    private void OnSavedInternal(ShiftReminderPolicyReadDto saved) => MudDialog.Close(DialogResult.Ok(saved));

    private void Close() => MudDialog.Cancel();
}
