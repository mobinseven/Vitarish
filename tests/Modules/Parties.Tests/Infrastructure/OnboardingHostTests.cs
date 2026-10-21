using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Vitarish.Parties.Application;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Application.SignIn;
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
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    public async Task Admits_a_role_free_session_only_on_the_onboarding_or_sign_in_host(bool onboardingHost, bool signInHost, bool expected)
    {
        Mock<ISender> sender = new();
        sender.Setup(item => item.Send(It.IsAny<IsOnboardingHostQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(onboardingHost);
        sender.Setup(item => item.Send(It.IsAny<IsSignInHostQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(signInHost);

        bool allowed = await new PlatformHostRoleFreeSessionPolicy(sender.Object).AllowsRoleFreeSessionAsync();

        allowed.Should().Be(expected);
    }

    [Fact]
    public async Task Recognizes_only_the_live_sign_in_tenant_as_the_sign_in_host()
    {
        string database = Guid.NewGuid().ToString("N");
        Guid signIn = await SeedAsync(database, SignInTenant.AppId);
        Guid onboarding = await SeedAsync(database, OnboardingTenant.AppId);
        Guid deletedSignIn = await SeedAsync(database, SignInTenant.AppId, deleted: true);

        (await IsSignInHostAsync(database, signIn.ToString())).Should().BeTrue();
        (await IsSignInHostAsync(database, onboarding.ToString())).Should().BeFalse();
        (await IsSignInHostAsync(database, deletedSignIn.ToString())).Should().BeFalse();
        (await IsSignInHostAsync(database, null)).Should().BeFalse();
    }

    private static async Task<bool> IsSignInHostAsync(string database, string? tenantId)
    {
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(context => context.TenantId).Returns(tenantId);
        await using PartiesDbContext db = CreateContext(database);
        return await new IsSignInHostQueryHandler(new AppTenantDirectory(db), requestContext.Object)
            .Handle(new IsSignInHostQuery(), CancellationToken.None);
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
