using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Contracts.Authorization;

namespace Vitarish.Clinic.Api.Authorization;

public static class ClinicAuthorizationRegistrationExtensions
{
    public static IServiceCollection AddClinicApiAuthorization(this IServiceCollection services)
    {
        _ = services.AddAuthorization(options =>
            options.AddPolicy(ClinicAuthorizationPolicies.PlanSurgeries, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(SurgeryPlanningAuthorizationRequirement.Instance)));
        _ = services.AddScoped<SurgeryPlanningAuthorizationHandler>();
        _ = services.AddScoped<IAuthorizationHandler>(provider => provider.GetRequiredService<SurgeryPlanningAuthorizationHandler>());
        return services;
    }
}

internal sealed class SurgeryPlanningAuthorizationRequirement : IAuthorizationRequirement
{
    public static SurgeryPlanningAuthorizationRequirement Instance { get; } = new();

    private SurgeryPlanningAuthorizationRequirement()
    {
    }
}

internal sealed class SurgeryPlanningAuthorizationHandler(ISurgeryPlanningAuthorization authorization)
    : AuthorizationHandler<SurgeryPlanningAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SurgeryPlanningAuthorizationRequirement requirement)
    {
        if (authorization.CanPlan())
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
