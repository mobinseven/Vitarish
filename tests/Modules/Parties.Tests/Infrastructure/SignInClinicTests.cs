using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Application.SignIn;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Services;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Queries;

namespace Vitarish.Parties.Tests.Infrastructure;

public sealed class SignInClinicTests
{
    private const string UserId = "user-1";

    [Fact]
    public async Task Lists_each_live_clinic_the_user_has_a_tenant_role_in_at_its_active_primary_host()
    {
        string database = Guid.NewGuid().ToString("N");
        Guid owned = await SeedTenantAsync(database, "Owned clinic", "owned.clinic.example.test", logoUrl: "/logo");
        Guid joined = await SeedTenantAsync(database, "Joined clinic", "joined.clinic.example.test");
        Guid deleted = await SeedTenantAsync(database, "Deleted clinic", "deleted.clinic.example.test", deleted: true);
        Guid inactiveHost = await SeedTenantAsync(database, "Inactive clinic", "inactive.clinic.example.test", status: IndicatorStatus.Disabled);
        Guid notMine = await SeedTenantAsync(database, "Other clinic", "other.clinic.example.test");

        IReadOnlyList<SignInClinic> clinics = await ListAsync(database,
            $"{owned}:{IdentityDefinitions.TenantUser}",
            $"{owned}:{IdentityDefinitions.TenantOwner}",
            $"{joined}:{IdentityDefinitions.TenantUser}",
            $"{deleted}:{IdentityDefinitions.TenantOwner}",
            $"{inactiveHost}:{IdentityDefinitions.TenantOwner}",
            IdentityDefinitions.PlatformAdmin,
            "not-a-tenant-role");

        clinics.Should().BeEquivalentTo([
            new SignInClinic(owned, "Owned clinic", "owned.clinic.example.test", IdentityDefinitions.TenantOwner, HasLogo: true),
            new SignInClinic(joined, "Joined clinic", "joined.clinic.example.test", IdentityDefinitions.TenantUser, HasLogo: false)]);
        clinics.Should().NotContain(clinic => clinic.TenantId == notMine);
    }

    [Fact]
    public async Task Hands_off_only_to_a_clinic_of_the_user_at_its_own_host()
    {
        Guid mine = Guid.NewGuid();
        Mock<ITenantHandoffIssuer> issuer = new();
        issuer.Setup(item => item.IssueAsync(UserId, "mine.clinic.example.test", It.IsAny<CancellationToken>())).ReturnsAsync("token");
        IssueSignInHandoffCommandHandler handler = CreateHandler(issuer, [new SignInClinic(mine, "Mine", "mine.clinic.example.test", IdentityDefinitions.TenantOwner, HasLogo: false)]);

        SignInHandoff? handoff = await handler.Handle(new IssueSignInHandoffCommand(mine), CancellationToken.None);
        SignInHandoff? foreign = await handler.Handle(new IssueSignInHandoffCommand(Guid.NewGuid()), CancellationToken.None);

        handoff.Should().Be(new SignInHandoff("mine.clinic.example.test", "token"));
        foreign.Should().BeNull();
        issuer.Verify(item => item.IssueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Hands_off_to_the_configured_onboarding_host_and_refuses_without_one()
    {
        Mock<ITenantHandoffIssuer> issuer = new();
        issuer.Setup(item => item.IssueAsync(UserId, "start.clinic.example.test", It.IsAny<CancellationToken>())).ReturnsAsync("token");

        SignInHandoff? configured = await CreateHandler(issuer, [], "https://start.clinic.example.test")
            .Handle(new IssueSignInHandoffCommand(null), CancellationToken.None);
        SignInHandoff? missing = await CreateHandler(issuer, [], null)
            .Handle(new IssueSignInHandoffCommand(null), CancellationToken.None);

        configured.Should().Be(new SignInHandoff("start.clinic.example.test", "token"));
        missing.Should().BeNull();
    }

    [Fact]
    public async Task Refuses_a_handoff_without_a_signed_in_user()
    {
        Mock<ITenantHandoffIssuer> issuer = new();

        SignInHandoff? handoff = await CreateHandler(issuer, [], "https://start.clinic.example.test", userId: null)
            .Handle(new IssueSignInHandoffCommand(null), CancellationToken.None);

        handoff.Should().BeNull();
        issuer.VerifyNoOtherCalls();
    }

    private static IssueSignInHandoffCommandHandler CreateHandler(
        Mock<ITenantHandoffIssuer> issuer,
        IReadOnlyList<SignInClinic> clinics,
        string? onboardingUrl = null,
        string? userId = UserId)
    {
        Mock<ISignInClinicDirectory> directory = new();
        directory.Setup(item => item.ListAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(clinics);
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(context => context.UserId).Returns(userId);
        return new IssueSignInHandoffCommandHandler(
            directory.Object,
            issuer.Object,
            Options.Create(new OnboardingHostOptions { OnboardingUrl = onboardingUrl }),
            requestContext.Object);
    }

    private static async Task<IReadOnlyList<SignInClinic>> ListAsync(string database, params string[] roleNames)
    {
        Mock<ISender> sender = new();
        sender.Setup(item => item.Send(It.Is<GetUserRolesQuery>(query => query.UserId == UserId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(roleNames.Select(name => new IdentityRole(name)));
        await using PartiesDbContext db = CreateContext(database);
        return await new SignInClinicDirectory(sender.Object, db).ListAsync(UserId);
    }

    private static async Task<Guid> SeedTenantAsync(
        string database,
        string name,
        string hostname,
        bool deleted = false,
        IndicatorStatus status = IndicatorStatus.Active,
        string? logoUrl = null)
    {
        Guid tenantId = Guid.NewGuid();
        await using PartiesDbContext db = CreateContext(database);
        _ = db.Tenants.Add(new Tenant { Id = tenantId, OwnerId = Guid.NewGuid(), Name = name, LogoUrl = logoUrl, AppId = "clinic", DeletedAt = deleted ? DateTime.UtcNow : null });
        _ = db.TenantIndicators.Add(new TenantIndicator
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Method = IndicatorMethod.Hostname,
            Value = hostname,
            Status = status,
            IsPrimary = true
        });
        _ = await db.SaveChangesAsync();
        return tenantId;
    }

    private static PartiesDbContext CreateContext(string database) => new(
        new DbContextOptionsBuilder<PartiesDbContext>().UseInMemoryDatabase(database).Options,
        Mock.Of<IPublisher>(),
        Array.Empty<IInterceptor>());
}
