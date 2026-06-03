using FluentValidation.TestHelper;
using Vitarish.Users.Application.Commands;

namespace Users.Tests.Application;

public class CreateUserWithEmailCommandValidatorTests
{
    private readonly CreateUserWithEmailCommandValidator validator = new();

    [Fact]
    public void Validate_WithValidEmail_Passes()
    {
        CreateUserWithEmailCommand command = new("user@example.com", "First", "Last");

        TestValidationResult<CreateUserWithEmailCommand> result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_WithInvalidEmail_Fails(string email)
    {
        CreateUserWithEmailCommand command = new(email, "First", "Last");

        TestValidationResult<CreateUserWithEmailCommand> result = validator.TestValidate(command);

        _ = result.ShouldHaveValidationErrorFor(c => c.Email);
    }
}
