namespace Vitarish.Calendar.Infrastructure.Radicale;

public class RadicaleAuthException(string resourceUrl, int statusCode)
    : Exception($"Authentication/Authorization failed for resource '{resourceUrl}' (HTTP {statusCode}).");
