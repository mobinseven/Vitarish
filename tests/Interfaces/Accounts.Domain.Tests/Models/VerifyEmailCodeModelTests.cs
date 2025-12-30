using Vitarish.Accounts.Domain.Models;
using Vitarish.Accounts.Domain.Tests.TestInfrastructure;

namespace Vitarish.Accounts.Domain.Tests.Models;

public class VerifyEmailCodeModelTests
{
    [Fact]
    public void VerifyEmailCodeModel_MissingEmail_FailsValidation()
    {
        VerifyEmailCodeModel model = new()
        {
            Email = string.Empty,
            Code = "123456"
        };

        IReadOnlyList<ValidationResult> results = ValidationTestHelper.ValidateModel(model);

        results.Should().ContainSingle(result => result.MemberNames.Contains(nameof(VerifyEmailCodeModel.Email)));
    }

    [Fact]
    public void VerifyEmailCodeModel_MissingCode_FailsValidation()
    {
        VerifyEmailCodeModel model = new()
        {
            Email = "user@example.com",
            Code = string.Empty
        };

        IReadOnlyList<ValidationResult> results = ValidationTestHelper.ValidateModel(model);

        results.Should().ContainSingle(result => result.ErrorMessage == "EmptyCodeFieldError");
    }

    [Fact]
    public void VerifyEmailCodeModel_ValidInput_PassesValidation()
    {
        VerifyEmailCodeModel model = new()
        {
            Email = "user@example.com",
            Code = "123456"
        };

        IReadOnlyList<ValidationResult> results = ValidationTestHelper.ValidateModel(model);

        results.Should().BeEmpty();
    }
}
