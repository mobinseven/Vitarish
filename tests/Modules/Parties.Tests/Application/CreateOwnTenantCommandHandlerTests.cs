using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Parties.Application.Addresses;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Parties.Tests.Application;

public sealed class CreateOwnTenantCommandHandlerTests
{
    private const string BaseDomain = "clinic.example.test";
    private const string CreatorUserId = "user-1";
    private const string IdempotencyKey = "idem-key-1234";
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly SelfServiceTenantProfile Profile = new("clinic-app", ["Clinic"]);

    [Fact]
    public async Task Creates_a_tenant_with_trimmed_fields_and_records_a_succeeded_audit_event()
    {
        Mock<ITenantCreatorDirectory> creators = new();
        creators.Setup(item => item.FindAsync(CreatorUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantCreator(CreatorUserId, HasVerifiedPhone: true));

        NewSelfServiceTenant? captured = null;
        Mock<ISelfServiceTenantStore> store = new();
        store.Setup(item => item.FindAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExistingSelfServiceTenant?)null);
        store.Setup(item => item.CountCreatedByAsync(CreatorUserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        store.Setup(item => item.CreateAsync(It.IsAny<NewSelfServiceTenant>(), It.IsAny<CancellationToken>()))
            .Callback<NewSelfServiceTenant, CancellationToken>((tenant, _) => captured = tenant)
            .ReturnsAsync(TenantProvisioningOutcome.Created);

        Mock<ISecurityAuditWriter> audit = AuditWriter();
        CreateOwnTenantCommandHandler handler = CreateHandler(creators.Object, store.Object, audit.Object);

        CreateOwnTenantResult result = await handler.Handle(
            new CreateOwnTenantCommand(CreatorUserId, "  Rezaei Clinic  ", " Rezaei ", "   ", IdempotencyKey),
            CancellationToken.None);

        captured.Should().NotBeNull();
        result.Status.Should().Be(CreateOwnTenantStatus.Created);
        result.Hostname.Should().Be("rezaei.clinic.example.test");
        result.TenantId.Should().Be(captured!.TenantId);
        captured.Name.Should().Be("Rezaei Clinic");
        captured.Description.Should().BeNull();
        captured.Profile.Should().BeSameAs(Profile);
        captured.CreatorUserId.Should().Be(CreatorUserId);

        audit.Verify(writer => writer.WriteAsync(
            It.Is<SecurityAuditEvent>(entry =>
                entry.Outcome == SecurityAuditOutcome.Succeeded &&
                entry.EventName == PartiesSecurityAuditEvents.SelfServiceTenantCreation &&
                entry.TenantId == captured.TenantId.ToString()),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Derives_a_deterministic_tenant_id_per_creator_and_idempotency_key()
    {
        Mock<ITenantCreatorDirectory> creators = new();
        creators.Setup(item => item.FindAsync(CreatorUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantCreator(CreatorUserId, HasVerifiedPhone: true));

        List<NewSelfServiceTenant> captured = [];
        Mock<ISelfServiceTenantStore> store = new();
        store.Setup(item => item.CreateAsync(It.IsAny<NewSelfServiceTenant>(), It.IsAny<CancellationToken>()))
            .Callback<NewSelfServiceTenant, CancellationToken>((tenant, _) => captured.Add(tenant))
            .ReturnsAsync(TenantProvisioningOutcome.Created);

        CreateOwnTenantCommandHandler handler = CreateHandler(creators.Object, store.Object, AuditWriter().Object);

        CreateOwnTenantResult first = await handler.Handle(
            new CreateOwnTenantCommand(CreatorUserId, "Clinic", "rezaei", null, "key-alpha-1"),
            CancellationToken.None);
        CreateOwnTenantResult secondSameKey = await handler.Handle(
            new CreateOwnTenantCommand(CreatorUserId, "Clinic", "rezaei", null, "key-alpha-1"),
            CancellationToken.None);
        CreateOwnTenantResult differentKey = await handler.Handle(
            new CreateOwnTenantCommand(CreatorUserId, "Clinic", "rezaei", null, "key-beta-2"),
            CancellationToken.None);

        captured.Should().HaveCount(3);
        first.TenantId.Should().Be(secondSameKey.TenantId);
        captured[0].TenantId.Should().Be(captured[1].TenantId);
        captured[0].OwnerId.Should().Be(captured[1].OwnerId);
        differentKey.TenantId.Should().NotBe(first.TenantId!.Value);
        captured[2].TenantId.Should().NotBe(captured[0].TenantId);
    }

    [Fact]
    public async Task Replays_an_existing_matching_tenant_without_looking_up_the_creator_or_creating_again()
    {
        Mock<ITenantCreatorDirectory> creators = new(MockBehavior.Strict);
        Mock<ISelfServiceTenantStore> store = new();
        Guid existingId = Guid.Empty;
        store.Setup(item => item.FindAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((id, _) => existingId = id)
            .ReturnsAsync((Guid id, CancellationToken _) => new ExistingSelfServiceTenant(
                id,
                "Clinic",
                "Care",
                "rezaei.clinic.example.test"));

        CreateOwnTenantCommandHandler handler = CreateHandler(creators.Object, store.Object, AuditWriter().Object);

        CreateOwnTenantResult result = await handler.Handle(
            new CreateOwnTenantCommand(CreatorUserId, "Clinic", "rezaei", "Care", IdempotencyKey),
            CancellationToken.None);

        result.Status.Should().Be(CreateOwnTenantStatus.Replayed);
        result.TenantId.Should().Be(existingId);
        result.Hostname.Should().Be("rezaei.clinic.example.test");
        store.Verify(item => item.CreateAsync(It.IsAny<NewSelfServiceTenant>(), It.IsAny<CancellationToken>()), Times.Never);
        creators.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Reports_an_idempotency_conflict_when_the_replayed_name_differs()
    {
        Mock<ITenantCreatorDirectory> creators = new(MockBehavior.Strict);
        Mock<ISelfServiceTenantStore> store = new();
        store.Setup(item => item.FindAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => new ExistingSelfServiceTenant(
                id,
                "Original Clinic",
                null,
                "rezaei.clinic.example.test"));

        CreateOwnTenantCommandHandler handler = CreateHandler(creators.Object, store.Object, AuditWriter().Object);

        CreateOwnTenantResult result = await handler.Handle(
            new CreateOwnTenantCommand(CreatorUserId, "Different Clinic", "rezaei", null, IdempotencyKey),
            CancellationToken.None);

        result.Status.Should().Be(CreateOwnTenantStatus.IdempotencyConflict);
        store.Verify(item => item.CreateAsync(It.IsAny<NewSelfServiceTenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("excessive-name", null)]
    [InlineData("Valid Name", "excessive-description")]
    public async Task Rejects_invalid_name_or_description_without_touching_the_store(string nameToken, string? descriptionToken)
    {
        string name = nameToken == "excessive-name"
            ? new string('n', CreateOwnTenantCommandHandler.NameMaximumLength + 1)
            : nameToken;
        string? description = descriptionToken == "excessive-description"
            ? new string('d', CreateOwnTenantCommandHandler.DescriptionMaximumLength + 1)
            : descriptionToken;

        Mock<ITenantCreatorDirectory> creators = new(MockBehavior.Strict);
        Mock<ISelfServiceTenantStore> store = new(MockBehavior.Strict);
        CreateOwnTenantCommandHandler handler = CreateHandler(creators.Object, store.Object, AuditWriter().Object);

        CreateOwnTenantResult result = await handler.Handle(
            new CreateOwnTenantCommand(CreatorUserId, name, "rezaei", description, IdempotencyKey),
            CancellationToken.None);

        result.Status.Should().Be(CreateOwnTenantStatus.Invalid);
        store.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("ab", CreateOwnTenantStatus.AddressMalformed)]
    [InlineData("admin", CreateOwnTenantStatus.AddressReserved)]
    public async Task Rejects_malformed_and_reserved_subdomains(string subdomain, CreateOwnTenantStatus expected)
    {
        Mock<ITenantCreatorDirectory> creators = new(MockBehavior.Strict);
        Mock<ISelfServiceTenantStore> store = new(MockBehavior.Strict);
        CreateOwnTenantCommandHandler handler = CreateHandler(creators.Object, store.Object, AuditWriter().Object);

        CreateOwnTenantResult result = await handler.Handle(
            new CreateOwnTenantCommand(CreatorUserId, "Clinic", subdomain, null, IdempotencyKey),
            CancellationToken.None);

        result.Status.Should().Be(expected);
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Denies_when_creator_is_not_found_and_audits_the_denial()
    {
        Mock<ITenantCreatorDirectory> creators = new();
        creators.Setup(item => item.FindAsync(CreatorUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantCreator?)null);
        Mock<ISelfServiceTenantStore> store = new();
        Mock<ISecurityAuditWriter> audit = AuditWriter();
        CreateOwnTenantCommandHandler handler = CreateHandler(creators.Object, store.Object, audit.Object);

        CreateOwnTenantResult result = await handler.Handle(
            new CreateOwnTenantCommand(CreatorUserId, "Clinic", "rezaei", null, IdempotencyKey),
            CancellationToken.None);

        result.Status.Should().Be(CreateOwnTenantStatus.CreatorNotFound);
        store.Verify(item => item.CreateAsync(It.IsAny<NewSelfServiceTenant>(), It.IsAny<CancellationToken>()), Times.Never);
        audit.Verify(writer => writer.WriteAsync(
            It.Is<SecurityAuditEvent>(entry => entry.Outcome == SecurityAuditOutcome.Denied),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Denies_when_creator_phone_is_not_verified_and_audits_the_denial()
    {
        Mock<ITenantCreatorDirectory> creators = new();
        creators.Setup(item => item.FindAsync(CreatorUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantCreator(CreatorUserId, HasVerifiedPhone: false));
        Mock<ISelfServiceTenantStore> store = new();
        Mock<ISecurityAuditWriter> audit = AuditWriter();
        CreateOwnTenantCommandHandler handler = CreateHandler(creators.Object, store.Object, audit.Object);

        CreateOwnTenantResult result = await handler.Handle(
            new CreateOwnTenantCommand(CreatorUserId, "Clinic", "rezaei", null, IdempotencyKey),
            CancellationToken.None);

        result.Status.Should().Be(CreateOwnTenantStatus.PhoneNotVerified);
        store.Verify(item => item.CreateAsync(It.IsAny<NewSelfServiceTenant>(), It.IsAny<CancellationToken>()), Times.Never);
        audit.Verify(writer => writer.WriteAsync(
            It.Is<SecurityAuditEvent>(entry => entry.Outcome == SecurityAuditOutcome.Denied),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Denies_when_the_creator_has_reached_the_daily_creation_limit()
    {
        Mock<ITenantCreatorDirectory> creators = new();
        creators.Setup(item => item.FindAsync(CreatorUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantCreator(CreatorUserId, HasVerifiedPhone: true));

        DateTime expectedSince = FixedNow.UtcDateTime - TimeSpan.FromDays(1);
        Mock<ISelfServiceTenantStore> store = new();
        store.Setup(item => item.CountCreatedByAsync(CreatorUserId, expectedSince, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        Mock<ISecurityAuditWriter> audit = AuditWriter();
        CreateOwnTenantCommandHandler handler = CreateHandler(creators.Object, store.Object, audit.Object);

        CreateOwnTenantResult result = await handler.Handle(
            new CreateOwnTenantCommand(CreatorUserId, "Clinic", "rezaei", null, IdempotencyKey),
            CancellationToken.None);

        result.Status.Should().Be(CreateOwnTenantStatus.LimitReached);
        store.Verify(item => item.CountCreatedByAsync(CreatorUserId, expectedSince, It.IsAny<CancellationToken>()), Times.Once);
        store.Verify(item => item.CreateAsync(It.IsAny<NewSelfServiceTenant>(), It.IsAny<CancellationToken>()), Times.Never);
        audit.Verify(writer => writer.WriteAsync(
            It.Is<SecurityAuditEvent>(entry => entry.Outcome == SecurityAuditOutcome.Denied),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reports_address_taken_and_audits_denial_when_store_reports_the_address_is_taken()
    {
        Mock<ITenantCreatorDirectory> creators = new();
        creators.Setup(item => item.FindAsync(CreatorUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantCreator(CreatorUserId, HasVerifiedPhone: true));

        Mock<ISelfServiceTenantStore> store = new();
        store.Setup(item => item.FindAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExistingSelfServiceTenant?)null);
        store.Setup(item => item.CountCreatedByAsync(CreatorUserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        store.Setup(item => item.CreateAsync(It.IsAny<NewSelfServiceTenant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TenantProvisioningOutcome.AddressTaken);

        Mock<ISecurityAuditWriter> audit = AuditWriter();
        CreateOwnTenantCommandHandler handler = CreateHandler(creators.Object, store.Object, audit.Object);

        CreateOwnTenantResult result = await handler.Handle(
            new CreateOwnTenantCommand(CreatorUserId, "Clinic", "rezaei", null, IdempotencyKey),
            CancellationToken.None);

        result.Status.Should().Be(CreateOwnTenantStatus.AddressTaken);
        audit.Verify(writer => writer.WriteAsync(
            It.Is<SecurityAuditEvent>(entry => entry.Outcome == SecurityAuditOutcome.Denied),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Preserves_the_created_result_when_the_security_audit_store_is_unavailable()
    {
        Mock<ITenantCreatorDirectory> creators = new();
        creators.Setup(item => item.FindAsync(CreatorUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantCreator(CreatorUserId, HasVerifiedPhone: true));

        Mock<ISelfServiceTenantStore> store = new();
        store.Setup(item => item.CreateAsync(It.IsAny<NewSelfServiceTenant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TenantProvisioningOutcome.Created);

        Mock<ISecurityAuditWriter> audit = new();
        audit.Setup(writer => writer.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SecurityAuditStoreException("Audit store unavailable.", new InvalidOperationException()));

        CreateOwnTenantCommandHandler handler = CreateHandler(creators.Object, store.Object, audit.Object);

        CreateOwnTenantResult result = await handler.Handle(
            new CreateOwnTenantCommand(CreatorUserId, "Clinic", "rezaei", null, IdempotencyKey),
            CancellationToken.None);

        result.Status.Should().Be(CreateOwnTenantStatus.Created);
        result.Hostname.Should().Be("rezaei.clinic.example.test");
    }

    [Fact]
    public async Task Fails_when_no_tenant_base_domain_is_configured()
    {
        CreateOwnTenantCommandHandler handler = new(
            Options.Create(new TenantAddressOptions()),
            Profile,
            Mock.Of<ITenantCreatorDirectory>(),
            Mock.Of<ISelfServiceTenantStore>(),
            Mock.Of<ISecurityAuditWriter>(),
            Mock.Of<IRequestContext>(),
            new FixedTimeProvider(FixedNow));

        Func<Task> act = () => handler.Handle(
            new CreateOwnTenantCommand(CreatorUserId, "Clinic", "rezaei", null, IdempotencyKey),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static CreateOwnTenantCommandHandler CreateHandler(
        ITenantCreatorDirectory creators,
        ISelfServiceTenantStore store,
        ISecurityAuditWriter auditWriter)
    {
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(context => context.UserId).Returns(CreatorUserId);
        return new CreateOwnTenantCommandHandler(
            Options.Create(new TenantAddressOptions { TenantBaseDomain = BaseDomain }),
            Profile,
            creators,
            store,
            auditWriter,
            requestContext.Object,
            new FixedTimeProvider(FixedNow));
    }

    private static Mock<ISecurityAuditWriter> AuditWriter()
    {
        Mock<ISecurityAuditWriter> audit = new();
        audit.Setup(writer => writer.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        return audit;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
