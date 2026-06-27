using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Vitarish.Modules.Base.Endpoints;

public static class ModuleEndpointMappingExtensions
{
    public static RouteHandlerBuilder MapModuleGet(this RouteGroupBuilder group, string pattern, Delegate handler, string operationId, Type? successType = null, int successStatusCode = 200, ModuleEndpointProfile profile = ModuleEndpointProfile.ProtectedJson, string? anonymousJustification = null, string? contentType = null) =>
        group.MapGet(pattern, handler).ApplyModuleOpenApiConvention(new(operationId, successStatusCode, successType, profile, anonymousJustification, contentType));

    public static RouteHandlerBuilder MapModulePost(this RouteGroupBuilder group, string pattern, Delegate handler, string operationId, Type? successType = null, int successStatusCode = 200, ModuleEndpointProfile profile = ModuleEndpointProfile.ProtectedJson, string? anonymousJustification = null, string? contentType = null) =>
        group.MapPost(pattern, handler).ApplyModuleOpenApiConvention(new(operationId, successStatusCode, successType, profile, anonymousJustification, contentType));

    public static RouteHandlerBuilder MapModulePut(this RouteGroupBuilder group, string pattern, Delegate handler, string operationId, Type? successType = null, int successStatusCode = 200, ModuleEndpointProfile profile = ModuleEndpointProfile.ProtectedJson, string? anonymousJustification = null, string? contentType = null) =>
        group.MapPut(pattern, handler).ApplyModuleOpenApiConvention(new(operationId, successStatusCode, successType, profile, anonymousJustification, contentType));

    public static RouteHandlerBuilder MapModuleDelete(this RouteGroupBuilder group, string pattern, Delegate handler, string operationId, Type? successType = null, int successStatusCode = 204, ModuleEndpointProfile profile = ModuleEndpointProfile.ProtectedJson, string? anonymousJustification = null, string? contentType = null) =>
        group.MapDelete(pattern, handler).ApplyModuleOpenApiConvention(new(operationId, successStatusCode, successType, profile, anonymousJustification, contentType));

    public static RouteHandlerBuilder MapModulePatch(this RouteGroupBuilder group, string pattern, Delegate handler, string operationId, Type? successType = null, int successStatusCode = 200, ModuleEndpointProfile profile = ModuleEndpointProfile.ProtectedJson, string? anonymousJustification = null, string? contentType = null) =>
        group.MapPatch(pattern, handler).ApplyModuleOpenApiConvention(new(operationId, successStatusCode, successType, profile, anonymousJustification, contentType));

    public static RouteHandlerBuilder MapModuleHead(this RouteGroupBuilder group, string pattern, Delegate handler, string operationId, Type? successType = null, int successStatusCode = 200, ModuleEndpointProfile profile = ModuleEndpointProfile.BinaryDownload, string? contentType = null) =>
        group.MapMethods(pattern, ["HEAD"], handler).ApplyModuleOpenApiConvention(new(operationId, successStatusCode, successType, profile, null, contentType));
}
