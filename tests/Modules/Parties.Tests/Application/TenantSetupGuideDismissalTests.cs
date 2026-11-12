using System.Security.Claims;
using FluentAssertions;
using Moq;
using Vitarish.Parties.Application.Profiles;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Parties.Tests.Application;

public sealed class TenantSetupGuideDismissalTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 20, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task An_owner_of_the_tenant_or_a_platform_admin_sets_setup_aside_at_the_current_time(bool owner, bool platformAdmin)
    {
        Mock<ITenantBusinessProfileStore> store = new();
        store.Setup(item => item.DismissSetupGuideAsync(TenantId, FixedNow.UtcDateTime, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        TenantSetupGuideDismissalStatus result = await Handler(store.Object, Roles(owner, platformAdmin))
            .Handle(new DismissTenantSetupGuideCommand(), CancellationToken.None);

        result.Should().Be(TenantSetupGuideDismissalStatus.Updated);
        store.VerifyAll();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Staff_are_forbidden_and_nothing_is_written(bool hasTenantUserRole)
    {
        Mock<ITenantBusinessProfileStore> store = new(MockBehavior.Strict);

        TenantSetupGuideDismissalStatus result = await Handler(store.Object, hasTenantUserRole ? [$"{TenantId}:{IdentityDefinitions.TenantUser}"] : [])
            .Handle(new DismissTenantSetupGuideCommand(), CancellationToken.None);

        result.Should().Be(TenantSetupGuideDismissalStatus.Forbidden);
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task An_owner_of_another_tenant_is_forbidden_and_nothing_is_written()
    {
        Mock<ITenantBusinessProfileStore> store = new(MockBehavior.Strict);

        TenantSetupGuideDismissalStatus result = await Handler(store.Object, [$"{Guid.NewGuid()}:{IdentityDefinitions.TenantOwner}"])
            .Handle(new DismissTenantSetupGuideCommand(), CancellationToken.None);

        result.Should().Be(TenantSetupGuideDismissalStatus.Forbidden);
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Answers_not_found_without_a_tenant_or_when_the_store_has_no_such_tenant()
    {
        Mock<ITenantBusinessProfileStore> store = new();
        store.Setup(item => item.DismissSetupGuideAsync(TenantId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        TenantSetupGuideDismissalStatus missingTenant = await Handler(store.Object, Roles(true, false), hasTenant: false)
            .Handle(new DismissTenantSetupGuideCommand(), CancellationToken.None);
        TenantSetupGuideDismissalStatus unknownTenant = await Handler(store.Object, Roles(true, false))
            .Handle(new DismissTenantSetupGuideCommand(), CancellationToken.None);

        missingTenant.Should().Be(TenantSetupGuideDismissalStatus.NotFound);
        unknownTenant.Should().Be(TenantSetupGuideDismissalStatus.NotFound);
    }

    private static string[] Roles(bool owner, bool platformAdmin) =>
    [
        .. owner ? [$"{TenantId}:{IdentityDefinitions.TenantOwner}"] : Array.Empty<string>(),
        .. platformAdmin ? [IdentityDefinitions.PlatformAdmin] : Array.Empty<string>()
    ];

    private static DismissTenantSetupGuideCommandHandler Handler(ITenantBusinessProfileStore store, string[] roles, bool hasTenant = true)
    {
        Mock<IRequestContext> context = new();
        context.SetupGet(item => item.TenantId).Returns(hasTenant ? TenantId.ToString() : null);
        context.SetupGet(item => item.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(roles.Select(role => new Claim(ClaimTypes.Role, role)), "test")));
        return new(store, context.Object, new FixedTimeProvider(FixedNow));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
