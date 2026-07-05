using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using Vitarish.Journey.Api.Endpoints;
using Vitarish.Journey.Application.Abstractions;
using Vitarish.Journey.Domain;
using Vitarish.Modules.Base.Core.Identity;

namespace Journey.Tests;

public sealed class CurrentJourneyEndpointsTests
{
    [Fact]
    public async Task GetCurrentJourneyAsync_succeeds_for_authenticated_principal_with_canonical_subject_id()
    {
        var resolver = new Mock<IJourneyResolver>();
        resolver.Setup(r => r.ResolveAsync(
                It.Is<JourneyRequest>(request => request.UserId == "subject-user"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JourneyStep("/home", JourneyDestinationKind.UserHome));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/modules/journey/current";
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(AuthenticatedUserIdResolver.SubjectClaimType, "subject-user")
        ], "Bearer"));

        IResult result = await CurrentJourneyEndpoints.GetCurrentJourneyAsync(resolver.Object, principal, httpContext, CancellationToken.None);

        result.Should().BeAssignableTo<Ok>();
    }
}
