using Vitarish.Entities.Server.Extensions;

namespace Vitarish.Entities.Server.Services;

public abstract class EntityService<TEntity, TDescriptor, TReadDto, TWriteDto, TDescriptorDto> : IEntityService<TReadDto, TWriteDto, TDescriptorDto>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor
    where TReadDto : EntityReadDto
    where TWriteDto : EntityWriteDto
    where TDescriptorDto : EntityDescriptorDto
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Constructor using auto-generated extension methods for mapping.
    /// </summary>
    protected EntityService(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    protected IMediator Mediator => _mediator;

    public Task<EntityResult<TReadDto>> AddAsync(TWriteDto dto, CancellationToken cancellationToken) => ExecuteDtoAsync(
                mediator => mediator.Send(new AddEntityCommand<TEntity, TDescriptor>(dto.ToDomain()), cancellationToken),
                MissingEntityError,
                cancellationToken);

    public Task<EntityResult<TReadDto>> AddAsync(TEntity entity, CancellationToken cancellationToken) => ExecuteDtoAsync(
                mediator => mediator.Send(new AddEntityCommand<TEntity, TDescriptor>(entity), cancellationToken),
                MissingEntityError,
                cancellationToken);

    public Task<EntityResult<TEntity>> AddEntityAsync(TEntity entity, CancellationToken cancellationToken) => ExecuteEntityAsync(
                mediator => mediator.Send(new AddEntityCommand<TEntity, TDescriptor>(entity), cancellationToken),
                MissingEntityError,
                cancellationToken);

    public Task<EntityResult<IReadOnlyList<TReadDto>>> AddAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken) => ExecuteListAsync(
                mediator => mediator.Send(new AddEntitiesCommand<TEntity, TDescriptor>(entities), cancellationToken),
                entity => entity.ToDto(),
                cancellationToken);

    public Task<EntityResult<IReadOnlyList<TReadDto>>> AddAsync(IReadOnlyList<TWriteDto> dtos, CancellationToken cancellationToken) => ExecuteListAsync(
                mediator => mediator.Send(new AddEntitiesCommand<TEntity, TDescriptor>(MapList(dtos, dto => dto.ToDomain())), cancellationToken),
                entity => entity.ToDto(),
                cancellationToken);

    public Task<EntityResult<IReadOnlyList<TEntity>>> AddEntitiesAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken) => ExecuteEntityListAsync(mediator => mediator.Send(new AddEntitiesCommand<TEntity, TDescriptor>(entities), cancellationToken), cancellationToken);

    public Task<EntityResult<TReadDto>> UpdateAsync(TEntity entity, CancellationToken cancellationToken) => ExecuteDtoAsync(
                mediator => mediator.Send(new UpdateEntityCommand<TEntity, TDescriptor>(entity), cancellationToken),
                MissingEntityError,
                cancellationToken);

    public Task<EntityResult<TReadDto>> UpdateAsync(TWriteDto dto, CancellationToken cancellationToken) => ExecuteDtoAsync(
                mediator => mediator.Send(new UpdateEntityCommand<TEntity, TDescriptor>(dto.ToDomain()), cancellationToken),
                MissingEntityError,
                cancellationToken);

    public Task<EntityResult<TEntity>> UpdateEntityAsync(TEntity entity, CancellationToken cancellationToken) => ExecuteEntityAsync(
                mediator => mediator.Send(new UpdateEntityCommand<TEntity, TDescriptor>(entity), cancellationToken),
                MissingEntityError,
                cancellationToken);

    public Task<EntityResult<IReadOnlyList<TReadDto>>> UpdateAsync(IReadOnlyList<TWriteDto> dtos, CancellationToken cancellationToken) => ExecuteListAsync(
                mediator => mediator.Send(new UpdateEntitiesCommand<TEntity, TDescriptor>(MapList(dtos, dto => dto.ToDomain())), cancellationToken),
                entity => entity.ToDto(),
                cancellationToken);

    public Task<EntityResult<IReadOnlyList<TReadDto>>> UpdateAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken) => ExecuteListAsync(
                mediator => mediator.Send(new UpdateEntitiesCommand<TEntity, TDescriptor>(entities), cancellationToken),
                entity => entity.ToDto(),
                cancellationToken);

    public Task<EntityResult<IReadOnlyList<TEntity>>> UpdateEntitiesAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken) => ExecuteEntityListAsync(mediator => mediator.Send(new UpdateEntitiesCommand<TEntity, TDescriptor>(entities), cancellationToken), cancellationToken);

    public Task<EntityResult<TReadDto>> AddOrUpdateAsync(TWriteDto dto, CancellationToken cancellationToken) => ExecuteDtoAsync(
                mediator => mediator.Send(new AddOrUpdateEntityCommand<TEntity, TDescriptor>(dto.ToDomain()), cancellationToken),
                MissingEntityError,
                cancellationToken);

    public Task<EntityResult<TReadDto>> AddOrUpdateAsync(TEntity entity, CancellationToken cancellationToken) => ExecuteDtoAsync(
                mediator => mediator.Send(new AddOrUpdateEntityCommand<TEntity, TDescriptor>(entity), cancellationToken),
                MissingEntityError,
                cancellationToken);

    public Task<EntityResult<TEntity>> AddOrUpdateEntityAsync(TEntity entity, CancellationToken cancellationToken) => ExecuteEntityAsync(
                mediator => mediator.Send(new AddOrUpdateEntityCommand<TEntity, TDescriptor>(entity), cancellationToken),
                MissingEntityError,
                cancellationToken);

    public Task<EntityResult> RemoveAsync(TWriteDto dto, CancellationToken cancellationToken) => ExecuteAsync(mediator => mediator.Send(new RemoveEntityCommand<TEntity, TDescriptor>(dto.ToDomain()), cancellationToken), cancellationToken);

    public Task<EntityResult> RemoveAsync(TEntity entity, CancellationToken cancellationToken) => ExecuteAsync(mediator => mediator.Send(new RemoveEntityCommand<TEntity, TDescriptor>(entity), cancellationToken), cancellationToken);

    public Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken) => ExecuteAsync(mediator => mediator.Send(new RemoveEntityByIdCommand<TEntity, TDescriptor>(id), cancellationToken), cancellationToken);

    public Task<EntityResult> RemoveAsync(IReadOnlyList<TWriteDto> dtos, CancellationToken cancellationToken) => ExecuteAsync(mediator => mediator.Send(new RemoveEntitiesCommand<TEntity, TDescriptor>(MapList(dtos, dto => dto.ToDomain())), cancellationToken), cancellationToken);

    public Task<EntityResult> RemoveAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken) => ExecuteAsync(mediator => mediator.Send(new RemoveEntitiesCommand<TEntity, TDescriptor>(entities), cancellationToken), cancellationToken);

    public Task<EntityResult> RemoveByDescriptorAsync(TDescriptor descriptor, CancellationToken cancellationToken) => ExecuteAsync(mediator => mediator.Send(new RemoveEntitiesByDescriptorCommand<TEntity, TDescriptor>(descriptor), cancellationToken), cancellationToken);

    public Task<EntityResult> RemoveByDescriptorAsync(TDescriptorDto descriptor, CancellationToken cancellationToken) => RemoveByDescriptorAsync(descriptor.ToDomain(), cancellationToken);

    public Task<EntityResult<TReadDto>> GetAsync(Guid id, CancellationToken cancellationToken) => GetAsync(new GetEntityByIdQuery<TEntity, TDescriptor>(id), cancellationToken);

    public Task<EntityResult<TEntity>> GetEntityAsync(Guid id, CancellationToken cancellationToken) => GetEntityAsync(new GetEntityByIdQuery<TEntity, TDescriptor>(id), cancellationToken);

    public Task<EntityResult<TReadDto>> GetAsync<TQuery>(TQuery query, CancellationToken cancellationToken)
        where TQuery : IRequest<TEntity?> => ExecuteDtoAsync(
                mediator => mediator.Send(query, cancellationToken),
                NotFoundError,
                cancellationToken);

    public Task<EntityResult<TEntity>> GetEntityAsync<TQuery>(TQuery query, CancellationToken cancellationToken)
        where TQuery : IRequest<TEntity?> => ExecuteEntityAsync(
                mediator => mediator.Send(query, cancellationToken),
                NotFoundError,
                cancellationToken);

    public Task<EntityResult<TReadDto>> GetAsync<TQuery, TOtherEntity, TOtherDescriptor>(
        TQuery query,
        Func<TOtherEntity, TReadDto> map,
        CancellationToken cancellationToken)
        where TOtherEntity : Entity
        where TOtherDescriptor : EntityDescriptor
        where TQuery : IRequest<TOtherEntity?> => ExecuteMappedEntityAsync(
                mediator => mediator.Send(query, cancellationToken),
                map,
                () => EntityError.NotFound($"{typeof(TOtherEntity).Name} was not found."),
                cancellationToken);

    public Task<EntityResult<int>> CountAsync(TDescriptor descriptor, CancellationToken cancellationToken) => ExecuteValueAsync(mediator => mediator.Send(new CountEntitiesQuery<TEntity, TDescriptor>(descriptor), cancellationToken), cancellationToken);

    public Task<EntityResult<int>> CountAsync(TDescriptorDto descriptor, CancellationToken cancellationToken) => CountAsync(descriptor.ToDomain(), cancellationToken);

    public Task<EntityResult<int>> CountAsync<TOtherEntity, TOtherDescriptor>(TOtherDescriptor descriptor, CancellationToken cancellationToken)
        where TOtherEntity : Entity
        where TOtherDescriptor : EntityDescriptor => ExecuteValueAsync(mediator => mediator.Send(new CountEntitiesQuery<TOtherEntity, TOtherDescriptor>(descriptor), cancellationToken), cancellationToken);

    public Task<EntityResult<IReadOnlyList<TReadDto>>> GetListAsync(TDescriptor descriptor, CancellationToken cancellationToken) => ExecuteListAsync(
                mediator => mediator.Send(new GetEntitiesQuery<TEntity, TDescriptor>(descriptor), cancellationToken),
                entity => entity.ToDto(),
                cancellationToken);

    public Task<EntityResult<IReadOnlyList<TReadDto>>> GetListAsync(TDescriptorDto descriptor, CancellationToken cancellationToken) => GetListAsync(descriptor.ToDomain(), cancellationToken);

    public Task<EntityResult<IReadOnlyList<TReadDto>>> GetListAsync<TOtherEntity, TOtherDescriptor>(
        TOtherDescriptor descriptor,
        Func<TOtherEntity, TReadDto> map,
        CancellationToken cancellationToken)
        where TOtherEntity : Entity
        where TOtherDescriptor : EntityDescriptor => ExecuteListAsync<TOtherEntity, TReadDto>(
                mediator => mediator.Send(new GetEntitiesQuery<TOtherEntity, TOtherDescriptor>(descriptor), cancellationToken),
                map,
                cancellationToken);

    public Task<EntityResult<IReadOnlyList<TReadDto>>> GetListAsync<TQuery>(TQuery query, CancellationToken cancellationToken)
        where TQuery : IRequest<IReadOnlyList<TEntity>> => ExecuteListAsync(mediator => mediator.Send(query, cancellationToken), entity => entity.ToDto(), cancellationToken);

    public Task<EntityResult<IReadOnlyList<TReadDto>>> GetListAsync<TQuery, TOtherEntity>(
        TQuery query,
        Func<TOtherEntity, TReadDto> map,
        CancellationToken cancellationToken)
        where TOtherEntity : Entity
        where TQuery : IRequest<IReadOnlyList<TOtherEntity>> => ExecuteListAsync(mediator => mediator.Send(query, cancellationToken), map, cancellationToken);

    public async Task<EntityResult<PartedResult<TReadDto>>> GetPartedAsync(TDescriptor descriptor, CancellationToken cancellationToken)
    {
        EntityResult<IReadOnlyList<TReadDto>> itemsResult = await ExecuteListAsync(
            mediator => mediator.Send(new GetEntitiesQuery<TEntity, TDescriptor>(descriptor), cancellationToken),
            entity => entity.ToDto(),
            cancellationToken);

        if (itemsResult.IsFailure || itemsResult.Value is null)
        {
            return EntityResult<PartedResult<TReadDto>>.Failure(itemsResult.Errors.ToArray());
        }

        TDescriptor countDescriptor = descriptor with { Skip = null, Take = null };
        EntityResult<int> countResult = await ExecuteValueAsync(
            mediator => mediator.Send(new CountEntitiesQuery<TEntity, TDescriptor>(countDescriptor), cancellationToken),
            cancellationToken);

        return countResult.IsFailure
            ? EntityResult<PartedResult<TReadDto>>.Failure(countResult.Errors.ToArray())
            : EntityResult<PartedResult<TReadDto>>.Success(new PartedResult<TReadDto>(itemsResult.Value, countResult.Value));
    }

    public Task<EntityResult<PartedResult<TReadDto>>> GetPartedAsync(TDescriptorDto descriptor, CancellationToken cancellationToken) => GetPartedAsync(descriptor.ToDomain(), cancellationToken);

    public Task<EntityResult<PartedResult<TReadDto>>> GetPartedAsync<TQuery>(TQuery query, CancellationToken cancellationToken)
        where TQuery : IRequest<PartedResult<TEntity>> => ExecutePartedAsync(mediator => mediator.Send(query, cancellationToken), entity => entity.ToDto(), cancellationToken);

    private async Task<EntityResult> ExecuteAsync(Func<IMediator, Task> action, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await action(Mediator);
            return EntityResult.Success();
        }
        catch (EntityFailureException ex)
        {
            return EntityResult.Failure(ex.Error);
        }
    }

    private Task<EntityResult<TReadDto>> ExecuteDtoAsync(Func<IMediator, Task<TEntity?>> action, Func<EntityError> nullErrorFactory, CancellationToken cancellationToken) => ExecuteMappedEntityAsync(action, entity => entity.ToDto(), nullErrorFactory, cancellationToken);

    private Task<EntityResult<TEntity>> ExecuteEntityAsync(Func<IMediator, Task<TEntity?>> action, Func<EntityError> nullErrorFactory, CancellationToken cancellationToken) => ExecuteMappedEntityAsync(action, static entity => entity, nullErrorFactory, cancellationToken);

    private async Task<EntityResult<TProjection>> ExecuteMappedEntityAsync<TSource, TProjection>(
        Func<IMediator, Task<TSource?>> action,
        Func<TSource, TProjection> map,
        Func<EntityError> nullErrorFactory,
        CancellationToken cancellationToken)
        where TSource : class
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (map is null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        if (nullErrorFactory is null)
        {
            throw new ArgumentNullException(nameof(nullErrorFactory));
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            TSource? entity = await action(Mediator);
            return entity is null ? EntityResult<TProjection>.Failure(nullErrorFactory()) : EntityResult<TProjection>.Success(map(entity));
        }
        catch (EntityFailureException ex)
        {
            return EntityResult<TProjection>.Failure(ex.Error);
        }
    }

    private async Task<EntityResult<IReadOnlyList<TProjection>>> ExecuteListAsync<TSource, TProjection>(
        Func<IMediator, Task<IReadOnlyList<TSource>>> action,
        Func<TSource, TProjection> map,
        CancellationToken cancellationToken)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (map is null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<TSource> entities = await action(Mediator);
            return EntityResult<IReadOnlyList<TProjection>>.Success(MapList(entities, map));
        }
        catch (EntityFailureException ex)
        {
            return EntityResult<IReadOnlyList<TProjection>>.Failure(ex.Error);
        }
    }

    private Task<EntityResult<IReadOnlyList<TReadDto>>> ExecuteListAsync(
        Func<IMediator, Task<IReadOnlyList<TEntity>>> action,
        Func<TEntity, TReadDto> map,
        CancellationToken cancellationToken) => ExecuteListAsync<TEntity, TReadDto>(action, map, cancellationToken);

    private Task<EntityResult<IReadOnlyList<TReadDto>>> ExecuteListAsync(Func<IMediator, Task<IReadOnlyList<TEntity>>> action, CancellationToken cancellationToken) => ExecuteListAsync(action, entity => entity.ToDto(), cancellationToken);

    private Task<EntityResult<IReadOnlyList<TEntity>>> ExecuteEntityListAsync(Func<IMediator, Task<IReadOnlyList<TEntity>>> action, CancellationToken cancellationToken) => ExecuteListAsync(action, static entity => entity, cancellationToken);

    private async Task<EntityResult<PartedResult<TProjection>>> ExecutePartedAsync<TSource, TProjection>(
        Func<IMediator, Task<PartedResult<TSource>>> action,
        Func<TSource, TProjection> map,
        CancellationToken cancellationToken)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (map is null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            PartedResult<TSource> result = await action(Mediator);
            return EntityResult<PartedResult<TProjection>>.Success(
                new PartedResult<TProjection>(MapList(result.Items, map), result.TotalItems));
        }
        catch (EntityFailureException ex)
        {
            return EntityResult<PartedResult<TProjection>>.Failure(ex.Error);
        }
    }

    private async Task<EntityResult<int>> ExecuteValueAsync(Func<IMediator, Task<int>> action, CancellationToken cancellationToken)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            int value = await action(Mediator);
            return EntityResult<int>.Success(value);
        }
        catch (EntityFailureException ex)
        {
            return EntityResult<int>.Failure(ex.Error);
        }
    }

    /// <summary>
    /// Efficiently maps a read-only list using a projection function without LINQ overhead.
    /// Avoids Select().ToList() allocations by directly projecting into a pre-sized list.
    /// </summary>
    private static IReadOnlyList<TProjection> MapList<TSource, TProjection>(
        IReadOnlyList<TSource> source,
        Func<TSource, TProjection> map)
    {
        if (source.Count == 0)
        {
            return [];
        }

        List<TProjection> result = new(source.Count);
        for (int i = 0; i < source.Count; i++)
        {
            result.Add(map(source[i]));
        }
        return result;
    }

    private EntityError MissingEntityError() => EntityError.Unknown($"{typeof(TEntity).Name} was not returned by the repository.");

    private EntityError NotFoundError() => EntityError.NotFound($"{typeof(TEntity).Name} was not found.");
}
