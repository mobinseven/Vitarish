using Duende.IdentityServer.AspNetIdentity;
using Duende.IdentityServer.EntityFramework.Stores;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Vorn.Aaas.Host.Infrastructure.IdentityServer;
using Vorn.Aaas.Users.Domain.Entities;

public static class IdentityServerWebApplicationBuilderExtensions
{
    public static void AddAaasIdentityServer(this WebApplicationBuilder builder, Action<DbContextOptionsBuilder> configureSqlServer)
    {
        string domain = builder.Configuration["Vorn:Aaas:Authority"] ?? throw new Exception("Vorn:Aaas:Authority not set");
        string pfxPassword = builder.Configuration["Vorn:Aaas:DataProtectionPassword"] ?? throw new Exception("Vorn:Aaas:DataProtectionPassword not set");
        System.Security.Cryptography.X509Certificates.X509Certificate2 cert = AaasDataProtectionExtensions.GenerateCertificate(domain, pfxPassword);
        builder.Services
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

        builder.Services.AddScoped<IEventSink, SecurityEventLogger>();
        builder.Services.AddTransient<IPersistedGrantStore, PersistedGrantStore>();
    }
}
