using Vitarish.Accounts.Domain.Models;
using Vitarish.Accounts.Domain.Tests.TestInfrastructure;

namespace Vitarish.Accounts.Domain.Tests.Models;

public class VerifySmsCodeModelTests
{
    [Fact]
    public void VerifySmsCodeModel_MissingPhoneNumber_FailsValidation()
    {
        VerifySmsCodeModel model = new()
        {
            PhoneNumber = string.Empty,
            Code = "123456"
        };

        IReadOnlyList<ValidationResult> results = ValidationTestHelper.ValidateModel(model);

        results.Should().ContainSingle(result => result.ErrorMessage == "EmptyFieldErrorPhoneNumber");
    }

    [Fact]
    public void VerifySmsCodeModel_MissingCode_FailsValidation()
    {
        VerifySmsCodeModel model = new()
        {
            PhoneNumber = "09123456789",
            Code = string.Empty
        };

        IReadOnlyList<ValidationResult> results = ValidationTestHelper.ValidateModel(model);

        results.Should().ContainSingle(result => result.ErrorMessage == "EmptyCodeFieldError");
    }

    [Fact]
    public void VerifySmsCodeModel_ValidInput_PassesValidation()
    {
        VerifySmsCodeModel model = new()
        {
            PhoneNumber = "09123456789",
            Code = "123456"
        };

        IReadOnlyList<ValidationResult> results = ValidationTestHelper.ValidateModel(model);

        results.Should().BeEmpty();
    }
}
