using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Vitarish.Users.Infrastructure.Data;

/// <summary>
/// Recognizes the unique-constraint violations that mean a sign-in identifier
/// (email, phone number, or user name) is already owned by another account, so
/// write paths can answer with a typed conflict instead of a server error.
/// </summary>
internal static class ContactIdentifierUniqueViolation
{
    internal static bool Matches(DbUpdateException exception) =>
        exception.GetBaseException() is PostgresException postgresException &&
        postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
        IsContactIdentifierConstraint(postgresException.ConstraintName);

    private static bool IsContactIdentifierConstraint(string? constraintName) =>
        constraintName == UsersContactIdentifierIndexNames.EmailIndex ||
        constraintName == UsersContactIdentifierIndexNames.PhoneNumberIndex ||
        constraintName == UsersContactIdentifierIndexNames.UserNameIndex;
}
