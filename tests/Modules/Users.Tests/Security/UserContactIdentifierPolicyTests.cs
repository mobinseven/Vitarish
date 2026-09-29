using FluentAssertions;
using Vitarish.Users.Application.Security;

namespace Vitarish.Users.Tests.Security;

public sealed class UserContactIdentifierPolicyTests
{
    [Theory]
    [InlineData("+12", true)]
    [InlineData("+14155552671", true)]
    [InlineData("+989121234567", true)]
    [InlineData("+123456789012345", true)]
    [InlineData("+1", false)]
    [InlineData("+01234567", false)]
    [InlineData("09121234567", false)]
    [InlineData("+1 4155552671", false)]
    [InlineData("+1234567890123456", false)]
    [InlineData("+۱۲۳۴۵۶۷۸۹", false)]
    public void Phone_identifier_must_use_e164_format(string phoneNumber, bool expected)
    {
        UserContactIdentifierPolicy.IsValidE164PhoneNumber(phoneNumber).Should().Be(expected);
        UserContactIdentifierPolicy.IsValid(null, phoneNumber, null, null).Should().Be(expected);
    }

    [Fact]
    public void Email_remains_a_valid_identifier_without_a_phone_number()
    {
        UserContactIdentifierPolicy.IsValid("user@example.com", null, null, null).Should().BeTrue();
    }

    [Fact]
    public void At_least_one_contact_identifier_is_required()
    {
        UserContactIdentifierPolicy.IsValid(null, null, "User", "Name").Should().BeFalse();
    }
}
