namespace Vitarish.Clinical.Domain.Models;

public enum VisitWorkflowStatus
{
    Draft = 0,
    CheckedIn = 1,
    InTriage = 2,
    ReadyForProvider = 3,
    InProgress = 4,
    Signed = 5,
    Completed = 6,
    Cancelled = 7
}
