namespace Vitarish.Entities.Server.Services;

public abstract class EntityService<TEntity, TDescriptor, TReadDto, TWriteDto, TDescriptorDto>(
    IServiceScopeFactory serviceScopeFactory,
    Func<TEntity, TReadDto> outgoingMap,
    Func<TWriteDto, TEntity> incomingMap,
    Func<TDescriptorDto, TDescriptor> descriptorMap) : IEntityService<TReadDto, TWriteDto, TDescriptorDto>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor
    where TReadDto : EntityReadDto
    where TWriteDto : EntityWriteDto
    where TDescriptorDto : EntityDescriptorDto
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
    private readonly Func<TEntity, TReadDto> _outgoingMap = outgoingMap ?? throw new ArgumentNullException(nameof(outgoingMap));
    private readonly Func<TWriteDto, TEntity> _incomingMap = incomingMap ?? throw new ArgumentNullException(nameof(incomingMap));

    protected Func<TDescriptorDto, TDescriptor> DescriptorMap { get; } = descriptorMap ?? throw new ArgumentNullException(nameof(descriptorMap));

    /// <summary>
    /// Executes a MediatR operation within a new service scope to prevent IServiceProvider disposal issues.
    /// This ensures that the service provider used by MediatR to resolve handlers is not disposed during async operations.
    /// </summary>
    private async Task<TResult> ExecuteInScopeAsync<TResult>(Func<IMediator, Task<TResult>> operation, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await operation(mediator);
    }

    /// <summary>
    /// Executes a MediatR operation within a new service scope to prevent IServiceProvider disposal issues.
    /// This overload is for operations that don't return a value.
    /// </summary>
    private async Task ExecuteInScopeAsync(Func<IMediator, Task> operation, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await operation(mediator);
    }

    public Task<EntityResult<TReadDto>> AddAsync(TWriteDto dto, CancellationToken cancellationToken)
    {
        return ExecuteDtoAsync(
                mediator => mediator.Send(new AddEntityCommand<TEntity, TDescriptor>(_incomingMap(dto)), cancellationToken),
                MissingEntityError,
                cancellationToken);
    }

    public Task<EntityResult<TReadDto>> AddAsync(TEntity entity, CancellationToken cancellationToken)
    {
        return ExecuteDtoAsync(
                mediator => mediator.Send(new AddEntityCommand<TEntity, TDescriptor>(entity), cancellationToken),
                MissingEntityError,
                cancellationToken);
    }

    public Task<EntityResult<TEntity>> AddEntityAsync(TEntity entity, CancellationToken cancellationToken)
    {
        return ExecuteEntityAsync(
                mediator => mediator.Send(new AddEntityCommand<TEntity, TDescriptor>(entity), cancellationToken),
                MissingEntityError,
                cancellationToken);
    }

    public Task<EntityResult<IReadOnlyList<TReadDto>>> AddAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken)
    {
        return ExecuteListAsync(
                mediator => mediator.Send(new AddEntitiesCommand<TEntity, TDescriptor>(entities), cancellationToken),
                _outgoingMap,
                cancellationToken);
    }

    public Task<EntityResult<IReadOnlyList<TReadDto>>> AddAsync(IReadOnlyList<TWriteDto> dtos, CancellationToken cancellationToken)
    {
        return ExecuteListAsync(
                mediator => mediator.Send(new AddEntitiesCommand<TEntity, TDescriptor>(MapList(dtos, _incomingMap)), cancellationToken),
                _outgoingMap,
                cancellationToken);
    }

    public Task<EntityResult<IReadOnlyList<TEntity>>> AddEntitiesAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken)
    {
        return ExecuteEntityListAsync(mediator => mediator.Send(new AddEntitiesCommand<TEntity, TDescriptor>(entities), cancellationToken), cancellationToken);
    }

    public Task<EntityResult<TReadDto>> UpdateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        return ExecuteDtoAsync(
                mediator => mediator.Send(new UpdateEntityCommand<TEntity, TDescriptor>(entity), cancellationToken),
                MissingEntityError,
                cancellationToken);
    }

    public Task<EntityResult<TReadDto>> UpdateAsync(TWriteDto dto, CancellationToken cancellationToken)
    {
        return ExecuteDtoAsync(
                mediator => mediator.Send(new UpdateEntityCommand<TEntity, TDescriptor>(_incomingMap(dto)), cancellationToken),
                MissingEntityError,
                cancellationToken);
    }

    public Task<EntityResult<TEntity>> UpdateEntityAsync(TEntity entity, CancellationToken cancellationToken)
    {
        return ExecuteEntityAsync(
                mediator => mediator.Send(new UpdateEntityCommand<TEntity, TDescriptor>(entity), cancellationToken),
                MissingEntityError,
                cancellationToken);
    }

    public Task<EntityResult<IReadOnlyList<TReadDto>>> UpdateAsync(IReadOnlyList<TWriteDto> dtos, CancellationToken cancellationToken)
    {
        return ExecuteListAsync(
                mediator => mediator.Send(new UpdateEntitiesCommand<TEntity, TDescriptor>(MapList(dtos, _incomingMap)), cancellationToken),
                _outgoingMap,
                cancellationToken);
    }

    public Task<EntityResult<IReadOnlyList<TReadDto>>> UpdateAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken)
    {
        return ExecuteListAsync(
                mediator => mediator.Send(new UpdateEntitiesCommand<TEntity, TDescriptor>(entities), cancellationToken),
                _outgoingMap,
                cancellationToken);
    }

    public Task<EntityResult<IReadOnlyList<TEntity>>> UpdateEntitiesAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken)
    {
        return ExecuteEntityListAsync(mediator => mediator.Send(new UpdateEntitiesCommand<TEntity, TDescriptor>(entities), cancellationToken), cancellationToken);
    }

    public Task<EntityResult<TReadDto>> AddOrUpdateAsync(TWriteDto dto, CancellationToken cancellationToken)
    {
        return ExecuteDtoAsync(
                mediator => mediator.Send(new AddOrUpdateEntityCommand<TEntity, TDescriptor>(_incomingMap(dto)), cancellationToken),
                MissingEntityError,
                cancellationToken);
    }

    public Task<EntityResult<TReadDto>> AddOrUpdateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        return ExecuteDtoAsync(
                mediator => mediator.Send(new AddOrUpdateEntityCommand<TEntity, TDescriptor>(entity), cancellationToken),
                MissingEntityError,
                cancellationToken);
    }

    public Task<EntityResult<TEntity>> AddOrUpdateEntityAsync(TEntity entity, CancellationToken cancellationToken)
    {
        return ExecuteEntityAsync(
                mediator => mediator.Send(new AddOrUpdateEntityCommand<TEntity, TDescriptor>(entity), cancellationToken),
                MissingEntityError,
                cancellationToken);
    }

    public Task<EntityResult> RemoveAsync(TWriteDto dto, CancellationToken cancellationToken)
    {
        return ExecuteAsync(mediator => mediator.Send(new RemoveEntityCommand<TEntity, TDescriptor>(_incomingMap(dto)), cancellationToken), cancellationToken);
    }

    public Task<EntityResult> RemoveAsync(TEntity entity, CancellationToken cancellationToken)
    {
        return ExecuteAsync(mediator => mediator.Send(new RemoveEntityCommand<TEntity, TDescriptor>(entity), cancellationToken), cancellationToken);
    }

    public Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        return ExecuteAsync(mediator => mediator.Send(new RemoveEntityByIdCommand<TEntity, TDescriptor>(id), cancellationToken), cancellationToken);
    }

    public Task<EntityResult> RemoveAsync(IReadOnlyList<TWriteDto> dtos, CancellationToken cancellationToken)
    {
        return ExecuteAsync(mediator => mediator.Send(new RemoveEntitiesCommand<TEntity, TDescriptor>(MapList(dtos, _incomingMap)), cancellationToken), cancellationToken);
    }

    public Task<EntityResult> RemoveAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken)
    {
        return ExecuteAsync(mediator => mediator.Send(new RemoveEntitiesCommand<TEntity, TDescriptor>(entities), cancellationToken), cancellationToken);
    }

    public Task<EntityResult> RemoveByDescriptorAsync(TDescriptor descriptor, CancellationToken cancellationToken)
    {
        return ExecuteAsync(mediator => mediator.Send(new RemoveEntitiesByDescriptorCommand<TEntity, TDescriptor>(descriptor), cancellationToken), cancellationToken);
    }

    public Task<EntityResult> RemoveByDescriptorAsync(TDescriptorDto descriptor, CancellationToken cancellationToken)
    {
        return RemoveByDescriptorAsync(DescriptorMap(descriptor), cancellationToken);
    }

    public Task<EntityResult<TReadDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return GetAsync(new GetEntityByIdQuery<TEntity, TDescriptor>(id), cancellationToken);
    }

    public Task<EntityResult<TEntity>> GetEntityAsync(Guid id, CancellationToken cancellationToken)
    {
        return GetEntityAsync(new GetEntityByIdQuery<TEntity, TDescriptor>(id), cancellationToken);
    }

    public Task<EntityResult<TReadDto>> GetAsync<TQuery>(TQuery query, CancellationToken cancellationToken)
        where TQuery : IRequest<TEntity?>
    {
        return ExecuteDtoAsync(
                mediator => mediator.Send(query, cancellationToken),
                NotFoundError,
                cancellationToken);
    }

    public Task<EntityResult<TEntity>> GetEntityAsync<TQuery>(TQuery query, CancellationToken cancellationToken)
        where TQuery : IRequest<TEntity?>
    {
        return ExecuteEntityAsync(
                mediator => mediator.Send(query, cancellationToken),
                NotFoundError,
                cancellationToken);
    }

    public Task<EntityResult<TReadDto>> GetAsync<TQuery, TOtherEntity, TOtherDescriptor>(
        TQuery query,
        Func<TOtherEntity, TReadDto> map,
        CancellationToken cancellationToken)
        where TOtherEntity : Entity
        where TOtherDescriptor : EntityDescriptor
        where TQuery : IRequest<TOtherEntity?>
    {
        return ExecuteMappedEntityAsync(
                mediator => mediator.Send(query, cancellationToken),
                map,
                () => EntityError.NotFound($"{typeof(TOtherEntity).Name} was not found."),
                cancellationToken);
    }

    public Task<EntityResult<int>> CountAsync(TDescriptor descriptor, CancellationToken cancellationToken)
    {
        return ExecuteValueAsync(mediator => mediator.Send(new CountEntitiesQuery<TEntity, TDescriptor>(descriptor), cancellationToken), cancellationToken);
    }

    public Task<EntityResult<int>> CountAsync(TDescriptorDto descriptor, CancellationToken cancellationToken)
    {
        return CountAsync(DescriptorMap(descriptor), cancellationToken);
    }

    public Task<EntityResult<int>> CountAsync<TOtherEntity, TOtherDescriptor>(TOtherDescriptor descriptor, CancellationToken cancellationToken)
        where TOtherEntity : Entity
        where TOtherDescriptor : EntityDescriptor
    {
        return ExecuteValueAsync(mediator => mediator.Send(new CountEntitiesQuery<TOtherEntity, TOtherDescriptor>(descriptor), cancellationToken), cancellationToken);
    }

    public Task<EntityResult<IReadOnlyList<TReadDto>>> GetListAsync(TDescriptor descriptor, CancellationToken cancellationToken)
    {
        return ExecuteListAsync(
                mediator => mediator.Send(new GetEntitiesQuery<TEntity, TDescriptor>(descriptor), cancellationToken),
                _outgoingMap,
                cancellationToken);
    }

    public Task<EntityResult<IReadOnlyList<TReadDto>>> GetListAsync(TDescriptorDto descriptor, CancellationToken cancellationToken)
    {
        return GetListAsync(DescriptorMap(descriptor), cancellationToken);
    }

    public Task<EntityResult<IReadOnlyList<TReadDto>>> GetListAsync<TOtherEntity, TOtherDescriptor>(
        TOtherDescriptor descriptor,
        Func<TOtherEntity, TReadDto> map,
        CancellationToken cancellationToken)
        where TOtherEntity : Entity
        where TOtherDescriptor : EntityDescriptor
    {
        return ExecuteListAsync<TOtherEntity, TReadDto>(
                mediator => mediator.Send(new GetEntitiesQuery<TOtherEntity, TOtherDescriptor>(descriptor), cancellationToken),
                map,
                cancellationToken);
    }

    public Task<EntityResult<IReadOnlyList<TReadDto>>> GetListAsync<TQuery>(TQuery query, CancellationToken cancellationToken)
        where TQuery : IRequest<IReadOnlyList<TEntity>>
    {
        return ExecuteListAsync(mediator => mediator.Send(query, cancellationToken), _outgoingMap, cancellationToken);
    }

    public Task<EntityResult<IReadOnlyList<TReadDto>>> GetListAsync<TQuery, TOtherEntity>(
        TQuery query,
        Func<TOtherEntity, TReadDto> map,
        CancellationToken cancellationToken)
        where TOtherEntity : Entity
        where TQuery : IRequest<IReadOnlyList<TOtherEntity>>
    {
        return ExecuteListAsync(mediator => mediator.Send(query, cancellationToken), map, cancellationToken);
    }

    public async Task<EntityResult<PartedResult<TReadDto>>> GetPartedAsync(TDescriptor descriptor, CancellationToken cancellationToken)
    {
        EntityResult<IReadOnlyList<TReadDto>> itemsResult = await ExecuteListAsync(
            mediator => mediator.Send(new GetEntitiesQuery<TEntity, TDescriptor>(descriptor), cancellationToken),
            _outgoingMap,
            cancellationToken);

        if(itemsResult.IsFailure || itemsResult.Value is null)
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

    public Task<EntityResult<PartedResult<TReadDto>>> GetPartedAsync(TDescriptorDto descriptor, CancellationToken cancellationToken)
    {
        return GetPartedAsync(DescriptorMap(descriptor), cancellationToken);
    }

    public Task<EntityResult<PartedResult<TReadDto>>> GetPartedAsync<TQuery>(TQuery query, CancellationToken cancellationToken)
        where TQuery : IRequest<PartedResult<TEntity>>
    {
        return ExecutePartedAsync(mediator => mediator.Send(query, cancellationToken), _outgoingMap, cancellationToken);
    }

    private async Task<EntityResult> ExecuteAsync(Func<IMediator, Task> action, CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteInScopeAsync(action, cancellationToken);
            return EntityResult.Success();
        }
        catch(EntityFailureException ex)
        {
            return EntityResult.Failure(ex.Error);
        }
    }

    private Task<EntityResult<TReadDto>> ExecuteDtoAsync(Func<IMediator, Task<TEntity?>> action, Func<EntityError> nullErrorFactory, CancellationToken cancellationToken)
    {
        return ExecuteMappedEntityAsync(action, _outgoingMap, nullErrorFactory, cancellationToken);
    }

    private Task<EntityResult<TEntity>> ExecuteEntityAsync(Func<IMediator, Task<TEntity?>> action, Func<EntityError> nullErrorFactory, CancellationToken cancellationToken)
    {
        return ExecuteMappedEntityAsync(action, static entity => entity, nullErrorFactory, cancellationToken);
    }

    private async Task<EntityResult<TProjection>> ExecuteMappedEntityAsync<TSource, TProjection>(
        Func<IMediator, Task<TSource?>> action,
        Func<TSource, TProjection> map,
        Func<EntityError> nullErrorFactory,
        CancellationToken cancellationToken)
        where TSource : class
    {
        if(action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if(map is null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        if(nullErrorFactory is null)
        {
            throw new ArgumentNullException(nameof(nullErrorFactory));
        }

        try
        {
            TSource? entity = await ExecuteInScopeAsync(action, cancellationToken);
            return entity is null ? EntityResult<TProjection>.Failure(nullErrorFactory()) : EntityResult<TProjection>.Success(map(entity));
        }
        catch(EntityFailureException ex)
        {
            return EntityResult<TProjection>.Failure(ex.Error);
        }
    }

    private async Task<EntityResult<IReadOnlyList<TProjection>>> ExecuteListAsync<TSource, TProjection>(
        Func<IMediator, Task<IReadOnlyList<TSource>>> action,
        Func<TSource, TProjection> map,
        CancellationToken cancellationToken)
    {
        if(action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if(map is null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        try
        {
            IReadOnlyList<TSource> entities = await ExecuteInScopeAsync(action, cancellationToken);
            return EntityResult<IReadOnlyList<TProjection>>.Success(MapList(entities, map));
        }
        catch(EntityFailureException ex)
        {
            return EntityResult<IReadOnlyList<TProjection>>.Failure(ex.Error);
        }
    }

    private Task<EntityResult<IReadOnlyList<TReadDto>>> ExecuteListAsync(
        Func<IMediator, Task<IReadOnlyList<TEntity>>> action,
        Func<TEntity, TReadDto> map,
        CancellationToken cancellationToken)
    {
        return ExecuteListAsync<TEntity, TReadDto>(action, map, cancellationToken);
    }

    private Task<EntityResult<IReadOnlyList<TReadDto>>> ExecuteListAsync(Func<IMediator, Task<IReadOnlyList<TEntity>>> action, CancellationToken cancellationToken)
    {
        return ExecuteListAsync(action, _outgoingMap, cancellationToken);
    }

    private Task<EntityResult<IReadOnlyList<TEntity>>> ExecuteEntityListAsync(Func<IMediator, Task<IReadOnlyList<TEntity>>> action, CancellationToken cancellationToken)
    {
        return ExecuteListAsync(action, static entity => entity, cancellationToken);
    }

    private async Task<EntityResult<PartedResult<TProjection>>> ExecutePartedAsync<TSource, TProjection>(
        Func<IMediator, Task<PartedResult<TSource>>> action,
        Func<TSource, TProjection> map,
        CancellationToken cancellationToken)
    {
        if(action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if(map is null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        try
        {
            PartedResult<TSource> result = await ExecuteInScopeAsync(action, cancellationToken);
            return EntityResult<PartedResult<TProjection>>.Success(
                new PartedResult<TProjection>(MapList(result.Items, map), result.TotalItems));
        }
        catch(EntityFailureException ex)
        {
            return EntityResult<PartedResult<TProjection>>.Failure(ex.Error);
        }
    }

    private async Task<EntityResult<int>> ExecuteValueAsync(Func<IMediator, Task<int>> action, CancellationToken cancellationToken)
    {
        if(action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        try
        {
            int value = await ExecuteInScopeAsync(action, cancellationToken);
            return EntityResult<int>.Success(value);
        }
        catch(EntityFailureException ex)
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
        if(source.Count == 0)
        {
            return [];
        }

        List<TProjection> result = new(source.Count);
        for(int i = 0; i < source.Count; i++)
        {
            result.Add(map(source[i]));
        }
        return result;
    }

    private EntityError MissingEntityError()
    {
        return EntityError.Unknown($"{typeof(TEntity).Name} was not returned by the repository.");
    }

    private EntityError NotFoundError()
    {
        return EntityError.NotFound($"{typeof(TEntity).Name} was not found.");
    }
}
