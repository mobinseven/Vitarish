using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Sea.Contracts.Authorization;

namespace Vitarish.Sea.Api.Authorization;

public static class SeaAuthorizationRegistrationExtensions
{
    public static IServiceCollection AddSeaApiAuthorization(this IServiceCollection services)
    {
        _ = services.AddAuthorization(options =>
        {
            options.AddPolicy(SeaAuthorizationPolicies.Read, policy => policy.AddRequirements(SeaAuthorizationOperations.ReadManagement));
            options.AddPolicy(SeaAuthorizationPolicies.CreateProject, policy => policy.AddRequirements(SeaAuthorizationOperations.CreateProject));
            options.AddPolicy(SeaAuthorizationPolicies.UpdateProject, policy => policy.AddRequirements(SeaAuthorizationOperations.UpdateProject));
        });

        _ = services.AddScoped<SeaManagementAuthorizationHandler>();
        _ = services.AddScoped<SeaEventAuthorizationHandler>();
        _ = services.AddScoped<SeaAttachmentAuthorizationHandler>();
        _ = services.AddScoped<SeaProcedureAuthorizationHandler>();
        _ = services.AddScoped<SeaProjectAuthorizationHandler>();
        _ = services.AddScoped<IAuthorizationHandler>(provider => provider.GetRequiredService<SeaManagementAuthorizationHandler>());
        _ = services.AddScoped<IAuthorizationHandler>(provider => provider.GetRequiredService<SeaEventAuthorizationHandler>());
        _ = services.AddScoped<IAuthorizationHandler>(provider => provider.GetRequiredService<SeaAttachmentAuthorizationHandler>());
        _ = services.AddScoped<IAuthorizationHandler>(provider => provider.GetRequiredService<SeaProcedureAuthorizationHandler>());
        _ = services.AddScoped<IAuthorizationHandler>(provider => provider.GetRequiredService<SeaProjectAuthorizationHandler>());
        return services;
    }
}
