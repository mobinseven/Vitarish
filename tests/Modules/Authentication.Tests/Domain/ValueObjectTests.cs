using FluentAssertions;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Users;

namespace Authentication.Tests.Domain;

public class ValueObjectTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void UserId_Create_throws_for_empty_input(string? value)
    {
        var act = () => AuthenticationUserId.Create(value!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UserId_Create_returns_struct_with_value()
    {
        var userId = AuthenticationUserId.Create("user-123");

        userId.Value.Should().Be("user-123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ChallengeId_Create_throws_for_empty_input(string? value)
    {
        var act = () => AuthenticationChallengeId.Create(value!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ChallengeId_Create_returns_struct_with_value()
    {
        var id = AuthenticationChallengeId.Create("challenge-abc");

        id.Value.Should().Be("challenge-abc");
    }
}
