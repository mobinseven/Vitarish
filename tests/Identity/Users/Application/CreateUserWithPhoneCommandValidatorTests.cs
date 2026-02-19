using FluentValidation.TestHelper;
using Vitarish.Users.Application.Commands;

namespace Users.Tests.Application;

public class CreateUserWithPhoneCommandValidatorTests
{
    private readonly CreateUserWithPhoneCommandValidator validator = new();

    [Fact]
    public void Validate_WithValidPhone_Passes()
    {
        CreateUserWithPhoneCommand command = new("123456789", "First", "Last");

        TestValidationResult<CreateUserWithPhoneCommand> result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyPhone_Fails(string? phone)
    {
        CreateUserWithPhoneCommand command = new(phone!, "First", "Last");

        TestValidationResult<CreateUserWithPhoneCommand> result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.PhoneNumber);
    }
}
