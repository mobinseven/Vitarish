using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Finbuckle.MultiTenant.Abstractions;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Contents.Application;
using Vitarish.Contents.Application.Abstractions;
using Vitarish.Contents.Api;
using Vitarish.Contents.Contracts.Routes;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Contents.Tests.Api;

public sealed class ContentsPublicDeliveryEndpointTests
{
    [Fact]
    public void Public_operations_are_anonymous_justified_and_rate_limited()
    {
        using WebApplication app = CreateApp();
        string[] publicOperationIds =
        [
            ContentsApiOperations.GetPublicSite,
            ContentsApiOperations.GetPublicEntry,
            ContentsApiOperations.ListPublicEntries
        ];
        RouteEndpoint[] publicEndpoints = FindEndpoints(app)
            .Where(endpoint => publicOperationIds.Contains(OperationId(endpoint), StringComparer.Ordinal))
            .ToArray();

        publicEndpoints.Select(OperationId).OrderBy(name => name, StringComparer.Ordinal)
            .Should().Equal(publicOperationIds.OrderBy(name => name, StringComparer.Ordinal));
        publicEndpoints.Should().AllSatisfy(endpoint =>
        {
            endpoint.Metadata.GetMetadata<IAllowAnonymous>().Should().NotBeNull();
            endpoint.Metadata.GetMetadata<AnonymousModuleEndpointMetadata>()?.Justification
                .Should().Be(ContentsPublicDelivery.AnonymousJustification);
            endpoint.Metadata.GetMetadata<ProtectedModuleEndpointMetadata>().Should().BeNull();
            endpoint.Metadata.GetOrderedMetadata<EnableRateLimitingAttribute>()
                .Should().ContainSingle().Which.PolicyName.Should().Be(ContentsPublicDelivery.RateLimitPolicy);
        });
    }

    [Fact]
    public void Every_management_operation_is_in_the_protected_group()
    {
        using WebApplication app = CreateApp();
        string[] publicOperationIds =
        [
            ContentsApiOperations.GetPublicSite,
            ContentsApiOperations.GetPublicEntry,
            ContentsApiOperations.ListPublicEntries
        ];
        RouteEndpoint[] managementEndpoints = FindEndpoints(app)
            .Where(endpoint => !publicOperationIds.Contains(OperationId(endpoint), StringComparer.Ordinal))
            .ToArray();

        managementEndpoints.Should().NotBeEmpty();
        managementEndpoints.Should().AllSatisfy(endpoint =>
        {
            OperationId(endpoint).Should().NotBeNullOrWhiteSpace();
            endpoint.Metadata.GetMetadata<ProtectedModuleEndpointMetadata>().Should().NotBeNull();
            endpoint.Metadata.GetMetadata<IAllowAnonymous>().Should().BeNull();
        });
    }

    private static WebApplication CreateApp()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        IRequestContext requestContext = Mock.Of<IRequestContext>();
        IContentAuthoringStore authoringStore = Mock.Of<IContentAuthoringStore>();
        builder.Services.AddSingleton(requestContext);
        builder.Services.AddSingleton(authoringStore);
        builder.Services.AddSingleton<IContentManagementService>(Mock.Of<IContentManagementService>());
        builder.Services.AddSingleton<IContentAuthoring>(Mock.Of<IContentAuthoring>());
        builder.Services.AddSingleton(new ContentAuthoringQueries(authoringStore, requestContext));
        builder.Services.AddSingleton(new ContentDelivery(authoringStore, requestContext));
        builder.Services.AddSingleton<IMultiTenantContextAccessor>(Mock.Of<IMultiTenantContextAccessor>());
        builder.Services.AddSingleton<ISecurityAuditWriter>(Mock.Of<ISecurityAuditWriter>());
        WebApplication app = builder.Build();
        app.MapContentsApi();
        return app;
    }

    private static RouteEndpoint[] FindEndpoints(WebApplication app) =>
        ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToArray();

    private static string OperationId(RouteEndpoint endpoint) =>
        endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName
        ?? throw new InvalidOperationException("Mapped Contents endpoints must have an operation ID.");
}
