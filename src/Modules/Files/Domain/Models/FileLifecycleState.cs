namespace Vitarish.Files.Domain.Models;

/// <summary>
/// Retention state is independent from storage integrity verification.
/// </summary>
public enum FileLifecycleState
{
    Temporary = 0,
    Reserved = 1,
    Persistent = 2,
    Expired = 3,
    DeletionFailed = 4
}
