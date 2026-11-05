using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Authentication.Module.DependencyInjection;
using Vitarish.Modules.Base.Core;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Infrastructure;

namespace Vitarish.Authentication.Module;

public sealed class AuthenticationModule : Modules.Base.Core.Module
{
    public override string Name => "Authentication";
    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        builder.Services.AddAuthenticationModuleServices(AuthenticationProfilePolicy.Default, configureDbContext);
        return Task.CompletedTask;
    }
    public override async Task Initialize(WebApplication app)
    {
        _ = await app.MigrateDatabaseAsync<AuthenticationDbContext>();
        using IServiceScope scope = app.Services.CreateScope();
        SecurityAccessPolicyStore policyStore = scope.ServiceProvider.GetRequiredService<SecurityAccessPolicyStore>();
        await policyStore.InitializeAsync(CancellationToken.None);
    }

    public override void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) =>
        endpoints.MapAuthenticationModuleEndpoints(
            endpoints.ServiceProvider.GetRequiredService<AuthenticationProfilePolicy>());
}
