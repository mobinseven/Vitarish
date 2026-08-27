using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Authentication.Module.DependencyInjection;
using Vitarish.Modules.Base.Core;
using Vitarish.Authentication.Domain.Security;
using BaseModule = Vitarish.Modules.Base.Core.Module;

namespace Vitarish.Authentication.Module;

public sealed class AuthenticationModule : Modules.Base.Core.Module
{
    public override string Name => "Authentication";
    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        bool productEvaluatedProfile = builder.Configuration.GetValue<bool>(BaseModule.EvaluatedConfigurationEnabledKey) &&
            string.Equals(
            builder.Configuration[BaseModule.EvaluatedConfigurationIdentityKey],
            BaseModule.ProductEvaluatedProfileIdentity,
            StringComparison.Ordinal);
        bool openApiGeneration = string.Equals(
            Environment.GetEnvironmentVariable("VITARISH_OPENAPI_GENERATION"),
            "true",
            StringComparison.OrdinalIgnoreCase);
        AuthenticationProfilePolicy profilePolicy = productEvaluatedProfile
            ? AuthenticationProfilePolicy.ProductEvaluated
            : openApiGeneration
                ? AuthenticationProfilePolicy.OpenApi
                : AuthenticationProfilePolicy.Default;
        builder.Services.AddAuthenticationModuleServices(profilePolicy);
        return Task.CompletedTask;
    }
    public override Task Initialize(WebApplication app)
    {
        return Task.CompletedTask;
    }

    public override void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) =>
        endpoints.MapAuthenticationModuleEndpoints(
            endpoints.ServiceProvider.GetRequiredService<AuthenticationProfilePolicy>());
}
