using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Services;
using Vitarish.Users.Application.Commands;

namespace Vitarish.Parties.Tests.Infrastructure;

public sealed class SelfServiceTenantStoreTests
{
    private static readonly SelfServiceTenantProfile Profile = new("clinic-app");

    [Fact]
    public async Task Persists_owner_tenant_and_primary_subdomain_indicator_and_assigns_the_creator_as_owner()
    {
        await using PartiesDbContext db = CreateContext();
        Mock<ISender> sender = new();
        Guid tenantId = Guid.NewGuid();
        Guid ownerId = Guid.NewGuid();
        sender.Setup(item => item.Send(new AssignNewTenantOwnerCommand("user-1", tenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AssignNewTenantOwnerResult.Assigned);
        SelfServiceTenantStore store = CreateStore(db, sender.Object);

        TenantProvisioningOutcome outcome = await store.CreateAsync(
            new NewSelfServiceTenant(
                tenantId,
                ownerId,
                "user-1",
                "Rezaei Clinic",
                "Dental care",
                "Rezaei.Clinic.Example.Test.",
                Profile,
                new TenantCreationAllowance(DateTime.UtcNow.AddDays(-1), 3)),
            CancellationToken.None);

        outcome.Should().Be(TenantProvisioningOutcome.Created);

        Owner owner = await db.Owners.AsNoTracking().SingleAsync();
        owner.Id.Should().Be(ownerId);
        owner.Name.Should().Be("Rezaei Clinic");

        Tenant tenant = await db.Tenants.AsNoTracking().SingleAsync();
        tenant.Id.Should().Be(tenantId);
        tenant.OwnerId.Should().Be(ownerId);
        tenant.Name.Should().Be("Rezaei Clinic");
        tenant.Description.Should().Be("Dental care");
        tenant.AppId.Should().Be("clinic-app");

        TenantIndicator indicator = await db.TenantIndicators.AsNoTracking().SingleAsync();
        indicator.TenantId.Should().Be(tenantId);
        indicator.Method.Should().Be(IndicatorMethod.Subdomain);
        indicator.Status.Should().Be(IndicatorStatus.Active);
        indicator.IsPrimary.Should().BeTrue();
        indicator.Value.Should().Be("rezaei.clinic.example.test");

        sender.Verify(item => item.Send(new AssignNewTenantOwnerCommand("user-1", tenantId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reports_address_taken_when_any_indicator_already_holds_the_hostname()
    {
        await using PartiesDbContext db = CreateContext();
        Guid existingTenantId = Guid.NewGuid();
        db.TenantIndicators.Add(new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = existingTenantId,
            Method = IndicatorMethod.Hostname,
            Value = "rezaei.clinic.example.test",
            Status = IndicatorStatus.Disabled,
            IsPrimary = false
        });
        await db.SaveChangesAsync();

        Mock<ISender> sender = new(MockBehavior.Strict);
        SelfServiceTenantStore store = CreateStore(db, sender.Object);

        TenantProvisioningOutcome outcome = await store.CreateAsync(
            new NewSelfServiceTenant(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "user-1",
                "Clinic",
                null,
                "rezaei.clinic.example.test",
                Profile,
                new TenantCreationAllowance(DateTime.UtcNow.AddDays(-1), 3)),
            CancellationToken.None);

        outcome.Should().Be(TenantProvisioningOutcome.AddressTaken);
        (await db.Owners.CountAsync()).Should().Be(0);
        (await db.Tenants.CountAsync()).Should().Be(0);
        (await db.TenantIndicators.CountAsync()).Should().Be(1);
        sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Removes_persisted_rows_and_throws_when_owner_assignment_reports_existing_members()
    {
        await using PartiesDbContext db = CreateContext();
        Mock<ISender> sender = new();
        Guid tenantId = Guid.NewGuid();
        sender.Setup(item => item.Send(new AssignNewTenantOwnerCommand("user-1", tenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AssignNewTenantOwnerResult.TenantHasMembers);
        SelfServiceTenantStore store = CreateStore(db, sender.Object);

        Func<Task> act = () => store.CreateAsync(
            new NewSelfServiceTenant(
                tenantId,
                Guid.NewGuid(),
                "user-1",
                "Clinic",
                null,
                "rezaei.clinic.example.test",
                Profile,
                new TenantCreationAllowance(DateTime.UtcNow.AddDays(-1), 3)),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        (await db.Owners.CountAsync()).Should().Be(0);
        (await db.Tenants.CountAsync()).Should().Be(0);
        (await db.TenantIndicators.CountAsync()).Should().Be(0);
        sender.Verify(item => item.Send(new RevokeNewTenantOwnerCommand("user-1", tenantId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Removes_persisted_rows_and_propagates_the_exception_when_owner_assignment_throws()
    {
        await using PartiesDbContext db = CreateContext();
        Mock<ISender> sender = new();
        Guid tenantId = Guid.NewGuid();
        sender.Setup(item => item.Send(new AssignNewTenantOwnerCommand("user-1", tenantId), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated Users failure."));
        SelfServiceTenantStore store = CreateStore(db, sender.Object);

        Func<Task> act = () => store.CreateAsync(
            new NewSelfServiceTenant(
                tenantId,
                Guid.NewGuid(),
                "user-1",
                "Clinic",
                null,
                "rezaei.clinic.example.test",
                Profile,
                new TenantCreationAllowance(DateTime.UtcNow.AddDays(-1), 3)),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Simulated Users failure.");
        (await db.Owners.CountAsync()).Should().Be(0);
        (await db.Tenants.CountAsync()).Should().Be(0);
        (await db.TenantIndicators.CountAsync()).Should().Be(0);
        sender.Verify(item => item.Send(new RevokeNewTenantOwnerCommand("user-1", tenantId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Finds_an_existing_tenant_with_its_primary_hostname_and_returns_null_when_unknown()
    {
        await using PartiesDbContext db = CreateContext();
        Guid tenantId = Guid.NewGuid();
        db.Tenants.Add(new Tenant
        {
            Id = tenantId,
            OwnerId = Guid.NewGuid(),
            Name = "Rezaei Clinic",
            Description = "Skin clinic",
            AppId = "clinic-app"
        });
        db.TenantIndicators.AddRange(
            new TenantIndicator
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Method = IndicatorMethod.Subdomain,
                Value = "rezaei.clinic.example.test",
                Status = IndicatorStatus.Active,
                IsPrimary = true
            },
            new TenantIndicator
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Method = IndicatorMethod.Hostname,
                Value = "secondary.example.test",
                Status = IndicatorStatus.Active,
                IsPrimary = false
            });
        await db.SaveChangesAsync();

        SelfServiceTenantStore store = CreateStore(db, Mock.Of<ISender>());

        ExistingSelfServiceTenant? existing = await store.FindAsync(tenantId, CancellationToken.None);
        ExistingSelfServiceTenant? unknown = await store.FindAsync(Guid.NewGuid(), CancellationToken.None);

        existing.Should().Be(new ExistingSelfServiceTenant(tenantId, "Rezaei Clinic", "Skin clinic", "rezaei.clinic.example.test"));
        unknown.Should().BeNull();
    }

    [Fact]
    public async Task Returns_null_when_finding_a_soft_deleted_tenant()
    {
        await using PartiesDbContext db = CreateContext();
        Guid tenantId = Guid.NewGuid();
        db.Tenants.Add(new Tenant
        {
            Id = tenantId,
            OwnerId = Guid.NewGuid(),
            Name = "Deleted Clinic",
            AppId = "clinic-app",
            DeletedAt = DateTime.UtcNow
        });
        db.TenantIndicators.Add(new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Method = IndicatorMethod.Subdomain,
            Value = "deleted.clinic.example.test",
            Status = IndicatorStatus.Active,
            IsPrimary = true
        });
        await db.SaveChangesAsync();

        SelfServiceTenantStore store = CreateStore(db, Mock.Of<ISender>());

        ExistingSelfServiceTenant? result = await store.FindAsync(tenantId, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_limit_reached_without_adding_rows_or_sending_commands_when_creator_has_reached_the_allowance()
    {
        await using PartiesDbContext db = CreateContext();
        DateTime now = new(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);
        db.Tenants.AddRange(
            new Tenant
            {
                Id = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Name = "First",
                AppId = "clinic-app",
                CreatedBy = "user-1",
                CreatedAt = now
            },
            new Tenant
            {
                Id = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Name = "Second",
                AppId = "clinic-app",
                CreatedBy = "user-1",
                CreatedAt = now
            },
            new Tenant
            {
                Id = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Name = "Third",
                AppId = "clinic-app",
                CreatedBy = "user-1",
                CreatedAt = now
            });
        await db.SaveChangesAsync();

        Mock<ISender> sender = new(MockBehavior.Strict);
        SelfServiceTenantStore store = CreateStore(db, sender.Object);

        TenantProvisioningOutcome outcome = await store.CreateAsync(
            new NewSelfServiceTenant(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "user-1",
                "Fourth Clinic",
                null,
                "fourth.clinic.example.test",
                Profile,
                new TenantCreationAllowance(now.AddDays(-1), 3)),
            CancellationToken.None);

        outcome.Should().Be(TenantProvisioningOutcome.LimitReached);
        (await db.Owners.CountAsync()).Should().Be(0);
        (await db.Tenants.CountAsync()).Should().Be(3);
        (await db.TenantIndicators.CountAsync()).Should().Be(0);
        sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Does_not_count_tenants_created_before_the_allowance_window_or_by_other_users()
    {
        await using PartiesDbContext db = CreateContext();
        DateTime now = new(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);
        DateTime since = now.AddDays(-1);
        db.Tenants.AddRange(
            new Tenant
            {
                Id = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Name = "First Within Window",
                AppId = "clinic-app",
                CreatedBy = "user-1",
                CreatedAt = since
            },
            new Tenant
            {
                Id = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Name = "Second Within Window",
                AppId = "clinic-app",
                CreatedBy = "user-1",
                CreatedAt = now
            },
            new Tenant
            {
                Id = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Name = "Before Cutoff",
                AppId = "clinic-app",
                CreatedBy = "user-1",
                CreatedAt = since.AddMinutes(-1)
            },
            new Tenant
            {
                Id = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Name = "Other User",
                AppId = "clinic-app",
                CreatedBy = "user-2",
                CreatedAt = now
            });
        await db.SaveChangesAsync();

        Guid thirdTenantId = Guid.NewGuid();
        Mock<ISender> sender = new();
        sender.Setup(item => item.Send(new AssignNewTenantOwnerCommand("user-1", thirdTenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AssignNewTenantOwnerResult.Assigned);
        SelfServiceTenantStore store = CreateStore(db, sender.Object);

        TenantProvisioningOutcome outcome = await store.CreateAsync(
            new NewSelfServiceTenant(
                thirdTenantId,
                Guid.NewGuid(),
                "user-1",
                "Third Within Window",
                null,
                "third.clinic.example.test",
                Profile,
                new TenantCreationAllowance(since, 3)),
            CancellationToken.None);

        outcome.Should().Be(TenantProvisioningOutcome.Created);
    }

    [Fact]
    public async Task Ends_the_creators_sessions_as_replaced_before_assigning_the_owner_role()
    {
        await using PartiesDbContext db = CreateContext();
        List<string> calls = [];
        Mock<IAuthenticationSessionRegistry> sessions = new();
        sessions.Setup(item => item.RevokeActiveSessionsAsync("user-1", Vitarish.Authentication.Domain.Sessions.AuthenticationSessionTerminationReason.ReplacedByNewSession, It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("revoke"))
            .ReturnsAsync(1);
        Mock<ISender> sender = new();
        sender.Setup(item => item.Send(It.IsAny<AssignNewTenantOwnerCommand>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("assign"))
            .ReturnsAsync(AssignNewTenantOwnerResult.Assigned);
        SelfServiceTenantStore store = new(db, sender.Object, sessions.Object, NullLogger<SelfServiceTenantStore>.Instance);

        TenantProvisioningOutcome outcome = await store.CreateAsync(
            new NewSelfServiceTenant(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "user-1",
                "Clinic",
                null,
                "rezaei.clinic.example.test",
                Profile,
                new TenantCreationAllowance(DateTime.UtcNow.AddDays(-1), 3)),
            CancellationToken.None);

        outcome.Should().Be(TenantProvisioningOutcome.Created);
        calls.Should().Equal("revoke", "assign");
    }

    private static SelfServiceTenantStore CreateStore(PartiesDbContext db, ISender sender) =>
        new(db, sender, Mock.Of<IAuthenticationSessionRegistry>(), NullLogger<SelfServiceTenantStore>.Instance);

    private static PartiesDbContext CreateContext() => new(
        new DbContextOptionsBuilder<PartiesDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options,
        Mock.Of<IPublisher>(),
        Array.Empty<IInterceptor>());
}
