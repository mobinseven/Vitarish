namespace Vitarish.Medical.Interface.Components.CheckIn;

public enum CheckInStep
{
    /// <summary>Identify the patient by IdentificationCode or advanced search, or create a new patient.</summary>
    Identify,
    /// <summary>Select a shift slot and confirm the booking.</summary>
    Book,
    /// <summary>Display the outcome summary.</summary>
    Confirm
}

public enum CheckInOutcome
{
    None,
    CurrentShift,
    FutureShift
}

public class PatientCheckInContext
{
    public CheckInStep CurrentStep { get; set; } = CheckInStep.Identify;

    public ProjectReadDto? SelectedPatient { get; set; }

    public ShiftReadDto? SelectedShift { get; set; }

    public ShiftReservationReadDto? SelectedOccurrence { get; set; }

    public ReservationReadDto? ConfirmedReservation { get; set; }

    public CheckInOutcome Outcome { get; set; } = CheckInOutcome.None;

    /// <summary>When true the booking step will also mark the reservation as Present (walk-in).</summary>
    public bool MarkAsArrived { get; set; } = true;

    public void Reset()
    {
        CurrentStep = CheckInStep.Identify;
        SelectedPatient = null;
        SelectedShift = null;
        SelectedOccurrence = null;
        ConfirmedReservation = null;
        Outcome = CheckInOutcome.None;
        MarkAsArrived = true;
    }
}
