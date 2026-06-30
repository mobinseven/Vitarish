using Microsoft.AspNetCore.Routing;
using Vitarish.Authentication.Api.DependencyInjection;

namespace Vitarish.Authentication.Module.DependencyInjection;

public static class AuthenticationModuleEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAuthenticationModuleEndpoints(this IEndpointRouteBuilder endpoints) => endpoints.MapAuthenticationApi();
}
