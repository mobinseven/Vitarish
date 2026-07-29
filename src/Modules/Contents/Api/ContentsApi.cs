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

    private static async Task<IResult> ListAsync(IContentItemsService service, CancellationToken ct)
        => ToResult(await service.GetListAsync(new ContentItemDescriptorDto(), ct), items => items.Select(ToResponse).ToArray());
    private static async Task<IResult> GetAsync(Guid contentId, IContentItemsService service, CancellationToken ct)
        => ToResult(await service.GetAsync(contentId, ct), ToResponse);
    private static async Task<IResult> CreateAsync(ContentItemRequest request, IRequestContext context, IContentItemsService service, CancellationToken ct)
    {
        if (!Guid.TryParse(context.UserId, out Guid authorId)) return Results.Forbid();
        return ToResult(await service.AddAsync(ToWrite(Guid.NewGuid(), authorId, request), ct), ToResponse);
    }
    private static async Task<IResult> UpdateAsync(Guid contentId, ContentItemRequest request, IContentItemsService service, CancellationToken ct)
    {
        EntityResult<ContentItemReadDto> current = await service.GetAsync(contentId, ct);
        if (current.IsFailure || current.Value is null) return ToError(current.Errors);
        return ToResult(await service.UpdateAsync(ToWrite(contentId, current.Value.AuthorId, request), ct), ToResponse);
    }
    private static async Task<IResult> ArchiveAsync(Guid contentId, IContentItemsService service, CancellationToken ct)
    {
        EntityResult<ContentItemReadDto> current = await service.GetAsync(contentId, ct);
        if (current.IsFailure || current.Value is null) return ToError(current.Errors);
        ContentItemReadDto item = current.Value;
        ContentItemWriteDto write = new() { Id = item.Id, Type = item.Type, Status = Domain.Enums.ContentStatus.Archived,
            Slug = item.Slug, CanonicalPath = item.CanonicalPath, Title = item.Title, Excerpt = item.Excerpt,
            Body = item.Body, AuthorId = item.AuthorId, Locale = item.Locale, PublishedAt = item.PublishedAt, Metadata = item.Metadata };
        return ToResult(await service.UpdateAsync(write, ct), ToResponse);
    }
    private static async Task<IResult> ListRevisionsAsync(Guid contentId, IContentRevisionsService service, CancellationToken ct)
        => ToResult(await service.GetListAsync(new ContentRevisionDescriptorDto { ContentId = contentId }, ct), revisions =>
            revisions.OrderByDescending(r => r.Version).Select(r => new Wire.ContentRevisionResponse(r.Id, r.Version, r.CreatedAt)).ToArray());

    private static ContentItemWriteDto ToWrite(Guid id, Guid authorId, ContentItemRequest request) => new()
    {
        Id = id, AuthorId = authorId, Type = (Domain.Enums.ContentItemType)(int)request.Type,
        Status = (Domain.Enums.ContentStatus)(int)request.Status, Slug = request.Slug.Trim(),
        CanonicalPath = request.CanonicalPath.Trim(), Title = request.Title.Trim(), Excerpt = request.Excerpt.Trim(),
        Locale = request.Locale.Trim(), Body = request.Body.Select(block => new Domain.Models.ContentBodyBlock
        { Type = block.Type, Text = block.Text, AssetId = block.AssetId }).ToArray()
    };
    private static Wire.ContentItemResponse ToResponse(ContentItemReadDto item) => new(item.Id,
        (Wire.ContentItemType)(int)item.Type, (Wire.ContentStatus)(int)item.Status, item.Slug, item.CanonicalPath,
        item.Title, item.Excerpt, item.Body.Select(block => new Wire.ContentBodyBlock(block.Type, block.Text, block.AssetId)).ToArray(),
        item.Locale, item.PublishedAt, item.CreatedAt, item.UpdatedAt);
    private static IResult ToResult<TSource, TResponse>(EntityResult<TSource> result, Func<TSource, TResponse> map)
        => result.IsSuccess && result.Value is not null ? Results.Ok(map(result.Value)) : ToError(result.Errors);
    private static IResult ToError(IReadOnlyList<EntityError> errors) => errors.FirstOrDefault()?.Code switch
    {
        EntityErrorCode.NotFound => Results.NotFound(new { errors }),
        EntityErrorCode.Conflict or EntityErrorCode.Concurrency => Results.Conflict(new { errors }),
        _ => Results.BadRequest(new { errors })
    };
}
