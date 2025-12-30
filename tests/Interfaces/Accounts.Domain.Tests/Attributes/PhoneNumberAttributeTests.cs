using Vitarish.Accounts.Domain.Tests.TestInfrastructure;

namespace Vitarish.Accounts.Domain.Tests.Attributes;

public class PhoneNumberAttributeTests
{
    private const string AttributeTypeName = "Vitarish.Accounts.Domain.Attributes.PhoneNumberAttribute";

    [Fact]
    public void PhoneNumberAttribute_ValidE164_PassesValidation()
    {
        ValidationAttribute attribute = ValidationTestHelper.CreateInternalAttribute(AttributeTypeName);

        ValidationResult? result = attribute.GetValidationResult("98912345678", new ValidationContext(new object()));

        result.Should().BeNull();
    }

    [Fact]
    public void PhoneNumberAttribute_ValidLocal_PassesValidation()
    {
        ValidationAttribute attribute = ValidationTestHelper.CreateInternalAttribute(AttributeTypeName);

        ValidationResult? result = attribute.GetValidationResult("09123456789", new ValidationContext(new object()));

        result.Should().BeNull();
    }

    [Fact]
    public void PhoneNumberAttribute_InvalidCharacters_FailsValidation()
    {
        ValidationAttribute attribute = ValidationTestHelper.CreateInternalAttribute(AttributeTypeName);

        ValidationResult? result = attribute.GetValidationResult("09123-456789", new ValidationContext(new object()));

        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("PhoneNumberValidator");
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("091234567890")]
    public void PhoneNumberAttribute_TooShortOrLong_FailsValidation(string input)
    {
        ValidationAttribute attribute = ValidationTestHelper.CreateInternalAttribute(AttributeTypeName);

        ValidationResult? result = attribute.GetValidationResult(input, new ValidationContext(new object()));

        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("PhoneNumberValidator");
    }
}
