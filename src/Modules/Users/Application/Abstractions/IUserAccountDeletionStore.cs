namespace Vitarish.Users.Application.Abstractions;

public sealed record UserAccountDeletionSnapshot(
    string Id, string? Revision, bool Enabled, bool Deleted,
    bool PlatformAdministrator, int OtherPlatformAdministrators, bool LastTenantOwner);

/// <summary>One transaction holding the account against sign-in and competing writes.</summary>
public interface IUserAccountDeletionStore
{
    Task<IUserAccountDeletionScope> BeginAsync(string userId, CancellationToken cancellationToken);
}

public interface IUserAccountDeletionScope : IAsyncDisposable
{
    UserAccountDeletionSnapshot? Account { get; }
    Task EraseAsync(DateTime utcNow, string securityStamp, string revision, CancellationToken cancellationToken);
}
