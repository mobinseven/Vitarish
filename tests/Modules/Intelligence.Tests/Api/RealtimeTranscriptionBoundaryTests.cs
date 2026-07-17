using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Vitarish.Intelligence.Api;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Application.LiveTranscription;
using Vitarish.Intelligence.Contracts;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Intelligence.Tests.Api;

public sealed class RealtimeTranscriptionBoundaryTests
{
    [Fact]
    public void Origin_policy_allows_only_exact_configured_http_origins()
    {
        RealtimeTranscriptionOriginPolicy policy = new([
            "https://clinic.example",
            "http://localhost:3000"
        ]);

        policy.IsTrusted("https://clinic.example").Should().BeTrue();
        policy.IsTrusted("https://CLINIC.example:443").Should().BeTrue();
        policy.IsTrusted("http://localhost:3000").Should().BeTrue();
        policy.IsTrusted("https://clinic.example.evil.test").Should().BeFalse();
        policy.IsTrusted("http://clinic.example").Should().BeFalse();
        policy.IsTrusted(null).Should().BeFalse();
        policy.IsTrusted("null").Should().BeFalse();
    }

    [Theory]
    [InlineData("*")]
    [InlineData("https://clinic.example/path")]
    [InlineData("file:///tmp/socket")]
    public void Origin_policy_rejects_unsafe_configuration(string origin)
    {
        Action create = () => _ = new RealtimeTranscriptionOriginPolicy([origin]);

        create.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Capability_and_websocket_endpoints_are_protected_and_only_capability_is_openapi_visible()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(Mock.Of<IResponder>());
        builder.Services.AddSingleton(Mock.Of<ITranscriber>());
        builder.Services.AddSingleton(Mock.Of<ITranscriptionProvider>(provider => provider.IsAvailable));
        builder.Services.AddSingleton(new LiveTranscriptionSettings());
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<LiveTranscriptionCapabilitiesQuery>();
        builder.Services.AddSingleton<LiveTranscriptionSessionFactory>();
        builder.Services.AddSingleton(new RealtimeTranscriptionOriginPolicy(["https://clinic.example"]));
        using WebApplication app = builder.Build();
        _ = app.MapIntelligenceApi();

        Endpoint[] endpoints = [..
            ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)];
        Endpoint capability = Find(endpoints, "IntelligenceGetLiveTranscriptionCapabilities");
        Endpoint webSocket = Find(endpoints, "IntelligenceLiveTranscription");

        capability.Metadata.GetMetadata<ProtectedModuleEndpointMetadata>().Should().NotBeNull();
        capability.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>()
            .Should().Contain(metadata =>
                metadata.StatusCode == StatusCodes.Status200OK
                && metadata.Type == typeof(RealtimeTranscriptionCapabilities));

        webSocket.Metadata.GetMetadata<ProtectedModuleEndpointMetadata>().Should().NotBeNull();
        webSocket.Metadata.GetMetadata<StreamingEndpointMetadata>().Should().NotBeNull();
        webSocket.Metadata.GetMetadata<OpenApiExclusionJustificationMetadata>().Should().NotBeNull();
        webSocket.Metadata.GetMetadata<IExcludeFromDescriptionMetadata>()?.ExcludeFromDescription
            .Should().BeTrue();
        capability.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Should().Contain(metadata =>
                metadata.AuthenticationSchemes == ModuleEndpointAuthenticationSchemes.BrowserOrBearer);
        webSocket.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Should().Contain(metadata =>
                metadata.AuthenticationSchemes == ModuleEndpointAuthenticationSchemes.BrowserOrBearer);
    }

    private static Endpoint Find(IEnumerable<Endpoint> endpoints, string name) =>
        endpoints.Single(endpoint =>
            endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == name);
}
