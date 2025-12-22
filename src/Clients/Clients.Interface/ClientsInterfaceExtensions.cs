using Microsoft.Extensions.Configuration;
using Vitarish.Clients.Interface.Files.DependencyInjection;
using Vitarish.Clients.Interface.Intelligence.DependencyInjection;
using Vitarish.Clients.Interface.Localization;
using Vitarish.Clients.Interface.Sea.Abstractions;
using Vitarish.Clients.Interface.Sea.Internal;
using Vitarish.Clients.Modules;

namespace Vitarish.Clients.Interface;

public static class ClientsInterfaceExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds interface modules to the service collection, including identity components, modules, intelligence module components, files module components, and Vitarish clients interface.
        /// </summary>
        /// <param name="configuration">The configuration instance used to configure the modules.</param>
        /// <returns>The modified service collection.</returns>
        public async Task<IServiceCollection> AddInterfaceModules(IConfiguration configuration)
        {
            _ = await services.AddIdentityComponents(configuration);
            _ = services.AddModules();
            _ = services.AddIntelligenceModuleComponents();
            _ = services.AddFilesModuleComponents();
            _ = services.AddVitarishClientsInterface();
            _ = services.AddLocalization();
            _ = services.ConfigureCompositeStringLocalizer(typeof(Strings));
            _ = services.AddSingleton<IStringLocalizer>(sp => sp.GetRequiredService<IStringLocalizer<Strings>>());
            _ = services.AddMudServices();
            _ = services.AddScoped<ISeaUsersService, SeaUsersService>();
            _ = services.Configure<RequestLocalizationOptions>(options =>
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
            return services;
        }
    }

    extension(WebApplication app)
    {
        /// <summary>
        /// Maps interface modules to the web application, including the identity module.
        /// </summary>
        /// <returns>The modified web application.</returns>
        public WebApplication MapInterfaceModules()
        {
            _ = app.MapIdentityModule();
            return app;
        }

        /// <summary>
        /// Applies common middlewares to the web application, including HSTS, HTTPS redirection, static files, authentication, authorization, and antiforgery.
        /// </summary>
        /// <returns>The modified web application.</returns>
        public WebApplication UseCommonMiddlewares()
        {
            _ = app.UseHsts();
            _ = app.UseHttpsRedirection();
            _ = app.UseStaticFiles();
            _ = app.UseAuthentication();
            _ = app.UseAuthorization();
            _ = app.UseAntiforgery();
            return app;
        }
    }
}
