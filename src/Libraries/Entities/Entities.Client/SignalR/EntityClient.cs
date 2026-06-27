
using Microsoft.AspNetCore.SignalR.Client;
using Vitarish.Entities.Client.Models;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Entities.Client.SignalR;
// -----------------------------------------------------------------------------
// Models
// -----------------------------------------------------------------------------
public sealed record EntityInterceptionConfig
{
    public string? UserId { get; init; }
    public DateTime? Time { get; init; }
}

public delegate void EntityNotificationHandler(object? sender, EntityNotification dto);

// -----------------------------------------------------------------------------
// Ambient interception provider (scoped + AsyncLocal) 
// -----------------------------------------------------------------------------
public interface IEntityInterceptionProvider
{
    /// <summary>Returns the current interception config for this logical call (if any).</summary>
    EntityInterceptionConfig? Current { get; }

    /// <summary>Temporarily pushes an interception config for the current async flow. Dispose to pop.</summary>
    IDisposable Use(EntityInterceptionConfig? config);
}

/// <summary>
/// Default implementation that is safe for concurrent requests: keeps state in AsyncLocal
/// and is typically registered as Scoped in DI.
/// </summary>
public sealed class AmbientEntityInterceptionProvider : IEntityInterceptionProvider
{
    private readonly AsyncLocal<Holder> _current = new();

    public EntityInterceptionConfig? Current => _current.Value?.Value;

    public IDisposable Use(EntityInterceptionConfig? config)
    {
        Holder? prior = _current.Value;
        _current.Value = new Holder(config);
        return new Pop(() => _current.Value = prior);
    }

    private sealed class Holder(EntityInterceptionConfig? value)
    {
        public EntityInterceptionConfig? Value { get; } = value;
    }

    private sealed class Pop(Action action) : IDisposable
    {
        private bool _done;

        public void Dispose()
        {
            if (_done)
            {
                return;
            }

            _done = true;
            action();
        }
    }
}

// -----------------------------------------------------------------------------
// Refactored client with smart interception
// -----------------------------------------------------------------------------
public class EntityClient<TReadDto, TWriteDto, TDescriptorDto>(IEntityConnectionFactory<TReadDto> connectionFactory, IEntityInterceptionProvider interceptionProvider) : IAsyncDisposable, IEntityClient<TReadDto, TWriteDto, TDescriptorDto>
    where TReadDto : EntityReadDto
    where TWriteDto : EntityWriteDto
    where TDescriptorDto : EntityDescriptorDto
{
    public event EntityNotificationHandler? EntityNotificationReceived;

    protected HubConnection? Connection { get; private set; }

    private readonly IEntityConnectionFactory<TReadDto> _connectionFactory = connectionFactory;
    private readonly IEntityInterceptionProvider _interceptionProvider = interceptionProvider;

    // Cache the last config we sent to the server so we don't resend on every call
    private EntityInterceptionConfig? _lastSentConfig;

    // ---------------------------------------------------------------------
    // Connection lifecycle
    // ---------------------------------------------------------------------
    protected async Task EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        if (Connection is not null)
        {
            return;
        }

        Connection = await _connectionFactory.CreateConnection();
        RegisterEvents(Connection);
        await Connection.StartAsync(cancellationToken).ConfigureAwait(false);

        // When we reconnect, re-apply the last known interception config once.
        Connection.Reconnected += async _ =>
        {
            try
            { await ApplyInterceptionIfNeededAsync(_interceptionProvider.Current, CancellationToken.None).ConfigureAwait(false); }
            catch { /* swallow - reconnect flow should not crash */ }
        };

        // Apply any ambient config once at startup as well.
        await ApplyInterceptionIfNeededAsync(_interceptionProvider.Current, cancellationToken).ConfigureAwait(false);
    }

    private void RegisterEvents(HubConnection connection) => connection.On<EntityNotification>(nameof(EntityNotification), EntityNotification);

    private void UnregisterEvents(HubConnection connection) => connection.Remove(nameof(EntityNotification));

    public void EntityNotification(EntityNotification dto) => EntityNotificationReceived?.Invoke(this, dto);

    public async Task StartAsync(CancellationToken ct = default) => await EnsureConnectionAsync(ct).ConfigureAwait(false);

    public async Task StopAsync(CancellationToken ct = default)
    {
        if (Connection is null)
        {
            return;
        }

        UnregisterEvents(Connection);
        await Connection.StopAsync(ct).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (Connection is not null)
            {
                await StopAsync(CancellationToken.None).ConfigureAwait(false);
                await Connection.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch { /* ignore */ }
        finally { GC.SuppressFinalize(this); }
    }

    // ---------------------------------------------------------------------
    // Interception handling
    // ---------------------------------------------------------------------
    /// <summary>
    /// Sets interception on the hub connection only if the effective config changed.
    /// This avoids a round-trip before every hub invocation.
    /// </summary>
    private async Task ApplyInterceptionIfNeededAsync(EntityInterceptionConfig? effective, CancellationToken ct)
    {
        if (effective is null)
        {
            return; // Nothing to set.
        }

        if (Equals(effective, _lastSentConfig))
        {
            return; // No change since last time.
        }

        await EnsureConnectionAsync(ct).ConfigureAwait(false);
        await Connection!.InvokeCoreAsync(nameof(SetInterceptionConfig), new object?[] { effective }, ct).ConfigureAwait(false);
        _lastSentConfig = effective;
    }

    /// <summary>
    /// Explicitly allow callers to set the interception config for subsequent operations.
    /// Most code won't need this if an <see cref="IEntityInterceptionProvider"/> is used.
    /// </summary>
    public Task SetInterceptionConfig(EntityInterceptionConfig? interceptionConfig)
        => InvokeNoInterceptAsync(nameof(SetInterceptionConfig), CancellationToken.None, interceptionConfig);

    // ---------------------------------------------------------------------
    // Invocation helpers
    // ---------------------------------------------------------------------
    // NOTE: Use InvokeCoreAsync to avoid the big switch with up to 10 parameters.

    /// <summary>
    /// For write operations (Add/Update/Remove...) we apply interception if the provider has a current config
    /// or if a per-call override is supplied.
    /// </summary>
    protected async Task<TResult> InvokeHubAsync<TResult>(string method, CancellationToken cancellationToken, object?[] args, EntityInterceptionConfig? perCallOverride = null)
    {
        // Merge: per-call override wins, otherwise ambient provider
        EntityInterceptionConfig? effective = perCallOverride ?? _interceptionProvider.Current;
        await ApplyInterceptionIfNeededAsync(effective, cancellationToken).ConfigureAwait(false);

        await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await Connection!.InvokeCoreAsync<TResult>(method, args, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// For read-only operations we skip interception entirely (mirrors your original Count/Get behavior).
    /// </summary>
    protected async Task<TResult> InvokeHubReadAsync<TResult>(string method, CancellationToken cancellationToken, object?[] args)
    {
        await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await Connection!.InvokeCoreAsync<TResult>(method, args, cancellationToken).ConfigureAwait(false);
    }

    protected async Task InvokeNoInterceptAsync(string method, CancellationToken cancellationToken, params object?[] args)
    {
        await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
        await Connection!.InvokeCoreAsync(method, args, cancellationToken).ConfigureAwait(false);
    }

    // ---------------------------------------------------------------------
    // CRUD & Queries  (same surface area, but no interception parameter needed)
    // ---------------------------------------------------------------------

    public Task<EntityResult<TReadDto>> AddAsync(TWriteDto dto, CancellationToken ct = default)
        => InvokeHubAsync<EntityResult<TReadDto>>(nameof(AddAsync), ct, [dto]);

    public Task<EntityResult<IReadOnlyList<TReadDto>>> AddRangeAsync(IReadOnlyList<TWriteDto> dtos, CancellationToken ct = default)
        => InvokeHubAsync<EntityResult<IReadOnlyList<TReadDto>>>(nameof(AddRangeAsync), ct, [dtos]);

    public Task<EntityResult<TReadDto>> AddOrUpdateAsync(TWriteDto dto, CancellationToken ct = default)
        => InvokeHubAsync<EntityResult<TReadDto>>(nameof(AddOrUpdateAsync), ct, [dto]);

    public Task<EntityResult<TReadDto>> UpdateAsync(TWriteDto dto, CancellationToken ct = default)
        => InvokeHubAsync<EntityResult<TReadDto>>(nameof(UpdateAsync), ct, [dto]);

    public Task<EntityResult<IReadOnlyList<TReadDto>>> UpdateRangeAsync(IReadOnlyList<TWriteDto> dtos, CancellationToken ct = default)
        => InvokeHubAsync<EntityResult<IReadOnlyList<TReadDto>>>(nameof(UpdateRangeAsync), ct, [dtos]);

    public Task<EntityResult> RemoveAsync(TWriteDto dto, CancellationToken ct = default)
        => InvokeHubAsync<EntityResult>(nameof(RemoveAsync), ct, [dto]);

    public Task<EntityResult> RemoveRangeAsync(IReadOnlyList<TWriteDto> dtos, CancellationToken ct = default)
        => InvokeHubAsync<EntityResult>(nameof(RemoveRangeAsync), ct, [dtos]);

    public Task<EntityResult> RemoveByIdAsync(Guid id, CancellationToken ct = default)
        => InvokeHubAsync<EntityResult>(nameof(RemoveByIdAsync), ct, [id]);

    public Task<EntityResult> RemoveByDescriptorAsync(TDescriptorDto descriptor, CancellationToken ct = default)
        => InvokeHubAsync<EntityResult>(nameof(RemoveByDescriptorAsync), ct, [descriptor]);

    // READS (no interception)
    public Task<EntityResult<int>> CountAsync(TDescriptorDto descriptor, CancellationToken ct = default)
        => InvokeHubReadAsync<EntityResult<int>>(nameof(CountAsync), ct, [descriptor]);

    public Task<EntityResult<TReadDto>> GetAsync(Guid id, CancellationToken ct = default)
        => InvokeHubReadAsync<EntityResult<TReadDto>>(nameof(GetAsync), ct, [id]);

    public Task<EntityResult<IReadOnlyList<TReadDto>>> GetListAsync(TDescriptorDto descriptor, CancellationToken ct = default)
        => InvokeHubReadAsync<EntityResult<IReadOnlyList<TReadDto>>>(nameof(GetListAsync), ct, [descriptor]);

    public Task<EntityResult<PartedResult<TReadDto>>> GetPartedAsync(TDescriptorDto descriptor, CancellationToken ct = default)
        => InvokeHubReadAsync<EntityResult<PartedResult<TReadDto>>>(nameof(GetPartedAsync), ct, [descriptor]);
}

