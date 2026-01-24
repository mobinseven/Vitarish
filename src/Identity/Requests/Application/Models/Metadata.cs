namespace Vitarish.Requests.Application.Models;

public class Metadata : Dictionary<string, object?>
{
    public event Action<string, object?>? OnUpdate;
    public Metadata() : base() { }
    public Metadata(IDictionary<string, object?>? source = null)
    {
        if(source == null)
        {
            return;
        }
        foreach (var kv in source)
        {
            TryAdd(kv.Key, kv.Value);
        }
    }
    public new void Add(string key, object? value)
    {
        base.TryAdd(key, value);
        OnUpdate?.Invoke(key, value);
    }

    public new bool Remove(string key)
    {
        if (TryGetValue(key, out object? oldValue))
        {
            bool removed = base.Remove(key);
            if (removed)
            {
                OnUpdate?.Invoke(key, oldValue);
            }

            return removed;
        }
        return false;
    }

    public new object? this[string key]
    {
        get => base.TryGetValue(key, out object? value) ? value : null;
        set
        {
            base[key] = value;
            OnUpdate?.Invoke(key, value);
        }
    }
}
