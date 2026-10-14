using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Vitarish.Clinic.Api;
using Vitarish.Clinic.Contracts.Authorization;
using Vitarish.Clinic.Contracts.Routes;
using Vitarish.Sea.Contracts.Authorization;

namespace Clinic.Tests.Api;

public sealed class ClinicEndpointMetadataTests
{
    [Theory]
    [InlineData("ClinicListDashboardAppointments", "/dashboard/appointments")]
    public void Dashboard_read_endpoints_are_protected_and_use_sea_read_policy(
        string operationId,
        string route)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(Mock.Of<ISender>());
        using WebApplication app = builder.Build();
        _ = app.MapClinicApi();

        Endpoint endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .Single(candidate => candidate.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == operationId);

        endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Should().ContainSingle().Which.Should().Be("GET");
        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Should().Contain(metadata => metadata.Policy == SeaAuthorizationPolicies.Read);
        endpoint.Metadata.GetMetadata<IRouteDiagnosticsMetadata>()!.Route.Should().Contain(route);
    }

    [Theory]
    [InlineData("ClinicGetSurgeryCatalogs", "GET", SeaAuthorizationPolicies.Read, ClinicApiRoutes.SurgeryCatalogs)]
    [InlineData("ClinicCreateSurgeryCatalog", "POST", ClinicAuthorizationPolicies.PlanSurgeries, ClinicApiRoutes.SurgeryCatalogs)]
    [InlineData("ClinicUpdateSurgeryCatalog", "PUT", ClinicAuthorizationPolicies.PlanSurgeries, ClinicApiRoutes.SurgeryCatalogById)]
    [InlineData("ClinicDeleteSurgeryCatalog", "POST", ClinicAuthorizationPolicies.PlanSurgeries, ClinicApiRoutes.SurgeryCatalogDelete)]
    [InlineData("ClinicEnsureDefaultSurgeryCatalog", "POST", ClinicAuthorizationPolicies.PlanSurgeries, ClinicApiRoutes.SurgeryDefaultCatalog)]
    [InlineData("ClinicGetSurgerySettings", "GET", SeaAuthorizationPolicies.Read, ClinicApiRoutes.SurgerySettings)]
    [InlineData("ClinicUpdateSurgerySettings", "PUT", ClinicAuthorizationPolicies.PlanSurgeries, ClinicApiRoutes.SurgerySettings)]
    [InlineData("ClinicEnsureDefaultSurgerySettings", "POST", ClinicAuthorizationPolicies.PlanSurgeries, ClinicApiRoutes.SurgeryDefaultSettings)]
    [InlineData("ClinicListSurgeryCalendar", "GET", SeaAuthorizationPolicies.Read, ClinicApiRoutes.SurgeryCalendar)]
    [InlineData("ClinicFindSurgeryConflicts", "GET", SeaAuthorizationPolicies.Read, ClinicApiRoutes.SurgeryConflicts)]
    [InlineData("ClinicGetSurgerySuggestions", "GET", SeaAuthorizationPolicies.Read, ClinicApiRoutes.SurgerySuggestions)]
    [InlineData("ClinicGetSurgery", "GET", SeaAuthorizationPolicies.Read, ClinicApiRoutes.SurgeryById)]
    [InlineData("ClinicCreatePatientSurgery", "POST", ClinicAuthorizationPolicies.PlanSurgeries, ClinicApiRoutes.PatientSurgeries)]
    [InlineData("ClinicUpdateSurgery", "PUT", ClinicAuthorizationPolicies.PlanSurgeries, ClinicApiRoutes.SurgeryById)]
    [InlineData("ClinicCompleteSurgery", "POST", ClinicAuthorizationPolicies.PlanSurgeries, ClinicApiRoutes.SurgeryComplete)]
    [InlineData("ClinicCancelSurgery", "POST", ClinicAuthorizationPolicies.PlanSurgeries, ClinicApiRoutes.SurgeryCancel)]
    public void Surgery_endpoints_keep_their_operation_ids_methods_policies_and_routes(
        string operationId,
        string method,
        string policy,
        string route)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(Mock.Of<ISender>());
        using WebApplication app = builder.Build();
        _ = app.MapClinicApi();

        Endpoint endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .Single(candidate => candidate.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == operationId);

        endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Should().ContainSingle().Which.Should().Be(method);
        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Should().Contain(metadata => metadata.Policy == policy);
        endpoint.Metadata.GetMetadata<IRouteDiagnosticsMetadata>()!.Route.Should().Contain(route);
    }

    [Theory]
    [InlineData("ClinicGetPatientAssistantAction", "GET")]
    [InlineData("ClinicConfirmPatientAssistantAction", "POST")]
    [InlineData("ClinicRejectPatientAssistantAction", "POST")]
    public void Patient_agent_action_endpoints_keep_operation_ids_and_require_authentication(
        string operationId,
        string method)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(Mock.Of<ISender>());
        using WebApplication app = builder.Build();
        _ = app.MapClinicApi();

        Endpoint endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .Single(candidate => candidate.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == operationId);

        endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Should().ContainSingle().Which.Should().Be(method);
        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Should().NotBeEmpty();
        endpoint.Metadata.GetMetadata<IRouteDiagnosticsMetadata>()!.Route.Should()
            .Contain("/patients/{patientId:guid}/assistant/actions/{actionId:guid}");
    }

    [Fact]
    public void Patient_agent_conversation_delete_keeps_its_contract_and_requires_authentication()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(Mock.Of<ISender>());
        using WebApplication app = builder.Build();
        _ = app.MapClinicApi();

        Endpoint endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .Single(candidate => candidate.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName
                == "ClinicDeletePatientAssistantConversation");

        endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Should().ContainSingle().Which.Should().Be("DELETE");
        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Should().NotBeEmpty();
        endpoint.Metadata.GetMetadata<IRouteDiagnosticsMetadata>()!.Route.Should()
            .Contain("/patients/{patientId:guid}/assistant/{conversationId:guid}");
    }

    [Theory]
    [InlineData("ClinicGetAssistantAction", "GET", "/assistant/actions/{actionId:guid}")]
    [InlineData("ClinicConfirmAssistantAction", "POST", "/assistant/actions/{actionId:guid}/confirm")]
    [InlineData("ClinicRejectAssistantAction", "POST", "/assistant/actions/{actionId:guid}/reject")]
    public void Canonical_agent_action_endpoints_are_protected_and_keep_their_routes(
        string operationId,
        string method,
        string route)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(Mock.Of<ISender>());
        using WebApplication app = builder.Build();
        _ = app.MapClinicApi();

        Endpoint endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .Single(candidate => candidate.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == operationId);

        endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Should().ContainSingle().Which.Should().Be(method);
        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Should().NotBeEmpty();
        endpoint.Metadata.GetMetadata<IRouteDiagnosticsMetadata>()!.Route.Should().Contain(route);
    }

    [Fact]
    public void Canonical_agent_action_confirmation_uses_only_the_route_action_id()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(Mock.Of<ISender>());
        using WebApplication app = builder.Build();
        _ = app.MapClinicApi();

        Endpoint endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .Single(candidate => candidate.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName
                == "ClinicConfirmAssistantAction");

        endpoint.Metadata.GetMetadata<IAcceptsMetadata>().Should().BeNull();
    }

    [Fact]
    public void Patient_candidate_resolution_keeps_its_operation_id_and_requires_authentication()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(Mock.Of<ISender>());
        using WebApplication app = builder.Build();
        _ = app.MapClinicApi();

        Endpoint endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .Single(candidate => candidate.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName
                == "ClinicResolveClinicAssistantCandidate");

        endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Should().ContainSingle().Which.Should().Be("POST");
        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Should().NotBeEmpty();
        endpoint.Metadata.GetMetadata<IRouteDiagnosticsMetadata>()!.Route.Should().Contain("/assistant/patient-candidates/resolve");
        endpoint.Metadata.GetMetadata<IAcceptsMetadata>().Should().NotBeNull();
    }

    [Fact]
    public void Find_or_create_keeps_its_operation_id_and_requires_sea_create_project_authorization()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(Mock.Of<ISender>());
        using WebApplication app = builder.Build();
        _ = app.MapClinicApi();

        Endpoint endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .Single(candidate => candidate.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName
                == "ClinicFindOrCreatePatient");

        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Should().Contain(metadata => metadata.Policy == SeaAuthorizationPolicies.CreateProject);
    }

    [Fact]
    public void Start_visit_prescription_keeps_its_operation_id_and_requires_sea_create_project_authorization()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(Mock.Of<ISender>());
        using WebApplication app = builder.Build();
        _ = app.MapClinicApi();

        Endpoint endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .Single(candidate => candidate.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName
                == "ClinicStartVisitPrescriptionDraft");

        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Should().Contain(metadata => metadata.Policy == SeaAuthorizationPolicies.CreateProject);
    }
}
