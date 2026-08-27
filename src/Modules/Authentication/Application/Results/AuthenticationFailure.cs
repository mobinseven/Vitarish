using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Application.Results;

public sealed record AuthenticationFailure(AuthenticationFailureReason Reason, string Message, string? ChallengeId = null);
