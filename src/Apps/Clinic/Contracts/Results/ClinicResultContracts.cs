using Vitarish.Clinic.Contracts.Errors;
namespace Vitarish.Clinic.Contracts.Results;
public sealed record ClinicResult<T>(T? Value, ClinicError? Error = null){ public bool Succeeded => Error is null; public static ClinicResult<T> Success(T value)=>new(value); public static ClinicResult<T> Failure(ClinicError error)=>new(default,error); }
public sealed record ClinicCommandResult(bool Succeeded, Guid? EntityId = null, ClinicError? Error = null);
