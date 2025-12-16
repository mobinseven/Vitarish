using System.Collections.ObjectModel;

namespace Vitarish.Entities.Server.Models;
/// <summary>
/// Represents the outcome of an entity operation without a payload.
/// </summary>
public readonly record struct EntityResult(bool IsSuccess, IReadOnlyList<EntityError> Errors)
{
    public bool IsFailure => !IsSuccess;

    public static EntityResult Success()
    {
        return new(true, Array.Empty<EntityError>());
    }

    public static EntityResult Failure(params EntityError[] errors)
    {
        return new(false, Normalize(errors));
    }

    private static IReadOnlyList<EntityError> Normalize(EntityError[] errors)
    {
        return errors is null
            ? throw new ArgumentNullException(nameof(errors))
            : errors.Length == 0 ? Array.Empty<EntityError>() : new ReadOnlyCollection<EntityError>(errors.ToArray());
    }
}

/// <summary>
/// Represents the outcome of an entity operation that yields a payload.
/// </summary>
public readonly record struct EntityResult<T>(bool IsSuccess, T? Value, IReadOnlyList<EntityError> Errors)
{
    public bool IsFailure => !IsSuccess;

    public static EntityResult<T> Success(T value)
    {
        return new(true, value, Array.Empty<EntityError>());
    }

    public static EntityResult<T> Failure(params EntityError[] errors)
    {
        return new(false, default, Normalize(errors));
    }

    private static IReadOnlyList<EntityError> Normalize(EntityError[] errors)
    {
        return errors is null
            ? throw new ArgumentNullException(nameof(errors))
            : errors.Length == 0 ? Array.Empty<EntityError>() : new ReadOnlyCollection<EntityError>(errors.ToArray());
    }
}
