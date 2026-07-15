using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Enums;

namespace Vitarish.Sea.Api.Authorization;

internal sealed class SeaEventAuthorizationHandler(
    IUserRolesService userRolesService,
    IEventRolesService eventRolesService,
    IRequestContext requestContext,
    IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<OperationAuthorizationRequirement, SeaEventResource>
{
    private bool _userRolesLoaded;
    private bool _subjectIsValid;
    private HashSet<Guid> _readRoleIds = [];
    private HashSet<Guid> _writeRoleIds = [];
    private readonly Dictionary<Guid, (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)> _eventRoles = [];

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        SeaEventResource resource)
    {
        if (await IsAuthorizedAsync(context.User, requirement, resource))
        {
            context.Succeed(requirement);
        }
    }

    internal async Task<bool> IsAuthorizedAsync(
        System.Security.Claims.ClaimsPrincipal principal,
        OperationAuthorizationRequirement requirement,
        SeaEventResource resource)
    {
        if (requirement.Name != SeaAuthorizationOperations.Read.Name && requirement.Name != SeaAuthorizationOperations.Write.Name)
        {
            return false;
        }

        CancellationToken cancellationToken = httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        await EnsureUserRolesAsync(principal, cancellationToken);
        if (!_subjectIsValid)
        {
            return false;
        }

        if (SeaPrincipalAccess.CanManage(principal, requestContext.TenantId))
        {
            return true;
        }

        (IReadOnlyList<Guid> eventReadRoleIds, IReadOnlyList<Guid> eventWriteRoleIds) =
            await GetEventRolesAsync(resource.EventId, cancellationToken);

        bool canWrite = eventWriteRoleIds.Any(_writeRoleIds.Contains);
        bool canRead = canWrite || eventReadRoleIds.Any(_readRoleIds.Contains);

        return requirement.Name == SeaAuthorizationOperations.Write.Name ? canWrite : canRead;
    }

    private async Task EnsureUserRolesAsync(System.Security.Claims.ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        if (_userRolesLoaded)
        {
            return;
        }

        _userRolesLoaded = true;
        AuthenticatedUserIdResolution resolution = AuthenticatedUserIdResolver.Resolve(principal);
        if (resolution is not AuthenticatedUserIdResolution.Success success ||
            !Guid.TryParse(success.UserId.Value, out Guid userId))
        {
            return;
        }

        IReadOnlyList<UserRoleReadDto> roles = (await userRolesService.GetListAsync(
            new UserRoleDescriptorDto { UserId = userId }, cancellationToken)).Value ?? [];

        _writeRoleIds = [.. roles.Where(role => role.Type == RoleType.Write).Select(role => role.RoleId)];
        _readRoleIds = [.. roles.Where(role => role.Type == RoleType.Read).Select(role => role.RoleId), .. _writeRoleIds];
        _subjectIsValid = true;
    }

    private async Task<(IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)> GetEventRolesAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        await PrimeEventRolesAsync([eventId], cancellationToken);
        return _eventRoles[eventId];
    }

    internal async Task PrimeEventRolesAsync(IEnumerable<Guid> eventIds, CancellationToken cancellationToken)
    {
        Guid[] missingIds = [.. eventIds.Distinct().Where(id => !_eventRoles.ContainsKey(id))];
        if (missingIds.Length == 0)
        {
            return;
        }

        IReadOnlyDictionary<Guid, (IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)> loaded =
            await eventRolesService.GetIdsForEventsAsync(missingIds, cancellationToken);
        foreach (Guid eventId in missingIds)
        {
            _eventRoles[eventId] = loaded.TryGetValue(eventId, out var roles)
                ? roles
                : (Array.Empty<Guid>(), Array.Empty<Guid>());
        }
    }
}
