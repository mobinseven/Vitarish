namespace Vitarish.Medical.Interface.Components.Appointments;

public partial class TodayAppointmentsWidget : Vitarish.Modules.Base.TenantCommonBase
{
    [Inject] private IAccessControlledSessionManager SessionsService { get; set; } = default!;
    [Inject] private IAccessControlledReservationManager ReservationManager { get; set; } = default!;
    [Inject] private IAccessControlledProjectsService ProjectsService { get; set; } = default!;

    [Parameter] public EventCallback<Guid> OnSessionClicked { get; set; }
    [Parameter] public EventCallback OnSessionsClicked { get; set; }

    private List<(SessionReadDto Session, List<ReservationReadDto> Reservations)> todaySessions = [];
    // Cache: party ID string → display name (user full name or patient name)
    private Dictionary<string, string> displayNameByPartyId = [];

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        SessionDescriptorDto sessionDescriptor = new()
        {
            Status = SessionStatus.Active,
            Skip = 0,
            Take = int.MaxValue
        };

        IReadOnlyList<SessionReadDto> sessions = await SessionsService
            .GetListAsync(sessionDescriptor, CancellationToken.None)
            .GetValueOrDefaultAsync() ?? [];

        sessions = [.. sessions.OrderBy(s => s.Title)];

        List<(SessionReadDto, List<ReservationReadDto>)> result = [];

        foreach (SessionReadDto session in sessions)
        {
            ReservationDescriptorDto reservationDescriptor = new()
            {
                SessionId = session.Id,
                Date = today,
                Skip = 0,
                Take = int.MaxValue
            };

            IReadOnlyList<ReservationReadDto> reservations = await ReservationManager
                .GetListAsync(reservationDescriptor, CancellationToken.None)
                .GetValueOrDefaultAsync() ?? [];

            result.Add((session, [.. reservations.OrderBy(r => r.Status).ThenBy(r => r.ReservingPartyId)]));
        }

        todaySessions = result;

        // Pre-resolve display names. First try user lookup (fast, in-memory); for opaque
        // GUID-shaped IDs that don't match any user, attempt a project lookup — these are
        // patients booked without a linked user account (Medical-layer responsibility only).
        displayNameByPartyId = [];
        IEnumerable<string> distinctPartyIds = todaySessions
            .SelectMany(s => s.Item2)
            .Select(r => r.ReservingPartyId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()!;

        foreach (string partyId in distinctPartyIds)
        {
            string? userName = GetUserFullNameById(partyId);
            if (!string.IsNullOrWhiteSpace(userName))
            {
                displayNameByPartyId[partyId] = userName;
                continue;
            }

            // No matching user — try to interpret as a patient project ID.
            if (Guid.TryParse(partyId, out Guid projectId))
            {
                ProjectReadDto? project = await ProjectsService.GetAsync(projectId, CancellationToken.None).GetValueOrDefaultAsync();
                if (project is not null)
                {
                    displayNameByPartyId[partyId] = project.Name;
                }
            }
        }

        await InvokeAsync(StateHasChanged);
    }

    private string GetReservationDisplayName(ReservationReadDto reservation)
    {
        if (reservation.ReservingPartyId is not null &&
            displayNameByPartyId.TryGetValue(reservation.ReservingPartyId, out string? name))
        {
            return name;
        }

        return reservation.ReservingPartyId ?? "?";
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

    private static Color GetAvatarColor(ReservationStatus status) => status switch
    {
        ReservationStatus.Pending => Color.Warning,
        ReservationStatus.Confirmed => Color.Success,
        ReservationStatus.Present => Color.Secondary,
        ReservationStatus.Completed => Color.Dark,
        ReservationStatus.Cancelled => Color.Default,
        _ => Color.Default
    };
}
