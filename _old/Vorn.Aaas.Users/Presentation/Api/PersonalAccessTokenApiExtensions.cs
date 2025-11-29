namespace Vorn.Aaas.Users.Presentation.Api;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vorn.Aaas.Users;
using Vorn.Aaas.Users.Application.Queries;
using Vorn.Aaas.Users.Presentation.Service;
using Vorn.Aaas.Users.ReadModel.Dtos;

public static class PersonalAccessTokenApiExtensions
{
    public static void MapPersonalAccessTokenEndpoints(
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

        group.MapPost(string.Empty, async (
            [FromBody] CreatePersonalAccessTokenRequest request,
            [FromServices] IPersonalAccessTokenGenerator generator,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken) =>
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
                if (string.Equals(claim.Type, AaasConstants.AccessClaim, StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(claim.Value))
                {
                    scopeSet.Add(claim.Value);
                }
            }

            string token = await generator.CreateTokenAsync(
                request.UserId,
                request.LifetimeDays,
                scopeSet,
                request.IsReferenceToken,
                cancellationToken);
            return Results.Ok(new PersonalAccessTokenResponse(token));
        })
        .WithName("CreatePersonalAccessToken")
        .Accepts<CreatePersonalAccessTokenRequest>("application/json")
        .Produces<PersonalAccessTokenResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .WithOpenApi(op =>
        {
            op.Summary = "Generate a personal access token for the current user.";
            op.Description = "Creates a personal access token bound to the authenticated user from the current request.";
            return op;
        });
    }
}
