namespace Vitarish.Entities.Server.Services;

public class EntityTimeService : IEntityTimeService
{
    private DateTimeOffset? time;
    public DateTimeOffset Now => time ?? new(DateTime.Now, TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow));
    public void Set(DateTimeOffset? now)
    {
        time = now;
    }
}
