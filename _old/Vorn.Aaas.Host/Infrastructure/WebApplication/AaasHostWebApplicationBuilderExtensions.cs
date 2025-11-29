using Microsoft.AspNetCore.Identity;
using Vorn.Aaas.Host.Infrastructure.Authentication;
using Vorn.Aaas.Host.Infrastructure.Options;
using Vorn.Aaas.Users;
using Vorn.Aaas.Users.Domain.Entities;
using Vorn.Aaas.Users.Infrastructure.Data;

public static class AaasHostWebApplicationBuilderExtensions
{
    public static void AddAaasAuthentication(this WebApplicationBuilder builder)
    {

        builder.Services
            .AddAuthentication()
            .AddLocalApi(options =>
            {
                options.ExpectedScope = AaasConstants.AccessScope;
            });

        builder.Services.ConfigureApplicationCookie(options =>
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
        builder.Services.AddIdentity<User, IdentityRole>(o =>
        {
            o.Stores.MaxLengthForKeys = 128;
        })
            .AddEntityFrameworkStores<UsersDbContext>()
            .AddSignInManager<AuditingSignInManager>()
            .AddClaimsPrincipalFactory<AaasUserClaimsPrincipalFactory<User>>()
            .AddDefaultTokenProviders();
    }

    public static void AddAaasHostOptions(this WebApplicationBuilder builder)
    {
        VornOptions vornOptions = new();
        builder.Configuration.GetSection(VornOptions.Section).Bind(vornOptions);
        builder.Services.AddSingleton(vornOptions);
    }
}
