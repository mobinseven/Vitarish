namespace Vitarish.Audit.Domain;

public static class SecurityAuditExportPolicy
{
    public const int MaximumRows = 10_000;
    public const int ProbeRows = MaximumRows + 1;
}
