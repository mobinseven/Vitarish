using System.Reflection;

namespace Vitarish.Interfaces.Base.Services.CalendarAnnotations;

internal sealed class EmbeddedIcsCalendarAnnotationSource : ICalendarAnnotationSource
{
    private static readonly Assembly HostAssembly = typeof(EmbeddedIcsCalendarAnnotationSource).Assembly;

    public Task<CalendarAnnotationSourceResult> GetAnnotationsAsync(
        CalendarAnnotationSourceRequest request,
        CancellationToken cancellationToken)
    {
        string? icsContent = ReadEmbeddedIcs(request.CountryCode);

        if (icsContent is null)
        {
            return Task.FromResult(new CalendarAnnotationSourceResult(false, []));
        }

        IReadOnlyList<CalendarAnnotation> annotations = IcsCalendarAnnotationSource.Parse(
            icsContent,
            request.StartDate,
            request.EndDate);

        return Task.FromResult(new CalendarAnnotationSourceResult(true, annotations));
    }

    internal static string? ReadEmbeddedIcs(string countryCode)
    {
        string normalizedCode = countryCode.Trim().ToUpperInvariant();
        string resourceName = $"Vitarish.Interfaces.Base.Resources.CalendarData.{normalizedCode}.ics";

        using Stream? stream = HostAssembly.GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            return null;
        }

        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
