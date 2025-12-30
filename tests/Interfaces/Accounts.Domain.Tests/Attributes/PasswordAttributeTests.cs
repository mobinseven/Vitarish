using Vitarish.Accounts.Domain.Attributes;
namespace Vitarish.Accounts.Domain.Tests.Attributes;

public class PasswordAttributeTests
{
    private readonly PasswordAttribute attribute = new();

    [Fact]
    public void PasswordAttribute_ValidPassword_PassesValidation()
    {
        ValidationResult? result = attribute.GetValidationResult("GoodPass1!", new ValidationContext(new object()));

        result.Should().BeNull();
    }

    [Fact]
    public void PasswordAttribute_TooShort_FailsValidation()
    {
        ValidationResult? result = attribute.GetValidationResult("A1!a", new ValidationContext(new object()));

        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("PasswordLengthError");
    }

    [Fact]
    public void PasswordAttribute_MissingRequiredCharClasses_FailsValidation()
    {
        ValidationResult? result = attribute.GetValidationResult("abcdef", new ValidationContext(new object()));

        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("PasswordUppercaseError");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void PasswordAttribute_NullOrEmpty_FailsValidation(string? input)
    {
        ValidationResult? result = attribute.GetValidationResult(input, new ValidationContext(new object()));

        result.Should().NotBeNull();
    }
}
