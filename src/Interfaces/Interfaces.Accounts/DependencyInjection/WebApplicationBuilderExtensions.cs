using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Vitarish.Interfaces.Accounts.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static void AddAccountsInterface(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddMudServices();
        _ = builder.Services.AddHttpContextAccessor();
        _ = builder.Services.AddCascadingAuthenticationState();
        _ = builder.Services.AddScoped<ReturnUrlManager>();
        _ = builder.Services.AddSingleton(TimeProvider.System);
        _ = builder.Services.AddSingleton<IVerificationSmsRequestShield, InMemoryVerificationSmsRequestShield>();
        _ = builder.Services.AddScoped<IUserService, UserService>();
        _ = builder.Services.AddScoped<UserProfileCultureInfoProvider>();
        _ = builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
        _ = builder.Services.AddTransient<ISmsAuthenticationService, SmsAuthenticationService>();
        _ = builder.Services.AddTransient<IEmailAuthenticationService, EmailAuthenticationService>();
        _ = builder.Services.AddTransient<IPersonalAccessTokenService, PersonalAccessTokenService>();
        _ = builder.Services.AddScoped<WebAuthnInteropService>();
        _ = builder.Services.AddScoped<IWebAuthnInterop>(static sp => sp.GetRequiredService<WebAuthnInteropService>());

        _ = builder.Services.AddHttpClient();

        _ = builder.Services.AddLocalization();

        _ = builder.Services.Configure<RequestLocalizationOptions>(options =>
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
    public static void MapAccountsInterface(this WebApplication app) => _ = app.MapAdditionalIdentityEndpoints();
}
