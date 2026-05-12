namespace Vitarish.Medical.Interface.Components.CheckIn;

public enum CheckInStep
{
    /// <summary>Identify the patient by IdentificationCode or advanced search, or create a new patient.</summary>
    Identify,
    /// <summary>Select a session slot and confirm the booking.</summary>
    Book,
    /// <summary>Display the outcome summary.</summary>
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
    public CheckInStep CurrentStep { get; set; } = CheckInStep.Identify;

    /// <summary>The IdentificationCode typed by the receptionist (primary lookup key).</summary>
    public string? IdentificationCode { get; set; }

    /// <summary>Advanced search query (name / phone / DoB). Shown only behind the advanced-search toggle.</summary>
    public string? SearchQuery { get; set; }

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

    /// <summary>When true the booking step will also mark the reservation as Present (walk-in).</summary>
    public bool MarkAsArrived { get; set; } = true;

    public void Reset()
    {
        CurrentStep = CheckInStep.Identify;
        IdentificationCode = null;
        SearchQuery = null;
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
        MarkAsArrived = true;
    }
}
