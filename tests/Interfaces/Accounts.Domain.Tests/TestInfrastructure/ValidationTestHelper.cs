namespace Vitarish.Accounts.Domain.Tests.TestInfrastructure;

public static class ValidationTestHelper
{
    public static IReadOnlyList<ValidationResult> ValidateModel(object model)
    {
        ValidationContext context = new(model);
        List<ValidationResult> results = new();
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    public static ValidationAttribute CreateInternalAttribute(string attributeTypeName)
    {
        Type attributeType = typeof(global::Vitarish.Accounts.Domain.Options.AaasOptions).Assembly.GetType(attributeTypeName, throwOnError: true)
            ?? throw new InvalidOperationException($"Attribute type '{attributeTypeName}' not found.");
        return (ValidationAttribute)Activator.CreateInstance(attributeType)!;
    }
}
