namespace Vorn.Aaas.Client.Api.Internal;

using System;
using System.Linq;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Vorn.Aaas.Client.Api.Abstractions;

internal static class RequestIdentityContextHeaders
{
    internal const string OwnerId = "x-vorn-owner-id";
    internal const string TenantId = "x-vorn-tenant-id";
    internal const string RequestId = "x-request-id";

    internal static void Apply(HttpRequestHeaders headers, IRequestIdentityContext context, string? ownerOverride = null)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(context);

        string? ownerId = SelectOwnerId(ownerOverride, context);
        string? tenantId = SelectTenantId(context, ownerId);

        SetHeader(headers, OwnerId, ownerId);
        SetHeader(headers, TenantId, tenantId);
        SetHeader(headers, RequestId, context.RequestId);
    }

    internal static string? GetOwnerId(IHeaderDictionary headers) => GetHeader(headers, OwnerId);
    internal static string? GetTenantId(IHeaderDictionary headers) => GetHeader(headers, TenantId);
    internal static string? GetRequestId(IHeaderDictionary headers) => GetHeader(headers, RequestId);

    private static string? SelectOwnerId(string? ownerOverride, IRequestIdentityContext context)
    {
        if(!string.IsNullOrWhiteSpace(ownerOverride))
        {
            return ownerOverride;
        }

        if(!string.IsNullOrWhiteSpace(context.OwnerId))
        {
            return context.OwnerId;
        }

        return context.ClientId;
    }

    private static string? SelectTenantId(IRequestIdentityContext context, string? ownerId)
    {
        if(!string.IsNullOrWhiteSpace(context.TenantId))
        {
            return context.TenantId;
        }

        return string.IsNullOrWhiteSpace(ownerId) ? null : $"{context.ClientId}.{ownerId}";
    }

    private static void SetHeader(HttpRequestHeaders headers, string name, string? value)
    {
        headers.Remove(name);
        if(!string.IsNullOrWhiteSpace(value))
        {
            headers.TryAddWithoutValidation(name, value);
        }
    }

    private static string? GetHeader(IHeaderDictionary headers, string name) =>
        headers.TryGetValue(name, out StringValues values) ? values.FirstOrDefault() : null;
}
