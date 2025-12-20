using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Security.Claims;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Hosting.Middleware;

namespace Vitarish.Main.Services;

/// <summary>
/// Middleware that populates <see cref="IRequestContext"/> from <see cref="HttpContext.User"/> claims.
/// </summary>
public sealed class RequestsContextMiddleware
{
    private readonly RequestDelegate _next;

    public RequestsContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var result = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);

        if (result.Succeeded)
        {
            context.User = result.Principal!;
        }

        IRequestContextWriter? writer = context.RequestServices.GetService<IRequestContextWriter>();
        if (writer is not null)
        {
            RequestsContextExtensions.PopulateContext(context, writer);
        }

        await _next(context);
    }
}
