namespace Vitarish.Appointments.Domain.Entities;

public class ShiftReminderPolicy : Entity
{
    public Guid ShiftId
    {
        get;
        set => SetField(ref field, value, nameof(ShiftId));
    }

    public bool IsEnabled
    {
        get;
        set => SetField(ref field, value, nameof(IsEnabled));
    } = true;

    public ShiftReminderChannel Channel
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

    [Obsolete("Templates are now resolved from Platform locale notification records at send time. This property will be removed in a future migration.")]
    public string SubjectTemplate
    {
        get;
        set => SetField(ref field, value, nameof(SubjectTemplate));
    } = string.Empty;

    [Obsolete("Templates are now resolved from Platform locale notification records at send time. This property will be removed in a future migration.")]
    public string BodyTemplate
    {
        get;
        set => SetField(ref field, value, nameof(BodyTemplate));
    } = string.Empty;
}
