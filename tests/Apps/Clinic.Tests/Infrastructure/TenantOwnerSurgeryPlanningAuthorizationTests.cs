using System.Security.Claims;
using FluentAssertions;
using Vitarish.Clinic.Infrastructure.Handlers;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;
using Vitarish.Users.Application.Authorization;

namespace Vitarish.Clinic.Tests.Infrastructure;

public sealed class TenantOwnerSurgeryPlanningAuthorizationTests
{
    private const string TenantId = "44444444-4444-4444-4444-444444444444";
    private static readonly Guid TenantGuid = Guid.Parse(TenantId);

    [Fact]
    public void Platform_administrator_can_plan() =>
        Create(Principal(new Claim(ClaimTypes.Role, IdentityDefinitions.PlatformAdmin)), TenantId).CanPlan().Should().BeTrue();

    [Fact]
    public void Tenant_owner_role_can_plan() =>
        Create(Principal(new Claim(ClaimTypes.Role, OwnerRole())), TenantId).CanPlan().Should().BeTrue();

    [Fact]
    public void Tenant_owner_access_claim_can_plan() =>
        Create(Principal(new Claim(IdentityDefinitions.AccessClaim, OwnerRole())), TenantId).CanPlan().Should().BeTrue();

    [Fact]
    public void Tenant_member_without_the_owner_role_cannot_plan() =>
        Create(Principal(new Claim(ClaimTypes.Role, TenantRoleDefinitions.BuildRoleName(TenantGuid, IdentityDefinitions.TenantUser))), TenantId).CanPlan().Should().BeFalse();

    [Fact]
    public void Owner_of_another_tenant_cannot_plan() =>
        Create(Principal(new Claim(ClaimTypes.Role, OwnerRole())), "55555555-5555-5555-5555-555555555555").CanPlan().Should().BeFalse();

    [Fact]
    public void Anonymous_or_invalid_tenant_cannot_plan()
    {
        Create(null, TenantId).CanPlan().Should().BeFalse();
        Create(Principal(new Claim(ClaimTypes.Role, IdentityDefinitions.PlatformAdmin)), "not-a-guid").CanPlan().Should().BeFalse();
        Create(Principal(new Claim(ClaimTypes.Role, IdentityDefinitions.PlatformAdmin)), null).CanPlan().Should().BeFalse();
    }

    private static string OwnerRole() => TenantRoleDefinitions.BuildRoleName(TenantGuid, IdentityDefinitions.TenantOwner);

    private static ClaimsPrincipal Principal(params Claim[] claims) => new(new ClaimsIdentity(claims, "test"));

    private static TenantOwnerSurgeryPlanningAuthorization Create(ClaimsPrincipal? user, string? tenantId)
        => new(new TestRequestContext(user, tenantId));

    private sealed class TestRequestContext(ClaimsPrincipal? user, string? tenantId) : IRequestContext
    {
        public string Authority => "clinic.tests";
        public string? ClientId => null;
        public string? OwnerId => null;
        public string? UserId => null;
        public string? TenantId => tenantId;
        public string? ResourceId => null;
        public string? TraceId => null;
        public string? RequestId => "clinic-authorization-test";
        public string? AdditionalInfo => null;
        public ClaimsPrincipal? User => user;
        public Request GetRequest() => new() { RequestId = "clinic-authorization-test", Authority = Authority, TenantId = TenantId };
    }
}
