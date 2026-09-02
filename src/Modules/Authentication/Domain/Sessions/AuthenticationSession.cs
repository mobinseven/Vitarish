namespace Vitarish.Authentication.Domain.Sessions;

public enum AuthenticationSessionTerminationReason
{
    UserLogout,
    ReplacedByNewSession,
    InactivityTimeout,
    SecurityAttributeChanged,
    CookieSignInFailed,
    AdministrativeRevocation
}

public sealed class AuthenticationSession
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public string? ClientIpAddress { get; set; }
    public DateTime? TerminatedAt { get; set; }
    public AuthenticationSessionTerminationReason? TerminationReason { get; set; }
}

public sealed class AuthenticationSessionNotice
{
    public const string ParallelSessionEstablishedNoticeType = "parallel_session_established";

    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string NoticeType { get; set; } = string.Empty;
    public DateTime? AcknowledgedAt { get; set; }
}

public sealed class AuthenticationAccessHistory
{
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public DateTime? PreviousSuccessfulAt { get; set; }
    public DateTime? LastSuccessfulAt { get; set; }
    public DateTime? LastUnsuccessfulAt { get; set; }
    public int FailedAttemptsSincePreviousSuccessful { get; set; }
    public int CurrentFailedAttempts { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
}
