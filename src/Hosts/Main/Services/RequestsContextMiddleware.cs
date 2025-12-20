using Microsoft.AspNetCore.Authentication.JwtBearer;
using Vitarish.Main.Services;

public sealed class RequestsContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string[] _authSchemes = new[]
    {
        JwtBearerDefaults.AuthenticationScheme,
        IdentityConstants.ApplicationScheme
    };

    public RequestsContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Try each configured authentication scheme
        foreach (string scheme in _authSchemes)
        {
            var result = await context.AuthenticateAsync(scheme);
            if (result?.Succeeded == true && result.Principal != null)
            {
                context.User = result.Principal;
                break;
            }
        }

        // Populate request context if we have a writer
        var writer = context.RequestServices.GetService<IRequestContextWriter>();
        if (writer != null)
        {
            await RequestsContextExtensions.PopulateContext(context, writer);
        }

        await _next(context);
    }
}
