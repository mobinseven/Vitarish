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

namespace Clinic.Tests.Api;

public sealed class ClinicEndpointMetadataTests
{
    [Theory]
    [InlineData("ClinicGetPatientAgentAction", "GET")]
    [InlineData("ClinicConfirmPatientAgentAction", "POST")]
    [InlineData("ClinicRejectPatientAgentAction", "POST")]
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
            .Contain("/patients/{patientId:guid}/agent/actions/{actionId:guid}");
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
                == "ClinicDeletePatientAgentConversation");

        endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Should().ContainSingle().Which.Should().Be("DELETE");
        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Should().NotBeEmpty();
        endpoint.Metadata.GetMetadata<IRouteDiagnosticsMetadata>()!.Route.Should()
            .Contain("/patients/{patientId:guid}/agent/{conversationId:guid}");
    }

    [Theory]
    [InlineData("ClinicGetAgentAction", "GET", "/agent/actions/{actionId:guid}")]
    [InlineData("ClinicConfirmAgentAction", "POST", "/agent/actions/{actionId:guid}/confirm")]
    [InlineData("ClinicRejectAgentAction", "POST", "/agent/actions/{actionId:guid}/reject")]
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
                == "ClinicConfirmAgentAction");

        endpoint.Metadata.GetMetadata<IAcceptsMetadata>().Should().BeNull();
    }

    [Fact]
    public void Find_or_create_keeps_its_operation_id_and_requires_sea_manage_authorization()
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
            .Should().Contain(metadata => metadata.Policy == "Sea.Manage");
    }

    [Fact]
    public void Start_visit_prescription_keeps_its_operation_id_and_requires_sea_manage_authorization()
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
            .Should().Contain(metadata => metadata.Policy == "Sea.Manage");
    }
}
