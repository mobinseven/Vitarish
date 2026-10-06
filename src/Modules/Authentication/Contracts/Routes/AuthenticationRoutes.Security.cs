namespace Vitarish.Authentication.Contracts.Routes;

public static partial class AuthenticationRoutes
{
    public const string AccessHistory = "/access-history";
    public const string AccessHistoryAcknowledge = AccessHistory + "/acknowledge";
    public const string SessionNotices = "/session/notices";
    public const string SessionNoticeAcknowledge = SessionNotices + "/acknowledge";
    public const string SecuritySessionPolicy = "/security/session-policy";
    public const string SecurityAuthenticationPolicy = "/security/authentication-policy";
    public const string SecurityUserInactivityPolicy = "/security/users/{userId}/inactivity-timeout";
    public const string SecurityUserSessionsRevoke = "/security/users/{userId}/sessions/revoke";
}
