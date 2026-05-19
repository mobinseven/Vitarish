using FluentValidation.TestHelper;
using Vitarish.Users.Application.Queries;

namespace Users.Tests.Application;

public class GetUsersByIdentificationCodeQueryValidatorTests
{
    private readonly GetUsersByIdentificationCodeQueryValidator validator = new();

    [Fact]
    public void Validate_WithEmptyCode_Fails()
    {
        var result = validator.TestValidate(new GetUsersByIdentificationCodeQuery(""));
        result.ShouldHaveValidationErrorFor(x => x.IdentificationCode);
    }
}
