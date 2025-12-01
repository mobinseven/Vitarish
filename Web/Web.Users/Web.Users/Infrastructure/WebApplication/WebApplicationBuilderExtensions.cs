using Vorn.Aaas.Users.Presentation.Abstractions;

public static class WebApplicationBuilderExtensions
{
    public static void AddAaasInterface(this WebApplicationBuilder builder)
    {
        builder.Services.AddMudServices();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        builder.Services.AddRazorPages(options =>
        {
            options.RootDirectory = "/Pages";
        });
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<IdentityRedirectManager>();
        builder.Services.AddScoped<UserProfileService>();
        builder.Services.AddScoped<UserProfileCultureInfoProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
        builder.Services.AddScoped<IAdminContext, AdminContext>();
        builder.Services.AddTransient<ISmsAuthenticationService, SmsAuthenticationService>();
        builder.Services.AddTransient<IEmailAuthenticationService, EmailAuthenticationService>();
        // TODO: Restore when Vorn.Caas dependency is available
        // builder.AddCaasClient();
        builder.Services.AddTransient<IPersonalAccessTokenService, PersonalAccessTokenService>();
        // IMessageQueue is now registered in Host via Hangfire configuration
        builder.Services.AddTransient<ICipherService, CipherService>();
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(AaasConstants.Admin, policy => policy.RequireClaim(AaasConstants.AccessClaim, "admin.local:::Admin"));
        });
        builder.Services.AddLocalization();
        builder.AddAaasApiClient();
    }
    public static void MapAaasInterface(this WebApplication app)
    {
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
        app.MapRazorPages();
        app.MapAdditionalIdentityEndpoints();
    }
}
