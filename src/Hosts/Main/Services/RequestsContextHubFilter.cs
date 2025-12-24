using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.SignalR;
using Vitarish.Main.Extensions;

namespace Vitarish.Main.Services
{
    public class RequestsContextHubFilter : IHubFilter
    {
        public async ValueTask<object?> InvokeMethodAsync(
            HubInvocationContext invocationContext,
            Func<HubInvocationContext, ValueTask<object?>> next)
        {
            IHttpContextFeature? httpConnectionFeature = invocationContext
                .Context
                .Features
                .Get<IHttpContextFeature>();

            HttpContext? httpContext = httpConnectionFeature?.HttpContext;
            if (httpContext != null)
            {
                if (httpContext.User.Identity?.IsAuthenticated ?? false)
                {
                    IRequestContextWriter writer = invocationContext.ServiceProvider.GetRequiredService<IRequestContextWriter>();
                    await RequestsContextExtensions.PopulateContext(httpContext, writer);
                }
            }
            return await next(invocationContext);
        }
    }
}
