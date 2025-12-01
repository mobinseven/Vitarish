namespace Vitarish.Web.Users.Infrastructure.Services;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vorn.Aaas.Users.Presentation.Abstractions;

public interface IPersonalAccessTokenService
{
    Task<string> CreateTokenAsync(
        string subjectId,
        int? lifetimeDays = null,
        IEnumerable<string>? scopes = null,
        bool isReferenceToken = false,
        CancellationToken cancellationToken = default);
}

internal sealed class PersonalAccessTokenService(IPersonalAccessTokenGenerator generator)
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
