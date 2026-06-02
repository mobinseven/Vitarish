using FluentValidation.TestHelper;
using Vitarish.Users.Application.Queries;

namespace Users.Tests.Application;

public class GetUsersByIdentificationCodeQueryValidatorTests
{
    private readonly GetUsersByIdentificationCodeQueryValidator validator = new();

    [Fact]
    public void Validate_WithEmptyCode_Fails()
    {
        TestValidationResult<GetUsersByIdentificationCodeQuery> result = validator.TestValidate(new GetUsersByIdentificationCodeQuery(""));
        _ = result.ShouldHaveValidationErrorFor(x => x.IdentificationCode);
    }
}
