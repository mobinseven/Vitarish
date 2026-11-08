namespace Vitarish.Authentication.Domain.ReturnUrls;

public static class AuthenticationBrowserPaths
{
    public const string Login = "/login";
    public const string Continue = "/continue";
    public const string Handoff = "/handoff";
    public const string Account = "/account";
}

public static class AuthenticationBrowserQuery
{
    public const string ReturnUrl = "returnUrl";
    public const string Request = "request";
}
