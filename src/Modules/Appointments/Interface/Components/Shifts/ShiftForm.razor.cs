namespace Vitarish.Appointments.Interface.Components.Shifts;

public partial class ShiftForm : ShiftsBase
{
    [Parameter] public ShiftReadDto? Shift { get; set; }
    [Parameter] public EventCallback<Guid> OnSaved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }
    [Parameter] public DayOfWeek? DayOfWeek { get; set; }
    [Inject] private IEnumerable<IShiftFormPlugin> FormPlugins { get; set; } = [];
    private IShiftFormPlugin? Plugin => FormPlugins.SingleOrDefault(p => p.Id == TenantService?.PluginIdentifier);

    private ShiftWriteDto shiftWriteDto = new();

    protected override void OnParametersSet()
    {
        if (Shift is not null)
        {
            shiftWriteDto = Shift.ToWrite();
        }
        else
        {
            if (DayOfWeek is not null)
                shiftWriteDto.DayOfWeek = DayOfWeek.Value;
        }


        shiftWriteDto.TimeZone = string.IsNullOrWhiteSpace(shiftWriteDto.TimeZone)
            ? TimeZoneInfo.Local.Id
            : shiftWriteDto.TimeZone;

        shiftWriteDto = shiftWriteDto with
        {
            BookingPolicy = shiftWriteDto.BookingPolicy ?? new ShiftBookingPolicyWriteDto()
        };
    }

    private async Task SaveAsync()
    {
        EntityResult<ShiftReadDto> result = shiftWriteDto.Id == Guid.Empty
            ? await ShiftManager.AddAsync(shiftWriteDto, CancellationToken.None)
            : await ShiftManager.UpdateAsync(shiftWriteDto, CancellationToken.None);

        if (result.IsSuccess && result.Value is not null)
        {
            await OnSaved.InvokeAsync(result.Value.Id);
        }
    }

    private async Task Cancel() => await OnCancelled.InvokeAsync();

}
