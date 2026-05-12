namespace Vitarish.Medical.Interface.Components.CheckIn;

public enum CheckInIntent
{
    WalkIn,
    ArrivalForExisting,
    ScheduleForLater
}

public enum CheckInStep
{
    Intent,
    Lookup,
    Create,
    Session,
    FutureSession,
    Confirm
}

public enum CheckInOutcome
{
    None,
    CurrentSession,
    FutureSession
}

public class PatientCheckInContext
{
    public CheckInIntent Intent { get; set; } = CheckInIntent.WalkIn;

    public CheckInStep CurrentStep { get; set; } = CheckInStep.Intent;

    public string? SearchQuery { get; set; }

    public string? SecondaryIdentifier { get; set; }

    public IReadOnlyList<ProjectReadDto> SearchResults { get; set; } = [];

    public IReadOnlyList<ProjectReadDto> DuplicateCandidates { get; set; } = [];

    public ProjectReadDto? SelectedPatient { get; set; }

    public ProjectWriteDto? NewPatientDraft { get; set; }

    public IReadOnlyList<(SessionReadDto Session, SessionReservationReadDto? Occurrence)> TodaySessions { get; set; } = [];

    public IReadOnlyList<ReservationReadDto> PatientTodayReservations { get; set; } = [];

    public SessionReadDto? SelectedSession { get; set; }

    public SessionReservationReadDto? SelectedOccurrence { get; set; }

    public ReservationReadDto? ConfirmedReservation { get; set; }

    public CheckInOutcome Outcome { get; set; } = CheckInOutcome.None;

    public void Reset()
    {
        Intent = CheckInIntent.WalkIn;
        CurrentStep = CheckInStep.Intent;
        SearchQuery = null;
        SecondaryIdentifier = null;
        SearchResults = [];
        DuplicateCandidates = [];
        SelectedPatient = null;
        NewPatientDraft = null;
        TodaySessions = [];
        PatientTodayReservations = [];
        SelectedSession = null;
        SelectedOccurrence = null;
        ConfirmedReservation = null;
        Outcome = CheckInOutcome.None;
    }
}
