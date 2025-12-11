using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Accounts.Infrastructure.Services;

public interface IPersonalAccessTokenService
{
    Task<string> CreateTokenAsync(
        string subjectId,
        int? lifetimeDays = null,
        IEnumerable<string>? scopes = null,
        bool isReferenceToken = false,
        CancellationToken cancellationToken = default);
}

public sealed class PersonalAccessTokenService(IPersonalAccessTokenGenerator generator)
    : IPersonalAccessTokenService
{
    public Task<string> CreateTokenAsync(
        string subjectId,
        int? lifetimeDays,
        IEnumerable<string>? scopes,
        bool isReferenceToken,
        CancellationToken cancellationToken) =>
        generator.CreateTokenAsync(subjectId, lifetimeDays, scopes, isReferenceToken, cancellationToken);
}
