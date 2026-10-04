namespace Vitarish.Users.Infrastructure.Data;

/// <summary>
/// Database index names that enforce one sign-in namespace across email,
/// phone number, and user name. The identity configuration owns
/// <see cref="UserNameIndex"/>; these constants are the single source used by
/// the model configuration and by the unique-violation mapping.
/// </summary>
internal static class UsersContactIdentifierIndexNames
{
    /// <summary>ASP.NET Identity's framework index on NormalizedUserName.</summary>
    internal const string UserNameIndex = "UserNameIndex";

    internal const string EmailIndex = "EmailIndex";
    internal const string PhoneNumberIndex = "PhoneNumberIndex";
}
