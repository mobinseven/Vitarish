namespace Vitarish.Audit.Server;

public sealed class SecurityAuditOptions
{
    public const string SectionName = "Vitarish:Security:Audit";
    public const string CapacityWarningBytesKey = SectionName + ":CapacityWarningBytes";

    public long CapacityWarningBytes { get; init; }
}
