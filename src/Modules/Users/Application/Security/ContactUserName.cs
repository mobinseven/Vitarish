using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Security;

public static class ContactUserName
{
    /// <summary>
    /// Moves <see cref="User.UserName"/> with the contact identifier it tracks.
    /// Returns the replacement user name when a move happened, otherwise null.
    /// </summary>
    public static string? MoveWithContact(User user, string? previousEmail, string? previousPhone, string? email, string? phoneNumber)
    {
        bool matchedEmail = !string.IsNullOrEmpty(previousEmail) &&
            string.Equals(user.UserName, previousEmail, StringComparison.OrdinalIgnoreCase);
        bool matchedPhone = !string.IsNullOrEmpty(previousPhone) &&
            string.Equals(user.UserName, previousPhone, StringComparison.Ordinal);
        if (!matchedEmail && !matchedPhone)
            return null;

        string? replacement = matchedEmail ? email ?? phoneNumber : phoneNumber ?? email;
        if (string.IsNullOrWhiteSpace(replacement))
            return null;

        user.UserName = replacement;
        return replacement;
    }
}
