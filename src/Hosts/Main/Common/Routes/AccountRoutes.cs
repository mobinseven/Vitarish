using Microsoft.AspNetCore.WebUtilities;

namespace Vitarish.Main.Common;

public static partial class AppRoutes
{
    public static class Account
    {
        public const string Login = "/account/login";
        public const string LoginPhone = "/account/login/phone";
        public const string LoginEmail = "/account/login/email";
        public const string LoginPassword = "/account/login/password";
        public const string LoginPasskey = "/account/login/passkey";
        public const string Logout = "/account/logout";
        public const string Unauthorized = "/account/unauthorized";

        public const string Settings = "/account/settings";
        public const string SettingsName = "/account/settings/name";
        public const string SettingsEmail = "/account/settings/email";
        public const string SettingsPhone = "/account/settings/phone";
        public const string SettingsPassword = "/account/settings/password";
        public const string SettingsPasskeys = "/account/settings/passkeys";
        public const string SettingsAppearance = "/account/settings/appearance";

        public static string UnauthorizedWithRequestedPath(string? requestedPath) =>
            string.IsNullOrWhiteSpace(requestedPath)
                ? Unauthorized
                : QueryHelpers.AddQueryString(Unauthorized, QueryKeys.RequestedPath, requestedPath);
    }
}
