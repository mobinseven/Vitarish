using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Requests.Application.Abstractions;
public static class MultitenantHelpers
{
    public static Task<string?> ResolveTenantAsync(HttpContext context)
    {
        if (context is null)
        {
            return Task.FromResult<string?>(null);
        }

        IRequestContext ric = context.RequestServices.GetRequiredService<IRequestContext>();

        return !string.IsNullOrEmpty(ric.TenantId) ? Task.FromResult<string?>(ric.TenantId) : !string.IsNullOrEmpty(ric.OwnerId) ? Task.FromResult<string?>(ric.OwnerId) : Task.FromResult<string?>(ric.ClientId);
    }
}
