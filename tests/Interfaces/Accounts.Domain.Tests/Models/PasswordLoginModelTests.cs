using Vitarish.Accounts.Domain.Models;
using Vitarish.Accounts.Domain.Tests.TestInfrastructure;

namespace Vitarish.Accounts.Domain.Tests.Models;

public class PasswordLoginModelTests
{
    [Fact]
    public void PasswordLoginModel_MissingUsername_FailsValidation()
    {
        PasswordLoginModel model = new()
        {
            Username = string.Empty,
            Password = "Valid1!"
        };

        IReadOnlyList<ValidationResult> results = ValidationTestHelper.ValidateModel(model);

        results.Should().ContainSingle(result => result.MemberNames.Contains(nameof(PasswordLoginModel.Username)));
    }

    [Fact]
    public void PasswordLoginModel_MissingPassword_FailsValidation()
    {
        PasswordLoginModel model = new()
        {
            Username = "user",
            Password = string.Empty
        };

        IReadOnlyList<ValidationResult> results = ValidationTestHelper.ValidateModel(model);

        results.Should().ContainSingle(result => result.MemberNames.Contains(nameof(PasswordLoginModel.Password)));
    }

    [Fact]
    public void PasswordLoginModel_ValidInput_PassesValidation()
    {
        PasswordLoginModel model = new()
        {
            Username = "user",
            Password = "Valid1!"
        };

        IReadOnlyList<ValidationResult> results = ValidationTestHelper.ValidateModel(model);

        results.Should().BeEmpty();
    }
}
