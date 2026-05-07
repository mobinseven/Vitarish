namespace Vitarish.Appointments.Domain.Entities;

public class SessionReminderPolicy : Entity
{
    public Guid SessionId
    {
        get;
        set => SetField(ref field, value, nameof(SessionId));
    }

    public bool IsEnabled
    {
        get;
        set => SetField(ref field, value, nameof(IsEnabled));
    } = true;

    public SessionReminderKind Kind
    {
        get;
        set => SetField(ref field, value, nameof(Kind));
    } = SessionReminderKind.Email;

    public TimeSpan OffsetBeforeOccurrenceStart
    {
        get;
        set => SetField(ref field, value, nameof(OffsetBeforeOccurrenceStart));
    } = TimeSpan.FromHours(24);

    public HashSet<ReservationStatus> QualifyingReservationStatuses
    {
        get;
        set => SetField(ref field, value, nameof(QualifyingReservationStatuses));
    } = [ReservationStatus.Confirmed];

    public string SubjectTemplate
    {
        get;
        set => SetField(ref field, value, nameof(SubjectTemplate));
    } = string.Empty;

    public string BodyTemplate
    {
        get;
        set => SetField(ref field, value, nameof(BodyTemplate));
    } = string.Empty;
}
