using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Users.Application.Authorization;

/// <summary>
/// Owns the account-resource rule that non-administrative account endpoints may
/// operate only on the authenticated subject.
/// </summary>
public sealed class UserSubjectAccessPolicy(IRequestContext requestContext)
{
    public bool AllowsSelf(string userId) =>
        !string.IsNullOrWhiteSpace(userId) &&
        string.Equals(requestContext.UserId, userId, StringComparison.Ordinal);
}
