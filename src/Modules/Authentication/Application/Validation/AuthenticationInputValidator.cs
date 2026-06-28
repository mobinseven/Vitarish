using Vitarish.Authentication.Domain.ReturnUrls;

namespace Vitarish.Authentication.Application.Validation;

public static class AuthenticationInputValidator
{
    public static bool HasText(string? value) => !string.IsNullOrWhiteSpace(value);
    public static string SafeReturnUrl(string? value) => ReturnUrlPolicy.NormalizeOrDefault(value).Value;

    public static string NormalizeVerificationCode(string code)
    {
        if (code.Length > 128) return code;
        Span<char> normalized = stackalloc char[code.Length];
        for (int i = 0; i < code.Length; i++)
        {
            normalized[i] = code[i] switch
            {
                >= '۰' and <= '۹' => (char)('0' + code[i] - '۰'),
                >= '٠' and <= '٩' => (char)('0' + code[i] - '٠'),
                _ => code[i]
            };
        }
        return new string(normalized);
    }
}
