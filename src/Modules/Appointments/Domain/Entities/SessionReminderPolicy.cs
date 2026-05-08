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

    public ReminderKind Channel
    {
        get;
        set => SetField(ref field, value, nameof(Channel));
    }

    public TimeSpan OffsetBefore
    {
        get;
        set => SetField(ref field, value, nameof(OffsetBefore));
    }

    public IList<ReservationStatus> QualifyingStatuses
    {
        get;
        set => SetField(ref field, value, nameof(QualifyingStatuses));
    } = [];

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
