using FluentValidation.TestHelper;
using Vitarish.Users.Application.Queries;

namespace Vitarish.Users.Tests.Application;

public class CheckUserEmailInUseQueryValidatorTests
{
    private readonly CheckUserEmailInUseQueryValidator validator = new();

    [Fact]
    public void Validate_WithValidEmail_Passes()
    {
        CheckUserEmailInUseQuery query = new("user@example.com", null);

        TestValidationResult<CheckUserEmailInUseQuery> result = validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid")]
    public void Validate_WithInvalidEmail_Fails(string? email)
    {
        CheckUserEmailInUseQuery query = new(email!, null);

        TestValidationResult<CheckUserEmailInUseQuery> result = validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.Email);
    }
}
