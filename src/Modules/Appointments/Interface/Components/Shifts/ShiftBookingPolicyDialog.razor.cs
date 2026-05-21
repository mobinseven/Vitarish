namespace Vitarish.Appointments.Interface.Components.Shifts;

public partial class ShiftBookingPolicyDialog : ShiftBookingPoliciesBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public Guid ShiftId { get; set; }
    [Parameter] public ShiftBookingPolicyReadDto? Policy { get; set; }

    private void OnSavedInternal(ShiftBookingPolicyReadDto saved) => MudDialog.Close(DialogResult.Ok(saved));

    private void Close() => MudDialog.Cancel();
}
