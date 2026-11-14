using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Journey.Application.Abstractions;
using Vitarish.Journey.Infrastructure.ReturnUrl;
using Vitarish.Parties.Application.Apps;
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

        IReadOnlyList<WorkspaceDirectoryEntry> clinics = await ListAsync(database,
            $"{owned}:{IdentityDefinitions.TenantUser}",
            $"{owned}:{IdentityDefinitions.TenantOwner}",
            $"{joined}:{IdentityDefinitions.TenantUser}",
            $"{deleted}:{IdentityDefinitions.TenantOwner}",
            $"{inactiveHost}:{IdentityDefinitions.TenantOwner}",
            IdentityDefinitions.PlatformAdmin,
            "not-a-tenant-role");

        clinics.Should().BeEquivalentTo([
            new WorkspaceDirectoryEntry(owned, "clinic", "Owned clinic", "owned.clinic.example.test", IdentityDefinitions.TenantOwner, HasLogo: true),
            new WorkspaceDirectoryEntry(joined, "clinic", "Joined clinic", "joined.clinic.example.test", IdentityDefinitions.TenantUser, HasLogo: false)]);
        clinics.Should().NotContain(clinic => clinic.TenantId == notMine);
    }

    [Fact]
    public async Task Lists_only_tenants_for_catalog_apps_that_host_workspaces()
    {
        string database = Guid.NewGuid().ToString("N");
        Guid clinic = await SeedTenantAsync(database, "Clinic", "clinic.example.test", appId: "clinic");
        Guid landing = await SeedTenantAsync(database, "Landing", "landing.example.test", appId: "landing");
        Guid platform = await SeedTenantAsync(database, "Platform", "platform.example.test", appId: "platform");
        PlatformAppCatalog catalog = new([
            new PlatformApp("clinic", "Clinic", PlatformAppAvailability.Live, HostsWorkspaces: true, SelfService: null),
            new PlatformApp("landing", "Landing", PlatformAppAvailability.Live, HostsWorkspaces: false, SelfService: null),
            new PlatformApp("platform", "Platform", PlatformAppAvailability.Live, HostsWorkspaces: false, SelfService: null)]);

        IReadOnlyList<WorkspaceDirectoryEntry> workspaces = await ListAsync(
            database,
            catalog,
            $"{clinic}:{IdentityDefinitions.TenantOwner}",
            $"{landing}:{IdentityDefinitions.TenantOwner}",
            $"{platform}:{IdentityDefinitions.TenantOwner}");

        workspaces.Should().ContainSingle().Which.Should().Be(
            new WorkspaceDirectoryEntry(clinic, "clinic", "Clinic", "clinic.example.test", IdentityDefinitions.TenantOwner, HasLogo: false));
    }

    [Fact]
    public async Task Lists_only_the_requested_apps_workspaces_when_an_app_is_given()
    {
        Guid clinic = Guid.NewGuid();
        Guid project = Guid.NewGuid();
        Mock<IWorkspaceDirectory> directory = new();
        directory.Setup(item => item.ListAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync([
            new WorkspaceDirectoryEntry(project, "projects", "Projects", "projects.example.test", IdentityDefinitions.TenantOwner, HasLogo: false),
            new WorkspaceDirectoryEntry(clinic, "clinic", "Clinic", "clinic.example.test", IdentityDefinitions.TenantOwner, HasLogo: false)]);
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(item => item.UserId).Returns(UserId);
        ListSignInClinicsQueryHandler handler = new(directory.Object, requestContext.Object);

        IReadOnlyList<WorkspaceDirectoryEntry>? clinicsOnly = await handler.Handle(new ListSignInClinicsQuery("clinic"), CancellationToken.None);
        IReadOnlyList<WorkspaceDirectoryEntry>? all = await handler.Handle(new ListSignInClinicsQuery(), CancellationToken.None);

        clinicsOnly.Should().ContainSingle().Which.TenantId.Should().Be(clinic);
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task Hands_off_only_to_a_clinic_of_the_user_at_its_own_host()
    {
        Guid mine = Guid.NewGuid();
        Mock<ITenantHandoffIssuer> issuer = new();
        issuer.Setup(item => item.IssueEndingSessionAsync(UserId, "mine.clinic.example.test", It.IsAny<CancellationToken>())).ReturnsAsync("token");
        IssueSignInHandoffCommandHandler handler = CreateHandler(issuer, [new WorkspaceDirectoryEntry(mine, "clinic", "Mine", "mine.clinic.example.test", IdentityDefinitions.TenantOwner, HasLogo: false)]);

        SignInHandoff? handoff = await handler.Handle(new IssueSignInHandoffCommand(mine), CancellationToken.None);
        SignInHandoff? foreign = await handler.Handle(new IssueSignInHandoffCommand(Guid.NewGuid()), CancellationToken.None);

        handoff.Should().Be(new SignInHandoff("mine.clinic.example.test", "token"));
        foreign.Should().BeNull();
        issuer.Verify(item => item.IssueEndingSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        issuer.Verify(item => item.IssueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Hands_off_to_the_configured_onboarding_host_and_refuses_without_one()
    {
        Mock<ITenantHandoffIssuer> issuer = new();
        issuer.Setup(item => item.IssueEndingSessionAsync(UserId, "start.clinic.example.test", It.IsAny<CancellationToken>())).ReturnsAsync("token");

        SignInHandoff? configured = await CreateHandler(issuer, [], "https://start.clinic.example.test")
            .Handle(new IssueSignInHandoffCommand(null), CancellationToken.None);
        SignInHandoff? missing = await CreateHandler(issuer, [], null)
            .Handle(new IssueSignInHandoffCommand(null), CancellationToken.None);

        configured.Should().Be(new SignInHandoff("start.clinic.example.test", "token"));
        missing.Should().BeNull();
    }

    [Theory]
    [InlineData("/workspaces", "/workspaces")]
    [InlineData("https://evil.example.test/workspaces", null)]
    [InlineData("//evil.example.test/workspaces", null)]
    public async Task Uses_configured_landing_host_and_normalizes_return_paths(string returnTo, string? expectedReturnTo)
    {
        Mock<ITenantHandoffIssuer> issuer = new();
        issuer.Setup(item => item.IssueEndingSessionAsync(UserId, "landing.example.test", It.IsAny<CancellationToken>())).ReturnsAsync("token");
        IssueSignInHandoffCommandHandler handler = CreateHandler(
            issuer,
            [],
            landingUrl: "https://landing.example.test/home",
            returnUrlPolicy: new JourneyReturnUrlPolicy());

        SignInHandoff? handoff = await handler.Handle(
            new IssueSignInHandoffCommand(null, SignInHandoffTarget.Home, returnTo),
            CancellationToken.None);

        handoff.Should().Be(new SignInHandoff("landing.example.test", "token", expectedReturnTo));
        issuer.Verify(item => item.IssueEndingSessionAsync(UserId, "landing.example.test", It.IsAny<CancellationToken>()), Times.Once);
        issuer.Verify(item => item.IssueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refuses_the_onboarding_handoff_while_tenant_onboarding_is_off()
    {
        Mock<ITenantHandoffIssuer> issuer = new();

        SignInHandoff? handoff = await CreateHandler(issuer, [], "https://start.clinic.example.test", tenantOnboardingEnabled: false)
            .Handle(new IssueSignInHandoffCommand(null), CancellationToken.None);

        handoff.Should().BeNull();
        issuer.VerifyNoOtherCalls();
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
        IReadOnlyList<WorkspaceDirectoryEntry> clinics,
        string? onboardingUrl = null,
        string? userId = UserId,
        bool tenantOnboardingEnabled = true,
        string? landingUrl = null,
        IJourneyReturnUrlPolicy? returnUrlPolicy = null)
    {
        Mock<IWorkspaceDirectory> directory = new();
        directory.Setup(item => item.ListAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(clinics);
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(context => context.UserId).Returns(userId);
        return new IssueSignInHandoffCommandHandler(
            directory.Object,
            issuer.Object,
            Options.Create(new OnboardingHostOptions { OnboardingUrl = onboardingUrl, TenantOnboardingEnabled = tenantOnboardingEnabled }),
            Options.Create(new SignInHomeOptions { LandingUrl = landingUrl ?? string.Empty }),
            returnUrlPolicy ?? new JourneyReturnUrlPolicy(),
            requestContext.Object);
    }

    private static Task<IReadOnlyList<WorkspaceDirectoryEntry>> ListAsync(string database, params string[] roleNames) =>
        ListAsync(database, new PlatformAppCatalog([new PlatformApp("clinic", "Clinic", PlatformAppAvailability.Live, HostsWorkspaces: true, SelfService: null)]), roleNames);

    private static async Task<IReadOnlyList<WorkspaceDirectoryEntry>> ListAsync(string database, IPlatformAppCatalog appCatalog, params string[] roleNames)
    {
        Mock<ISender> sender = new();
        sender.Setup(item => item.Send(It.Is<GetUserRolesQuery>(query => query.UserId == UserId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(roleNames.Select(name => new IdentityRole(name)));
        await using PartiesDbContext db = CreateContext(database);
        return await new WorkspaceDirectory(sender.Object, db, appCatalog).ListAsync(UserId);
    }

    private static async Task<Guid> SeedTenantAsync(
        string database,
        string name,
        string hostname,
        string appId = "clinic",
        bool deleted = false,
        IndicatorStatus status = IndicatorStatus.Active,
        string? logoUrl = null)
    {
        Guid tenantId = Guid.NewGuid();
        await using PartiesDbContext db = CreateContext(database);
        _ = db.Tenants.Add(new Tenant { Id = tenantId, OwnerId = Guid.NewGuid(), Name = name, LogoUrl = logoUrl, AppId = appId, DeletedAt = deleted ? DateTime.UtcNow : null });
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
