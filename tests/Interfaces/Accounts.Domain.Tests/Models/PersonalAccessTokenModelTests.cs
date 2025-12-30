using Vitarish.Accounts.Domain.Models;
using Vitarish.Accounts.Domain.Tests.TestInfrastructure;

namespace Vitarish.Accounts.Domain.Tests.Models;

public class PersonalAccessTokenModelTests
{
    [Fact]
    public void PersonalAccessTokenModel_ValidInput_PassesValidation()
    {
        PersonalAccessTokenModel model = new()
        {
            LifetimeDays = 30,
            IsReferenceToken = true
        };

        IReadOnlyList<ValidationResult> results = ValidationTestHelper.ValidateModel(model);

        results.Should().BeEmpty();
    }
}
