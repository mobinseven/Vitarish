
using Microsoft.AspNetCore.Components.Authorization;
using Vorn.Aaas.Client.Abstractions;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Aaas.Client.Constants;
using Vorn.Aaas.Client.Contracts;
using Vorn.Aaas.Client.Extensions;

namespace Vorn.Aaas.Client.Services;

public class AccessService(
    AuthenticationStateProvider authenticationStateProvider,
    IUserAccessApiClient apiClient,
    IRequestIdentityContext vornContext) : IAccessService
{
    private List<UserDto> users { get; set; } = [];

    public Access Access { get; private set; } = new();

    public IEnumerable<string> Roles { get; private set; } = Array.Empty<string>();

    public IReadOnlyList<UserDto> Users => users;

    public UserDto? User { get; private set; }

    public void RenameRole(string roleName, string roleNewName)
    {
        users.Where(r => r.Access.Writers.Contains(roleName))
            .ToList()
            .ForEach(r => r.Access.Writers = r.Access.Writers.Except([roleName]).Union([roleNewName]).ToArray());

        users.Where(r => r.Access.Readers.Contains(roleName))
            .ToList()
            .ForEach(r => r.Access.Readers = r.Access.Readers.Except([roleName]).Union([roleNewName]).ToArray());
    }

    public async Task InitializeAsync(string? clientId = null)
    {
        if (User is not null)
        {
            return;
        }

        clientId ??= vornContext.ClientId;
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

            ClaimDto? accessClaim = await apiClient.GetUserAccessClaim(userId, clientId) ?? throw new Exception("This can not happen as the user has passed hasaccess query");

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

            Roles = [.. users.SelectMany(u => u.Access.Roles).Distinct()];

            if (Access.Admin)
            {
                foreach (UserDto user in users)
                {
                    user.Access.PropertyChanged += async (_, _) =>
                    {
                        await apiClient.SetUserAccess(user.Id, clientId, $"{clientId}:{user.Access.GetClaimAccessValue()}");
                    };
                }
            }
        }
    }

    /* ---------- Queries ---------- */

    public async Task<IEnumerable<ClaimDto>> GetUserClaimsAsync(string userId, CancellationToken ct = default) =>
        await apiClient.GetUserClaims(userId, ct) ?? [];

    public Task<ClaimDto?> GetUserAccessClaimAsync(string userId, string clientId, CancellationToken ct = default) =>
        apiClient.GetUserAccessClaim(userId, clientId, ct);

    public async Task<IEnumerable<ClaimDto>> GetUserAccessClaimsAsync(string userId, string claimPrefix, CancellationToken ct = default)
    {
        IEnumerable<ClaimDto> claims = await GetUserClaimsAsync(userId, ct);

        return claims
            .Where(claim => string.Equals(claim.Type, AaasConstants.AccessClaim, StringComparison.OrdinalIgnoreCase) &&
                             (string.IsNullOrWhiteSpace(claimPrefix) ||
                              (claim.Value?.StartsWith(claimPrefix, StringComparison.OrdinalIgnoreCase) ?? false)));
    }

    public Task<bool> HasUserAccessAsync(string userId, string clientId, CancellationToken ct = default) =>
        apiClient.HasUserAccess(userId, clientId, ct);

    public async Task<IEnumerable<UserDto>> GetClientUsersAsync(string clientId, CancellationToken ct = default) =>
        await apiClient.GetClientUsers(clientId, ct) ?? [];

    public Task<UserDto?> GetClientUserAsync(string clientId, string userId, CancellationToken ct = default) =>
        apiClient.GetClientUser(clientId, userId, ct);

    /* ---------- Commands ---------- */

    public Task SetUserAccessAsync(string userId, string clientId, string accessValue, CancellationToken ct = default) =>
        apiClient.SetUserAccess(userId, clientId, accessValue, ct);

    public Task RemoveUserAccessAsync(string userId, string clientId, CancellationToken ct = default) =>
        apiClient.RemoveUserAccess(userId, clientId, ct);
}

