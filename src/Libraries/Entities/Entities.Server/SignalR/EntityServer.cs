using Microsoft.AspNetCore.SignalR;
using Vitarish.Entities.Server.Infrastructure;
using Vitarish.Entities.Server.Services;

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
/// <b>Service Lifetime Management:</b> This hub uses <see cref="IServiceScopeFactory"/> to create a new service scope
/// for each operation, ensuring that scoped services (including <see cref="IMediator"/> and its handlers) are not disposed
/// prematurely during async operations. This prevents <see cref="ObjectDisposedException"/> when the Blazor Server
/// circuit or SignalR connection is disposed while operations are still running.
/// </para>
/// </remarks>
public class EntityServer<TReadDto, TWriteDto, TDescriptorDto, TEntityService>(
    IServiceScopeFactory serviceScopeFactory,
    IEntityUserService? entityUserService = null,
    IEntityTimeService? entityTimeService = null,
    IEntityHubConnectionTracker? connectionTracker = null)
    : Hub<IEntityClient>
    where TReadDto : EntityReadDto
    where TWriteDto : EntityWriteDto
    where TDescriptorDto : EntityDescriptorDto
    where TEntityService : IEntityService<TReadDto, TWriteDto, TDescriptorDto>
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
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
    public sealed class EntityNotificationHandler(IHubContext<EntityServer<TReadDto, TWriteDto, TDescriptorDto, TEntityService>, IEntityClient> hubContext) : INotificationHandler<EntityNotificationCore>
    {
        public async Task Handle(EntityNotificationCore notification, CancellationToken cancellationToken) => await hubContext.Clients.All.EntityNotification(notification);
    }

    public Task SetInterceptionConfig(EntityInterceptionConfig? interceptionConfig)
    {
        entityTimeService?.Set(interceptionConfig?.Time);
        entityUserService?.Set(interceptionConfig?.UserId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes an entity service operation within a new service scope to prevent disposal issues.
    /// Preserves interception configuration by copying values to the new scope's services.
    /// </summary>
    private async Task<TResult> ExecuteInScopeAsync<TResult>(Func<TEntityService, Task<TResult>> operation, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using IServiceScope scope = _serviceScopeFactory.CreateScope();

        // Copy interception config to the new scope's services to preserve user/time context
        if (entityUserService != null)
        {
            IEntityUserService? scopedUserService = scope.ServiceProvider.GetService<IEntityUserService>();
            scopedUserService?.Set(entityUserService.UserId);
        }

        if (entityTimeService != null)
        {
            IEntityTimeService? scopedTimeService = scope.ServiceProvider.GetService<IEntityTimeService>();
            if (scopedTimeService != null && entityTimeService.Now != default)
            {
                scopedTimeService.Set(entityTimeService.Now);
            }
        }

        TEntityService entityService = scope.ServiceProvider.GetRequiredService<TEntityService>();
        return await operation(entityService);
    }

    // Use Context.ConnectionAborted to properly cancel operations when client disconnects
    // Each operation creates a new service scope to prevent IServiceProvider disposal issues
    public Task<EntityResult<TReadDto>> AddAsync(TWriteDto dto) => ExecuteInScopeAsync(service => service.AddAsync(dto, Context.ConnectionAborted), Context.ConnectionAborted);

    public Task<EntityResult<IReadOnlyList<TReadDto>>> AddRangeAsync(IReadOnlyList<TWriteDto> dtos) => ExecuteInScopeAsync(service => service.AddAsync(dtos, Context.ConnectionAborted), Context.ConnectionAborted);

    public Task<EntityResult<TReadDto>> AddOrUpdateAsync(TWriteDto dto) => ExecuteInScopeAsync(service => service.AddOrUpdateAsync(dto, Context.ConnectionAborted), Context.ConnectionAborted);

    public Task<EntityResult<TReadDto>> UpdateAsync(TWriteDto dto) => ExecuteInScopeAsync(service => service.UpdateAsync(dto, Context.ConnectionAborted), Context.ConnectionAborted);

    public Task<EntityResult<IReadOnlyList<TReadDto>>> UpdateRangeAsync(IReadOnlyList<TWriteDto> dtos) => ExecuteInScopeAsync(service => service.UpdateAsync(dtos, Context.ConnectionAborted), Context.ConnectionAborted);

    public Task<EntityResult<int>> CountAsync(TDescriptorDto descriptor) => ExecuteInScopeAsync(service => service.CountAsync(descriptor, Context.ConnectionAborted), Context.ConnectionAborted);

    public Task<EntityResult<TReadDto>> GetAsync(Guid id) => ExecuteInScopeAsync(service => service.GetAsync(id, Context.ConnectionAborted), Context.ConnectionAborted);

    public Task<EntityResult<IReadOnlyList<TReadDto>>> GetListAsync(TDescriptorDto descriptor) => ExecuteInScopeAsync(service => service.GetListAsync(descriptor, Context.ConnectionAborted), Context.ConnectionAborted);

    public Task<EntityResult<PartedResult<TReadDto>>> GetPartedAsync(TDescriptorDto descriptor) => ExecuteInScopeAsync(service => service.GetPartedAsync(descriptor, Context.ConnectionAborted), Context.ConnectionAborted);

    public Task<EntityResult> RemoveAsync(TWriteDto dto) => ExecuteInScopeAsync(service => service.RemoveAsync(dto, Context.ConnectionAborted), Context.ConnectionAborted);

    public Task<EntityResult> RemoveRangeAsync(IReadOnlyList<TWriteDto> dtos) => ExecuteInScopeAsync(service => service.RemoveAsync(dtos, Context.ConnectionAborted), Context.ConnectionAborted);

    public Task<EntityResult> RemoveByIdAsync(Guid id) => ExecuteInScopeAsync(service => service.RemoveAsync(id, Context.ConnectionAborted), Context.ConnectionAborted);

    public Task<EntityResult> RemoveByDescriptorAsync(TDescriptorDto descriptor) => ExecuteInScopeAsync(service => service.RemoveByDescriptorAsync(descriptor, Context.ConnectionAborted), Context.ConnectionAborted);
}
