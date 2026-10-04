using System.Text.Json.Serialization;

namespace Vitarish.Users.Contracts.Responses;

public sealed record UsersCommandResponse(bool Succeeded, IReadOnlyList<UsersCommandError> Errors)
{
    public static UsersCommandResponse Success() => new(true, []);
    public static UsersCommandResponse Failed(IReadOnlyList<UsersCommandError> errors) => new(false, errors);
}

public sealed record UsersCommandError(
    string Code,
    string Description,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] UsersCommandErrorKind? FailureCode = null);
