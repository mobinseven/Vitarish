using FluentValidation.TestHelper;
using Vitarish.Users.Application.Queries;

namespace Users.Tests.Application;

public class GetUserQueryValidatorTests
{
    private readonly GetUserQueryValidator validator = new();

    [Fact]
    public void Validate_WithValidId_Passes()
    {
        GetUserQuery query = new("user-1");

        TestValidationResult<GetUserQuery> result = validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyId_Fails(string? id)
    {
        GetUserQuery query = new(id!);

        TestValidationResult<GetUserQuery> result = validator.TestValidate(query);

        _ = result.ShouldHaveValidationErrorFor(q => q.Id);
    }
}
