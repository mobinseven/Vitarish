using System.Collections.Concurrent;

namespace Vitarish.Main.Infrastructure.Utilities;


internal sealed class KeyedAsyncLock
{
    public sealed class Entry
    {
        public readonly SemaphoreSlim Semaphore = new(1, 1);
        public int RefCount;
    }

    private readonly ConcurrentDictionary<string, Entry> _entries = new();

    public async ValueTask<Releaser> LockAsync(string key, CancellationToken cancellationToken = default)
    {
        // Get or create the entry
        Entry entry = _entries.GetOrAdd(key, _ => new Entry());

        // Mark this caller as a "user" of this entry (prevents premature removal)
        _ = Interlocked.Increment(ref entry.RefCount);

        try
        {
            await entry.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new Releaser(_entries, key, entry);
        }
        catch
        {
            // If we fail to acquire, undo the refcount bump
            ReleaseRef(_entries, key, entry);
            throw;
        }
    }

    private static void ReleaseRef(ConcurrentDictionary<string, Entry> entries, string key, Entry entry)
    {
        if (Interlocked.Decrement(ref entry.RefCount) == 0)
        {
            // Remove only if the dictionary still points to THIS entry (prevents races)
            _ = entries.TryRemove(new KeyValuePair<string, Entry>(key, entry));
        }
    }

    public readonly struct Releaser(ConcurrentDictionary<string, KeyedAsyncLock.Entry> entries, string key, KeyedAsyncLock.Entry entry) : IAsyncDisposable, IDisposable
    {
        public void Dispose()
        {
            _ = entry.Semaphore.Release();
            ReleaseRef(entries, key, entry);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
