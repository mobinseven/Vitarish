using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Infrastructure.Data;
using Vitarish.Clinical.Infrastructure.Services;

namespace Vitarish.Clinical.Infrastructure.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddClinicalInfrastructure(this IServiceCollection services)
    {
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ClinicalDbContext).Assembly));
        _ = services.AddScoped<IFhirGateway, FhirGateway>();
        _ = services.AddScoped<IClinicalConnectorHealthService, ClinicalConnectorHealthService>();
        _ = services.AddScoped<IClinicalOperationsService, ClinicalOperationsService>();
        _ = services.AddScoped<IClinicalResourceLinkService, ClinicalResourceLinkService>();
        _ = services.AddScoped<IClinicalAuditWriter, ClinicalAuditWriter>();
        _ = services.AddScoped<IClinicalResourceProjectionService, ClinicalResourceProjectionService>();
        _ = services.AddScoped<IClinicalAccessPolicyService, ClinicalAccessPolicyService>();

        // Workflow orchestration services
        _ = services.AddScoped<IVisitWorkflowService, VisitWorkflowService>();
        _ = services.AddScoped<IClinicalActionService, ClinicalActionService>();
        _ = services.AddScoped<IWorkflowPolicyEngine, WorkflowPolicyEngine>();
        _ = services.AddScoped<IFhirResourceMapper, FhirResourceMapperService>();
        _ = services.AddScoped<IValidationCoordinator, ValidationCoordinatorService>();
        _ = services.AddScoped<IStatusCoordinator, StatusCoordinatorService>();
        _ = services.AddScoped<IConflictHandler, ConflictHandlerService>();
        _ = services.AddScoped<IProjectionBuilder, ProjectionBuilderService>();
        _ = services.AddScoped<IWorkflowEventPublisher, WorkflowEventPublisher>();

        return services;
    }
}
