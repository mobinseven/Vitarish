using System.Collections.Concurrent;
using Microsoft.AspNetCore.Identity;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Main.Mock.Authentication;

public sealed class PreviewUserRoleStore : IUserStore<User>, IUserSecurityStampStore<User>, IUserRoleStore<User>
{
    private static readonly User PreviewUser = new()
    {
        Id = "preview-user",
        UserName = "preview-user",
        NormalizedUserName = "PREVIEW-USER",
        SecurityStamp = "preview-stamp"
    };

    private readonly ConcurrentDictionary<string, User> _users = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _userRoles = new();

    public PreviewUserRoleStore()
    {
        _ = _users.TryAdd(PreviewUser.Id, PreviewUser);
    }

    public Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
    {
        if (_users.TryAdd(user.Id, user))
        {
            return Task.FromResult(IdentityResult.Success);
        }

        return Task.FromResult(IdentityResult.Failed(new IdentityError
        {
            Code = "DuplicateUserId",
            Description = $"User with ID '{user.Id}' already exists."
        }));
    }

    public Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
    {
        _ = _userRoles.TryRemove(user.Id, out _);
        if (_users.TryRemove(user.Id, out _))
        {
            return Task.FromResult(IdentityResult.Success);
        }

        return Task.FromResult(IdentityResult.Failed(new IdentityError
        {
            Code = "UserNotFound",
            Description = $"User with ID '{user.Id}' not found."
        }));
    }

    public void Dispose()
    {
    }

    public Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        => Task.FromResult(_users.TryGetValue(userId, out User? user) ? user : null);

    public Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        User? user = _users.Values.FirstOrDefault(u => u.NormalizedUserName == normalizedUserName);
        return Task.FromResult(user);
    }

    public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.NormalizedUserName ?? user.UserName?.ToUpperInvariant());

    public Task<string?> GetSecurityStampAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.SecurityStamp);

    public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.Id ?? string.Empty);

    public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(user.UserName);

    public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetSecurityStampAsync(User user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    public Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
    {
        if (!_users.TryGetValue(user.Id, out User? existingUser))
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "UserNotFound",
                Description = "User not found."
            }));
        }

        if (_users.TryUpdate(user.Id, user, existingUser))
        {
            return Task.FromResult(IdentityResult.Success);
        }

        return Task.FromResult(IdentityResult.Failed(new IdentityError
        {
            Code = "ConcurrencyFailure",
            Description = "Optimistic concurrency failure, object has been modified."
        }));
    }

    public Task AddToRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        ConcurrentDictionary<string, byte> roles = GetOrCreateRoles(user.Id);
        roles.TryAdd(roleName, 0);
        return Task.CompletedTask;
    }

    public Task RemoveFromRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        if (_userRoles.TryGetValue(user.Id, out ConcurrentDictionary<string, byte>? roles))
        {
            _ = roles.TryRemove(roleName, out _);
        }

        return Task.CompletedTask;
    }

    public Task<IList<string>> GetRolesAsync(User user, CancellationToken cancellationToken)
    {
        if (!_userRoles.TryGetValue(user.Id, out ConcurrentDictionary<string, byte>? roles))
        {
            return Task.FromResult<IList<string>>([]);
        }

        return Task.FromResult<IList<string>>([.. roles.Keys]);
    }

    public Task<bool> IsInRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        bool isInRole = _userRoles.TryGetValue(user.Id, out ConcurrentDictionary<string, byte>? roles)
            && roles.ContainsKey(roleName);
        return Task.FromResult(isInRole);
    }

    public Task<IList<User>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        List<User> usersInRole = _userRoles
            .Where(kvp => kvp.Value.ContainsKey(roleName))
            .Select(kvp => _users.GetValueOrDefault(kvp.Key))
            .OfType<User>()
            .ToList();

        return Task.FromResult<IList<User>>(usersInRole);
    }

    private ConcurrentDictionary<string, byte> GetOrCreateRoles(string userId)
    {
        return _userRoles.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase));
    }
}
