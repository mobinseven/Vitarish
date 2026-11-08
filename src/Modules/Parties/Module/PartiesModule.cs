using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vitarish.Parties.Application;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Application.SignIn;
using Vitarish.Parties.Domain.Models;
using Vitarish.Parties.Module.DependencyInjection;
using Vitarish.Parties.Infrastructure.Services;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Api;
using Vitarish.Modules.Base.Core;

namespace Vitarish.Parties.Module;

/// <summary>
/// Module composition root for Parties registration.
/// </summary>
public sealed class PartiesModule : Modules.Base.Core.Module
{
    public override string Name => "Parties";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        builder.AddParties(configureDbContext ?? (_ => { }));

        return Task.CompletedTask;
    }

    public override async Task Initialize(WebApplication app)
    {
        _ = await app.MigrateDatabaseAsync<PartiesDbContext>();
        await app.InitializeViews();
        await EnsurePlatformAdminTenantAsync(app);
        await EnsureOnboardingTenantAsync(app);
        await EnsureSignInTenantAsync(app);
        await EnsureBrowserApplicationTenantsAsync(app);
    }

    private static async Task EnsureBrowserApplicationTenantsAsync(WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        BrowserApplicationHostOptions options = scope.ServiceProvider.GetRequiredService<IOptions<BrowserApplicationHostOptions>>().Value;
        IAppTenantProvisioner provisioner = scope.ServiceProvider.GetRequiredService<IAppTenantProvisioner>();
        ITenantHostnameRegistry hostnames = scope.ServiceProvider.GetRequiredService<ITenantHostnameRegistry>();
        foreach (var (url, appId, name) in new[]
        {
            (options.PatientsUrl, BrowserApplicationTenants.PatientsAppId, BrowserApplicationTenants.PatientsName),
            (options.ProjectManagementUrl, BrowserApplicationTenants.ProjectManagementAppId, BrowserApplicationTenants.ProjectManagementName),
            (options.KnowledgeQaUrl, BrowserApplicationTenants.KnowledgeQaAppId, BrowserApplicationTenants.KnowledgeQaName)
        })
        {
            if (url is null) continue;
            string host = new Uri(url).Host;
            // Existing operator-owned tenant bindings remain authoritative.
            if (!await hostnames.IsClaimedAsync(host, CancellationToken.None))
                _ = await provisioner.EnsureAsync(new EnsureAppTenantRequest(SignInTenant.OwnerName, appId, name, host), CancellationToken.None);
        }
    }

    private static async Task EnsureSignInTenantAsync(WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        string? url = scope.ServiceProvider.GetRequiredService<IOptions<SignInHostOptions>>().Value.SignInUrl;
        if (string.IsNullOrWhiteSpace(url))
            return;

        _ = await scope.ServiceProvider.GetRequiredService<IAppTenantProvisioner>().EnsureAsync(new EnsureAppTenantRequest(
            SignInTenant.OwnerName,
            SignInTenant.AppId,
            SignInTenant.Name,
            new Uri(url, UriKind.Absolute).Host), CancellationToken.None);
    }

    private static async Task EnsureOnboardingTenantAsync(WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        string? url = scope.ServiceProvider.GetRequiredService<IOptions<OnboardingHostOptions>>().Value.OnboardingUrl;
        if (string.IsNullOrWhiteSpace(url))
            return;

        _ = await scope.ServiceProvider.GetRequiredService<IAppTenantProvisioner>().EnsureAsync(new EnsureAppTenantRequest(
            OnboardingTenant.OwnerName,
            OnboardingTenant.AppId,
            OnboardingTenant.Name,
            new Uri(url, UriKind.Absolute).Host), CancellationToken.None);
    }

    private static async Task EnsurePlatformAdminTenantAsync(WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        string? url = scope.ServiceProvider.GetRequiredService<IOptions<PlatformAdminHostOptions>>().Value.PlatformAdminUrl;
        if (string.IsNullOrWhiteSpace(url))
            return;

        _ = await scope.ServiceProvider.GetRequiredService<IAppTenantProvisioner>().EnsureAsync(new EnsureAppTenantRequest(
            PlatformAdminTenant.OwnerName,
            PlatformAdminTenant.AppId,
            PlatformAdminTenant.Name,
            new Uri(url, UriKind.Absolute).Host), CancellationToken.None);
    }

    public override void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) => endpoints.MapPartiesApi();
}
