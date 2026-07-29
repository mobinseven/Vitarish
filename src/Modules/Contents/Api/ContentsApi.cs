using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Contents.Application.Abstractions;
using Vitarish.Contents.Contracts.Requests;
using Vitarish.Contents.Contracts.Routes;
using Vitarish.Contents.Domain.Dto;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Requests.Application.Abstractions;
using Wire = Vitarish.Contents.Contracts.Models;

namespace Vitarish.Contents.Api;

public static class ContentsApi
{
    public static IEndpointRouteBuilder MapContentsApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(ContentsApiRoutes.Base);
        group.RequireAuthorization(IdentityDefinitions.TenantOwner);
        group.MapModuleGet(ContentsApiRoutes.Items, ListAsync, "ContentsListItems", typeof(Wire.ContentItemResponse[]));
        group.MapModuleGet(ContentsApiRoutes.ItemById, GetAsync, "ContentsGetItem", typeof(Wire.ContentItemResponse));
        group.MapModulePost(ContentsApiRoutes.Items, CreateAsync, "ContentsCreateItem", typeof(Wire.ContentItemResponse));
        group.MapModulePut(ContentsApiRoutes.ItemById, UpdateAsync, "ContentsUpdateItem", typeof(Wire.ContentItemResponse));
        group.MapModulePost(ContentsApiRoutes.ArchiveItem, ArchiveAsync, "ContentsArchiveItem", typeof(Wire.ContentItemResponse));
        group.MapModuleGet(ContentsApiRoutes.Revisions, ListRevisionsAsync, "ContentsListRevisions", typeof(Wire.ContentRevisionResponse[]));
        return endpoints;
    }

    private static async Task<IResult> ListAsync(IContentManagementService service, CancellationToken ct) =>
        ToResult(await service.ListAsync(ct), items => items.Select(ToResponse).ToArray());

    private static async Task<IResult> GetAsync(Guid contentId, IContentManagementService service, CancellationToken ct)
        => ToResult(await service.GetAsync(contentId, ct), ToResponse);

    private static async Task<IResult> CreateAsync(
        ContentItemRequest request,
        IRequestContext context,
        IContentManagementService service,
        CancellationToken ct)
    {
        if (Validate(request) is { } validationError)
        {
            return validationError;
        }

        if (!Guid.TryParse(context.UserId, out Guid authorId))
        {
            return Results.Forbid();
        }

        return ToResult(await service.CreateAsync(ToWrite(Guid.NewGuid(), authorId, request), ct), ToResponse);
    }

    private static async Task<IResult> UpdateAsync(
        Guid contentId,
        ContentItemRequest request,
        IContentManagementService service,
        CancellationToken ct)
    {
        if (Validate(request) is { } validationError)
        {
            return validationError;
        }

        return ToResult(await service.UpdateAsync(contentId, ToWrite(contentId, Guid.Empty, request), ct), ToResponse);
    }

    private static async Task<IResult> ArchiveAsync(
        Guid contentId,
        IContentManagementService service,
        CancellationToken ct)
    {
        return ToResult(await service.ArchiveAsync(contentId, ct), ToResponse);
    }

    private static async Task<IResult> ListRevisionsAsync(
        Guid contentId,
        IContentManagementService service,
        CancellationToken ct) =>
        ToResult(
            await service.ListRevisionsAsync(contentId, ct),
            revisions => revisions
                .OrderByDescending(revision => revision.Version)
                .Select(revision => new Wire.ContentRevisionResponse(revision.Id, revision.Version, revision.CreatedAt))
                .ToArray());

    private static ContentItemWriteDto ToWrite(Guid id, Guid authorId, ContentItemRequest request) => new()
    {
        Id = id,
        AuthorId = authorId,
        Type = (Domain.Enums.ContentItemType)(int)request.Type,
        Status = (Domain.Enums.ContentStatus)(int)request.Status,
        Slug = request.Slug.Trim(),
        CanonicalPath = request.CanonicalPath.Trim(),
        Title = request.Title.Trim(),
        Excerpt = request.Excerpt.Trim(),
        Locale = request.Locale.Trim(),
        Body = request.Body.Select(block => new Domain.Models.ContentBodyBlock
        {
            Type = block.Type,
            Text = block.Text,
            AssetId = block.AssetId
        }).ToArray()
    };

    private static IResult? Validate(ContentItemRequest request)
    {
        Dictionary<string, string[]> errors = [];
        AddRequiredAndLengthError(errors, nameof(request.Title), request.Title, 512);
        AddRequiredAndLengthError(errors, nameof(request.Slug), request.Slug, 256);
        AddRequiredAndLengthError(errors, nameof(request.CanonicalPath), request.CanonicalPath, 512);
        AddRequiredAndLengthError(errors, nameof(request.Locale), request.Locale, 32);

        if (request.Body is null)
        {
            errors[nameof(request.Body)] = ["Body is required."];
        }
        else if (request.Body.Count > 100)
        {
            errors[nameof(request.Body)] = ["Body cannot contain more than 100 blocks."];
        }
        else if (request.Body.Any(block => block is null || string.IsNullOrWhiteSpace(block.Type)))
        {
            errors[nameof(request.Body)] = ["Every body block requires a type."];
        }
        else if (request.Body.Sum(block => block.Text?.Length ?? 0) > 1_000_000)
        {
            errors[nameof(request.Body)] = ["Body text cannot exceed 1,000,000 characters."];
        }

        return errors.Count == 0 ? null : Results.ValidationProblem(errors);
    }

    private static void AddRequiredAndLengthError(
        IDictionary<string, string[]> errors,
        string field,
        string? value,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = [$"{field} is required."];
        }
        else if (value.Length > maximumLength)
        {
            errors[field] = [$"{field} cannot exceed {maximumLength} characters."];
        }
    }

    private static Wire.ContentItemResponse ToResponse(ContentItemReadDto item) => new(
        item.Id,
        (Wire.ContentItemType)(int)item.Type,
        (Wire.ContentStatus)(int)item.Status,
        item.Slug,
        item.CanonicalPath,
        item.Title,
        item.Excerpt,
        item.Body.Select(block => new Wire.ContentBodyBlock(block.Type, block.Text, block.AssetId)).ToArray(),
        item.Locale,
        item.PublishedAt,
        item.CreatedAt,
        item.UpdatedAt);

    private static IResult ToResult<TSource, TResponse>(EntityResult<TSource> result, Func<TSource, TResponse> map)
        => result.IsSuccess && result.Value is not null ? Results.Ok(map(result.Value)) : ToError(result.Errors);
    private static IResult ToError(IReadOnlyList<EntityError> errors)
    {
        EntityError? error = errors.FirstOrDefault();
        int statusCode = error?.Code switch
        {
            EntityErrorCode.NotFound => StatusCodes.Status404NotFound,
            EntityErrorCode.Conflict or EntityErrorCode.Concurrency => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
        return Results.Problem(
            statusCode: statusCode,
            title: "The content operation could not be completed.",
            detail: error?.Code == EntityErrorCode.Unknown ? null : error?.Message);
    }
}
