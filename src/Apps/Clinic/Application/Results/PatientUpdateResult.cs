using Vitarish.Clinic.Domain.Models;

namespace Vitarish.Clinic.Application.Results;

public enum PatientUpdateFailureReason
{
    Validation,
    Conflict,
    ReadModelUnavailable,
    Unexpected
}

public abstract record PatientUpdateResult
{
    public sealed record Success(PatientView Patient) : PatientUpdateResult;

    public sealed record NotFound : PatientUpdateResult;

    public sealed record Failure(
        PatientUpdateFailureReason Reason,
        string Message,
        IReadOnlyDictionary<string, string[]>? ValidationErrors = null) : PatientUpdateResult;
}
