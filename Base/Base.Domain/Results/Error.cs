namespace Vitarish.Base.Domain.Results;

/// <summary>
/// Represents an error with a code and description.
/// </summary>
/// <param name="Code">The error code.</param>
/// <param name="Description">The error description.</param>
public record Error(string Code, string Description)
{
    /// <summary>
    /// Represents no error.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>
    /// Represents a null value error.
    /// </summary>
    public static readonly Error NullValue = new("Error.NullValue", "The specified result value is null.");

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    public static Error Validation(string code, string description) => new(code, description);

    /// <summary>
    /// Creates a not found error.
    /// </summary>
    public static Error NotFound(string code, string description) => new(code, description);

    /// <summary>
    /// Creates a conflict error.
    /// </summary>
    public static Error Conflict(string code, string description) => new(code, description);

    /// <summary>
    /// Creates an unauthorized error.
    /// </summary>
    public static Error Unauthorized(string code, string description) => new(code, description);

    /// <summary>
    /// Creates a failure error.
    /// </summary>
    public static Error Failure(string code, string description) => new(code, description);
}
