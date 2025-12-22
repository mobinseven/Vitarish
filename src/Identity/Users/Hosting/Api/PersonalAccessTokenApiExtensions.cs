
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Contracts;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Models;
using Vitarish.Users.Infrastructure;

namespace Vitarish.Users.Hosting.Api;

public static class PersonalAccessTokenApiExtensions
{
    public static IEndpointConventionBuilder MapPersonalAccessTokenEndpoints(
        this WebApplication app,
        Action<RouteGroupBuilder>? configureFilters = null)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/pat")
            .WithTags("PersonalAccessTokens")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = "IdentityServerAccessToken"
            });

        configureFilters?.Invoke(group);

        _ = group.MapPost(string.Empty, CreatePersonalAccessTokenAsync)
            .WithName("CreatePersonalAccessToken")
            .Accepts<CreatePersonalAccessTokenCommand>("application/json")
            .Produces<PersonalAccessTokenResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithSummary("Create a personal access token")
            .WithDescription("Generates a personal access token for the specified user and returns the token details.");

        return group;
    }
    /// <summary>
    /// Creates a personal access token for the specified user.
    /// </summary>
    /// <param name="request">The request payload containing the target user and token settings.</param>
    /// <param name="generator">The token generator used to issue the personal access token.</param>
    /// <param name="mediator">The mediator used to retrieve user claims.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>A 200 response with the generated personal access token, or a 400 problem response when validation fails.</returns>
    private static async Task<IResult> CreatePersonalAccessTokenAsync(
        [FromBody] CreatePersonalAccessTokenCommand request,
        [FromServices] IPersonalAccessTokenGenerator generator,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Results.Problem(
                "User identifier is required to generate a personal access token.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        HashSet<string> scopeSet = (request.Scopes ?? Enumerable.Empty<string>())
            .Where(static scope => !string.IsNullOrWhiteSpace(scope))
            .ToHashSet(StringComparer.Ordinal);

        IEnumerable<ClaimDto> userClaims = await mediator.Send(
            new GetUserClaimsQuery(request.UserId),
            cancellationToken);

        foreach (ClaimDto claim in userClaims)
        {
            if (string.Equals(claim.Type, IdentityAccessConstants.AccessClaim, StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(claim.Value))
            {
                _ = scopeSet.Add(claim.Value);
            }
        }

        string token = await generator.CreateTokenAsync(
            request.UserId,
            request.LifetimeDays,
            scopeSet,
            request.IsReferenceToken,
            cancellationToken);
        return Results.Ok(new PersonalAccessTokenResponse(token));
    }
}
