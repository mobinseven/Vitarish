using FluentValidation.TestHelper;
using Vitarish.Users.Application.Queries;

namespace Users.Tests.Application;

public class GetUserByNameQueryValidatorTests
{
    private readonly GetUserByNameQueryValidator validator = new();

    [Fact]
    public void Validate_WithValidName_Passes()
    {
        GetUserByNameQuery query = new("user1");

        TestValidationResult<GetUserByNameQuery> result = validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyName_Fails(string? name)
    {
        GetUserByNameQuery query = new(name!);

        TestValidationResult<GetUserByNameQuery> result = validator.TestValidate(query);

        _ = result.ShouldHaveValidationErrorFor(q => q.UserName);
    }
}
