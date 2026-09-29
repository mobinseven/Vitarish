namespace Vitarish.Files.Contracts.Metadata;

public enum FileLifecycleStateDto
{
    Temporary = 0,
    Reserved = 1,
    Persistent = 2,
    Expired = 3,
    DeletionFailed = 4
}
