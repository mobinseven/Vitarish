using FluentValidation.TestHelper;
using Vitarish.Users.Application.Queries;

namespace Users.Tests.Application;

public class CheckUserPasswordQueryValidatorTests
{
    private readonly CheckUserPasswordQueryValidator validator = new();

    [Fact]
    public void Validate_WithValidInput_Passes()
    {
        CheckUserPasswordQuery query = new("user-1", "password");

        TestValidationResult<CheckUserPasswordQuery> result = validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyId_Fails(string? id)
    {
        CheckUserPasswordQuery query = new(id!, "password");

        TestValidationResult<CheckUserPasswordQuery> result = validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyPassword_Fails(string? password)
    {
        CheckUserPasswordQuery query = new("user-1", password!);

        TestValidationResult<CheckUserPasswordQuery> result = validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.Password);
    }
}
