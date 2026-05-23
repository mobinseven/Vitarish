namespace Vitarish.Appointments.Interface.Components.Shifts;

public partial class ShiftBookingPolicyForm : ShiftBookingPoliciesBase
{
    [Parameter, EditorRequired] public Guid ShiftId { get; set; }
    [Parameter] public ShiftBookingPolicyReadDto? Policy { get; set; }
    [Parameter] public EventCallback<ShiftBookingPolicyReadDto> OnSaved { get; set; }

    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private ShiftBookingPolicyWriteDto model = new();
    private ShiftReadDto? Shift;

    protected override async Task OnInitializedAsync()
    {
        model = Policy is null
            ? new ShiftBookingPolicyWriteDto { ShiftId = ShiftId, Date = DateOnly.FromDateTime(DateTime.UtcNow) }
            : new ShiftBookingPolicyWriteDto
            {
                Id = Policy.Id, ShiftId = Policy.ShiftId, Date = Policy.Date, BookingOpensBefore = Policy.BookingOpensBefore,
                BookingCloseBefore = Policy.BookingCloseBefore, Closed = Policy.Closed, Capacity = Policy.Capacity,
                RequiresApproval = Policy.RequiresApproval, CancellationDeadline = Policy.CancellationDeadline,
                EstimatedSessionLength = Policy.EstimatedSessionLength, AllowRescheduling = Policy.AllowRescheduling, MaxReservationsPerParty = Policy.MaxReservationsPerParty
            };
        Shift = await ShiftManager.GetAsync(ShiftId, CancellationToken.None).GetValueOrDefaultAsync();
    }

    private async Task SaveAsync()
    {
        model = model with { ShiftId = ShiftId };
        EntityResult<ShiftBookingPolicyReadDto> result = Policy is null
            ? await BookingPolicyManager.AddAsync(model, CancellationToken.None)
            : await BookingPolicyManager.UpdateAsync(model, CancellationToken.None);

        if (result.IsSuccess && result.Value is not null)
        {
            _ = Snackbar.Add(Localizer["Saved"], Severity.Success);
            await OnSaved.InvokeAsync(result.Value);
            return;
        }

        string message = result.Errors.Count > 0 ? string.Join(" ", result.Errors.Select(e => Localizer[e.Message].Value)) : Localizer["SaveFailed"].Value;
        _ = Snackbar.Add(message, Severity.Error);
    }
}
