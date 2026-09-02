namespace Vitarish.Authentication.Contracts.Requests;

public sealed record ChangeUserInactivityTimeoutRequest(int? InactivityTimeoutMinutes, long ExpectedVersion);
