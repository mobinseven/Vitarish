namespace Vitarish.Calendar.Infrastructure.Radicale;

public class RadicaleNotFoundException(string resourceUrl)
    : Exception($"Resource not found: '{resourceUrl}' (HTTP 404).");
