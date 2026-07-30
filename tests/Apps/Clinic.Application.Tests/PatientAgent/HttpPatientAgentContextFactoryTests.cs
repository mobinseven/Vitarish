using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Vitarish.Clinic.Application.PatientAgent;
using Vitarish.Clinic.Infrastructure.PatientAgent;
using Xunit;

namespace Vitarish.Clinic.Application.Tests.PatientAgent;

public sealed class HttpPatientAgentContextFactoryTests
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

        PatientAgentContext context = await new HttpPatientAgentContextFactory(accessor)
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

        PatientAgentContext context = await new HttpPatientAgentContextFactory(accessor)
            .CreateAsync(Guid.NewGuid(), null);

        context.Locale.Should().Be("fa-IR");
        context.TimeZone.Should().Be("Asia/Tehran");
        context.Calendar.Should().Be("persian");
        context.ConversationId.Should().NotBeNullOrWhiteSpace();
    }
}
