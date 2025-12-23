using Duende.IdentityServer.EntityFramework.Stores;
using Duende.IdentityServer.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Security.Cryptography.X509Certificates;
using Vitarish.Main.DependencyInjection;
using Vitarish.Main.Services;

public static class HostIdentityServerExtensions
{
    private const string ConfigSection = $"{nameof(Vitarish)}:Identity";
    public static async Task AddHostIdentityServer(this WebApplicationBuilder builder, Action<DbContextOptionsBuilder> configureSqlServer)
    {
        string domain = builder.Configuration[$"{ConfigSection}:Authority"] ?? throw new Exception($"{ConfigSection}:Authority not set");
        string pfxPassword = builder.Configuration[$"{ConfigSection}:DataProtectionPassword"] ?? throw new Exception($"{ConfigSection}:DataProtectionPassword not set");
        string redisConfig = builder.Configuration.GetConnectionString("Redis") ?? throw new InvalidOperationException("Redis connection string not configured.");
        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(redisConfig);
        X509Certificate2 cert = await HostDataProtectionExtensions.GetOrCreateFromRedisAsync(redis, domain, pfxPassword);
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
            .AddSigningCredential(cert, SecurityAlgorithms.RsaSha256)
            .AddClientStore<IdentityClientsStore>()
            .AddResourceStore<IdentityResourcesStore>()
            .AddOperationalStore(options => options.ConfigureDbContext = configureSqlServer)
            .AddAspNetIdentity<User>()
            .AddProfileService<PartyProfileService>()
            .AddCustomTokenRequestValidator<ApiClientClaimsService>();

        _ = builder.Services.AddScoped<IEventSink, SecurityEventLogger>();
        _ = builder.Services.AddTransient<IPersistedGrantStore, PersistedGrantStore>();
    }
}
