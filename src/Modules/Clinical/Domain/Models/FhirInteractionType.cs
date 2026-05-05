namespace Vitarish.Clinical.Domain.Models;

public enum FhirInteractionType
{
    Create = 0,
    Read = 1,
    Update = 2,
    Patch = 3,
    Delete = 4,
    Search = 5,
    Validate = 6,
    History = 7
}
