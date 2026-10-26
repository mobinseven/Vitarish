using Moq;
using Vitarish.Audit.Application;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Security;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Tests.Security;

internal static class TestSourceAttemptGuards
{
    /// <summary>
    /// A guard for a request without a client address, which is never counted or denied, so tests of
    /// the per-identifier and per-account policies are not affected by the per-address policy.
    /// </summary>
    public static AuthenticationSourceAttemptGuard WithoutClientAddress() => new(
        Mock.Of<IAuthenticationAttemptTracker>(),
        Mock.Of<IAuthenticationAttemptKeyFactory>(),
        Mock.Of<IClientIpAddressAccessor>(),
        Mock.Of<ISecurityAuditWriter>(),
        Mock.Of<IRequestContext>());
}
