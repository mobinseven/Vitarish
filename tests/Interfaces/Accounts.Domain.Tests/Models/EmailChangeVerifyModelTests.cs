using Vitarish.Accounts.Domain.Models;
using Vitarish.Accounts.Domain.Tests.TestInfrastructure;

namespace Vitarish.Accounts.Domain.Tests.Models;

public class EmailChangeVerifyModelTests
{
    [Fact]
    public void EmailChangeVerifyModel_MissingNewEmail_FailsValidation()
    {
        EmailChangeVerifyModel model = new()
        {
            Email = string.Empty,
            Code = "123456"
        };

        IReadOnlyList<ValidationResult> results = ValidationTestHelper.ValidateModel(model);

        results.Should().ContainSingle(result => result.MemberNames.Contains(nameof(EmailChangeVerifyModel.Email)));
    }

    [Fact]
    public void EmailChangeVerifyModel_MissingCode_FailsValidation()
    {
        EmailChangeVerifyModel model = new()
        {
            Email = "user@example.com",
            Code = string.Empty
        };

        IReadOnlyList<ValidationResult> results = ValidationTestHelper.ValidateModel(model);

        results.Should().ContainSingle(result => result.MemberNames.Contains(nameof(EmailChangeVerifyModel.Code)));
    }

    [Fact]
    public void EmailChangeVerifyModel_ValidInput_PassesValidation()
    {
        EmailChangeVerifyModel model = new()
        {
            Email = "user@example.com",
            Code = "123456"
        };

        IReadOnlyList<ValidationResult> results = ValidationTestHelper.ValidateModel(model);

        results.Should().BeEmpty();
    }
}
