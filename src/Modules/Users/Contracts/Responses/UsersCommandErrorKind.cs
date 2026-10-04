using System.Text.Json.Serialization;

namespace Vitarish.Users.Contracts.Responses;

/// <summary>
/// The Users module's own command-failure codes as a typed contract value.
/// Member names are the wire values of <see cref="UsersCommandError.Code"/>;
/// identity codes that are passed through unchanged keep
/// <see cref="UsersCommandError.FailureCode"/> null.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<UsersCommandErrorKind>))]
public enum UsersCommandErrorKind
{
    Unauthorized,
    ActiveSessionConflict,
    RoleNotFound,
    InvalidGlobalRole,
    InvalidTenantRole,
    UserNotFound,
    InvalidContact,
    ContactIdentifierInUse,
    ConcurrencyFailure,
    TenantRoleConflict,
    TenantMemberNotFound,
    TenantOwnerRemovalRejected,
    TenantMemberConflict,
    LastOwnerProtection
}

public static class UsersCommandErrorKinds
{
    /// <summary>
    /// Builds an error, attaching a <see cref="UsersCommandErrorKind"/> only
    /// when the code names one of the Users module's own failures.
    /// </summary>
    public static UsersCommandError ForCode(string code, string description) =>
        new(code, description, Enum.TryParse(code, out UsersCommandErrorKind kind) && Enum.IsDefined(kind) ? kind : null);
}
