using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;
using Vitarish.Sea.Domain.Entities;
using Vitarish.Sea.Domain.Enums;
using Vitarish.Sea.Domain.Models;
using Vitarish.Sea.Infrastructure.Data;
using Vitarish.Sea.Infrastructure.BackgroundJobs;

namespace Vitarish.Sea.Tests;

public sealed class AttachmentIntegrityPersistenceTests
{
    private const string TenantId = "tenant-a";

    [Fact]
    public async Task SaveChanges_enrolls_a_verified_record_with_the_canonical_digest()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        Attachment attachment = CreateAttachment();

        await using SeaDbContext context = CreateContext(databaseName, withIntegrityInterceptor: true);
        _ = await context.Attachments.AddAsync(attachment);
        _ = await context.SaveChangesAsync();

        AttachmentIntegrityRecord record = await context.Set<AttachmentIntegrityRecord>().SingleAsync();
        record.AttachmentId.Should().Be(attachment.Id);
        record.TenantId.Should().Be(TenantId);
        record.Digest.Should().Be(AttachmentIntegrityProjection.ComputeDigest(attachment));
        record.State.Should().Be(AttachmentIntegrityState.Verified);
        record.VerifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveChanges_rejects_a_mutation_after_stored_content_changes_outside_its_integrity_record()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        Attachment attachment = CreateAttachment();
        await using (SeaDbContext enrolled = CreateContext(databaseName, withIntegrityInterceptor: true))
        {
            _ = await enrolled.Attachments.AddAsync(attachment);
            _ = await enrolled.SaveChangesAsync();
        }

        await using (SeaDbContext tampered = CreateContext(databaseName, withIntegrityInterceptor: false))
        {
            Attachment stored = await tampered.Attachments.SingleAsync();
            stored.Content = "tampered attachment text";
            _ = await tampered.SaveChangesAsync();
        }

        await using SeaDbContext protectedContext = CreateContext(databaseName, withIntegrityInterceptor: true);
        Attachment protectedAttachment = await protectedContext.Attachments.SingleAsync();
        protectedAttachment.Content = "requested attachment change";

        Func<Task> save = () => protectedContext.SaveChangesAsync();
        await save.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Attachment integrity is unavailable.");
    }

    [Fact]
    public async Task SaveChanges_rejects_a_legacy_attachment_without_an_integrity_record()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        await using (SeaDbContext legacy = CreateContext(databaseName, withIntegrityInterceptor: false))
        {
            _ = await legacy.Attachments.AddAsync(CreateAttachment());
            _ = await legacy.SaveChangesAsync();
        }

        await using SeaDbContext protectedContext = CreateContext(databaseName, withIntegrityInterceptor: true);
        Attachment attachment = await protectedContext.Attachments.IgnoreQueryFilters().SingleAsync();
        attachment.Content = "requested attachment change";

        Func<Task> save = () => protectedContext.SaveChangesAsync();
        await save.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Attachment integrity is unavailable.");
    }

    [Fact]
    public async Task VerificationProcessor_enrolls_a_legacy_attachment_and_audits_success()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        await using (SeaDbContext legacy = CreateContext(databaseName, withIntegrityInterceptor: false))
        {
            _ = await legacy.Attachments.AddAsync(CreateAttachment());
            _ = await legacy.SaveChangesAsync();
        }

        TestRequestContext requestContext = new();
        TestAuditWriter auditWriter = new();
        await using SeaDbContext context = CreateContext(databaseName, withIntegrityInterceptor: false);
        AttachmentIntegrityVerificationTenantProcessor processor = new(context, auditWriter, requestContext, NullLogger<AttachmentIntegrityVerificationTenantProcessor>.Instance);

        await processor.ProcessAsync(CancellationToken.None);

        AttachmentIntegrityRecord record = await context.Set<AttachmentIntegrityRecord>().SingleAsync();
        record.State.Should().Be(AttachmentIntegrityState.Verified);
        auditWriter.Events.Should().ContainSingle(eventItem => eventItem.Outcome == SecurityAuditOutcome.Succeeded);
    }

    [Fact]
    public async Task QueryFilter_hides_a_legacy_attachment_until_integrity_enrollment()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        await using (SeaDbContext legacy = CreateContext(databaseName, withIntegrityInterceptor: false))
        {
            _ = await legacy.Attachments.AddAsync(CreateAttachment());
            _ = await legacy.SaveChangesAsync();
        }

        await using SeaDbContext context = CreateContext(databaseName, withIntegrityInterceptor: false);
        (await context.Attachments.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task VerificationProcessor_quarantines_changed_attachment_and_audits_failure()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        Attachment attachment = CreateAttachment();
        await using (SeaDbContext enrolled = CreateContext(databaseName, withIntegrityInterceptor: true))
        {
            _ = await enrolled.Attachments.AddAsync(attachment);
            _ = await enrolled.SaveChangesAsync();
        }

        await using (SeaDbContext tampered = CreateContext(databaseName, withIntegrityInterceptor: false))
        {
            (await tampered.Attachments.SingleAsync()).Content = "tampered attachment text";
            _ = await tampered.SaveChangesAsync();
        }

        TestRequestContext requestContext = new();
        TestAuditWriter auditWriter = new();
        await using SeaDbContext context = CreateContext(databaseName, withIntegrityInterceptor: false);
        AttachmentIntegrityVerificationTenantProcessor processor = new(context, auditWriter, requestContext, NullLogger<AttachmentIntegrityVerificationTenantProcessor>.Instance);

        await processor.ProcessAsync(CancellationToken.None);

        AttachmentIntegrityRecord record = await context.Set<AttachmentIntegrityRecord>().SingleAsync();
        record.State.Should().Be(AttachmentIntegrityState.Unavailable);
        auditWriter.Events.Should().ContainSingle(eventItem => eventItem.Outcome == SecurityAuditOutcome.Failed);
    }

    [Fact]
    public async Task VerificationProcessor_quarantines_a_missing_expected_attachment_and_audits_failure()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        Attachment attachment = CreateAttachment();
        await using (SeaDbContext enrolled = CreateContext(databaseName, withIntegrityInterceptor: true))
        {
            _ = await enrolled.Attachments.AddAsync(attachment);
            _ = await enrolled.SaveChangesAsync();
        }

        await using (SeaDbContext bypassedDeletion = CreateContext(databaseName, withIntegrityInterceptor: false))
        {
            bypassedDeletion.Remove(await bypassedDeletion.Attachments.IgnoreQueryFilters().SingleAsync());
            _ = await bypassedDeletion.SaveChangesAsync();
        }

        TestRequestContext requestContext = new();
        TestAuditWriter auditWriter = new();
        await using SeaDbContext context = CreateContext(databaseName, withIntegrityInterceptor: false);
        AttachmentIntegrityVerificationTenantProcessor processor = new(context, auditWriter, requestContext, NullLogger<AttachmentIntegrityVerificationTenantProcessor>.Instance);

        await processor.ProcessAsync(CancellationToken.None);

        AttachmentIntegrityRecord record = await context.Set<AttachmentIntegrityRecord>().SingleAsync();
        record.State.Should().Be(AttachmentIntegrityState.Unavailable);
        auditWriter.Events.Should().ContainSingle(eventItem => eventItem.Outcome == SecurityAuditOutcome.Failed);
    }

    [Fact]
    public async Task Authorized_attachment_deletion_removes_its_integrity_record()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        Attachment attachment = CreateAttachment();
        await using SeaDbContext context = CreateContext(databaseName, withIntegrityInterceptor: true);
        _ = await context.Attachments.AddAsync(attachment);
        _ = await context.SaveChangesAsync();

        context.Remove(attachment);
        _ = await context.SaveChangesAsync();

        (await context.Set<AttachmentIntegrityRecord>().IgnoreQueryFilters().ToListAsync()).Should().BeEmpty();
    }

    private static SeaDbContext CreateContext(string databaseName, bool withIntegrityInterceptor)
    {
        TestRequestContext requestContext = new();
        List<IInterceptor> interceptors = withIntegrityInterceptor
            ? [new SeaAttachmentIntegrityInterceptor(requestContext)]
            : [];
        DbContextOptions<SeaDbContext> options = new DbContextOptionsBuilder<SeaDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new SeaDbContext(options, interceptors, requestContext, new HttpContextAccessor());
    }

    private static Attachment CreateAttachment()
    {
        Attachment attachment = Attachment.Create(
            Guid.Parse("8a1b21cf-7afd-4ff8-95ae-768b2fb7a0c8"),
            "attachment text",
            "text/plain",
            "thumbnail reference",
            15,
            AttachmentKind.File,
            "report.txt").EnsureValue();
        attachment.Id = Guid.NewGuid();
        return attachment;
    }

    private sealed class TestRequestContext : IRequestContext
    {
        public string Authority => "test";
        public string? ClientId => null;
        public string? OwnerId => null;
        public string? UserId => null;
        public string? TenantId => AttachmentIntegrityPersistenceTests.TenantId;
        public string? ResourceId => null;
        public string? TraceId => null;
        public string? RequestId => null;
        public string? AdditionalInfo => null;
        public System.Security.Claims.ClaimsPrincipal? User => null;
        public Request GetRequest() => new() { TenantId = TenantId };
    }

    private sealed class TestAuditWriter : ISecurityAuditWriter
    {
        public List<SecurityAuditEvent> Events { get; } = [];

        public Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            Events.Add(auditEvent);
            return Task.FromResult(SecurityAuditWriteResult.Recorded);
        }
    }
}
