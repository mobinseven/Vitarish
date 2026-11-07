using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Application.Validation;

namespace Vitarish.Authentication.Application.Security;

public sealed record PasswordLoginSecondFactorChannel(
    AuthenticationMethod Method,
    PreAuthenticationAction PolicyAction,
    AuthenticationChallengePurpose ChallengePurpose,
    string? Destination)
{
    public static PasswordLoginSecondFactorChannel? Select(AuthenticationUserDescriptor user)
    {
        if (user.PhoneNumberConfirmed && AuthenticationInputValidator.HasText(user.PhoneNumber))
        {
            PasswordLoginSecondFactorChannel phone = ForPurpose(AuthenticationChallengePurpose.PasswordLoginOtp)!;
            return phone with { Destination = user.PhoneNumber };
        }

        if (user.EmailConfirmed && AuthenticationInputValidator.HasText(user.Email))
        {
            PasswordLoginSecondFactorChannel email = ForPurpose(AuthenticationChallengePurpose.PasswordLoginEmailOtp)!;
            return email with { Destination = user.Email };
        }

        return null;
    }

    public static PasswordLoginSecondFactorChannel? ForPurpose(AuthenticationChallengePurpose purpose) => purpose switch
    {
        AuthenticationChallengePurpose.PasswordLoginOtp => new(
            AuthenticationMethod.Phone,
            PreAuthenticationAction.PasswordPlusPhoneOtp,
            purpose,
            Destination: null),
        AuthenticationChallengePurpose.PasswordLoginEmailOtp => new(
            AuthenticationMethod.Email,
            PreAuthenticationAction.PasswordPlusEmailOtp,
            purpose,
            Destination: null),
        _ => null
    };

    public static PasswordLoginSecondFactorChannel? FromChallenge(AuthenticationChallenge challenge) =>
        ForPurpose(challenge.Purpose) is { } channel
            ? channel with { Destination = challenge.Destination }
            : null;

    public bool DestinationIsCurrentAndConfirmed(AuthenticationUserDescriptor? user)
    {
        if (user is null || string.IsNullOrWhiteSpace(Destination))
            return false;

        string destination = Destination.Trim();
        return Method switch
        {
            AuthenticationMethod.Phone => user.PhoneNumberConfirmed && Matches(user.PhoneNumber),
            AuthenticationMethod.Email => user.EmailConfirmed && Matches(user.Email),
            _ => false
        };

        bool Matches(string? current) =>
            !string.IsNullOrWhiteSpace(current) &&
            string.Equals(current.Trim(), destination, StringComparison.OrdinalIgnoreCase);
    }
}
