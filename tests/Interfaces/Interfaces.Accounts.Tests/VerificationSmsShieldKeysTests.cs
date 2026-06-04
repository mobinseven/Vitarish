using FluentAssertions;
using Vitarish.Interfaces.Accounts.Services;

namespace Interfaces.Accounts.Tests;

public sealed class VerificationSmsShieldKeysTests
{
    [Fact]
    public void ForPhoneLogin_RemovesPhoneNumberFormatting_ForStableShieldKey()
    {
        string key = VerificationSmsShieldKeys.ForPhoneLogin(" +32 (400) 000-000 ");

        key.Should().Be("login:+32400000000");
    }

    [Fact]
    public void ForPhoneChange_RemovesPhoneNumberFormatting_ForStableShieldKey()
    {
        string key = VerificationSmsShieldKeys.ForPhoneChange(" user-1 ", " +32 (400) 000-000 ");

        key.Should().Be("phone-change:user-1:+32400000000");
    }
}
