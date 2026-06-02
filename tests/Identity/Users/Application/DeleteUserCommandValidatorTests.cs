using FluentValidation.TestHelper;
using Vitarish.Users.Application.Commands;

namespace Users.Tests.Application;

public class DeleteUserCommandValidatorTests
{
    private readonly DeleteUserCommandValidator validator = new();

    [Fact]
    public void Validate_WithValidId_Passes()
    {
        DeleteUserCommand command = new("user-1");

        TestValidationResult<DeleteUserCommand> result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyId_Fails(string? id)
    {
        DeleteUserCommand command = new(id!);

        TestValidationResult<DeleteUserCommand> result = validator.TestValidate(command);

        _ = result.ShouldHaveValidationErrorFor(c => c.Id);
    }
}
