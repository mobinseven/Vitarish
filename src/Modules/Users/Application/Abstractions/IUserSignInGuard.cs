namespace Vitarish.Users.Application.Abstractions;

/// <summary>Holds a live account against irreversible deletion while its session is established.</summary>
public interface IUserSignInGuard
{
    Task<IUserSignInLease> AcquireAsync(string userId, CancellationToken cancellationToken);
}

public interface IUserSignInLease : IAsyncDisposable
{
    bool AccountAvailable { get; }
}
