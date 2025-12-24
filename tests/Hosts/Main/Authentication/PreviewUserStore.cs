using Microsoft.AspNetCore.Identity;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Main.Mock.Authentication;

public sealed class PreviewUserStore : IUserStore<User>, IUserSecurityStampStore<User>
{
    private static readonly User PreviewUser = new()
    {
        Id = "preview-user",
        UserName = "preview-user",
        NormalizedUserName = "PREVIEW-USER",
        SecurityStamp = "preview-stamp"
    };

    public Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(IdentityResult.Success);

    public Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
        => Task.FromResult(IdentityResult.Success);

    public void Dispose()
    {
    }

    public Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        => Task.FromResult(userId == PreviewUser.Id ? PreviewUser : null);

    public Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        => Task.FromResult(normalizedUserName == PreviewUser.NormalizedUserName ? PreviewUser : null);

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
        => Task.FromResult(IdentityResult.Success);
}
