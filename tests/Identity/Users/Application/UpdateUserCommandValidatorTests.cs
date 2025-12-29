using FluentValidation.TestHelper;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Tests.Application;

public class UpdateUserCommandValidatorTests
{
    private readonly UpdateUserCommandValidator validator = new();

    [Fact]
    public void Validate_WithValidUser_Passes()
    {
        User user = new() { Id = "user-1" };
        UpdateUserCommand command = new(user);

        TestValidationResult<UpdateUserCommand> result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNullUser_Fails()
    {
        UpdateUserCommand command = new(null!);

        Action act = () => validator.Validate(command);

        act.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void Validate_WithMissingUserId_Fails()
    {
        User user = new() { Id = null };
        UpdateUserCommand command = new(user);

        TestValidationResult<UpdateUserCommand> result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.User.Id);
    }
}
