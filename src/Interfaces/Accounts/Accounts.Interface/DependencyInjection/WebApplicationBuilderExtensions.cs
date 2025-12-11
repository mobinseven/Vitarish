using Vitarish.Accounts.Domain.Constants;
using Vitarish.Accounts.Infrastructure.Extensions;
using Vitarish.Accounts.Infrastructure.Services;
using Vitarish.Accounts.Interface.Components;

public static class WebApplicationBuilderExtensions
{
    public static void AddAccountsInterface(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddMudServices();
        _ = builder.Services.AddHttpContextAccessor();
        _ = builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        _ = builder.Services.AddRazorPages(options =>
        {
            options.RootDirectory = "/Presentation/Pages";
        });
        _ = builder.Services.AddCascadingAuthenticationState();
        _ = builder.Services.AddScoped<IdentityRedirectManager>();
        _ = builder.Services.AddScoped<UserProfileService>();
        _ = builder.Services.AddScoped<UserProfileCultureInfoProvider>();
        _ = builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
        _ = builder.Services.AddScoped<IAdminContext, AdminContext>();
        _ = builder.Services.AddTransient<ISmsAuthenticationService, SmsAuthenticationService>();
        _ = builder.Services.AddTransient<IEmailAuthenticationService, EmailAuthenticationService>();
        _ = builder.Services.AddTransient<IPersonalAccessTokenService, PersonalAccessTokenService>();
        _ = builder.Services.AddTransient<ICipherService, CipherService>();
        _ = builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(AaasConstants.Admin, policy => policy.RequireClaim(AaasConstants.AccessClaim, "admin.local:::Admin"));
        });
        _ = builder.Services.AddLocalization();
    }
    public static void MapAccountsInterface(this WebApplication app)
    {
        _ = app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
        _ = app.MapRazorPages();
        _ = app.MapAdditionalIdentityEndpoints();
    }
}
