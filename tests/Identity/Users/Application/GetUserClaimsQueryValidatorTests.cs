using FluentValidation.TestHelper;
using Vitarish.Users.Application.Queries;

namespace Vitarish.Users.Tests.Application;

public class GetUserClaimsQueryValidatorTests
{
    private readonly GetUserClaimsQueryValidator validator = new();

    [Fact]
    public void Validate_WithValidUserId_Passes()
    {
        GetUserClaimsQuery query = new("user-1");

        TestValidationResult<GetUserClaimsQuery> result = validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyUserId_Fails(string? userId)
    {
        GetUserClaimsQuery query = new(userId!);

        TestValidationResult<GetUserClaimsQuery> result = validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.UserId);
    }
}
