namespace Vitarish.Users.Application.Abstractions;

public interface ITemporaryAutoAuthLinkCleanup
{
    Task<int> CleanupAsync(
        DateTime now,
        CancellationToken cancellationToken = default);
}
