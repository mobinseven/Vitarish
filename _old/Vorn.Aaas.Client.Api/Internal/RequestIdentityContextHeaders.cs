using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Net.Http.Headers;
using Vorn.Aaas.Client.Api.Abstractions;

namespace Vorn.Aaas.Client.Api.Internal;

internal static class RequestIdentityContextHeaders
{
    internal const string OwnerId = "x-vorn-owner-id";
    internal const string TenantId = "x-vorn-tenant-id";
    internal const string RequestId = "x-request-id";

    internal static void Apply(HttpRequestHeaders headers, IRequestIdentityContext context)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(context);

        SetHeader(headers, OwnerId, context.OwnerId);
        SetHeader(headers, TenantId, context.TenantId);
        SetHeader(headers, RequestId, context.RequestId);
    }

    internal static string? GetOwnerId(IHeaderDictionary headers) => GetHeader(headers, OwnerId);
    internal static string? GetTenantId(IHeaderDictionary headers) => GetHeader(headers, TenantId);
    internal static string? GetRequestId(IHeaderDictionary headers) => GetHeader(headers, RequestId);

    private static void SetHeader(HttpRequestHeaders headers, string name, string? value)
    {
        _ = headers.Remove(name);
        if (!string.IsNullOrWhiteSpace(value))
        {
            _ = headers.TryAddWithoutValidation(name, value);
        }
    }

    private static string? GetHeader(IHeaderDictionary headers, string name) =>
        headers.TryGetValue(name, out StringValues values) ? values.FirstOrDefault() : null;
}
