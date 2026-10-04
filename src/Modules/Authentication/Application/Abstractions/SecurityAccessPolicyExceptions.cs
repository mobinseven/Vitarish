namespace Vitarish.Authentication.Application.Abstractions;

public sealed class SecurityAccessPolicyConcurrencyException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class SecurityAccessPolicyPersistenceException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class AuthenticationUserNotFoundException()
    : Exception("The requested authentication user does not exist.");
