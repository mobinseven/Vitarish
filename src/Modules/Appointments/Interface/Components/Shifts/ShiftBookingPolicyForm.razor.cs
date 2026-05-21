namespace Vitarish.Appointments.Interface.Components.Shifts;

public partial class ShiftBookingPolicyForm : ShiftBookingPoliciesBase
{
    [Parameter, EditorRequired] public Guid ShiftId { get; set; }
    [Parameter] public ShiftBookingPolicyReadDto? Policy { get; set; }
    [Parameter] public EventCallback<ShiftBookingPolicyReadDto> OnSaved { get; set; }

    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private ShiftBookingPolicyWriteDto dto = new();

    protected override void OnInitialized()
        => dto = Policy is not null ? Policy.ToWrite() : new ShiftBookingPolicyWriteDto { ShiftId = ShiftId };

    private async Task SaveAsync()
    {
        dto = dto with { ShiftId = ShiftId };

        EntityResult<ShiftBookingPolicyReadDto> result = await BookingPolicyManager.UpsertPolicyAsync(dto, CancellationToken.None);

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
