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

    public ProjectReadDto? SelectedPatient { get; set; }

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
        SelectedPatient = null;
        TodaySessions = [];
        PatientTodayReservations = [];
        SelectedSession = null;
        SelectedOccurrence = null;
        ConfirmedReservation = null;
        Outcome = CheckInOutcome.None;
        MarkAsArrived = true;
    }
}
