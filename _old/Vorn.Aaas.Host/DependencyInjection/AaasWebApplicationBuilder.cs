using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.ApiClients.Presentation.DependencyInjection;
using Vorn.Aaas.Host.Infrastructure.BackgroundJobs;
using Vorn.Aaas.Parties.Presentation.DependencyInjection;
using Vorn.Aaas.RequestIdentities.Presentation.DependencyInjection;
using Vorn.Aaas.Users.Presentation.DependencyInjection;

public static class AaasWebApplicationBuilder
{
    public static async Task<WebApplication> BuildWebApplication(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.AddCaasClient();
        builder.AddAaasInterface();
        builder.AddAaasHostOptions();
        void configureSqlServer(DbContextOptionsBuilder options)
        {
            _ = options.UseSqlServer(
            builder.Configuration.GetConnectionString("AaasDb"),
            b =>
            {
                _ = b.MigrationsAssembly("Vorn.Aaas.Host");
                _ = b.EnableRetryOnFailure();
            });
        }

        builder.AddAaasUsers(configureSqlServer);
        builder.AddAaasRequestIdentities(configureSqlServer);
        builder.AddAaasApiClients(configureSqlServer);
        builder.AddAaasAuthenticationClients(configureSqlServer);
        builder.AddAaasParties(configureSqlServer);
        await builder.AddAaasIdentityServer(configureSqlServer);
        builder.AddAaasAuthentication();
        builder.AddHangfireBackgroundJobs();
        builder.AddAaasPipeline();
        await builder.AddAaasDataProtection();
        _ = builder.Services.AddMemoryCache();
        _ = builder.Services.AddControllers();
        _ = builder.Services.AddEndpointsApiExplorer();
        _ = builder.Services.AddLocalization();
        _ = builder.AddDashboardServices();
        WebApplication app = builder.Build();
        return app;
    }
}
