namespace Vitarish.Clients.Interface.Identity.Extensions
{
    internal static class UserAccessAuthorizationHelpers
    {

        public static string BuildAuthenticationCacheKey(string userId, string r) =>
            $"UserAccess:{userId}:{r}";
    }
}
