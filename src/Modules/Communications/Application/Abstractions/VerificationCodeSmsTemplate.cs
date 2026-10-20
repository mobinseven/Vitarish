namespace Vitarish.Communications.Application.Abstractions;

/// <summary>The verification-code SMS template's parameters, as configured at the SMS provider.</summary>
public static class VerificationCodeSmsTemplate
{
    /// <summary>The parameter that carries the one-time code.</summary>
    public const string CodeParameter = "CODE";
}
