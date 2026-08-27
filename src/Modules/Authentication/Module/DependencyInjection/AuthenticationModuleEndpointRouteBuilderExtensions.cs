using Microsoft.AspNetCore.Routing;
using Vitarish.Authentication.Api.DependencyInjection;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Module.DependencyInjection;

public static class AuthenticationModuleEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAuthenticationModuleEndpoints(this IEndpointRouteBuilder endpoints, AuthenticationProfilePolicy profilePolicy) => endpoints.MapAuthenticationApi(profilePolicy);
}
