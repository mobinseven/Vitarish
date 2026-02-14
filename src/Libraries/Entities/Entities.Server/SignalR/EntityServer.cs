using Microsoft.AspNetCore.SignalR;
using Vitarish.Entities.Server.Abstractions;
using Vitarish.Entities.Server.Infrastructure;

namespace Vitarish.Entities.Server.SignalR;

public sealed record EntityInterceptionConfig
{
    public string? UserId { get; set; }
    public DateTimeOffset? Time { get; set; }
}

/// <summary>
/// Base SignalR hub for entity operations with real-time updates.
/// </summary>
/// <remarks>
/// <para>
/// <b>Connection Tracking:</b> The <paramref name="connectionTracker"/> parameter is optional.
/// If not provided (null), connection tracking will be disabled and no connection lifecycle events will be tracked.
/// To enable connection tracking, register <see cref="IEntityHubConnectionTracker"/> in DI:
/// <code>
/// services.AddSingleton&lt;IEntityHubConnectionTracker, EntityHubConnectionTracker&gt;();
/// </code>
/// </para>
/// <para>
/// <b>Service Lifetime:</b> The <typeparamref name="TEntityService"/> is injected via DI and executes
/// within the same scope as the SignalR hub invocation. This ensures consistent scoping behavior
/// where each hub method call operates within its own DI scope created by ASP.NET Core.
/// </para>
/// </remarks>
public class EntityServer<TReadDto, TWriteDto, TDescriptorDto, TEntityService>(
    TEntityService entityService,
    IEntityUserService? entityUserService = null,
    IEntityTimeService? entityTimeService = null,
    IEntityHubConnectionTracker? connectionTracker = null)
    : Hub<IEntityClient>
    where TReadDto : EntityReadDto
    where TWriteDto : EntityWriteDto
    where TDescriptorDto : EntityDescriptorDto
    where TEntityService : IEntityService<TReadDto, TWriteDto, TDescriptorDto>
{
    protected TEntityService EntityService { get; } = entityService ?? throw new ArgumentNullException(nameof(entityService));

    public override async Task OnConnectedAsync()
    {
        string? userId = Context.User?.Identity?.Name;
        connectionTracker?.AddConnection(Context.ConnectionId, userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        connectionTracker?.RemoveConnection(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
    public sealed class EntityNotificationHandler(IHubContext<EntityServer<TReadDto, TWriteDto, TDescriptorDto, TEntityService>, IEntityClient> hubContext) : INotificationHandler<EntityNotification>
    {
        public async Task Handle(EntityNotification notification, CancellationToken cancellationToken) => await hubContext.Clients.All.EntityNotification(notification);
    }

    public Task SetInterceptionConfig(EntityInterceptionConfig? interceptionConfig)
    {
        entityTimeService?.Set(interceptionConfig?.Time);
        entityUserService?.Set(interceptionConfig?.UserId);
        return Task.CompletedTask;
    }

    // Use Context.ConnectionAborted to properly cancel operations when client disconnects
    public Task<EntityResult<TReadDto>> AddAsync(TWriteDto dto) => EntityService.AddAsync(dto, Context.ConnectionAborted);

    public Task<EntityResult<IReadOnlyList<TReadDto>>> AddRangeAsync(IReadOnlyList<TWriteDto> dtos) => EntityService.AddAsync(dtos, Context.ConnectionAborted);

    public Task<EntityResult<TReadDto>> AddOrUpdateAsync(TWriteDto dto) => EntityService.AddOrUpdateAsync(dto, Context.ConnectionAborted);

    public Task<EntityResult<TReadDto>> UpdateAsync(TWriteDto dto) => EntityService.UpdateAsync(dto, Context.ConnectionAborted);

    public Task<EntityResult<IReadOnlyList<TReadDto>>> UpdateRangeAsync(IReadOnlyList<TWriteDto> dtos) => EntityService.UpdateAsync(dtos, Context.ConnectionAborted);

    public Task<EntityResult<int>> CountAsync(TDescriptorDto descriptor) => EntityService.CountAsync(descriptor, Context.ConnectionAborted);

    public Task<EntityResult<TReadDto>> GetAsync(Guid id) => EntityService.GetAsync(id, Context.ConnectionAborted);

    public Task<EntityResult<IReadOnlyList<TReadDto>>> GetListAsync(TDescriptorDto descriptor) => EntityService.GetListAsync(descriptor, Context.ConnectionAborted);

    public Task<EntityResult<PartedResult<TReadDto>>> GetPartedAsync(TDescriptorDto descriptor) => EntityService.GetPartedAsync(descriptor, Context.ConnectionAborted);

    public Task<EntityResult> RemoveAsync(TWriteDto dto) => EntityService.RemoveAsync(dto, Context.ConnectionAborted);

    public Task<EntityResult> RemoveRangeAsync(IReadOnlyList<TWriteDto> dtos) => EntityService.RemoveAsync(dtos, Context.ConnectionAborted);

    public Task<EntityResult> RemoveByIdAsync(Guid id) => EntityService.RemoveAsync(id, Context.ConnectionAborted);

    public Task<EntityResult> RemoveByDescriptorAsync(TDescriptorDto descriptor) => EntityService.RemoveByDescriptorAsync(descriptor, Context.ConnectionAborted);
}
