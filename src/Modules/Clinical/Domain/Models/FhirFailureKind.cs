namespace Vitarish.Clinical.Domain.Models;

public enum FhirFailureKind
{
    None = 0,
    Denied = 1,
    Validation = 2,
    Transport = 3,
    Timeout = 4,
    Conflict = 5,
    NotFound = 6,
    Unexpected = 7
}
