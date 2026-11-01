namespace Vitarish.Users.Application.Abstractions;

public sealed class UserAccountDeletionConcurrencyException(Exception innerException)
    : Exception("The account changed during deletion.", innerException);
