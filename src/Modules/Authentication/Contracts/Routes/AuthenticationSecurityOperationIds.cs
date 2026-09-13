namespace Vitarish.Authentication.Contracts.Routes;

public static class AuthenticationSecurityOperationIds
{
    public const string AccessHistoryGet = "Authentication_AccessHistory_Get";
    public const string AccessHistoryAcknowledge = "Authentication_AccessHistory_Acknowledge";
    public const string SessionNoticesGet = "Authentication_SessionNotices_Get";
    public const string SessionNoticeAcknowledge = "Authentication_SessionNotice_Acknowledge";
    public const string SessionPolicyGet = "Authentication_SessionPolicy_Get";
    public const string SessionPolicyPut = "Authentication_SessionPolicy_Put";
    public const string UserInactivityPolicyPut = "Authentication_UserInactivityPolicy_Put";
}
