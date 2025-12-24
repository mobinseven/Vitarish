using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using Vitarish.Main.Extensions;

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
        IAuthenticationService? authenticationService = context.RequestServices.GetService<IAuthenticationService>();
        IAuthenticationSchemeProvider? schemeProvider = context.RequestServices.GetService<IAuthenticationSchemeProvider>();
        if (authenticationService != null && schemeProvider != null)
        {
            var availableSchemes = (await schemeProvider.GetAllSchemesAsync())
                .Select(s => s.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (availableSchemes.Count > 0)
            {
                foreach (string scheme in _authSchemes)
                {
                    if (!availableSchemes.Contains(scheme))
                    {
                        continue;
                    }

                    AuthenticateResult? result = await authenticationService.AuthenticateAsync(context, scheme);
                    if (result?.Succeeded == true && result.Principal != null)
                    {
                        context.User = result.Principal;
                        break;
                    }
                }
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
