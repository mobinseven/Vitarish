using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Main.Mock.Services;

public sealed class MockPersonalAccessTokenGenerator : IPersonalAccessTokenGenerator
{
    public Task<string> CreateTokenAsync(
        string subjectId,
        int? lifetimeDays = null,
        IEnumerable<string>? scopes = null,
        bool isReferenceToken = false,
        CancellationToken cancellationToken = default)
        => Task.FromResult($"mock-token-{subjectId}-{Guid.NewGuid():N}");
}
