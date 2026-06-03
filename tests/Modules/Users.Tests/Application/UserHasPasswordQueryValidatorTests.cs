using FluentValidation.TestHelper;
using Vitarish.Users.Application.Queries;

namespace Users.Tests.Application;

public class UserHasPasswordQueryValidatorTests
{
    private readonly UserHasPasswordQueryValidator validator = new();

    [Fact]
    public void Validate_WithValidId_Passes()
    {
        UserHasPasswordQuery query = new("user-1");

        TestValidationResult<UserHasPasswordQuery> result = validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyId_Fails(string? id)
    {
        UserHasPasswordQuery query = new(id!);

        TestValidationResult<UserHasPasswordQuery> result = validator.TestValidate(query);

        _ = result.ShouldHaveValidationErrorFor(q => q.Id);
    }
}
