namespace Vitarish.Platform.Application.Contracts;

public sealed record ProcessMemorySample(
    long PrivateMemoryBytes,
    long WorkingSetBytes,
    long ManagedHeapBytes);
