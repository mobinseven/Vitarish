using System.Diagnostics;
using Vitarish.Platform.Application.Abstractions;
using Vitarish.Platform.Application.Contracts;

namespace Vitarish.Platform.Infrastructure.Services;

public sealed class ProcessMemorySampler : IConnectionMemorySampler
{
    private readonly Process _currentProcess;

    public ProcessMemorySampler()
    {
        _currentProcess = Process.GetCurrentProcess();
    }

    public ProcessMemorySample Sample()
    {
        _currentProcess.Refresh();
        long privateMemoryBytes = _currentProcess.PrivateMemorySize64;
        long workingSetBytes = _currentProcess.WorkingSet64;
        long managedHeapBytes = GC.GetTotalMemory(forceFullCollection: false);

        return new ProcessMemorySample(privateMemoryBytes, workingSetBytes, managedHeapBytes);
    }
}
