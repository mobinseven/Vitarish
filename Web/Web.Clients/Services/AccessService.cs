using Microsoft.AspNetCore.Components.Authorization;
using Vitarish.Web.Clients.Abstractions;
using Vitarish.Web.Clients.Constants;
using Vitarish.Web.Clients.Contracts;
using Vitarish.Web.Clients.Extensions;

namespace Vitarish.Web.Clients.Services;

/// <summary>
/// Service for managing user access and roles.
/// </summary>
public class AccessService(
    AuthenticationStateProvider authenticationStateProvider,
    IUserAccessApiClient apiClient,
    IRequestIdentityContext identityContext) : IAccessService
{
    private List<UserDto> users { get; set; } = [];

    /// <inheritdoc />
    public Access Access { get; private set; } = new();

    /// <inheritdoc />
    public IEnumerable<string> Roles { get; private set; } = Array.Empty<string>();

    /// <inheritdoc />
    public IReadOnlyList<UserDto> Users => users;

    /// <inheritdoc />
    public UserDto? User { get; private set; }

    /// <inheritdoc />
    public void RenameRole(string roleName, string roleNewName)
    {
        users.Where(r => r.Access!.Writers.Contains(roleName))
            .ToList()
            .ForEach(r => r.Access!.Writers = r.Access.Writers.Except([roleName]).Union([roleNewName]).ToArray());

        users.Where(r => r.Access!.Readers.Contains(roleName))
            .ToList()
            .ForEach(r => r.Access!.Readers = r.Access.Readers.Except([roleName]).Union([roleNewName]).ToArray());
    }

    /// <inheritdoc />
    public async Task InitializeAsync(string? clientId = null)
    {
        if (User is not null)
        {
            return;
        }

        clientId ??= identityContext.ClientId;
        System.Security.Claims.ClaimsPrincipal claimPrincipal = (await authenticationStateProvider.GetAuthenticationStateAsync()).User;

        if (claimPrincipal.Identity?.IsAuthenticated ?? false)
        {
            string? userId = claimPrincipal.GetId();

            if (userId is null)
            {
                return;
            }

            bool hasaccess = await apiClient.HasUserAccess(userId, clientId);

            if (!hasaccess)
            {
                return;
            }

            User = await apiClient.GetClientUser(clientId, userId);

            if (User == null)
            {
                return;
            }

            ClaimDto? accessClaim = await apiClient.GetUserAccessClaim(userId, clientId)
                ?? throw new InvalidOperationException("This can not happen as the user has passed hasaccess query");

            Access = AccessExtensions.GenerateAccess(accessClaim.Value!);

            IEnumerable<UserDto> usersDto = (await apiClient.GetClientUsers(clientId))?.DistinctBy(u => u.Id) ?? [];

            users = [.. usersDto.Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                AccessClaim = u.AccessClaim,
                Access = AccessExtensions.GenerateAccess(u.AccessClaim!.Value!),
                ProfilePreferences = u.ProfilePreferences
            })
            .OrderBy(u => u.LastName)];

            Roles = [.. users.SelectMany(u => u.Access!.Roles).Distinct()];

            if (Access.Admin)
            {
                foreach (UserDto user in users)
                {
                    user.Access!.PropertyChanged += async (_, _) =>
                    {
                        await apiClient.SetUserAccess(user.Id, clientId, $"{clientId}:{user.Access.GetClaimAccessValue()}");
                    };
                }
            }
        }
    }

    /* ---------- Queries ---------- */

    /// <inheritdoc />
    public async Task<IEnumerable<ClaimDto>> GetUserClaimsAsync(string userId, CancellationToken ct = default) =>
        await apiClient.GetUserClaims(userId, ct) ?? [];

    /// <inheritdoc />
    public Task<ClaimDto?> GetUserAccessClaimAsync(string userId, string clientId, CancellationToken ct = default) =>
        apiClient.GetUserAccessClaim(userId, clientId, ct);

    /// <inheritdoc />
    public async Task<IEnumerable<ClaimDto>> GetUserAccessClaimsAsync(string userId, string claimPrefix, CancellationToken ct = default)
    {
        IEnumerable<ClaimDto> claims = await GetUserClaimsAsync(userId, ct);

        return claims
            .Where(claim => string.Equals(claim.Type, AuthConstants.AccessClaim, StringComparison.OrdinalIgnoreCase) &&
                             (string.IsNullOrWhiteSpace(claimPrefix) ||
                              (claim.Value?.StartsWith(claimPrefix, StringComparison.OrdinalIgnoreCase) ?? false)));
    }

    /// <inheritdoc />
    public Task<bool> HasUserAccessAsync(string userId, string clientId, CancellationToken ct = default) =>
        apiClient.HasUserAccess(userId, clientId, ct);

    /// <inheritdoc />
    public async Task<IEnumerable<UserDto>> GetClientUsersAsync(string clientId, CancellationToken ct = default) =>
        await apiClient.GetClientUsers(clientId, ct) ?? [];

    /// <inheritdoc />
    public Task<UserDto?> GetClientUserAsync(string clientId, string userId, CancellationToken ct = default) =>
        apiClient.GetClientUser(clientId, userId, ct);

    /* ---------- Commands ---------- */

    /// <inheritdoc />
    public Task SetUserAccessAsync(string userId, string clientId, string accessValue, CancellationToken ct = default) =>
        apiClient.SetUserAccess(userId, clientId, accessValue, ct);

    /// <inheritdoc />
    public Task RemoveUserAccessAsync(string userId, string clientId, CancellationToken ct = default) =>
        apiClient.RemoveUserAccess(userId, clientId, ct);
}
