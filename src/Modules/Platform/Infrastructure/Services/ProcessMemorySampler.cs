using System.Diagnostics;

namespace Vitarish.Platform.Infrastructure.Services;

public sealed class ProcessMemorySampler
{
    private readonly Process _currentProcess;

    public ProcessMemorySampler()
    {
        _currentProcess = Process.GetCurrentProcess();
    }

    public (long privateMemoryBytes, long workingSetBytes, long managedHeapBytes) Sample()
    {
        _currentProcess.Refresh();
        long privateMemoryBytes = _currentProcess.PrivateMemorySize64;
        long workingSetBytes = _currentProcess.WorkingSet64;
        long managedHeapBytes = GC.GetTotalMemory(forceFullCollection: false);

        return (privateMemoryBytes, workingSetBytes, managedHeapBytes);
    }
}
