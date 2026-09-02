namespace Vitarish.Authentication.Application.Abstractions;

public sealed class SecurityAccessPolicyConcurrencyException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class SecurityAccessPolicyPersistenceException(string message, Exception? innerException = null)
    : Exception(message, innerException);
