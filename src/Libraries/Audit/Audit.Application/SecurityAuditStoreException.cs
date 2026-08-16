namespace Vitarish.Audit.Application;

/// <summary>
/// A mandatory audit write could not be confirmed. Initiating commands must
/// surface this as a dependency failure and must not report success.
/// </summary>
public sealed class SecurityAuditStoreException(string message, Exception innerException) : Exception(message, innerException);
