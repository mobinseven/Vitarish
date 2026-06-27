namespace Vitarish.Entities.Contracts.Models;
/// <summary>
/// Describes a structured error returned from an entity operation.
/// </summary>
public sealed record EntityError(
    EntityErrorCode Code,
    string Message,
    string? Target = null,
    IReadOnlyDictionary<string, string[]>? Details = null)
{
    public static EntityError Unknown(string message) => new(EntityErrorCode.Unknown, message);

    public static EntityError NotFound(string message, string? target = null) => new(EntityErrorCode.NotFound, message, target);

    public static EntityError Conflict(string message, string? target = null) => new(EntityErrorCode.Conflict, message, target);

    public static EntityError Validation(
        string message,
        string? target = null,
        IReadOnlyDictionary<string, string[]>? details = null) => new(EntityErrorCode.Validation, message, target, details);

    /// <summary>
    /// Creates a validation error from a dictionary with single error messages per property.
    /// </summary>
    /// <remarks>
    /// This is a convenience method that wraps each string in an array. For high-performance scenarios
    /// with large validation result sets, consider using the overload that accepts IReadOnlyDictionary&lt;string, string[]&gt;
    /// directly to avoid extra allocations.
    /// </remarks>
    public static EntityError Validation(
        string message,
        IReadOnlyDictionary<string, string> singleDetails) => new(EntityErrorCode.Validation, message, null,
                singleDetails.ToDictionary(kvp => kvp.Key, kvp => new[] { kvp.Value }));

    public static EntityError Concurrency(string message, string? target = null) => new(EntityErrorCode.Concurrency, message, target);
}
