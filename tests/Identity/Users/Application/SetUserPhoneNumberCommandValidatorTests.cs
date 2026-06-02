using FluentValidation.TestHelper;
using Vitarish.Users.Application.Commands;

namespace Users.Tests.Application;

public class SetUserPhoneNumberCommandValidatorTests
{
    private readonly SetUserPhoneNumberCommandValidator validator = new();

    [Fact]
    public void Validate_WithValidInput_Passes()
    {
        SetUserPhoneNumberCommand command = new("user-1", "123456");

        TestValidationResult<SetUserPhoneNumberCommand> result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyId_Fails(string? id)
    {
        SetUserPhoneNumberCommand command = new(id!, "123456");

        TestValidationResult<SetUserPhoneNumberCommand> result = validator.TestValidate(command);

        _ = result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyPhone_Fails(string? phone)
    {
        SetUserPhoneNumberCommand command = new("user-1", phone!);

        TestValidationResult<SetUserPhoneNumberCommand> result = validator.TestValidate(command);

        _ = result.ShouldHaveValidationErrorFor(c => c.PhoneNumber);
    }
}
