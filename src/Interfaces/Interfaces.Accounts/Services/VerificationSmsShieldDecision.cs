namespace Vitarish.Interfaces.Accounts.Services;

public sealed record VerificationSmsShieldDecision(bool CanSendSms, TimeSpan RemainingCooldown)
{
    public static VerificationSmsShieldDecision Allowed { get; } = new(true, TimeSpan.Zero);

    public static VerificationSmsShieldDecision Shielded(TimeSpan remainingCooldown) =>
        new(false, remainingCooldown < TimeSpan.Zero ? TimeSpan.Zero : remainingCooldown);
}
