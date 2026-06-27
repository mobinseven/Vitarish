using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Security.Claims;
using Vitarish.Requests.Contracts;

namespace Vitarish.Requests.Client;

public sealed class HttpContextRequestContextAccessor(
    IHttpContextAccessor httpContextAccessor,
    IRequestContextResolver resolver) : IRequestContextAccessor
{
    private static readonly object CacheKey = new();
    private static readonly IReadOnlyDictionary<string, string> EmptyAdditionalClaims = new Dictionary<string, string>(0);

    public string Authority => Current?.Authority ?? string.Empty;

    public string? ClientId => Current?.ClientId;

    public string? OwnerId => Current?.OwnerId;

    public string? TenantId => Current?.TenantId;

    public string? UserId => Current?.UserId;

    public string? ResourceId => Current?.ResourceId;

    public string? TraceId => Current?.TraceId;

    public string? RequestId => Current?.RequestId;

    public IReadOnlyDictionary<string, string> AdditionalClaims => Current?.AdditionalClaims ?? EmptyAdditionalClaims;

    public ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    private RequestContextDto? Current => GetCurrent().Context;

    public RequestContextResult GetCurrent()
    {
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return new RequestContextResult(
                null,
                new RequestContextFailure(
                    RequestContextFailureKind.Unauthenticated,
                    "No current HTTP context is available."));
        }

        if (httpContext.Items.TryGetValue(CacheKey, out object? cached) && cached is RequestContextResult cachedResult)
        {
            return cachedResult;
        }

        RequestContextResult result = resolver.Resolve(httpContext.User);
        httpContext.Items[CacheKey] = result;
        return result;
    }
}
