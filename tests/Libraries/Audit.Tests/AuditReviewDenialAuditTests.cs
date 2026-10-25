using System.Security.Claims;
using FluentAssertions;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Audit.Server;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;
using Xunit;

namespace Vitarish.Audit.Tests;

public sealed class AuditReviewDenialAuditTests
{
    [Fact]
    public async Task Records_a_catalogued_denied_review_naming_the_attempted_operation()
    {
        RecordingWriter writer = new();

        await AuditReviewDenialAudit.RecordAsync(writer, new TestRequestContext("tenant-a", "user-7"), SecurityAuditApiOperations.ListPlatformSecurityEvents);

        SecurityAuditEvent denial = writer.Written.Should().ContainSingle().Subject;
        denial.EventName.Should().Be(SecurityAuditEventCatalog.AuditReviewEvent);
        denial.Outcome.Should().Be(SecurityAuditOutcome.Denied);
        denial.RedactedReason.Should().Be(SecurityAuditReasonCatalog.Unauthorized);
        denial.ActorId.Should().Be("user-7");
        denial.TenantId.Should().Be("tenant-a");
        denial.Resource.Should().Be(SecurityAuditEventCatalog.EndpointResourcePrefix + SecurityAuditApiOperations.ListPlatformSecurityEvents);
        SecurityAuditEventCatalog.IsCatalogued(denial).Should().BeTrue();
    }

    [Fact]
    public async Task Records_a_denial_without_a_tenant_in_the_platform_scope()
    {
        RecordingWriter writer = new();

        await AuditReviewDenialAudit.RecordAsync(writer, new TestRequestContext(null, "user-7"), SecurityAuditApiOperations.ExportPlatformSecurityEvents);

        writer.Written.Single().TenantId.Should().Be(SecurityAuditScopeCatalog.PlatformScopeId);
    }

    [Fact]
    public async Task An_unavailable_audit_store_does_not_replace_the_denial()
    {
        Func<Task> record = () => AuditReviewDenialAudit.RecordAsync(new UnavailableWriter(), new TestRequestContext("tenant-a", "user-7"), SecurityAuditApiOperations.ListSecurityEvents);

        await record.Should().NotThrowAsync();
    }

    private sealed class RecordingWriter : ISecurityAuditWriter
    {
        public List<SecurityAuditEvent> Written { get; } = [];

        public Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            Written.Add(auditEvent);
            return Task.FromResult(SecurityAuditWriteResult.Recorded);
        }
    }

    private sealed class UnavailableWriter : ISecurityAuditWriter
    {
        public Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken) =>
            throw new SecurityAuditStoreException("Audit unavailable.", new InvalidOperationException());
    }

    private sealed class TestRequestContext(string? tenantId, string userId) : IRequestContext
    {
        public string Authority => "test";
        public string? ClientId => null;
        public string? OwnerId => null;
        public string? UserId => userId;
        public string? TenantId => tenantId;
        public string? ResourceId => null;
        public string? TraceId => null;
        public string? RequestId => "request-1";
        public string? AdditionalInfo => null;
        public ClaimsPrincipal? User => new(new ClaimsIdentity([], "test"));
        public Request GetRequest() => new() { Authority = Authority, TenantId = TenantId };
    }
}
