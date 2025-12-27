using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Vitarish.Accounts.Application.Abstractions;
using Vitarish.Accounts.Domain.Constants;
using Vitarish.Accounts.Infrastructure.Extensions;
using Vitarish.Accounts.Infrastructure.Services;
using Vitarish.Accounts.Interface.Components;
using Vitarish.Interfaces.Base.Localization;

public static class WebApplicationBuilderExtensions
{
    public static void AddAccountsInterface(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddMudServices();
        _ = builder.Services.AddHttpContextAccessor();
        _ = builder.Services.AddCascadingAuthenticationState();
        _ = builder.Services.AddScoped<IdentityRedirectManager>();
        _ = builder.Services.AddScoped<IUserProfileInterfaceService, UserProfileInterfaceService>();
        _ = builder.Services.AddScoped<UserProfileCultureInfoProvider>();
        _ = builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
        _ = builder.Services.AddScoped<IAdminContext, AdminContext>();
        _ = builder.Services.AddTransient<ISmsAuthenticationService, SmsAuthenticationService>();
        _ = builder.Services.AddTransient<IEmailAuthenticationService, EmailAuthenticationService>();
        _ = builder.Services.AddTransient<IPersonalAccessTokenService, PersonalAccessTokenService>();
        _ = builder.Services.AddTransient<ICipherService, CipherService>();
        _ = builder.Services.AddHttpClient();
        _ = builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(AaasConstants.Admin, policy => policy.RequireClaim(AaasConstants.AccessClaim, "admin.local:::Admin"));
        });
        builder.Services.AddLocalization();
        builder.Services.ConfigureCompositeStringLocalizer(typeof(Strings));
        builder.Services.AddSingleton<IStringLocalizer>(sp => sp.GetRequiredService<IStringLocalizer<Strings>>());
        builder.Services.Configure<RequestLocalizationOptions>(options =>
        {
            CultureInfo[] supportedCultures =
            [
                new("fa-IR"),
                new("en-US")
            ];
            options.DefaultRequestCulture = new RequestCulture(supportedCultures[0]);
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
            options.ApplyCurrentCultureToResponseHeaders = true;
        });
    }
    public static void MapAccountsInterface(this WebApplication app)
    {
        _ = app.MapAdditionalIdentityEndpoints();
    }
}
