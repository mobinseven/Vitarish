using FluentValidation.TestHelper;
using Vitarish.Users.Application.Queries;

namespace Users.Tests.Application;

public class GetUserProfileQueryValidatorTests
{
    private readonly GetUserProfileQueryValidator validator = new();

    [Fact]
    public void Validate_WithValidUserId_Passes()
    {
        GetUserProfileQuery query = new("user-1");

        TestValidationResult<GetUserProfileQuery> result = validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyUserId_Fails(string? userId)
    {
        GetUserProfileQuery query = new(userId!);

        TestValidationResult<GetUserProfileQuery> result = validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.UserId);
    }
}
