using Duende.IdentityServer.EntityFramework.Stores;
using Duende.IdentityServer.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using Vitarish.Main.DependencyInjection;
using Vitarish.Main.Infrastructure.IdentityServer;
using Vitarish.Main.Infrastructure.Security;

public static class HostIdentityServerExtensions
{
    private const string ConfigSection = $"{nameof(Vitarish)}:Identity";
    public static void AddHostIdentityServer(this WebApplicationBuilder builder, Action<DbContextOptionsBuilder> configureDb, X509Certificate2 signingCertificate)
    {
        string domain = builder.Configuration[$"{ConfigSection}:Authority"] ?? throw new Exception($"{ConfigSection}:Authority not set");
        _ = builder.Services
            .AddIdentityServer(options =>
            {
                options.KeyManagement.Enabled = false;
                options.UserInteraction.LoginUrl = "/Account/Login";
                options.UserInteraction.LogoutUrl = "/Account/SignOut";
                options.Endpoints.EnableTokenRevocationEndpoint = true;
                options.Events.RaiseSuccessEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
            })
            .AddSigningCredential(signingCertificate, SecurityAlgorithms.RsaSha256)
            .AddClientStore<IdentityClientsStore>()
            .AddResourceStore<IdentityResourcesStore>()
            .AddOperationalStore(options => options.ConfigureDbContext = configureDb)
            .AddAspNetIdentity<User>()
            .AddProfileService<PartyProfileService>()
            .AddCustomTokenRequestValidator<ApiClientTokenValidator>();

        _ = builder.Services.AddScoped<IEventSink, SecurityEventLogger>();
        _ = builder.Services.AddTransient<IPersistedGrantStore, PersistedGrantStore>();
    }
}

