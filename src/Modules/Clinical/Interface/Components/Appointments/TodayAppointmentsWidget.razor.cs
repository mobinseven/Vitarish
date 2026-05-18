namespace Vitarish.Clinical.Interface.Components.Appointments;

public partial class TodayAppointmentsWidget : Modules.Base.TenantCommonBase
{
    [Inject] private IAccessControlledShiftManager ShiftsService { get; set; } = default!;
    [Inject] private IAccessControlledReservationManager ReservationManager { get; set; } = default!;
    [Inject] private IAccessControlledProjectsService ProjectsService { get; set; } = default!;

    [Parameter] public EventCallback<Guid> OnShiftClicked { get; set; }
    [Parameter] public EventCallback OnShiftsClicked { get; set; }

    private List<(ShiftReadDto Shift, List<ReservationReadDto> Reservations)> todayShifts = [];
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
        ShiftDescriptorDto shiftDescriptor = new()
        {
            Status = ShiftStatus.Active,
            Skip = 0,
            Take = int.MaxValue
        };

        IReadOnlyList<ShiftReadDto> shifts = await ShiftsService
            .GetListAsync(shiftDescriptor, CancellationToken.None)
            .GetValueOrDefaultAsync() ?? [];

        shifts = [.. shifts
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ThenBy(s => s.EndTime)];

        List<(ShiftReadDto, List<ReservationReadDto>)> result = [];

        foreach (ShiftReadDto shift in shifts)
        {
            ReservationDescriptorDto reservationDescriptor = new()
            {
                ShiftId = shift.Id,
                DateFrom = today,
                DateTo = today,
                Skip = 0,
                Take = int.MaxValue
            };

            IReadOnlyList<ReservationReadDto> reservations = await ReservationManager
                .GetListAsync(reservationDescriptor, CancellationToken.None)
                .GetValueOrDefaultAsync() ?? [];

            result.Add((shift, [.. reservations.OrderBy(r => r.Status).ThenBy(r => r.ReservingPartyId)]));
        }

        todayShifts = result;

        // Pre-resolve display names. First try user lookup (fast, in-memory); for opaque
        // GUID-shaped IDs that don't match any user, attempt a project lookup — these are
        // patients booked without a linked user account (Medical-layer responsibility only).
        // Lookups for GUID-shaped IDs are issued in parallel to avoid sequential N+1 fetches.
        displayNameByPartyId = [];
        IReadOnlyList<string> distinctPartyIds = todayShifts
            .SelectMany(s => s.Item2)
            .Select(r => r.ReservingPartyId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList()!;

        List<Task> lookupTasks = [];
        foreach (string partyId in distinctPartyIds)
        {
            string? userName = GetUserFullNameById(partyId);
            if (!string.IsNullOrWhiteSpace(userName))
            {
                displayNameByPartyId[partyId] = userName;
                continue;
            }

            // No matching user — issue project lookups in parallel for GUID-shaped IDs.
            if (Guid.TryParse(partyId, out Guid projectId))
            {
                lookupTasks.Add(Task.Run(async () =>
                {
                    ProjectReadDto? project = await ProjectsService.GetAsync(projectId, CancellationToken.None).GetValueOrDefaultAsync();
                    if (project is not null)
                    {
                        displayNameByPartyId[partyId] = project.Name;
                    }
                }));
            }
        }

        if (lookupTasks.Count > 0)
        {
            await Task.WhenAll(lookupTasks);
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
