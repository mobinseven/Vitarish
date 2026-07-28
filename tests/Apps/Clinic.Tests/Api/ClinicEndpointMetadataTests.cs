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
