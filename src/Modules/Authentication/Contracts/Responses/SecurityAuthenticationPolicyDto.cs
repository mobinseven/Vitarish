namespace Vitarish.Authentication.Contracts.Responses;

public sealed record SecurityAuthenticationPolicyDto(
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
    IReadOnlyList<string> AvailableAuthenticationMechanisms,
    IReadOnlyList<string> RequiredAuthenticationMechanisms,
    IReadOnlyList<string> AvailablePreAuthenticationActions,
    IReadOnlyList<string> RequiredPreAuthenticationActions,
    IReadOnlyList<string> AvailableFailedAttemptResponses,
    int MandatoryPasswordMinimumLength,
    long Version);
