namespace Vitarish.Appointments.Interface.Components.Shifts;

public partial class ShiftForm : ShiftsBase
{
    [Parameter] public ShiftReadDto? Shift { get; set; }
    [Parameter] public EventCallback<Guid> OnSaved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }
    [Parameter] public DayOfWeek? DayOfWeek { get; set; }
    [Inject] private IEnumerable<IShiftFormPlugin> FormPlugins { get; set; } = [];
    [Inject] private IServiceProviderManager ServiceProviderManager { get; set; } = default!;
    [Inject] private IServiceManager ServiceManager { get; set; } = default!;
    private IShiftFormPlugin? Plugin => FormPlugins.SingleOrDefault(p => p.Id == TenantService?.PluginIdentifier);

    private ShiftWriteDto shiftWriteDto = new();
    private List<ServiceProviderReadDto> serviceProviders = [];
    private List<ServiceReadDto> availableServices = [];

    private const int ShiftTimeStepMinutes = 15;
    private static readonly TimeOnly LastSelectableEndTime = new(23, 59);

    private TimeOnly MinimumEndTime => GetMinimumEndTime(shiftWriteDto.StartTime);

    private static TimeOnly GetMinimumEndTime(TimeOnly startTime)
    {
        var minimumEndTime = startTime.AddMinutes(ShiftTimeStepMinutes);

        return minimumEndTime > startTime
            ? minimumEndTime
            : LastSelectableEndTime;
    }

    private void SetStartTime(TimeOnly startTime)
    {
        shiftWriteDto.StartTime = startTime;

        if (shiftWriteDto.EndTime <= shiftWriteDto.StartTime)
            shiftWriteDto.EndTime = MinimumEndTime;
    }

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

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Task<IReadOnlyList<ServiceProviderReadDto>?> providersTask = ServiceProviderManager
            .GetListAsync(new ServiceProviderDescriptorDto { Skip = 0, Take = int.MaxValue }, CancellationToken.None)
            .GetValueOrDefaultAsync();

        Task<IReadOnlyList<ServiceReadDto>?> servicesTask = ServiceManager
            .GetListAsync(new ServiceDescriptorDto { Skip = 0, Take = int.MaxValue }, CancellationToken.None)
            .GetValueOrDefaultAsync();

        await Task.WhenAll(providersTask, servicesTask);

        serviceProviders = [.. (providersTask.Result ?? [])];
        availableServices = [.. (servicesTask.Result ?? [])];
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
