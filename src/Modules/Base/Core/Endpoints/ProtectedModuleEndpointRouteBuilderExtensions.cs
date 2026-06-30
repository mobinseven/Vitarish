using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Vitarish.Modules.Base.Core.Endpoints;

public static class ProtectedModuleEndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapProtectedModuleGroup(this IEndpointRouteBuilder endpoints, string prefix, string scheme = ModuleEndpointAuthenticationSchemes.BrowserOrBearer)
    {
        RouteGroupBuilder group = endpoints.MapGroup(prefix);
        group.RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = scheme });
        group.WithMetadata(new ProtectedModuleEndpointMetadata(scheme));
        group.WithGroupName(NormalizeGroupName(prefix));
        group.WithTags(NormalizeTag(prefix));
        return group;
    }

    public static RouteGroupBuilder MapAnonymousModuleGroup(this IEndpointRouteBuilder endpoints, string prefix, string justification)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(justification);

        RouteGroupBuilder group = endpoints.MapGroup(prefix);
        group.AllowAnonymous();
        group.WithMetadata(new AnonymousModuleEndpointMetadata(justification));
        group.WithGroupName(NormalizeGroupName(prefix));
        group.WithTags(NormalizeTag(prefix));
        return group;
    }

    private static string NormalizeGroupName(string prefix) => NormalizeTag(prefix);

    private static string NormalizeTag(string prefix)
    {
        string[] parts = prefix.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string[] skip = ["api", "modules", "webhooks"];
        string candidate = parts.LastOrDefault(part =>
            !skip.Contains(part, StringComparer.OrdinalIgnoreCase)) ?? "Platform";
        return string.Concat(candidate.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }
}
