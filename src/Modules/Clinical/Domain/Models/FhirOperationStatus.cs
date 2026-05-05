namespace Vitarish.Clinical.Domain.Models;

public enum FhirOperationStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    Denied = 4
}
