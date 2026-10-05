using Vitarish.Authentication.Domain.Sessions;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Application.Abstractions;

public interface IAuthenticationSessionRegistry
{
    Task<IAuthenticationSessionPreparation> PrepareAsync(string userId, CancellationToken cancellationToken);
    Task<AuthenticationSessionEstablishmentResult> EstablishAsync(string userId, CancellationToken cancellationToken);
    Task RecordFailedAttemptAsync(string? userId, CancellationToken cancellationToken);
    Task<AuthenticationSessionValidationResult> ValidateAndTouchAsync(string sessionId, string userId, string? tenantId, CancellationToken cancellationToken);
    Task RevokeAsync(string sessionId, AuthenticationSessionTerminationReason reason, CancellationToken cancellationToken);
    Task<AuthenticationAccessHistory?> GetAccessHistoryAsync(string userId, string? tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuthenticationSessionNotice>> GetNoticesAsync(string sessionId, string userId, CancellationToken cancellationToken);
    Task<bool> AcknowledgeNoticeAsync(string noticeId, string sessionId, string userId, CancellationToken cancellationToken);
    Task<AuthenticationAccessHistory?> AcknowledgeAccessHistoryAsync(string userId, string? tenantId, CancellationToken cancellationToken);
    Task<int?> GetUserInactivityTimeoutAsync(string userId, CancellationToken cancellationToken);
    Task<(bool Exists, int? Minutes, long Version)> GetUserInactivityTimeoutStateAsync(string userId, CancellationToken cancellationToken);
    Task<long> SetUserInactivityTimeoutAsync(string userId, int? minutes, long expectedVersion, CancellationToken cancellationToken);
    Task RevokeSessionsExpiredByUserPolicyAsync(string userId, CancellationToken cancellationToken, int? inactivityTimeoutOverride = null);
    Task<int> RevokeActiveSessionsAsync(string userId, CancellationToken cancellationToken);
    Task<SecurityAccessPolicyState> SavePolicyAndRevokeInvalidatedSessionsAsync(SecurityAccessPolicy policy, long expectedVersion, CancellationToken cancellationToken);
}

public interface IAuthenticationSessionPreparation : IAsyncDisposable
{
    AuthenticationSessionEstablishmentResult Result { get; }
    Task CompleteAsync(CancellationToken cancellationToken);
    Task AbortAsync(CancellationToken cancellationToken);
}

public sealed record AuthenticationSessionEstablishmentResult(
    bool Succeeded,
    string? SessionId,
    AuthenticationFailure? Failure,
    AuthenticationAccessHistory? AccessHistory,
    int? InactivityTimeoutMinutes)
{
    public static AuthenticationSessionEstablishmentResult Success(string sessionId, AuthenticationAccessHistory history, int inactivityTimeoutMinutes) => new(true, sessionId, null, history, inactivityTimeoutMinutes);
    public static AuthenticationSessionEstablishmentResult Failed(AuthenticationFailure failure) => new(false, null, failure, null, null);
}

public sealed record AuthenticationSessionValidationResult(bool IsValid, int? InactivityTimeoutMinutes = null);
