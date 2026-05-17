namespace Vitarish.Users.Application.Abstractions;

public interface ITemporaryAutoAuthLinkCleanup
{
    Task<int> CleanupAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}
