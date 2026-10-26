namespace Vitarish.Audit.Server;

public sealed class SecurityAuditOptions
{
    public const string SectionName = "Vitarish:Security:Audit";
    public const string CapacityWarningBytesKey = SectionName + ":CapacityWarningBytes";

    public static readonly TimeSpan DefaultDeniedReviewDedupeWindow = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan DefaultDeniedReviewWriteTimeout = TimeSpan.FromSeconds(2);

    public long CapacityWarningBytes { get; init; }
    public TimeSpan DeniedReviewDedupeWindow { get; init; } = DefaultDeniedReviewDedupeWindow;
    public TimeSpan DeniedReviewWriteTimeout { get; init; } = DefaultDeniedReviewWriteTimeout;
}
