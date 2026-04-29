using Vitarish.Entities.Server.Extensions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Appointments.Infrastructure.Services;

internal class AccessControlledSessionsService(
    IRequestContext requestContext,
    ISessionsService sessionsService) : IAccessControlledSessionsService
{
    private bool IsAdmin => requestContext.IsTenantAdmin() || requestContext.IsTenantOwner();

    private static InvalidOperationException AdminRoleRequired => new("Admin role is required to perform this operation.");

    // ── Reads ──────────────────────────────────────────────────────────────────

    public Task<EntityResult<SessionReadDto>> GetAsync(Guid id, CancellationToken cancellationToken)
        => sessionsService.GetAsync(id, cancellationToken);

    public Task<EntityResult<IReadOnlyList<SessionReadDto>>> GetListAsync(SessionDescriptorDto descriptor, CancellationToken cancellationToken)
        => sessionsService.GetListAsync(ScopeDescriptor(descriptor), cancellationToken);

    public Task<EntityResult<PartedResult<SessionReadDto>>> GetPartedAsync(SessionDescriptorDto descriptor, CancellationToken cancellationToken)
        => sessionsService.GetPartedAsync(ScopeDescriptor(descriptor), cancellationToken);

    public Task<EntityResult<int>> CountAsync(SessionDescriptorDto descriptor, CancellationToken cancellationToken)
        => sessionsService.CountAsync(ScopeDescriptor(descriptor), cancellationToken);

    public Task<EntityResult<int>> GetAvailableCapacityAsync(Guid sessionId, CancellationToken cancellationToken)
        => sessionsService.GetAvailableCapacityAsync(sessionId, cancellationToken);

    // ── Writes (admin only) ────────────────────────────────────────────────────

    public Task<EntityResult<SessionReadDto>> AddAsync(SessionWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin ? sessionsService.AddAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<IReadOnlyList<SessionReadDto>>> AddAsync(IReadOnlyList<SessionWriteDto> dtos, CancellationToken cancellationToken)
        => IsAdmin ? sessionsService.AddAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<SessionReadDto>> UpdateAsync(SessionWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin ? sessionsService.UpdateAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<IReadOnlyList<SessionReadDto>>> UpdateAsync(IReadOnlyList<SessionWriteDto> dtos, CancellationToken cancellationToken)
        => IsAdmin ? sessionsService.UpdateAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<SessionReadDto>> AddOrUpdateAsync(SessionWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin ? sessionsService.AddOrUpdateAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(SessionWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin ? sessionsService.RemoveAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(IReadOnlyList<SessionWriteDto> dtos, CancellationToken cancellationToken)
        => IsAdmin ? sessionsService.RemoveAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken)
        => IsAdmin ? sessionsService.RemoveAsync(id, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveByDescriptorAsync(SessionDescriptorDto descriptor, CancellationToken cancellationToken)
        => IsAdmin ? sessionsService.RemoveByDescriptorAsync(descriptor, cancellationToken) : throw AdminRoleRequired;

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Non-admin callers only see active sessions.
    /// </summary>
    private SessionDescriptorDto ScopeDescriptor(SessionDescriptorDto descriptor)
        => descriptor;
}
