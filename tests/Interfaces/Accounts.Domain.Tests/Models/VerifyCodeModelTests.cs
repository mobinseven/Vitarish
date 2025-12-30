using Vitarish.Accounts.Domain.Models;
using Vitarish.Accounts.Domain.Tests.TestInfrastructure;

namespace Vitarish.Accounts.Domain.Tests.Models;

public class VerifyCodeModelTests
{
    [Fact]
    public void VerifyCodeModel_MissingCode_FailsValidation()
    {
        VerifyCodeModel model = new()
        {
            Code = string.Empty
        };

        IReadOnlyList<ValidationResult> results = ValidationTestHelper.ValidateModel(model);

        results.Should().ContainSingle(result => result.MemberNames.Contains(nameof(VerifyCodeModel.Code)));
    }

    [Fact]
    public void VerifyCodeModel_ValidInput_PassesValidation()
    {
        VerifyCodeModel model = new()
        {
            Code = "123456"
        };

        IReadOnlyList<ValidationResult> results = ValidationTestHelper.ValidateModel(model);

        results.Should().BeEmpty();
    }
}
