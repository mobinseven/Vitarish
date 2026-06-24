namespace Vitarish.Entities.Contracts;

/// <summary>
/// Represents the high level reason describing why an entity operation failed.
/// </summary>
public enum EntityErrorCode
{
    /// <summary>
    /// The failure reason could not be determined.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The client supplied data that violated validation rules.
    /// </summary>
    Validation = 1,

    /// <summary>
    /// The requested entity could not be found.
    /// </summary>
    NotFound = 2,

    /// <summary>
    /// The operation conflicted with an existing resource (e.g. duplicate values).
    /// </summary>
    Conflict = 3,

    /// <summary>
    /// The operation failed because another actor modified the entity concurrently.
    /// </summary>
    Concurrency = 4,

    /// <summary>
    /// The current principal is not allowed to execute the operation.
    /// </summary>
    Unauthorized = 5,
}
