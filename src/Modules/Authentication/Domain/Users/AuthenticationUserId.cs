namespace Vitarish.Authentication.Domain.Users;

public readonly record struct AuthenticationUserId(string Value) { public static AuthenticationUserId Create(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("User id is required.", nameof(value)) : new(value); }
