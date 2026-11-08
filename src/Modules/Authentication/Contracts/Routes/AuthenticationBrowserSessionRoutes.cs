namespace Vitarish.Authentication.Contracts.Routes;

public static partial class AuthenticationRoutes
{
    public const string BrowserSessionsPath = "/browser-sessions";
    public const string BrowserSessionRevokePath = BrowserSessionsPath + "/revoke";
}

public static class AuthenticationBrowserSessionOperationIds
{
    public const string Get = "Authentication_BrowserSessions_Get";
    public const string Revoke = "Authentication_BrowserSession_Revoke";
}
