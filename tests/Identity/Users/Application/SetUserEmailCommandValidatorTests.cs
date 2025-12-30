using FluentValidation.TestHelper;
using Vitarish.Users.Application.Commands;

namespace Vitarish.Users.Tests.Application;

public class SetUserEmailCommandValidatorTests
{
    private readonly SetUserEmailCommandValidator validator = new();

    [Fact]
    public void Validate_WithValidInput_Passes()
    {
        SetUserEmailCommand command = new("user-1", "user@example.com");

        TestValidationResult<SetUserEmailCommand> result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyId_Fails(string? id)
    {
        SetUserEmailCommand command = new(id!, "user@example.com");

        TestValidationResult<SetUserEmailCommand> result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid")]
    public void Validate_WithInvalidEmail_Fails(string? email)
    {
        SetUserEmailCommand command = new("user-1", email!);

        TestValidationResult<SetUserEmailCommand> result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }
}
