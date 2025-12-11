using Fido2NetLib;
using Vitarish.Host.Infrastructure.Options;
using Vitarish.Host.Services;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure;
using Vitarish.Users.Infrastructure.Data;

public static class HostAuthenticationExtensions
{
    public static void AddHostAuthentication(this WebApplicationBuilder builder)
    {

        _ = builder.Services
            .AddAuthentication()
            .AddLocalApi(options =>
            {
                options.ExpectedScope = AaasConstants.AccessScope;
            });

        _ = builder.Services.ConfigureApplicationCookie(options =>
        {
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
        _ = builder.Services.AddIdentity<User, IdentityRole>(o =>
        {
            o.Stores.MaxLengthForKeys = 128;
        })
            .AddEntityFrameworkStores<UsersDbContext>()
            .AddSignInManager<AuditingSignInManager>()
            .AddClaimsPrincipalFactory<UserInfoClaimsPrincipalFactory>()
            .AddDefaultTokenProviders();

        // Configure WebAuthn/FIDO2
        builder.AddWebAuthn();
    }

    /// <summary>
    /// Configures WebAuthn/FIDO2 services for passkey authentication.
    /// </summary>
    private static void AddWebAuthn(this WebApplicationBuilder builder)
    {
        // Bind WebAuthn configuration from appsettings
        WebAuthnOptions webAuthnOptions = new()
        {
            ServerDomain = "localhost",
            Origins = ["https://localhost"]
        };
        builder.Configuration.GetSection(WebAuthnOptions.SectionName).Bind(webAuthnOptions);

        // Register WebAuthn options
        _ = builder.Services.AddSingleton(webAuthnOptions);

        // Configure FIDO2 library
        _ = builder.Services.AddFido2(options =>
        {
            options.ServerDomain = webAuthnOptions.ServerDomain;
            options.ServerName = webAuthnOptions.ServerName;
            options.Origins = webAuthnOptions.Origins;
            options.TimestampDriftTolerance = (int)webAuthnOptions.Timeout;
        });
    }

    /// <summary>
    /// Adds and configures all application options using the centralized options pattern.
    /// This method uses the shared <see cref="ApplicationOptionsExtensions.AddApplicationOptions"/> extension
    /// to bind all module configuration in a consistent way.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    public static void AddAaasHostOptions(this WebApplicationBuilder builder)
    {
        _ = builder.AddApplicationOptions();
    }
}
