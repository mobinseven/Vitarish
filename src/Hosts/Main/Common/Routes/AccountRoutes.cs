using Microsoft.AspNetCore.WebUtilities;

namespace Vitarish.Main.Common;

public static partial class AppRoutes
{
    public static class Account
    {
        /// <summary>
        /// Pages starting with `/account` are reserved for authentication related tasks.
        /// </summary>
        public const string Login = "/account/login";
        public const string LoginPhone = "/account/login/phone";
        public const string LoginEmail = "/account/login/email";
        public const string LoginPassword = "/account/login/password";
        public const string LoginPasskey = "/account/login/passkey";
        public const string Logout = "/account/logout";
        public const string Unauthorized = "/account/unauthorized";

        public const string Settings = "/user/settings";
        public const string SettingsName = "/user/settings/name";
        public const string SettingsEmail = "/user/settings/email";
        public const string SettingsPhone = "/user/settings/phone";
        public const string SettingsPassword = "/user/settings/password";
        public const string SettingsPasskeys = "/user/settings/passkeys";
        public const string SettingsAppearance = "/user/settings/appearance";

        public static string UnauthorizedWithRequestedPath(string? requestedPath) =>
            string.IsNullOrWhiteSpace(requestedPath)
                ? Unauthorized
                : QueryHelpers.AddQueryString(Unauthorized, QueryKeys.RequestedPath, requestedPath);
    }
}
