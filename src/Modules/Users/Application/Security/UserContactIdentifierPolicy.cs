using System.ComponentModel.DataAnnotations;
using Vitarish.Users.Domain.Security;

namespace Vitarish.Users.Application.Security;

public static class UserContactIdentifierPolicy
{
    public const int MaximumContactLength = 256;
    public const int MaximumNameLength = 64;
    public static bool IsValid(string? email, string? phoneNumber, string? firstName, string? lastName)
    {
        bool validEmail = string.IsNullOrWhiteSpace(email) ||
                          (email.Length <= MaximumContactLength && new EmailAddressAttribute().IsValid(email));
        bool validPhone = string.IsNullOrWhiteSpace(phoneNumber) || UserPhoneNumberPolicy.IsValidE164(phoneNumber);
        bool validName = (firstName?.Length ?? 0) <= MaximumNameLength &&
                         (lastName?.Length ?? 0) <= MaximumNameLength;
        return validEmail && validPhone && validName &&
               (!string.IsNullOrWhiteSpace(email) || !string.IsNullOrWhiteSpace(phoneNumber));
    }

    public static bool IsValidE164PhoneNumber(string? phoneNumber) =>
        UserPhoneNumberPolicy.IsValidE164(phoneNumber);
}
