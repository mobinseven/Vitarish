namespace Vitarish.Calendar.Domain.Entities;

public class AvailabilityPolicy : Entity
{
    public Guid CalendarAccountId
    {
        get;
        set => SetField(ref field, value, nameof(CalendarAccountId));
    }

    public CalendarObjectCategory Category
    {
        get;
        set => SetField(ref field, value, nameof(Category));
    }

    public AvailabilityEffect Effect
    {
        get;
        set => SetField(ref field, value, nameof(Effect));
    } = AvailabilityEffect.Consumes;
}
