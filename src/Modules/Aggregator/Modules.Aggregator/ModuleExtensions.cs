using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Appointments.Interface;
using Vitarish.Commerce.Interface;
using Vitarish.Communications.Hosting;
using Vitarish.Contents.Interface;
using Vitarish.Files.Interface;
using Vitarish.Intelligence.Interface;
using Vitarish.Medical.Interface;
using Vitarish.Modules.Base;
using Vitarish.Social.Hosting;
using Vitarish.Sea.Interface;
using Vitarish.Search.Interface;
public static class ModulesExtensions
{
    public static List<IModule> GetModules() =>
            [
                Activator.CreateInstance<CommunicationModule>(),
                Activator.CreateInstance<IntelligenceModule>(),
                Activator.CreateInstance<FilesModule>(),
                Activator.CreateInstance<SearchModule>(),
                Activator.CreateInstance<ContentsModule>(),
                Activator.CreateInstance<SeaModule>(),
                Activator.CreateInstance<AppointmentsModule>(),
                Activator.CreateInstance<MedicalModule>(),
                Activator.CreateInstance<CommerceModule>(),
                Activator.CreateInstance<SocialModule>(),
            ];

    extension(WebApplicationBuilder builder)
    {
        public async Task<WebApplicationBuilder> RegisterModules(
        Action<DbContextOptionsBuilder>? configureDbContext = null)
        {
            List<IModule> Modules = GetModules();

            builder.Services.TryAddSingleton<IReadOnlyList<IModule>>(Modules);

            foreach (IModule Module in Modules)
            {
                await Module.Register(builder, configureDbContext);
            }

            return builder;
        }
    }

    extension(WebApplication app)
    {
        public async Task<WebApplication> InitializeModules()
        {
            IReadOnlyList<IModule>? Modules = app.Services.GetService<IReadOnlyList<IModule>>();
            if (Modules is null)
            {
                return app;
            }

            foreach (IModule Module in Modules)
            {
                await Module.Initialize(app);
            }

            return app;
        }
    }
}
