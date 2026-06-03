using FluentValidation.TestHelper;
using Vitarish.Users.Application.Commands;

namespace Users.Tests.Application;

public class UpdateUserProfileCommandValidatorTests
{
    private readonly UpdateUserProfileCommandValidator validator = new();

    [Fact]
    public void Validate_WithValidInput_Passes()
    {
        UpdateUserProfileCommand command = new("user-1", []);

        TestValidationResult<UpdateUserProfileCommand> result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyUserId_Fails(string? userId)
    {
        UpdateUserProfileCommand command = new(userId!, []);

        TestValidationResult<UpdateUserProfileCommand> result = validator.TestValidate(command);

        _ = result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public void Validate_WithNullPreferences_Fails()
    {
        UpdateUserProfileCommand command = new("user-2", null!);

        TestValidationResult<UpdateUserProfileCommand> result = validator.TestValidate(command);

        _ = result.ShouldHaveValidationErrorFor(c => c.Preferences);
    }
}
