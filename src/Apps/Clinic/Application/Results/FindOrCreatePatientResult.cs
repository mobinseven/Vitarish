using Vitarish.Clinic.Domain.Models;

namespace Vitarish.Clinic.Application.Results;

public enum PatientCreationFailureReason
{
    Validation,
    Conflict,
    ReadModelUnavailable,
    Unexpected
}

public abstract record PatientCreationResult
{
    public sealed record Success(PatientView Patient) : PatientCreationResult;

    public sealed record Failure(PatientCreationFailureReason Reason, string Message) : PatientCreationResult;
}

public abstract record FindOrCreatePatientResult
{
    public sealed record Success(PatientView Patient, bool Created) : FindOrCreatePatientResult;

    public sealed record Failure(
        PatientCreationFailureReason Reason,
        string Message,
        IReadOnlyDictionary<string, string[]>? ValidationErrors = null) : FindOrCreatePatientResult;
}
