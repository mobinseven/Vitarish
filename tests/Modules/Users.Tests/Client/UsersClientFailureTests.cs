using FluentAssertions;
using Vitarish.Users.Client;

namespace Users.Tests.Client;

public class UsersClientFailureTests
{
    [Fact]
    public void UsersClientFailureKind_has_all_expected_values()
    {
        var values = Enum.GetValues<UsersClientFailureKind>();

        values.Should().Contain(UsersClientFailureKind.Validation);
        values.Should().Contain(UsersClientFailureKind.Unauthorized);
        values.Should().Contain(UsersClientFailureKind.Forbidden);
        values.Should().Contain(UsersClientFailureKind.NotFound);
        values.Should().Contain(UsersClientFailureKind.Conflict);
        values.Should().Contain(UsersClientFailureKind.Timeout);
        values.Should().Contain(UsersClientFailureKind.Canceled);
        values.Should().Contain(UsersClientFailureKind.Transport);
        values.Should().Contain(UsersClientFailureKind.Deserialization);
        values.Should().Contain(UsersClientFailureKind.UnexpectedServerFailure);
    }

    [Fact]
    public void UsersClientFailure_preserves_status_code()
    {
        var failure = new UsersClientFailure(UsersClientFailureKind.Validation, "Bad request", 400);

        failure.StatusCode.Should().Be(400);
        failure.Kind.Should().Be(UsersClientFailureKind.Validation);
        failure.Message.Should().Be("Bad request");
    }

    [Fact]
    public void UsersClientResult_success_has_no_failure()
    {
        var result = UsersClientResult<string>.Success("ok");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("ok");
        result.Failure.Should().BeNull();
    }

    [Fact]
    public void UsersClientResult_failed_has_failure()
    {
        var failure = new UsersClientFailure(UsersClientFailureKind.NotFound, "Not found");
        var result = UsersClientResult<string>.Failed(failure);

        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Failure.Should().NotBeNull();
        result.Failure!.Kind.Should().Be(UsersClientFailureKind.NotFound);
    }
}
