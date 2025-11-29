using Microsoft.Extensions.Primitives;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Aaas.Client.Api.HubFilter;
using Vorn.Aaas.Client.Api.Internal;
using static System.Net.WebRequestMethods;

internal static class MultitenantHelpers
{
    public static Task<string?> ResolveTenantAsync(HttpContext context)
    {
        if(context is null)
        {
            return Task.FromResult<string?>(null);
        }

        using IDisposable? scope = RequestIdentityContextInitializer.Initialize<RequestIdentityContextHubFilter>(
            context,
            context.User);
        var ric = context.RequestServices.GetRequiredService<IRequestIdentityContext>();

        if(!string.IsNullOrEmpty(ric.OwnerId))
        {
            return Task.FromResult<string?>(ric.OwnerId);
        }
        return Task.FromResult<string?>(null);
    }
}