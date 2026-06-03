using Microsoft.AspNetCore.Http;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Infrastructure.Services;

namespace Requests.Tests;

public class RequestIdentityContextTests
{
    [Fact]
    public void DefaultContext_HasEmptyStringProperties()
    {
        // Arrange & Act
        RequestContext context = new(new HttpContextAccessor());

        // Assert
        _ = context.Authority.Should().BeEmpty();
        _ = context.ClientId.Should().BeNull();
        _ = context.OwnerId.Should().BeNull();
        _ = context.UserId.Should().BeNull();
        _ = context.TenantId.Should().BeNull();
        _ = context.ResourceId.Should().BeNull();
        _ = context.TraceId.Should().BeNull();
        _ = context.RequestId.Should().BeNull();
        _ = context.AdditionalInfo.Should().BeNull();
    }

    [Fact]
    public void SetContext_WithAllParameters_PopulatesAllProperties()
    {
        // Arrange
        RequestContext context = new(new HttpContextAccessor());
        const string authority = "https://auth.example.com";
        const string clientId = "test-client";
        const string ownerId = "owner-123";
        const string tenantId = "tenant-456";
        const string userId = "user-789";
        const string resourceId = "/api/test";
        const string traceId = "trace-abc";
        const string requestId = "request-def";
        const string additionalInfo = "extra-info";

        // Act
        IRequestContextWriter writer = context;
        writer.SetContext(
            authority,
            clientId,
            ownerId,
            tenantId,
            userId,
            resourceId,
            traceId,
            requestId,
            additionalInfo);

        // Assert
        _ = context.Authority.Should().Be(authority);
        _ = context.ClientId.Should().Be(clientId);
        _ = context.OwnerId.Should().Be(ownerId);
        _ = context.TenantId.Should().Be(tenantId);
        _ = context.UserId.Should().Be(userId);
        _ = context.ResourceId.Should().Be(resourceId);
        _ = context.TraceId.Should().Be(traceId);
        _ = context.RequestId.Should().Be(requestId);
        _ = context.AdditionalInfo.Should().Be(additionalInfo);
    }

    [Fact]
    public void SetContext_WithoutRequestId_GeneratesNewRequestId()
    {
        // Arrange
        RequestContext context = new(new HttpContextAccessor());

        // Act
        IRequestContextWriter writer = context;
        writer.SetContext(
            authority: "https://auth.example.com",
            clientId: "test-client",
            ownerId: "owner-123");

        // Assert
        _ = context.RequestId.Should().NotBeNullOrEmpty();
        _ = Guid.TryParse(context.RequestId, out _).Should().BeTrue("RequestId should be a valid GUID");
    }

    [Fact]
    public void SetContext_WithProvidedRequestId_UsesProvidedValue()
    {
        // Arrange
        RequestContext context = new(new HttpContextAccessor());
        const string expectedRequestId = "my-custom-request-id";

        // Act
        IRequestContextWriter writer = context;
        writer.SetContext(
            authority: "https://auth.example.com",
            clientId: "test-client",
            ownerId: "owner-123",
            requestId: expectedRequestId);

        // Assert
        _ = context.RequestId.Should().Be(expectedRequestId);
    }

    [Fact]
    public void SetContext_WithOnlyRequiredParameters_SetsOptionalToNull()
    {
        // Arrange
        RequestContext context = new(new HttpContextAccessor());

        // Act
        IRequestContextWriter writer = context;
        writer.SetContext(
            authority: "https://auth.example.com",
            clientId: null,
            ownerId: null);

        // Assert
        _ = context.Authority.Should().Be("https://auth.example.com");
        _ = context.ClientId.Should().BeNull();
        _ = context.OwnerId.Should().BeNull();
        _ = context.TenantId.Should().BeNull();
        _ = context.UserId.Should().BeNull();
        _ = context.ResourceId.Should().BeNull();
        _ = context.TraceId.Should().BeNull();
        _ = context.AdditionalInfo.Should().BeNull();
    }

    [Fact]
    public void Context_ImplementsBothInterfaces()
    {
        // Arrange & Act
        RequestContext context = new(new HttpContextAccessor());

        // Assert
        _ = context.Should().BeAssignableTo<IRequestContext>();
        _ = context.Should().BeAssignableTo<IRequestContextWriter>();
    }
}
