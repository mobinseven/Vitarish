using Fido2NetLib;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Hosting;
using Vitarish.Users.Domain.Options;
using Vitarish.Hosting.Infrastructure.Options;
using Vitarish.Authentication.Infrastructure.Identity;

public static class HostAuthenticationExtensions
{
    private const string ConfigSection = $"{nameof(Vitarish)}:Identity";
    public static void AddHostAuthentication(this WebApplicationBuilder builder, SecurityConfigurationOptions security)
    {

        AuthenticationBuilder authen = builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
                options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
            });
        _ = authen.AddLocalApi(options =>
        {
            options.ExpectedScope = IdentityDefinitions.AccessScope;
        });
        _ = authen.AddJwtBearer(jwt =>
                {
                    jwt.Authority = security.TokenValidation.Issuer;
                    jwt.RequireHttpsMetadata = true;
                    jwt.TokenValidationParameters.ValidIssuer = security.TokenValidation.Issuer;
                    jwt.TokenValidationParameters.ValidAudiences = security.TokenValidation.Audiences;
                    jwt.TokenValidationParameters.ValidateIssuer = true;
                    jwt.TokenValidationParameters.ValidateAudience = true;
                    jwt.TokenValidationParameters.ValidTypes = ["at+jwt"];
                });

        _ = builder.Services.ConfigureApplicationCookie(options =>
        {
            options.EventsType = typeof(AuthenticationCookieEvents);
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
                SecurePolicy = CookieSecurePolicy.Always
            };
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = true;
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
        _ = builder.Services.AddFido2(options => ApplyFido2Configuration(options, webAuthnOptions, webAuthnOptions.Origins));

        // Clinic hosts are not a fixed list, so each ceremony also accepts the origin the host already
        // trusts for this request (an active clinic's subdomain or a platform host); see ClientOriginTrust.
        builder.Services.Replace(ServiceDescriptor.Scoped<IFido2>(services =>
        {
            string? requestOrigin = ClientOriginTrust.TrustedOriginFor(services.GetRequiredService<IHttpContextAccessor>().HttpContext);
            HashSet<string> origins = requestOrigin is null ? webAuthnOptions.Origins : [.. webAuthnOptions.Origins, requestOrigin];
            return new Fido2(ApplyFido2Configuration(new Fido2Configuration(), webAuthnOptions, origins), services.GetRequiredService<IMetadataService>());
        }));
    }

    private static Fido2Configuration ApplyFido2Configuration(Fido2Configuration configuration, WebAuthnOptions webAuthnOptions, HashSet<string> origins)
    {
        configuration.ServerDomain = webAuthnOptions.ServerDomain;
        configuration.ServerName = webAuthnOptions.ServerName;
        configuration.Origins = origins;
        configuration.TimestampDriftTolerance = (int)webAuthnOptions.TimestampDriftTolerance;
        return configuration;
    }
}
