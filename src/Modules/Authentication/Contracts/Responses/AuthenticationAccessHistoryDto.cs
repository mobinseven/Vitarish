namespace Vitarish.Authentication.Contracts.Responses;

public sealed record AuthenticationAccessHistoryDto(
    DateTime? PreviousSuccessfulAt,
    DateTime? LastSuccessfulAt,
    DateTime? LastUnsuccessfulAt,
    int FailedAttemptsSincePreviousSuccessful,
    int CurrentFailedAttempts,
    DateTime? AcknowledgedAt,
    string? PreviousSuccessfulIpAddress,
    string? LastSuccessfulIpAddress,
    string? LastUnsuccessfulIpAddress);
