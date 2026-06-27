using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Modules.Base.Endpoints;
using Vitarish.Social.Contracts;

namespace Vitarish.Social.Api;

public static class SocialApi
{
    public static IEndpointRouteBuilder MapSocialApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(SocialApiRoutes.Base);
        _ = group.MapModuleGet(SocialApiRoutes.Connections, (CancellationToken _) => Results.Ok(Array.Empty<SocialConnection>()), "SocialListConnections", typeof(SocialConnection[]));
        _ = group.MapModulePost(SocialApiRoutes.ReplayWebhook, (Guid id, CancellationToken _) => Results.NoContent(), "SocialReplayWebhook", successStatusCode: StatusCodes.Status204NoContent);
        return endpoints;
    }
}
