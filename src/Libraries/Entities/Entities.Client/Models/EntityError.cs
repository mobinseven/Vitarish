namespace Vitarish.Entities.Client.Models;
/// <summary>
/// Describes a structured error returned from an entity operation.
/// </summary>
public sealed record EntityError(
    EntityErrorCode Code,
    string Message,
    string? Target = null,
    IReadOnlyDictionary<string, string[]>? Details = null)
{
    public static EntityError Unknown(string message)
        => new(EntityErrorCode.Unknown, message);

    public static EntityError NotFound(string message, string? target = null)
        => new(EntityErrorCode.NotFound, message, target);

    public static EntityError Conflict(string message, string? target = null)
        => new(EntityErrorCode.Conflict, message, target);

    public static EntityError Validation(
        string message,
        string? target = null,
        IReadOnlyDictionary<string, string[]>? details = null)
        => new(EntityErrorCode.Validation, message, target, details);

    public static EntityError Concurrency(string message, string? target = null)
        => new(EntityErrorCode.Concurrency, message, target);
}
