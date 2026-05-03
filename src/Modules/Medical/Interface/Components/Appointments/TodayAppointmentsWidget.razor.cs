using Vitarish.Appointments.Interface.Components.Appointments;

namespace Vitarish.Medical.Interface.Components.Appointments;

public partial class TodayAppointmentsWidget : Vitarish.Modules.Base.TenantCommonBase
{
    [Inject] private IAccessControlledAppointmentSessionsService SessionsService { get; set; } = default!;
    [Inject] private IAccessControlledAppointmentsService AppointmentsService { get; set; } = default!;

    private List<(AppointmentSessionReadDto Session, List<AppointmentReadDto> Appointments)> items = [];

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

        items = result;
        await InvokeAsync(StateHasChanged);
    }

    private async Task OpenDetailAsync(AppointmentReadDto appointment)
    {
        DialogParameters parameters = new()
        {
            { nameof(AdminAppointmentDetailDialog.Appointment), appointment }
        };

        IDialogReference dialog = await DialogService.ShowAsync<AdminAppointmentDetailDialog>(null, parameters);
        _ = await dialog.Result;
        await LoadAsync();
    }

    private static string GetInitials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "?";
        }

        string[] parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1].ToUpperInvariant(),
            _ => $"{parts[0][..1]}{parts[1][..1]}".ToUpperInvariant()
        };
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
