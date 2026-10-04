using FluentAssertions;
using Vitarish.Users.Domain.Security;

namespace Vitarish.Users.Tests.Security;

public sealed class UserPhoneNumberPolicyTests
{
    public static TheoryData<string, string?> NormalizationCases
    {
        get
        {
            TheoryData<string, string?> data = new();
            foreach ((string input, string? expected) in PhoneNormalizationCases.All)
                data.Add(input, expected);
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(NormalizationCases))]
    public void TryNormalize_matches_the_shared_case_table(string input, string? expected)
    {
        bool normalized = UserPhoneNumberPolicy.TryNormalize(input, out string? e164);

        normalized.Should().Be(expected is not null);
        e164.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(NormalizationCases))]
    public void TryNormalize_is_idempotent_for_converted_numbers(string input, string? expected)
    {
        if (expected is null)
            return;

        bool normalized = UserPhoneNumberPolicy.TryNormalize(input, out string? e164);
        bool normalizedAgain = UserPhoneNumberPolicy.TryNormalize(e164, out string? again);

        normalized.Should().BeTrue();
        e164.Should().Be(expected);
        normalizedAgain.Should().BeTrue();
        again.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryNormalize_rejects_missing_input_without_a_value(string? input)
    {
        bool normalized = UserPhoneNumberPolicy.TryNormalize(input, out string? e164);

        normalized.Should().BeFalse();
        e164.Should().BeNull();
    }

    [Fact]
    public void TryNormalize_rejects_input_that_cannot_convert_without_a_value()
    {
        bool normalized = UserPhoneNumberPolicy.TryNormalize("abc", out string? e164);

        normalized.Should().BeFalse();
        e164.Should().BeNull();
    }
}
