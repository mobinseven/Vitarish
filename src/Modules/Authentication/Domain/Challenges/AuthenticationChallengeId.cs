namespace Vitarish.Authentication.Domain.Challenges;

public readonly record struct AuthenticationChallengeId(string Value) { public static AuthenticationChallengeId Create(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Challenge id is required.", nameof(value)) : new(value); }
