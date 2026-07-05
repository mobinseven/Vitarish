using System.Security.Claims;

namespace Vitarish.Modules.Base.Core.Identity;

public static class AuthenticatedUserIdResolver
{
    public const string SubjectClaimType = "sub";

    public static AuthenticatedUserIdResolution Resolve(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        string? subjectId = principal.FindFirstValue(SubjectClaimType) ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(subjectId)
            ? new AuthenticatedUserIdResolution.MissingSubjectId()
            : new AuthenticatedUserIdResolution.Success(new AuthenticatedUserId(subjectId));
    }
}
