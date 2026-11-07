using Microsoft.AspNetCore.Http;
using Vitarish.Modules.Base.Core.Identity;

namespace Vitarish.Landing.Api;

public static class LandingRateLimitPolicies
{
    public const string PublicConversation = "landing-public-conversation";
    public const string PublicCatalog = "landing-public-catalog";
    public const string AuthenticatedVisitor = "landing-authenticated-visitor";

    public static string AuthenticatedVisitorPartitionKey(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return AuthenticatedUserIdResolver.Resolve(context.User) is AuthenticatedUserIdResolution.Success user
            ? user.UserId.Value
            : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
