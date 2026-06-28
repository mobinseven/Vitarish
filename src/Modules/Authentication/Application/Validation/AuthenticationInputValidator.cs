using Vitarish.Authentication.Domain.ReturnUrls;

namespace Vitarish.Authentication.Application.Validation;

public static class AuthenticationInputValidator { public static bool HasText(string? value) => !string.IsNullOrWhiteSpace(value); public static string SafeReturnUrl(string? value) => ReturnUrlPolicy.NormalizeOrDefault(value).Value; }
