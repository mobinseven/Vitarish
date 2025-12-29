using Vitarish.Accounts.Domain.Tests.TestInfrastructure;

namespace Vitarish.Accounts.Domain.Tests.Attributes;

public class AuthenticationCodeAttributeTests
{
    private const string AttributeTypeName = "Vitarish.Accounts.Domain.Attributes.AuthenticationCodeAttribute";

    [Fact]
    public void AuthenticationCodeAttribute_ValidLengthAndDigits_PassesValidation()
    {
        ValidationAttribute attribute = ValidationTestHelper.CreateInternalAttribute(AttributeTypeName);

        ValidationResult? result = attribute.GetValidationResult("123456", new ValidationContext(new object()));

        result.Should().BeNull();
    }

    [Fact]
    public void AuthenticationCodeAttribute_NonNumeric_FailsValidation()
    {
        ValidationAttribute attribute = ValidationTestHelper.CreateInternalAttribute(AttributeTypeName);

        ValidationResult? result = attribute.GetValidationResult("12a456", new ValidationContext(new object()));

        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("AuthenticationCodeValidator");
    }

    [Fact]
    public void AuthenticationCodeAttribute_WrongLength_FailsValidation()
    {
        ValidationAttribute attribute = ValidationTestHelper.CreateInternalAttribute(AttributeTypeName);

        ValidationResult? result = attribute.GetValidationResult("12345", new ValidationContext(new object()));

        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("AuthenticationCodeValidator");
    }
}
