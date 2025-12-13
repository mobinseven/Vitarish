using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Vitarish.Clients.Interface.Files.DependencyInjection;
using Vitarish.Clients.Interface.Intelligence.DependencyInjection;
using Vitarish.Clients.Modules;

namespace Vitarish.Clients.Interface;

public static class InterfaceClientsExtensions
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
            await services.AddIdentityComponents(configuration);
            _ = services.AddModules();
            _ = services.AddIntelligenceModuleComponents();
            _ = services.AddFilesModuleComponents();
            _ = services.AddVitarishClientsInterface();
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
            app.MapIdentityModule();
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
