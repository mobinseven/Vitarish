using FluentValidation.TestHelper;
using Vitarish.Users.Application.Queries;

namespace Vitarish.Users.Tests.Application;

public class GetUsersQueryValidatorTests
{
    private readonly GetUsersQueryValidator validator = new();

    [Fact]
    public void Validate_WithQuery_Passes()
    {
        GetUsersQuery query = new();

        TestValidationResult<GetUsersQuery> result = validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
