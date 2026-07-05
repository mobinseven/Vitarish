namespace Vitarish.Modules.Base.Core.Identity;

public abstract record AuthenticatedUserIdResolution
{
    private AuthenticatedUserIdResolution() { }

    public sealed record Success(AuthenticatedUserId UserId) : AuthenticatedUserIdResolution;

    public sealed record MissingSubjectId : AuthenticatedUserIdResolution;
}
