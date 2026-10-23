using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Vitarish.Intelligence.Application.Memory;
using Vitarish.Intelligence.Contracts;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Intelligence.Api;

/// <summary>What Vitarish remembers about the signed-in person, which only that person can see, correct or delete.</summary>
internal static class UserMemoryEndpointMapping
{
    internal static RouteGroupBuilder MapUserMemory(this RouteGroupBuilder group)
    {
        _ = group.MapModuleGet(IntelligenceApiRoutes.Memory, GetAsync, "IntelligenceGetUserMemory", typeof(UserMemoryResponse));
        _ = group.MapModulePut(IntelligenceApiRoutes.MemoryItem, UpdateAsync, "IntelligenceUpdateUserMemoryItem", successStatusCode: StatusCodes.Status204NoContent)
            .WithMetadata(new RequestSizeLimitAttribute(2_048));
        _ = group.MapModuleDelete(IntelligenceApiRoutes.MemoryItem, ForgetItemAsync, "IntelligenceForgetUserMemoryItem", successStatusCode: StatusCodes.Status204NoContent);
        _ = group.MapModuleDelete(IntelligenceApiRoutes.Memory, ForgetAllAsync, "IntelligenceForgetUserMemory", successStatusCode: StatusCodes.Status204NoContent);
        return group;
    }

    private static async Task<IResult> GetAsync([FromServices] IRequestContext requestContext, [FromServices] ISender sender, CancellationToken cancellationToken)
    {
        if (requestContext.UserId is not { Length: > 0 } userId)
            return Results.Unauthorized();
        IReadOnlyList<UserMemoryItem> items = await sender.Send(new GetUserMemoryQuery(userId), cancellationToken);
        return Results.Ok(new UserMemoryResponse(items.Select(ToResponse).ToArray()));
    }

    private static async Task<IResult> UpdateAsync(string itemId, UpdateUserMemoryItemRequest request, [FromServices] IRequestContext requestContext, [FromServices] ISender sender, CancellationToken cancellationToken)
    {
        if (requestContext.UserId is not { Length: > 0 } userId)
            return Results.Unauthorized();
        if (!UserMemoryRules.IsStorable(request.Text?.Trim()))
            return Results.BadRequest(new IntelligenceError(IntelligenceErrorCode.ValidationFailure, $"Remembered text must be 1 to {UserMemoryRules.MaxTextLength} characters without long numbers."));
        return await sender.Send(new UpdateUserMemoryItemCommand(userId, itemId, request.Text!), cancellationToken)
            ? Results.NoContent()
            : Results.NotFound();
    }

    private static async Task<IResult> ForgetItemAsync(string itemId, [FromServices] IRequestContext requestContext, [FromServices] ISender sender, CancellationToken cancellationToken)
    {
        if (requestContext.UserId is not { Length: > 0 } userId)
            return Results.Unauthorized();
        return await sender.Send(new ForgetUserMemoryCommand(userId, itemId), cancellationToken) ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> ForgetAllAsync([FromServices] IRequestContext requestContext, [FromServices] ISender sender, CancellationToken cancellationToken)
    {
        if (requestContext.UserId is not { Length: > 0 } userId)
            return Results.Unauthorized();
        _ = await sender.Send(new ForgetUserMemoryCommand(userId, null), cancellationToken);
        return Results.NoContent();
    }

    internal static UserMemoryItemResponse ToResponse(UserMemoryItem item) =>
        new(item.Id, item.Text, item.Source == UserMemorySource.Onboarding ? UserMemorySourceResponse.Onboarding : UserMemorySourceResponse.Landing, item.RememberedAt);
}
