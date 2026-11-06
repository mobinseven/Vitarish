using FluentAssertions;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Xunit;

namespace Vitarish.Authentication.Tests.Handlers;

public sealed class PasswordLoginSecondFactorChannelTests
{
    [Fact]
    public void Confirmed_phone_is_preferred_when_both_contacts_are_confirmed()
    {
        AuthenticationUserDescriptor user = User(emailConfirmed: true, phoneConfirmed: true);

        PasswordLoginSecondFactorChannel? channel = PasswordLoginSecondFactorChannel.Select(user);

        channel.Should().Be(new PasswordLoginSecondFactorChannel(
            AuthenticationMethod.Phone,
            PreAuthenticationAction.PasswordPlusPhoneOtp,
            AuthenticationChallengePurpose.PasswordLoginOtp,
            "+989121234567"));
    }

    [Fact]
    public void Confirmed_email_is_used_when_phone_is_unconfirmed()
    {
        AuthenticationUserDescriptor user = User(emailConfirmed: true, phoneConfirmed: false);

        PasswordLoginSecondFactorChannel? channel = PasswordLoginSecondFactorChannel.Select(user);

        channel.Should().Be(new PasswordLoginSecondFactorChannel(
            AuthenticationMethod.Email,
            PreAuthenticationAction.PasswordPlusEmailOtp,
            AuthenticationChallengePurpose.PasswordLoginEmailOtp,
            "person@example.test"));
    }

    [Fact]
    public void No_confirmed_contact_leaves_second_factor_unconfigured()
    {
        AuthenticationUserDescriptor user = User(emailConfirmed: false, phoneConfirmed: false);

        PasswordLoginSecondFactorChannel.Select(user).Should().BeNull();
    }

    [Fact]
    public void Email_channel_uses_email_method_for_failed_attempt_keys()
    {
        AuthenticationAttemptKeyMethods.MethodFor(AuthenticationChallengePurpose.PasswordLoginEmailOtp)
            .Should().Be(AuthenticationMethod.Email);
    }

    private static AuthenticationUserDescriptor User(bool emailConfirmed, bool phoneConfirmed) => new(
        "user-42",
        "user-42",
        null,
        "Given",
        "Family",
        "person@example.test",
        "+989121234567",
        false,
        emailConfirmed,
        phoneConfirmed);
}
