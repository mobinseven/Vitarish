using Vitarish.Entities.Contracts.Models;

public static class EntityResultExtensions
{
    public static T EnsureValue<T>(this EntityResult<T> result, string? operationName = null) => result.IsSuccess && result.Value is { } value
            ? value
            : throw new InvalidOperationException(BuildMessage(operationName, result.Errors));

    public static void EnsureSuccess(this EntityResult result, string? operationName = null)
    {
        if (result.IsSuccess)
        {
            return;
        }

        throw new InvalidOperationException(BuildMessage(operationName, result.Errors));
    }

    public static T? GetValueOrDefault<T>(this EntityResult<T> result) => result.IsSuccess ? result.Value : default;

    public static async Task<T> EnsureValueAsync<T>(this Task<EntityResult<T>> resultTask, string? operationName = null)
    {
        EntityResult<T> result = await resultTask.ConfigureAwait(false);
        return result.EnsureValue(operationName);
    }

    public static async Task<T?> GetValueOrDefaultAsync<T>(this Task<EntityResult<T>> resultTask)
    {
        EntityResult<T> result = await resultTask.ConfigureAwait(false);
        return result.GetValueOrDefault();
    }

    public static async Task EnsureSuccessAsync(this Task<EntityResult> resultTask, string? operationName = null)
    {
        EntityResult result = await resultTask.ConfigureAwait(false);
        result.EnsureSuccess(operationName);
    }

    private static string BuildMessage(string? operationName, IReadOnlyList<EntityError> errors)
    {
        string prefix = operationName is null
            ? "Entity operation failed."
            : $"Entity operation '{operationName}' failed.";

        if (errors.Count == 0)
        {
            return prefix;
        }

        string details = string.Join(", ", errors.Select(FormatError));
        return $"{prefix} Errors: {details}";
    }

    private static string FormatError(EntityError error) => string.IsNullOrWhiteSpace(error.Message) ? error.Code.ToString() : $"{error.Code}: {error.Message}";
}
