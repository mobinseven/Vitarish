using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Vitarish.Modules.Base.Core.Endpoints;

public static class InternalServiceEndpointConventions
{
    public const string CredentialHeaderName = "X-Vitarish-Agent-Credential";

    public static RouteGroupBuilder MapInternalServiceModuleGroup(this IEndpointRouteBuilder endpoints, string prefix)
    {
        RouteGroupBuilder group = endpoints.MapGroup(prefix);
        group.AllowAnonymous();
        group.WithMetadata(new InternalServiceEndpointMetadata());
        group.WithGroupName(prefix);
        return group;
    }
}

public sealed record InternalServiceEndpointMetadata;
