using Microsoft.AspNetCore.Identity;
using System.Collections.Concurrent;
using System.Security.Claims;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Main.Mock.Services;

public sealed class MockUsersService : IUsersService
{
    private const string DefaultPassword = "preview-password";
    private readonly ConcurrentDictionary<string, User> users = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, List<Claim>> userClaims = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> userPasswords = new(StringComparer.OrdinalIgnoreCase);

    public MockUsersService()
    {
        User previewUser = new()
        {
            Id = "preview-user",
            UserName = "preview-user",
            Email = "preview@example.com",
            PhoneNumber = "+15555550100",
            FirstName = "Preview",
            LastName = "User"
        };

        users[previewUser.Id] = previewUser;
        userPasswords[previewUser.Id] = DefaultPassword;
        userClaims[previewUser.Id] =
        [
            new Claim(ClaimTypes.NameIdentifier, previewUser.Id),
            new Claim(ClaimTypes.Name, previewUser.UserName ?? previewUser.Id)
        ];
    }

    public Task<IEnumerable<User>> GetUsersAsync(CancellationToken ct = default)
        => Task.FromResult<IEnumerable<User>>(users.Values.ToList());

    public Task<User?> GetUserAsync(string id, CancellationToken ct = default)
        => Task.FromResult(users.TryGetValue(id, out User? user) ? user : null);

    public Task<User?> GetUserByNameAsync(string userName, CancellationToken ct = default)
    {
        User? user = users.Values.FirstOrDefault(entry =>
            string.Equals(entry.UserName, userName, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task<User?> CreateUserWithPhoneAsync(CreateUserWithPhoneCommand command, CancellationToken ct = default)
    {
        User user = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            UserName = command.PhoneNumber,
            PhoneNumber = command.PhoneNumber,
            FirstName = command.FirstName,
            LastName = command.LastName
        };

        users[user.Id] = user;
        userClaims[user.Id] =
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? user.Id)
        ];

        return Task.FromResult<User?>(user);
    }

    public Task<User?> CreateUserWithEmailAsync(CreateUserWithEmailCommand command, CancellationToken ct = default)
    {
        User user = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            UserName = command.Email,
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName
        };

        users[user.Id] = user;
        userClaims[user.Id] =
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? user.Id)
        ];

        return Task.FromResult<User?>(user);
    }

    public Task<IdentityResult> UpdateUserAsync(UpdateUserCommand command, CancellationToken ct = default)
    {
        while (true)
        {
            if (!users.TryGetValue(command.User.Id, out User? existingUser))
            {
                return Task.FromResult(UserNotFound());
            }

            if (users.TryUpdate(command.User.Id, command.User, existingUser))
            {
                return Task.FromResult(IdentityResult.Success);
            }
        }
    }

    public Task<IdentityResult> DeleteUserAsync(DeleteUserCommand command, CancellationToken ct = default)
    {
        if (!users.TryRemove(command.Id, out _))
        {
            return Task.FromResult(UserNotFound());
        }

        userClaims.TryRemove(command.Id, out _);
        userPasswords.TryRemove(command.Id, out _);
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IList<Claim>> GetUserClaimsAsync(string userId, CancellationToken ct = default)
    {
        if (!userClaims.TryGetValue(userId, out List<Claim>? claims))
        {
            return Task.FromResult<IList<Claim>>(new List<Claim>());
        }

        return Task.FromResult<IList<Claim>>(claims.ToList());
    }

    public Task<IdentityResult> UpdateUserClaimsAsync(UpdateUserClaimsCommand command, CancellationToken ct = default)
    {
        if (!users.ContainsKey(command.User.Id))
        {
            return Task.FromResult(UserNotFound());
        }

        userClaims[command.User.Id] = command.Claims.ToList();
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<bool> UserHasPasswordAsync(string userId, CancellationToken ct = default)
        => Task.FromResult(userPasswords.ContainsKey(userId));

    public Task<bool> CheckUserPasswordAsync(string userId, string password, CancellationToken ct = default)
        => Task.FromResult(userPasswords.TryGetValue(userId, out string? storedPassword) && storedPassword == password);

    public Task<bool> CheckUserEmailInUseAsync(string email, string? excludeUserId = null, CancellationToken ct = default)
    {
        bool inUse = users.Values.Any(user =>
            string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(user.Id, excludeUserId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(inUse);
    }

    public Task<bool> CheckUserPhoneNumberInUseAsync(string phoneNumber, string? excludeUserId = null, CancellationToken ct = default)
    {
        bool inUse = users.Values.Any(user =>
            string.Equals(user.PhoneNumber, phoneNumber, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(user.Id, excludeUserId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(inUse);
    }

    public Task<IdentityResult> RemoveUserPasswordAsync(string userId, CancellationToken ct = default)
    {
        if (!users.ContainsKey(userId))
        {
            return Task.FromResult(UserNotFound());
        }

        userPasswords.TryRemove(userId, out _);
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> AddUserPasswordAsync(string userId, string password, CancellationToken ct = default)
    {
        if (!users.ContainsKey(userId))
        {
            return Task.FromResult(UserNotFound());
        }

        userPasswords[userId] = password;
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> SetUserPhoneNumberAsync(string userId, string phoneNumber, CancellationToken ct = default)
    {
        return UpdateUserAsync(userId, existingUser =>
        {
            User updatedUser = CloneUser(existingUser);
            updatedUser.PhoneNumber = phoneNumber;
            return updatedUser;
        });
    }

    public Task<IdentityResult> SetUserEmailAsync(string userId, string email, CancellationToken ct = default)
    {
        return UpdateUserAsync(userId, existingUser =>
        {
            User updatedUser = CloneUser(existingUser);
            updatedUser.Email = email;
            return updatedUser;
        });
    }

    private static IdentityResult UserNotFound()
        => IdentityResult.Failed(new IdentityError { Code = "UserNotFound", Description = "User not found." });

    private Task<IdentityResult> UpdateUserAsync(string userId, Func<User, User> update)
    {
        while (true)
        {
            if (!users.TryGetValue(userId, out User? existingUser))
            {
                return Task.FromResult(UserNotFound());
            }

            User updatedUser = update(existingUser);
            if (users.TryUpdate(userId, updatedUser, existingUser))
            {
                return Task.FromResult(IdentityResult.Success);
            }
        }
    }

    private static User CloneUser(User user)
        => new()
        {
            AccessFailedCount = user.AccessFailedCount,
            ConcurrencyStamp = user.ConcurrencyStamp,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            FirstName = user.FirstName,
            Id = user.Id,
            LastName = user.LastName,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd,
            NormalizedEmail = user.NormalizedEmail,
            NormalizedUserName = user.NormalizedUserName,
            PasswordHash = user.PasswordHash,
            PhoneNumber = user.PhoneNumber,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            ProfileJson = user.ProfileJson,
            SecurityStamp = user.SecurityStamp,
            TwoFactorEnabled = user.TwoFactorEnabled,
            UserName = user.UserName
        };
}
