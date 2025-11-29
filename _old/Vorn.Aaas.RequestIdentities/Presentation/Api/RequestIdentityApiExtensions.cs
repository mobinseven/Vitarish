namespace Vorn.Aaas.RequestIdentities.Presentation.Api;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Vorn.Aaas.RequestIdentities.Application.Commands;

public static class RequestIdentityApiExtensions
{
    public static void MapRequestIdentityEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/request-identities")
            .WithTags("RequestIdentities")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = "IdentityServerAccessToken"
            });

        group.MapPost("/", async (
            [FromBody] RecordRequestIdentityRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            await mediator.Send(request.ToCommand(), cancellationToken);
            return Results.NoContent();
        })
        .WithName("RecordRequestIdentity")
        .Produces(StatusCodes.Status204NoContent);
    }

    private sealed record RecordRequestIdentityRequest(
        string RequestId,
        string Authority,
        string ClientId,
        string OwnerId,
        string? TenantId,
        string? UserId,
        string? ResourceId,
        string? TraceId,
        string? AdditionalInfo)
    {
        public RecordRequestIdentityCommand ToCommand() => new(
            RequestId,
            Authority,
            ClientId,
            OwnerId,
            TenantId,
            UserId,
            ResourceId,
            TraceId,
            AdditionalInfo);
    }
}
