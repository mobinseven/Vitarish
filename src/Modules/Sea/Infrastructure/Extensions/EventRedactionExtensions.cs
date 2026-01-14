internal static class EventRedactionExtensions
{
     public static void Redact(this EventDto eventDto)
    {
        eventDto.Title = "Redacted";
        eventDto.Description = "Redacted";
    }
}
