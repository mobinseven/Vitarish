using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.SignalR;

namespace Vitarish.Main.Services
{
    public class RequestsContextHubFilter : IHubFilter
    {
        public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object>> next)
        {
            IRequestContextWriter writer = invocationContext.ServiceProvider.GetRequiredService<IRequestContextWriter>();
            IHttpContextFeature? httpConnectionFeature = invocationContext
                .Context
                .Features
                .Get<IHttpContextFeature>();

            HttpContext? httpContext = httpConnectionFeature?.HttpContext;
            if (httpContext != null)
            {
                RequestsContextExtensions.PopulateContext(httpContext, writer);
            }
            return await next(invocationContext);
        }
    }
}
