using FluentValidation.TestHelper;
using Vitarish.Users.Application.Queries;

namespace Vitarish.Users.Tests.Application;

public class CheckUserPhoneNumberInUseQueryValidatorTests
{
    private readonly CheckUserPhoneNumberInUseQueryValidator validator = new();

    [Fact]
    public void Validate_WithValidPhone_Passes()
    {
        CheckUserPhoneNumberInUseQuery query = new("123456", null);

        TestValidationResult<CheckUserPhoneNumberInUseQuery> result = validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyPhone_Fails(string? phone)
    {
        CheckUserPhoneNumberInUseQuery query = new(phone!, null);

        TestValidationResult<CheckUserPhoneNumberInUseQuery> result = validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.PhoneNumber);
    }
}
