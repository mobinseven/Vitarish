namespace Vitarish.Audit.Application;

public sealed class SecurityAuditExportLimitException : Exception
{
    public SecurityAuditExportLimitException()
        : base("The requested security audit export exceeds the supported row limit.")
    {
    }
}
