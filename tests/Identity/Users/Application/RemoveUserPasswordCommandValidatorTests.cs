using FluentValidation.TestHelper;
using Vitarish.Users.Application.Commands;

namespace Vitarish.Users.Tests.Application;

public class RemoveUserPasswordCommandValidatorTests
{
    private readonly RemoveUserPasswordCommandValidator validator = new();

    [Fact]
    public void Validate_WithValidId_Passes()
    {
        RemoveUserPasswordCommand command = new("user-1");

        TestValidationResult<RemoveUserPasswordCommand> result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyId_Fails(string? id)
    {
        RemoveUserPasswordCommand command = new(id!);

        TestValidationResult<RemoveUserPasswordCommand> result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Id);
    }
}
