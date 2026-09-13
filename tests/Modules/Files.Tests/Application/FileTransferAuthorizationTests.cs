using Vitarish.Files.Application.Access;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;

namespace Files.Tests.Application;

public sealed class FileTransferAuthorizationTests
{
    [Fact]
    public async Task CanUploadAsync_requires_a_trusted_tenant_and_subject()
    {
        FileTransferAuthorization missingSubject = new(new TestRequestContext("tenant-a", null));
        FileTransferAuthorization authorized = new(new TestRequestContext("tenant-a", "user-a"));

        (await missingSubject.CanUploadAsync()).Should().BeFalse();
        (await authorized.CanUploadAsync()).Should().BeTrue();
    }

    private sealed class TestRequestContext(string? tenantId, string? userId) : IRequestContext
    {
        public string Authority => "test";
        public string? ClientId => null;
        public string? OwnerId => null;
        public string? UserId => userId;
        public string? TenantId => tenantId;
        public string? ResourceId => null;
        public string? TraceId => null;
        public string? RequestId => null;
        public string? AdditionalInfo => null;
        public System.Security.Claims.ClaimsPrincipal? User => null;
        public Request GetRequest() => new() { TenantId = TenantId, UserId = UserId };
    }
}
