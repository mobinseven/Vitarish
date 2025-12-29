using Vitarish.Accounts.Domain.Models;
using Vitarish.Accounts.Domain.Tests.TestInfrastructure;

namespace Vitarish.Accounts.Domain.Tests.Models;

public class SendEmailCodeModelTests
{
    [Fact]
    public void SendEmailCodeModel_MissingEmail_FailsValidation()
    {
        SendEmailCodeModel model = new()
        {
            Email = string.Empty
        };

        IReadOnlyList<ValidationResult> results = ValidationTestHelper.ValidateModel(model);

        results.Should().ContainSingle(result => result.MemberNames.Contains(nameof(SendEmailCodeModel.Email)));
    }

    [Fact]
    public void SendEmailCodeModel_ValidInput_PassesValidation()
    {
        SendEmailCodeModel model = new()
        {
            Email = "user@example.com"
        };

        IReadOnlyList<ValidationResult> results = ValidationTestHelper.ValidateModel(model);

        results.Should().BeEmpty();
    }
}
