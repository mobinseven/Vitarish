using Microsoft.AspNetCore.Identity;

namespace Vitarish.Users.Infrastructure.Extensions;

/// <summary>
/// Carries the <see cref="IdentityResult"/> that prevented account creation so
/// callers can distinguish identifier conflicts from other failures. The
/// message contains error codes only: identity descriptions can embed the
/// attempted user name, which may be a phone number or email address.
/// </summary>
internal sealed class CreateUserFailureException(IdentityResult result)
    : Exception(string.Join('\n', result.Errors.Select(error => error.Code)))
{
    internal IdentityResult Result { get; } = result;
}
