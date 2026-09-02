namespace Vitarish.Users.Contracts.Responses;

public static class UsersCommandErrorCodes
{
    public const string ActiveSessionConflict = "ActiveSessionConflict";
    public const string ActiveSessionConflictDescription = "The mutation cannot be applied while the user has an active session.";
}
