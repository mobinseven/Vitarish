using Vitarish.Medical.Contracts.Errors;
namespace Vitarish.Medical.Contracts.Results;

public sealed record MedicalResult<T>(bool IsSuccess, T? Value, MedicalError? Failure)
{
    public static MedicalResult<T> Success(T value) => new(true, value, null);
    public static MedicalResult<T> Failed(MedicalError failure) => new(false, default, failure);
}
public sealed record MedicalCommandResult(Guid? EntityId = null, bool Accepted = true, string? UserSafeMessage = null, string? CorrelationId = null);
public sealed record MedicalQueryResult<T>(T? Value, string? CorrelationId = null);
