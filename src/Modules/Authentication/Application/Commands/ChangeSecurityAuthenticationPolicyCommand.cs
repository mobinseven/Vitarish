namespace Vitarish.Authentication.Application.Commands;

public sealed record ChangeSecurityAuthenticationPolicyCommand(
    int MinimumPasswordLength,
    bool RequirePasswordLowercase,
    bool RequirePasswordUppercase,
    bool RequirePasswordNumber,
    bool RequirePasswordSpecialCharacter,
    int FailedAttemptThreshold,
    int FailedAttemptDenialDurationSeconds,
    string FailedAttemptResponse,
    IReadOnlyList<string> EnabledAuthenticationMechanisms,
    IReadOnlyList<string> PermittedPreAuthenticationActions,
    long ExpectedVersion);
