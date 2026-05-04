using Vitarish.Appointments.Interface.Components.Appointments;

namespace Vitarish.Medical.Interface.Components.Appointments;

public partial class TodayAppointmentsWidget : Vitarish.Modules.Base.TenantCommonBase
{
    [Inject] private IAccessControlledAppointmentSessionsService SessionsService { get; set; } = default!;
    [Inject] private IAccessControlledAppointmentsService AppointmentsService { get; set; } = default!;

    [Parameter] public EventCallback<Guid> OnSessionClicked { get; set; }
    [Parameter] public EventCallback OnSessionsClicked { get; set; }

    private List<(AppointmentSessionReadDto Session, List<AppointmentReadDto> Appointments)> todaySessions = [];

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        DateTimeOffset today = DateTimeOffset.Now.Date;

        AppointmentSessionDescriptorDto sessionDescriptor = new()
        {
            DateFrom = today,
            DateTo = today.AddDays(1).AddTicks(-1),
            Descending = false,
            Skip = 0,
            Take = int.MaxValue
        };

        IReadOnlyList<AppointmentSessionReadDto> sessions = await SessionsService
            .GetListAsync(sessionDescriptor, CancellationToken.None)
            .GetValueOrDefaultAsync() ?? [];

        sessions = [.. sessions.OrderBy(s => s.DateTime)];

        List<(AppointmentSessionReadDto, List<AppointmentReadDto>)> result = [];

        foreach (AppointmentSessionReadDto session in sessions)
        {
            AppointmentDescriptorDto appointmentDescriptor = new()
            {
                SessionId = session.Id,
                Skip = 0,
                Take = int.MaxValue
            };

            IReadOnlyList<AppointmentReadDto> appointments = await AppointmentsService
                .GetListAsync(appointmentDescriptor, CancellationToken.None)
                .GetValueOrDefaultAsync() ?? [];

            result.Add((session, [.. appointments]));
        }

        todaySessions = result;
        await InvokeAsync(StateHasChanged);
    }

    private static string GetInitials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "?";
        }

        string[] parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string first = parts.Length > 0 && parts[0].Length > 0 ? parts[0][..1] : "?";
        string second = parts.Length > 1 && parts[1].Length > 0 ? parts[1][..1] : string.Empty;
        return $"{first}{second}".ToUpperInvariant();
    }

    private static Color GetAvatarColor(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Pending => Color.Warning,
        AppointmentStatus.Confirmed => Color.Success,
        AppointmentStatus.Present => Color.Secondary,
        AppointmentStatus.Completed => Color.Dark,
        AppointmentStatus.Cancelled => Color.Default,
        _ => Color.Default
    };
}
