using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Domain;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Services;
using Vitarish.Users.Application.Commands;

namespace Vitarish.Parties.Tests.Infrastructure;

public sealed class SelfServiceTenantStoreTests
{
    private static readonly SelfServiceTenantProfile Profile = new("clinic-app", ["Clinic", "Appointments"]);

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
            new NewSelfServiceTenant(tenantId, ownerId, "user-1", "Rezaei Clinic", "Dental care", "Rezaei.Clinic.Example.Test.", Profile),
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
        TenantMetadata.GetActiveModules(tenant.Metadata).Should().Equal("Clinic", "Appointments");

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
            new NewSelfServiceTenant(Guid.NewGuid(), Guid.NewGuid(), "user-1", "Clinic", null, "rezaei.clinic.example.test", Profile),
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
            new NewSelfServiceTenant(tenantId, Guid.NewGuid(), "user-1", "Clinic", null, "rezaei.clinic.example.test", Profile),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        (await db.Owners.CountAsync()).Should().Be(0);
        (await db.Tenants.CountAsync()).Should().Be(0);
        (await db.TenantIndicators.CountAsync()).Should().Be(0);
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
            new NewSelfServiceTenant(tenantId, Guid.NewGuid(), "user-1", "Clinic", null, "rezaei.clinic.example.test", Profile),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Simulated Users failure.");
        (await db.Owners.CountAsync()).Should().Be(0);
        (await db.Tenants.CountAsync()).Should().Be(0);
        (await db.TenantIndicators.CountAsync()).Should().Be(0);
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
    public async Task Counts_only_tenants_created_by_the_user_at_or_after_the_cutoff()
    {
        await using PartiesDbContext db = CreateContext();
        DateTime since = new(2026, 4, 19, 12, 0, 0, DateTimeKind.Utc);
        db.Tenants.AddRange(
            new Tenant
            {
                Id = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Name = "At Cutoff",
                AppId = "clinic-app",
                CreatedBy = "user-1",
                CreatedAt = since
            },
            new Tenant
            {
                Id = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Name = "After Cutoff",
                AppId = "clinic-app",
                CreatedBy = "user-1",
                CreatedAt = since.AddHours(2)
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
                CreatedAt = since.AddHours(1)
            });
        await db.SaveChangesAsync();

        SelfServiceTenantStore store = CreateStore(db, Mock.Of<ISender>());

        int count = await store.CountCreatedByAsync("user-1", since, CancellationToken.None);

        count.Should().Be(2);
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
            new NewSelfServiceTenant(Guid.NewGuid(), Guid.NewGuid(), "user-1", "Clinic", null, "rezaei.clinic.example.test", Profile),
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
