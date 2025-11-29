
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Vorn.Aaas.Client.Api.Internal;

namespace Vorn.Aaas.Client.Api.HubFilter;

public sealed class RequestIdentityContextHubFilter : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        HttpContext? http = invocationContext.Context.GetHttpContext();
        if (http is null)
        {
            return await next(invocationContext);
        }

        using IDisposable? scope = RequestIdentityContextInitializer.Initialize<RequestIdentityContextHubFilter>(
            http,
            invocationContext.Context.User);

        return await next(invocationContext);
    }

    public async ValueTask OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, ValueTask> next)
    {
        HttpContext? http = context.Context.GetHttpContext();
        if (http is null)
        {
            await next(context);
            return;
        }

        using IDisposable? scope = RequestIdentityContextInitializer.Initialize<RequestIdentityContextHubFilter>(
            http,
            context.Context.User);

        await next(context);
    }

    public async ValueTask OnDisconnectedAsync(
        HubLifetimeContext context,
        Exception? exception,
        Func<HubLifetimeContext, Exception?, ValueTask> next)
    {
        HttpContext? http = context.Context.GetHttpContext();
        if (http is null)
        {
            await next(context, exception);
            return;
        }

        using IDisposable? scope = RequestIdentityContextInitializer.Initialize<RequestIdentityContextHubFilter>(
            http,
            context.Context.User);

        await next(context, exception);
    }
}
