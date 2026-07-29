using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Vitarish.Modules.Base.Core.Endpoints;

public static class ModuleEndpointMappingExtensions
{
    public static RouteHandlerBuilder MapModuleGet(this RouteGroupBuilder group, string pattern, Delegate handler, string operationId, Type? successType = null, int successStatusCode = 200, ModuleEndpointProfile profile = ModuleEndpointProfile.ProtectedJson, string? anonymousJustification = null, string? contentType = null, bool includeStandardErrors = true, bool deprecated = false) =>
        group.MapGet(pattern, handler).ApplyModuleOpenApiConvention(new(operationId, successStatusCode, successType, profile, anonymousJustification, contentType, includeStandardErrors, deprecated));

    public static RouteHandlerBuilder MapModulePost(this RouteGroupBuilder group, string pattern, Delegate handler, string operationId, Type? successType = null, int successStatusCode = 200, ModuleEndpointProfile profile = ModuleEndpointProfile.ProtectedJson, string? anonymousJustification = null, string? contentType = null, bool includeStandardErrors = true) =>
        group.MapPost(pattern, handler).ApplyUnsafeModuleOpenApiConvention(new(operationId, successStatusCode, successType, profile, anonymousJustification, contentType, includeStandardErrors));

    public static RouteHandlerBuilder MapModulePut(this RouteGroupBuilder group, string pattern, Delegate handler, string operationId, Type? successType = null, int successStatusCode = 200, ModuleEndpointProfile profile = ModuleEndpointProfile.ProtectedJson, string? anonymousJustification = null, string? contentType = null, bool includeStandardErrors = true) =>
        group.MapPut(pattern, handler).ApplyUnsafeModuleOpenApiConvention(new(operationId, successStatusCode, successType, profile, anonymousJustification, contentType, includeStandardErrors));

    public static RouteHandlerBuilder MapModuleDelete(this RouteGroupBuilder group, string pattern, Delegate handler, string operationId, Type? successType = null, int successStatusCode = 204, ModuleEndpointProfile profile = ModuleEndpointProfile.ProtectedJson, string? anonymousJustification = null, string? contentType = null, bool includeStandardErrors = true) =>
        group.MapDelete(pattern, handler).ApplyUnsafeModuleOpenApiConvention(new(operationId, successStatusCode, successType, profile, anonymousJustification, contentType, includeStandardErrors));

    public static RouteHandlerBuilder MapModulePatch(this RouteGroupBuilder group, string pattern, Delegate handler, string operationId, Type? successType = null, int successStatusCode = 200, ModuleEndpointProfile profile = ModuleEndpointProfile.ProtectedJson, string? anonymousJustification = null, string? contentType = null, bool includeStandardErrors = true) =>
        group.MapPatch(pattern, handler).ApplyUnsafeModuleOpenApiConvention(new(operationId, successStatusCode, successType, profile, anonymousJustification, contentType, includeStandardErrors));

    public static RouteHandlerBuilder MapModuleHead(this RouteGroupBuilder group, string pattern, Delegate handler, string operationId, Type? successType = null, int successStatusCode = 200, ModuleEndpointProfile profile = ModuleEndpointProfile.BinaryDownload, string? contentType = null, bool includeStandardErrors = true) =>
        group.MapMethods(pattern, ["HEAD"], handler).ApplyModuleOpenApiConvention(new(operationId, successStatusCode, successType, profile, null, contentType, includeStandardErrors));

    private static RouteHandlerBuilder ApplyUnsafeModuleOpenApiConvention(this RouteHandlerBuilder builder, ModuleEndpointOpenApiConvention convention)
    {
        _ = builder.ApplyModuleOpenApiConvention(convention);

        if (convention.Profile is not ModuleEndpointProfile.AnonymousWebhook
            and not ModuleEndpointProfile.AnonymousJson)
        {
            _ = builder.WithMetadata(new ModuleEndpointAntiforgeryMetadata());
        }

        return builder;
    }
}
