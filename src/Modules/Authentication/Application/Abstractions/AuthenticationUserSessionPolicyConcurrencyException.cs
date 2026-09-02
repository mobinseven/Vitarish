namespace Vitarish.Authentication.Application.Abstractions;

public sealed class AuthenticationUserSessionPolicyConcurrencyException(Exception? innerException = null)
    : Exception("The user inactivity policy changed concurrently; retry from a fresh read.", innerException);
