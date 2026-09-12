namespace Vitarish.Audit.Domain;

public static class SecurityAuditRedaction
{
    public const string Replacement = "redacted";

    public static string? Apply(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return null;

        string trimmed = reason.Trim();
        if (trimmed.Length > 256)
            return Replacement;

        return SecurityAuditReasonCatalog.IsAllowed(trimmed) ? trimmed : Replacement;
    }
}
