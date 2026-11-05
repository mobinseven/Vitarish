using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Vitarish.Contents.Contracts.Routes;

namespace Vitarish.Contents.Api;

internal static class PublicContentResponse
{
    public static IResult Create<T>(HttpContext context, T value, JsonTypeInfo<T> typeInfo)
    {
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(value, typeInfo);
        string etag = '"' + Convert.ToHexString(SHA256.HashData(body)) + '"';
        context.Response.Headers.ETag = etag;
        context.Response.Headers.CacheControl = "public, max-age=60";
        context.Response.Headers.Vary = $"{HeaderNames.Host}, {HeaderNames.Origin}, {HeaderNames.Referer}, {ContentsPublicDelivery.TenantHostHeader}";

        bool matches = context.Request.Headers.IfNoneMatch
            .SelectMany(header => (header ?? string.Empty).Split(','))
            .Select(tag => tag.Trim())
            .Any(tag => tag == "*" || string.Equals(tag.StartsWith("W/", StringComparison.Ordinal) ? tag[2..] : tag,
                etag, StringComparison.Ordinal));
        return matches ? Results.StatusCode(StatusCodes.Status304NotModified) : Results.Bytes(body, "application/json; charset=utf-8");
    }
}
