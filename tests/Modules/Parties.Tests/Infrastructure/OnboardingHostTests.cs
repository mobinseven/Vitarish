using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Vitarish.Parties.Application;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Services;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Parties.Tests.Infrastructure;

public sealed class OnboardingHostTests
{
    [Fact]
    public async Task Recognizes_only_the_live_onboarding_tenant_as_the_onboarding_host()
    {
        string database = Guid.NewGuid().ToString("N");
        Guid onboarding = await SeedAsync(database, OnboardingTenant.AppId);
        Guid clinic = await SeedAsync(database, "clinic-app");
        Guid deletedOnboarding = await SeedAsync(database, OnboardingTenant.AppId, deleted: true);

        (await AllowsAsync(database, onboarding.ToString())).Should().BeTrue();
        (await AllowsAsync(database, clinic.ToString())).Should().BeFalse();
        (await AllowsAsync(database, deletedOnboarding.ToString())).Should().BeFalse();
        (await AllowsAsync(database, Guid.NewGuid().ToString())).Should().BeFalse();
        (await AllowsAsync(database, null)).Should().BeFalse();
        (await AllowsAsync(database, "not-a-tenant-id")).Should().BeFalse();
    }

    [Fact]
    public async Task Recognizes_only_the_live_admin_tenant_as_the_admin_host()
    {
        string database = Guid.NewGuid().ToString("N");
        Guid admin = await SeedAsync(database, PlatformAdminTenant.AppId);
        Guid onboarding = await SeedAsync(database, OnboardingTenant.AppId);
        Guid deletedAdmin = await SeedAsync(database, PlatformAdminTenant.AppId, deleted: true);

        (await IsAdminHostAsync(database, admin.ToString())).Should().BeTrue();
        (await IsAdminHostAsync(database, onboarding.ToString())).Should().BeFalse();
        (await IsAdminHostAsync(database, deletedAdmin.ToString())).Should().BeFalse();
        (await IsAdminHostAsync(database, null)).Should().BeFalse();
    }

    private static async Task<bool> IsAdminHostAsync(string database, string? tenantId)
    {
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(context => context.TenantId).Returns(tenantId);
        await using PartiesDbContext db = CreateContext(database);
        return await new IsPlatformAdminHostQueryHandler(new AppTenantDirectory(db), requestContext.Object)
            .Handle(new IsPlatformAdminHostQuery(), CancellationToken.None);
    }

    private static async Task<bool> AllowsAsync(string database, string? tenantId)
    {
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(context => context.TenantId).Returns(tenantId);
        await using PartiesDbContext db = CreateContext(database);
        return await new IsOnboardingHostQueryHandler(new AppTenantDirectory(db), requestContext.Object)
            .Handle(new IsOnboardingHostQuery(), CancellationToken.None);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Admits_a_role_free_session_exactly_when_the_request_is_on_the_onboarding_host(bool onboardingHost)
    {
        Mock<ISender> sender = new();
        sender.Setup(item => item.Send(It.IsAny<IsOnboardingHostQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(onboardingHost);

        bool allowed = await new OnboardingRoleFreeSessionPolicy(sender.Object).AllowsRoleFreeSessionAsync();

        allowed.Should().Be(onboardingHost);
    }

    private static async Task<Guid> SeedAsync(string database, string appId, bool deleted = false)
    {
        Guid tenantId = Guid.NewGuid();
        await using PartiesDbContext db = CreateContext(database);
        _ = db.Tenants.Add(new Tenant { Id = tenantId, OwnerId = Guid.NewGuid(), Name = appId, AppId = appId, DeletedAt = deleted ? DateTime.UtcNow : null });
        _ = await db.SaveChangesAsync();
        return tenantId;
    }

    private static PartiesDbContext CreateContext(string database) => new(
        new DbContextOptionsBuilder<PartiesDbContext>().UseInMemoryDatabase(database).Options,
        Mock.Of<IPublisher>(),
        Array.Empty<IInterceptor>());
}
