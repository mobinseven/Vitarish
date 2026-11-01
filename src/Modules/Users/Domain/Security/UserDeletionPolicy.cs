namespace Vitarish.Users.Domain.Security;

public enum UserDeletionRefusal
{
    None,
    SelfDeletion,
    LastPlatformAdministrator,
    LastOwnerProtection,
    ConcurrencyFailure
}

/// <summary>Deletion eligibility; persistence supplies facts, never the decision.</summary>
public static class UserDeletionPolicy
{
    public static UserDeletionRefusal Evaluate(
        string subjectId, string? actorId, string? expectedRevision, string? currentRevision,
        bool isPlatformAdministrator, int otherPlatformAdministrators, bool isLastTenantOwner)
    {
        if (string.Equals(subjectId, actorId, StringComparison.Ordinal))
            return UserDeletionRefusal.SelfDeletion;
        if (isPlatformAdministrator && otherPlatformAdministrators == 0)
            return UserDeletionRefusal.LastPlatformAdministrator;
        if (isLastTenantOwner)
            return UserDeletionRefusal.LastOwnerProtection;
        if (string.IsNullOrWhiteSpace(expectedRevision) || !string.Equals(expectedRevision, currentRevision, StringComparison.Ordinal))
            return UserDeletionRefusal.ConcurrencyFailure;
        return UserDeletionRefusal.None;
    }
}
