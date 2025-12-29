using Vitarish.Accounts.Domain.Models;
using Vitarish.Accounts.Domain.Tests.TestInfrastructure;

namespace Vitarish.Accounts.Domain.Tests.Models;

public class SendSmsCodeModelTests
{
    [Fact]
    public void SendSmsCodeModel_MissingPhoneNumber_FailsValidation()
    {
        SendSmsCodeModel model = new()
        {
            PhoneNumber = string.Empty
        };

        IReadOnlyList<ValidationResult> results = ValidationTestHelper.ValidateModel(model);

        results.Should().ContainSingle(result => result.ErrorMessage == "EmptyFieldErrorPhoneNumber");
    }

    [Fact]
    public void SendSmsCodeModel_ValidInput_PassesValidation()
    {
        SendSmsCodeModel model = new()
        {
            PhoneNumber = "09123456789"
        };

        IReadOnlyList<ValidationResult> results = ValidationTestHelper.ValidateModel(model);

        results.Should().BeEmpty();
    }
}
