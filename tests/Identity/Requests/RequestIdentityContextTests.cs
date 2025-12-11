using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Infrastructure.Services;

namespace Vitarish.Requests.Tests;

public class RequestIdentityContextTests
{
    [Fact]
    public void DefaultContext_HasEmptyStringProperties()
    {
        // Arrange & Act
        RequestContext context = new();

        // Assert
        context.Authority.Should().BeEmpty();
        context.ClientId.Should().BeEmpty();
        context.OwnerId.Should().BeEmpty();
        context.UserId.Should().BeNull();
        context.TenantId.Should().BeNull();
        context.ResourceId.Should().BeNull();
        context.TraceId.Should().BeNull();
        context.RequestId.Should().BeNull();
        context.AdditionalInfo.Should().BeNull();
    }

    [Fact]
    public void SetContext_WithAllParameters_PopulatesAllProperties()
    {
        // Arrange
        RequestContext context = new();
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
        context.Authority.Should().Be(authority);
        context.ClientId.Should().Be(clientId);
        context.OwnerId.Should().Be(ownerId);
        context.TenantId.Should().Be(tenantId);
        context.UserId.Should().Be(userId);
        context.ResourceId.Should().Be(resourceId);
        context.TraceId.Should().Be(traceId);
        context.RequestId.Should().Be(requestId);
        context.AdditionalInfo.Should().Be(additionalInfo);
    }

    [Fact]
    public void SetContext_WithoutRequestId_GeneratesNewRequestId()
    {
        // Arrange
        RequestContext context = new();

        // Act
        IRequestContextWriter writer = context;
        writer.SetContext(
            authority: "https://auth.example.com",
            clientId: "test-client",
            ownerId: "owner-123");

        // Assert
        context.RequestId.Should().NotBeNullOrEmpty();
        Guid.TryParse(context.RequestId, out _).Should().BeTrue("RequestId should be a valid GUID");
    }

    [Fact]
    public void SetContext_WithProvidedRequestId_UsesProvidedValue()
    {
        // Arrange
        RequestContext context = new();
        const string expectedRequestId = "my-custom-request-id";

        // Act
        IRequestContextWriter writer = context;
        writer.SetContext(
            authority: "https://auth.example.com",
            clientId: "test-client",
            ownerId: "owner-123",
            requestId: expectedRequestId);

        // Assert
        context.RequestId.Should().Be(expectedRequestId);
    }

    [Fact]
    public void SetContext_WithOnlyRequiredParameters_SetsOptionalToNull()
    {
        // Arrange
        RequestContext context = new();

        // Act
        IRequestContextWriter writer = context;
        writer.SetContext(
            authority: "https://auth.example.com",
            clientId: "test-client",
            ownerId: "owner-123");

        // Assert
        context.Authority.Should().Be("https://auth.example.com");
        context.ClientId.Should().Be("test-client");
        context.OwnerId.Should().Be("owner-123");
        context.TenantId.Should().BeNull();
        context.UserId.Should().BeNull();
        context.ResourceId.Should().BeNull();
        context.TraceId.Should().BeNull();
        context.AdditionalInfo.Should().BeNull();
    }

    [Fact]
    public void Context_ImplementsBothInterfaces()
    {
        // Arrange & Act
        RequestContext context = new();

        // Assert
        context.Should().BeAssignableTo<IRequestContext>();
        context.Should().BeAssignableTo<IRequestContextWriter>();
    }
}
