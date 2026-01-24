using Vitarish.Users.Application;
using Vitarish.Users.Domain.Options;

public static class HostAuthenticationExtensions
{
    private const string ConfigSection = $"{nameof(Vitarish)}:Identity";
    public static void AddHostAuthentication(this WebApplicationBuilder builder)
    {

        AuthenticationBuilder authen = builder.Services
            .AddAuthentication();
        _ = authen.AddLocalApi(options =>
        {
            options.ExpectedScope = IdentityDefinitions.AccessScope;
        });
        _ = authen.AddJwtBearer(jwt =>
                {
                    jwt.Authority = builder.Configuration[$"{ConfigSection}:Authority"];
                    jwt.RequireHttpsMetadata = true;
                    jwt.TokenValidationParameters.ValidateAudience = false;
                    jwt.TokenValidationParameters.ValidTypes = ["at+jwt"];
                });

        _ = builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Events.OnRedirectToLogin = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
            options.Cookie = new()
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                SecurePolicy = CookieSecurePolicy.Always,
                MaxAge = TimeSpan.FromDays(14)
            };
            options.SlidingExpiration = true;
            options.Events.OnSignedIn = (context) =>
            {
                context.Properties.IsPersistent = true;
                context.Properties.AllowRefresh = true;
                return Task.FromResult(0);
            };
        });

        // Configure WebAuthn/FIDO2
        builder.AddWebAuthn();
    }

    /// <summary>
    /// Configures WebAuthn/FIDO2 services for passkey authentication.
    /// </summary>
    private static void AddWebAuthn(this WebApplicationBuilder builder)
    {
        // Bind WebAuthn configuration from appsettings with fallback defaults
        WebAuthnOptions webAuthnOptions = new()
        {
            ServerDomain = "localhost",
            Origins = ["https://localhost"]
        };
        builder.Configuration.GetSection(WebAuthnOptions.SectionName).Bind(webAuthnOptions);

        // Apply fallbacks if not set by configuration
        if (string.IsNullOrEmpty(webAuthnOptions.ServerDomain))
        {
            webAuthnOptions.ServerDomain = "localhost";
        }

        if (webAuthnOptions.Origins == null || webAuthnOptions.Origins.Count == 0)
        {
            webAuthnOptions.Origins = ["https://localhost"];
        }

        // Register WebAuthn options
        _ = builder.Services.AddSingleton(webAuthnOptions);

        // Configure FIDO2 library
        _ = builder.Services.AddFido2(options =>
        {
            options.ServerDomain = webAuthnOptions.ServerDomain;
            options.ServerName = webAuthnOptions.ServerName;
            options.Origins = webAuthnOptions.Origins;
            options.TimestampDriftTolerance = (int)webAuthnOptions.TimestampDriftTolerance;
        });
    }
}
