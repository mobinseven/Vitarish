using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Application.Abstractions;

public interface IAuthenticationSecurityPolicyStore
{
    Task<SecurityAccessPolicyState> GetStateAsync(CancellationToken cancellationToken);
    Task<SecurityAccessPolicyState> SaveAsync(SecurityAccessPolicy policy, long expectedVersion, CancellationToken cancellationToken);
    Task InitializeAsync(CancellationToken cancellationToken);
}

public interface IAuthenticationSecurityPolicyProvider
{
    Task<SecurityAccessPolicy> GetAsync(CancellationToken cancellationToken);
}

public interface IAuthenticationAttemptTracker
{
    Task<bool> IsDeniedAsync(string identifier, CancellationToken cancellationToken);
    Task<int> RegisterFailureAsync(string identifier, int threshold, TimeSpan denialDuration, CancellationToken cancellationToken);
    Task ResetAsync(string identifier, CancellationToken cancellationToken);
}
