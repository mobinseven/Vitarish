using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Vitarish.Main.Common;
using Vitarish.Main.Infrastructure.Options;

namespace Vitarish.Main.Infrastructure.Links;

public static class LinkEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapLinkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        _ = endpoints.MapGet(AppRoutes.Links.QrTemplate, GetQrPngAsync).AllowAnonymous();
        return endpoints;
    }

    private static IResult GetQrPngAsync(
        string code,
        HttpContext httpContext,
        IQrCodePngGenerator qrCodePngGenerator,
        IOptions<FeaturesOptions> featuresOptions)
    {
        if (!featuresOptions.Value.QrLinks)
        {
            return Results.Redirect(AppRoutes.Home);
        }

        if (!IsValidCode(code))
        {
            return Results.NotFound();
        }

        string shortPath = AppRoutes.Links.Short(code);
        string payload = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.PathBase}{shortPath}";

        byte[] pngBytes = qrCodePngGenerator.Generate(payload);

        httpContext.Response.Headers.CacheControl = "public,max-age=300";
        return Results.File(pngBytes, contentType: "image/png");
    }

    private static bool IsValidCode(string code) =>
        !string.IsNullOrWhiteSpace(code)
        && code.Length <= 256
        && code.All(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.' or '~');
}
