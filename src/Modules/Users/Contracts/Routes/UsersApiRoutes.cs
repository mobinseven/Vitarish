namespace Vitarish.Users.Contracts.Routes;

public static class UsersApiRoutes
{
    public const string Base = "/api/modules/users";
    public const string Search = "/search";
    public const string ById = "/{userId}";
    public const string CheckEmail = "/email-in-use";
    public const string CheckPhone = "/phone-in-use";
    public const string CheckRole = "/role-membership";
    public const string CreateWithPhone = "/create/phone";
    public const string CreateWithEmail = "/create/email";
    public const string SetEmail = "/{userId}/email";
    public const string AddRole = "/{userId}/roles";
}
