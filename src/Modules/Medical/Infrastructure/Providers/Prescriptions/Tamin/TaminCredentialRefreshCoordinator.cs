using System.Collections.Concurrent;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

internal sealed class TaminCredentialRefreshCoordinator
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _gates = new();

    public Task WaitAsync(Guid credentialId, CancellationToken cancellationToken) =>
        _gates.GetOrAdd(credentialId, static _ => new SemaphoreSlim(1, 1))
            .WaitAsync(cancellationToken);

    public void Release(Guid credentialId)
    {
        if (_gates.TryGetValue(credentialId, out SemaphoreSlim? gate))
        {
            gate.Release();
        }
    }
}
