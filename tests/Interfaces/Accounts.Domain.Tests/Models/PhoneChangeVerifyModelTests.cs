using Vitarish.Accounts.Domain.Models;
using Vitarish.Accounts.Domain.Tests.TestInfrastructure;

namespace Vitarish.Accounts.Domain.Tests.Models;

public class PhoneChangeVerifyModelTests
{
    [Fact]
    public void PhoneChangeVerifyModel_MissingNewPhone_FailsValidation()
    {
        PhoneChangeVerifyModel model = new()
        {
            PhoneNumber = string.Empty,
            Code = "123456"
        };

        IReadOnlyList<ValidationResult> results = ValidationTestHelper.ValidateModel(model);

        results.Should().ContainSingle(result => result.ErrorMessage == "EmptyFieldErrorPhoneNumber");
    }

    [Fact]
    public void PhoneChangeVerifyModel_MissingCode_FailsValidation()
    {
        PhoneChangeVerifyModel model = new()
        {
            PhoneNumber = "09123456789",
            Code = string.Empty
        };

        IReadOnlyList<ValidationResult> results = ValidationTestHelper.ValidateModel(model);

        results.Should().ContainSingle(result => result.MemberNames.Contains(nameof(PhoneChangeVerifyModel.Code)));
    }

    [Fact]
    public void PhoneChangeVerifyModel_ValidInput_PassesValidation()
    {
        PhoneChangeVerifyModel model = new()
        {
            PhoneNumber = "09123456789",
            Code = "123456"
        };

        IReadOnlyList<ValidationResult> results = ValidationTestHelper.ValidateModel(model);

        results.Should().BeEmpty();
    }
}
