namespace Vitarish.Authentication.Contracts.Responses;

public sealed record UserInactivityTimeoutDto(long Version, int? InactivityTimeoutMinutes);
