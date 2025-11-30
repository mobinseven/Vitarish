using Duende.IdentityServer.AspNetIdentity;
using Duende.IdentityServer.EntityFramework.Stores;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Security.Cryptography.X509Certificates;
using Vorn.Aaas.Host.Services;
using Vorn.Aaas.Users.Domain.Entities;

public static class IdentityServerWebApplicationBuilderExtensions
{
    public static async Task AddAaasIdentityServer(this WebApplicationBuilder builder, Action<DbContextOptionsBuilder> configureSqlServer)
    {
        string domain = builder.Configuration["Vorn:Aaas:Authority"] ?? throw new Exception("Vorn:Aaas:Authority not set");
        string pfxPassword = builder.Configuration["Vorn:Aaas:DataProtectionPassword"] ?? throw new Exception("Vorn:Aaas:DataProtectionPassword not set");
        string? redisConfig = builder.Configuration.GetConnectionString("Redis");
        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(redisConfig);
        X509Certificate2 cert = await AaasDataProtectionExtensions.GetOrCreateFromRedisAsync(redis, domain, pfxPassword);
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
            .AddProfileService<ProfileService<User>>();

        _ = builder.Services.AddScoped<IEventSink, SecurityEventLogger>();
        _ = builder.Services.AddTransient<IPersistedGrantStore, PersistedGrantStore>();
    }
}
