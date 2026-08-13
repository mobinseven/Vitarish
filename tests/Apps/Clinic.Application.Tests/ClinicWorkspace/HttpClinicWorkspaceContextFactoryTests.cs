using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Vitarish.Clinic.Application.ClinicWorkspace;
using Vitarish.Clinic.Application.ClinicWorkspace.Operations;
using Vitarish.Clinic.Infrastructure.Assistant;
using Vitarish.Clinic.Infrastructure.Assistant.Operations;
using Vitarish.Users.Application.Abstractions;
using Moq;
using Xunit;

namespace Vitarish.Clinic.Application.Tests.ClinicWorkspace;

public sealed class HttpClinicWorkspaceContextFactoryTests
{
    [Fact]
    public async Task CreateAsync_uses_authenticated_scope_and_ignores_client_patient_scope()
    {
        DefaultHttpContext http = new();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", "clinician-1"),
            new Claim("tenant", "practice-1"),
            new Claim("access", "ClinicalEvents.Read"),
            new Claim("access", "ClinicalEvents.Create"),
            new Claim(ClaimTypes.Role, "Clinician")
        ], "test"));
        http.Request.Headers.AcceptLanguage = "en-US,en;q=0.8";
        http.Request.Headers["X-Time-Zone"] = "America/Toronto";
        http.Request.Headers["X-Calendar"] = "persian";
        HttpContextAccessor accessor = new() { HttpContext = http };

        ClinicWorkspaceContext context = await Factory(accessor)
            .CreateAsync(Guid.Parse("d2719f9f-c9c8-4e9d-a7ca-a4d205b93e80"), "  conversation-1  ");

        context.PracticeId.Should().Be("practice-1");
        context.PatientId.Should().Be(Guid.Parse("d2719f9f-c9c8-4e9d-a7ca-a4d205b93e80"));
        context.AuthenticatedUserId.Should().Be("clinician-1");
        context.Permissions.Should().BeEquivalentTo(["ClinicalEvents.Read", "ClinicalEvents.Create"]);
        context.Role.Should().Be("Clinician");
        context.Locale.Should().Be("en-US");
        context.TimeZone.Should().Be("America/Toronto");
        context.Calendar.Should().Be("persian");
        context.ConversationId.Should().Be("conversation-1");
    }

    [Fact]
    public async Task CreateAsync_uses_safe_localization_defaults_for_missing_or_invalid_headers()
    {
        DefaultHttpContext http = new();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
        [new Claim("sub", "clinician-1"), new Claim("tenant", "practice-1")], "test"));
        http.Request.Headers.AcceptLanguage = "not-a-locale";
        http.Request.Headers["X-Time-Zone"] = "not-a-time-zone";
        http.Request.Headers["X-Calendar"] = "not-a-calendar";
        HttpContextAccessor accessor = new() { HttpContext = http };

        ClinicWorkspaceContext context = await Factory(accessor)
            .CreateAsync(Guid.NewGuid(), null);

        context.Locale.Should().Be("fa-IR");
        context.TimeZone.Should().Be("Asia/Tehran");
        context.Calendar.Should().Be("persian");
        context.ConversationId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CreateOperationsAsync_derives_the_practice_subject_and_requires_an_owner_role()
    {
        DefaultHttpContext ownerHttp = new();
        ownerHttp.User = new ClaimsPrincipal(new ClaimsIdentity(
        [new Claim("sub", "owner-1"), new Claim("tenant", "practice-1"), new Claim(ClaimTypes.Role, "practice-1:TenantOwner")], "test"));

        ClinicWorkspaceContext owner = await Factory(new HttpContextAccessor { HttpContext = ownerHttp })
            .CreateOperationsAsync(" conversation-1 ");

        owner.SubjectKind.Should().Be("practice");
        owner.PatientId.Should().Be(Guid.Empty);
        owner.PracticeId.Should().Be("practice-1");
        owner.ConversationId.Should().Be("conversation-1");
        owner.Role.Should().Be(ClinicWorkspaceOperationsScope.TenantOwnerRole);

        DefaultHttpContext clinicianHttp = new();
        clinicianHttp.User = new ClaimsPrincipal(new ClaimsIdentity(
        [new Claim("sub", "clinician-1"), new Claim("tenant", "practice-1"), new Claim(ClaimTypes.Role, "TenantOwner")], "test"));
        Func<Task> createForClinician = () => Factory(new HttpContextAccessor { HttpContext = clinicianHttp })
            .CreateOperationsAsync(null);

        await createForClinician.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Operations_access_verifier_rechecks_the_current_user_role_for_the_practice()
    {
        Mock<IUsersService> users = new(MockBehavior.Strict);
        users.Setup(service => service.CheckUserRoleAsync("owner-1", ClinicWorkspaceOperationsScope.PlatformAdminRole, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        users.Setup(service => service.CheckUserRoleAsync("owner-1", "practice-1:TenantOwner", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        string? role = await new UserClinicWorkspaceOperationsAccessVerifier(users.Object)
            .RevalidateAsync("practice-1", "owner-1", CancellationToken.None);

        role.Should().Be(ClinicWorkspaceOperationsScope.TenantOwnerRole);
        users.VerifyAll();
    }

    [Fact]
    public async Task CreateAsync_rejects_a_route_patient_that_is_not_available_in_the_authenticated_practice()
    {
        DefaultHttpContext http = new();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
        [new Claim("sub", "clinician-1"), new Claim("tenant", "practice-1")], "test"));

        Func<Task> create = () => Factory(new HttpContextAccessor { HttpContext = http }, patientAvailable: false)
            .CreateAsync(Guid.NewGuid(), null);

        await create.Should().ThrowAsync<KeyNotFoundException>();
    }

    private static HttpClinicWorkspaceContextFactory Factory(IHttpContextAccessor accessor, bool patientAvailable = true)
    {
        Mock<IClinicWorkspaceRoutePatientResolver> resolver = new(MockBehavior.Strict);
        resolver.Setup(item => item.IsAvailableAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(patientAvailable);
        return new(accessor, resolver.Object);
    }
}
