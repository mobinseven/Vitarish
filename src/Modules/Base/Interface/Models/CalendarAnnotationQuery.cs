namespace Vitarish.Modules.Base.Interface.Models;

public sealed record CalendarAnnotationQuery(
    DateOnly StartDate,
    DateOnly EndDate,
    CultureInfo? Culture = null,
    string? CountryCode = null,
    Uri? ExternalApiEndpoint = null,
    Uri? IcsFeedUrl = null,
    TimeSpan? CacheDuration = null);
