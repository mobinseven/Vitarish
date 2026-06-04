namespace Vitarish.Interfaces.Accounts.Services;

public sealed class VerificationSmsShieldOptions
{
    public const string SectionName = "Vitarish:Accounts:VerificationSmsShield";

    [Range(1, int.MaxValue)]
    public int VerificationSmsCooldownSeconds { get; set; } = 60;

    [Range(1, int.MaxValue)]
    public int VerificationSmsRecentRequestCleanupIntervalSeconds { get; set; } = 30;

    [Range(1, int.MaxValue)]
    public int? VerificationSmsMaximumRecentRequestCount { get; set; }

    public TimeSpan Cooldown => TimeSpan.FromSeconds(VerificationSmsCooldownSeconds);

    public TimeSpan CleanupInterval => TimeSpan.FromSeconds(VerificationSmsRecentRequestCleanupIntervalSeconds);
}
