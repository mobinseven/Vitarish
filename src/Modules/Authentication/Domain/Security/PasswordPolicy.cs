namespace Vitarish.Authentication.Domain.Security;

public sealed record PasswordPolicy(
    int MinimumLength,
    bool RequireLowercase,
    bool RequireUppercase,
    bool RequireNumber,
    bool RequireSpecialCharacter)
{
    public static PasswordPolicy Default { get; } = new(8, true, true, true, true);

    public IReadOnlyList<PasswordPolicyViolation> Validate(string? password)
    {
        List<PasswordPolicyViolation> violations = [];
        if (password is null || password.Length < MinimumLength)
            violations.Add(PasswordPolicyViolation.MinimumLength);
        if (RequireLowercase && (password is null || !password.Any(char.IsLower)))
            violations.Add(PasswordPolicyViolation.Lowercase);
        if (RequireUppercase && (password is null || !password.Any(char.IsUpper)))
            violations.Add(PasswordPolicyViolation.Uppercase);
        if (RequireNumber && (password is null || !password.Any(char.IsDigit)))
            violations.Add(PasswordPolicyViolation.Number);
        if (RequireSpecialCharacter && (password is null || !password.Any(value => !char.IsLetterOrDigit(value))))
            violations.Add(PasswordPolicyViolation.SpecialCharacter);
        return violations;
    }

    public void ValidateConfiguration()
    {
        if (MinimumLength < Default.MinimumLength)
            throw new ArgumentOutOfRangeException(nameof(MinimumLength), "Password minimum length cannot be less than eight.");
        if (!RequireLowercase || !RequireUppercase || !RequireNumber || !RequireSpecialCharacter)
            throw new ArgumentException("Lowercase, uppercase, number, and special-character requirements are mandatory.");
    }
}

public enum PasswordPolicyViolation
{
    MinimumLength,
    Lowercase,
    Uppercase,
    Number,
    SpecialCharacter
}
