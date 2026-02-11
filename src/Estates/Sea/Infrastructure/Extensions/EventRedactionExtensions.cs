internal static class EventRedactionExtensions
{
    public static void Redact(this EventReadDto eventDto)
    {
        eventDto.Title = "Redacted";
        eventDto.Description = "Redacted";
    }
}
