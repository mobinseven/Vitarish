using FluentValidation.TestHelper;
using Vitarish.Users.Application.Commands;

namespace Users.Tests.Application;

public class AddUserPasswordCommandValidatorTests
{
    private readonly AddUserPasswordCommandValidator validator = new();

    [Fact]
    public void Validate_WithValidInput_Passes()
    {
        AddUserPasswordCommand command = new("user-1", "password");

        TestValidationResult<AddUserPasswordCommand> result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyId_Fails(string? id)
    {
        AddUserPasswordCommand command = new(id!, "password");

        TestValidationResult<AddUserPasswordCommand> result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyPassword_Fails(string? password)
    {
        AddUserPasswordCommand command = new("user-1", password!);

        TestValidationResult<AddUserPasswordCommand> result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Password);
    }
}
