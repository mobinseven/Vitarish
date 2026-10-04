using System.ComponentModel.DataAnnotations;
using Vitarish.Users.Domain.Security;

namespace Vitarish.Users.Application.Security;

public static class UserContactIdentifierPolicy
{
    public const int MaximumContactLength = 256;
    public const int MaximumNameLength = 64;
    public static bool IsValid(string? email, string? phoneNumber, string? firstName, string? lastName) =>
        IsValidEmail(email) &&
        IsValidPhoneNumber(phoneNumber) &&
        IsValidName(firstName) &&
        IsValidName(lastName) &&
        HasContactIdentifier(email, phoneNumber);

    public static bool IsValidEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ||
        (email.Length <= MaximumContactLength && new EmailAddressAttribute().IsValid(email));

    public static bool IsValidPhoneNumber(string? phoneNumber) =>
        string.IsNullOrWhiteSpace(phoneNumber) || UserPhoneNumberPolicy.IsValidE164(phoneNumber);

    public static bool IsValidName(string? name) => (name?.Length ?? 0) <= MaximumNameLength;

    public static bool HasContactIdentifier(string? email, string? phoneNumber) =>
        !string.IsNullOrWhiteSpace(email) || !string.IsNullOrWhiteSpace(phoneNumber);

    public static bool IsValidE164PhoneNumber(string? phoneNumber) =>
        UserPhoneNumberPolicy.IsValidE164(phoneNumber);
}
