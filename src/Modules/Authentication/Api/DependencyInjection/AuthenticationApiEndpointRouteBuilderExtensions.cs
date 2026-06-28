using Microsoft.AspNetCore.Builder;
using Vitarish.Authentication.Api.Endpoints;
using Vitarish.Authentication.Contracts.Routes;

namespace Vitarish.Authentication.Api.DependencyInjection;

public static class AuthenticationApiEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAuthenticationApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup(AuthenticationRoutes.BasePath).WithTags(AuthenticationEndpointTags.Authentication);
        group.MapAntiforgeryEndpoints(); group.MapCurrentSessionEndpoints(); group.MapPasswordLoginEndpoints(); group.MapPhoneLoginEndpoints(); group.MapEmailLoginEndpoints(); group.MapPasskeyLoginEndpoints(); group.MapLogoutEndpoints();
        return endpoints;
    }
}
