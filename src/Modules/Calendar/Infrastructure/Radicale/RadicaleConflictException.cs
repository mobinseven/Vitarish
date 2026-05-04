namespace Vitarish.Calendar.Infrastructure.Radicale;

public class RadicaleConflictException(string resourceUrl)
    : Exception($"Precondition failed for resource '{resourceUrl}' (HTTP 412). The ETag did not match.");
