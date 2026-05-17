namespace Vitarish.Financial.Application.Contracts;

public sealed record FinancialCommandResult<T>(bool Success, bool IsConflict, bool IsReplay, T? Value, IReadOnlyList<string> Errors)
{
    public static FinancialCommandResult<T> Ok(T value, bool isReplay = false) => new(true, false, isReplay, value, []);

    public static FinancialCommandResult<T> Validation(params string[] errors) => new(false, false, false, default, errors);

    public static FinancialCommandResult<T> Conflict(params string[] errors) => new(false, true, false, default, errors);
}
