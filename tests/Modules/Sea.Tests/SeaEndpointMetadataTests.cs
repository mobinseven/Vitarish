using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Sea.Api;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Contracts.Responses;

namespace Vitarish.Sea.Tests;

public sealed class SeaEndpointMetadataTests
{
    [Fact]
    public void Capability_and_legacy_endpoints_have_stable_openapi_and_failure_metadata()
    {
        WebApplication app = CreateApplication();
        _ = app.MapSeaApi();
        Endpoint[] endpoints = [.. ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)];

        Endpoint moduleCapabilities = endpoints.Single(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == "SeaGetCapabilities");
        Endpoint eventCapabilities = endpoints.Single(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == "SeaGetEventCapabilities");
        Endpoint legacy = endpoints.Single(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == "SeaGetEventWritePermission");

        AssertProtectedJsonMetadata(moduleCapabilities, typeof(SeaCapabilitiesReadDto));
        AssertProtectedJsonMetadata(eventCapabilities, typeof(EventCapabilitiesReadDto));
        AssertProtectedJsonMetadata(legacy, typeof(bool));
        legacy.Metadata.GetMetadata<ModuleEndpointOpenApiConvention>()!.Deprecated.Should().BeTrue();
    }

    [Fact]
    public void Every_sea_endpoint_is_classified_as_protected()
    {
        WebApplication app = CreateApplication();
        _ = app.MapSeaApi();

        Endpoint[] endpoints = [.. ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)];
        endpoints.Should().NotBeEmpty();
        foreach (Endpoint endpoint in endpoints)
        {
            endpoint.Metadata.GetMetadata<ProtectedModuleEndpointMetadata>().Should().NotBeNull();
        }
    }

    private static void AssertProtectedJsonMetadata(Endpoint endpoint, Type successType)
    {
        endpoint.Metadata.GetMetadata<ProtectedModuleEndpointMetadata>().Should().NotBeNull();
        endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>()
            .Should().Contain(metadata => metadata.StatusCode == StatusCodes.Status200OK && metadata.Type == successType)
            .And.Contain(metadata => metadata.StatusCode == StatusCodes.Status400BadRequest)
            .And.Contain(metadata => metadata.StatusCode == StatusCodes.Status401Unauthorized)
            .And.Contain(metadata => metadata.StatusCode == StatusCodes.Status403Forbidden)
            .And.Contain(metadata => metadata.StatusCode == StatusCodes.Status404NotFound);
    }

    private static WebApplication CreateApplication()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        AddService<IProjectsService>(builder.Services);
        AddService<IProceduresService>(builder.Services);
        AddService<IEventsService>(builder.Services);
        AddService<IAttachmentsService>(builder.Services);
        AddService<IReceiptsService>(builder.Services);
        AddService<IRolesService>(builder.Services);
        AddService<IUserRolesService>(builder.Services);
        AddService<IEventRolesService>(builder.Services);
        AddService<IDatabaseStatisticsService>(builder.Services);
        return builder.Build();
    }

    private static void AddService<T>(IServiceCollection services) where T : class =>
        services.AddSingleton(Mock.Of<T>());
}
